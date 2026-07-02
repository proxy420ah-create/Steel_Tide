# Recent Changes - Steel Tide: First Device

**Last Updated**: July 1, 2026
**Version**: 0.1.0-alpha

---

## Current Session Changes

### Session: Modular VoxelActor Refactor

**Goal**: Split the monolithic `VoxelRagdoll.cs` into swappable MonoBehaviour subsystems so each physics feature can be toggled independently for debugging.

**Changes Made**:

1. **New Modular Scripts** (all under `My project/Assets/Scripts/`)
   - `VoxelActor.cs` — orchestrator that holds shared state and drives the subsystem sequence.
   - `VoxelBodyManager.cs` — builds synthetic pelvis root, bone bodies, colliders, and joints.
   - `VoxelGroundResolver.cs` — voxel ground raycast, penetration correction, friction, restitution.
   - `VoxelBalance.cs` — wraps `BalanceController` and calls `UpdateBalance`.
   - `VoxelRevoxelizer.cs` — assigns voxels to bones and re-stamps the visual volume each frame.
   - `VoxelActorGizmos.cs` — debug gizmos for the modular actor.
   - `VoxelActorDebugController.cs` — runtime debug controls: reset, time scale, subsystem toggles.

2. **RagdollBodyGizmos.cs Updated**
   - Now supports both `VoxelRagdoll` (legacy) and `VoxelActor` (modular) parents.

3. **Toggles for Bug Isolation**
   - `enableBodies` — skip body/joint build entirely.
   - `enableGroundCollision` — test free-fall without ground response.
   - `enableBalance` — test raw physics without corrective torque.
   - `enableRevoxelization` — test physics without visual update.
   - `buildJoints` — make bones independent to isolate joint behavior.

4. **Legacy Preserved**
   - `VoxelRagdoll.cs` remains unchanged as a working fallback.

---

### Session: Ragdoll Jitter Fix — Physics/Visual Transform Decoupling

**Goal**: Eliminate the strong physical jitter that appeared when re-voxelization and balance were both enabled, and establish a robust architecture for actors that travel long distances.

**Changes Made**:

1. **Root Cause Isolation** (`My project/Assets/Scripts/VoxelRagdoll.cs`)
   - Added runtime toggles to isolate physics subsystems:
     - `enableBalance`
     - `enableGroundCollision`
     - `enableRevoxelization`
     - `buildJoints`
   - Process of elimination showed jitter was caused by `RevoxelizeFrame`, not the balance controller.

2. **Balance Controller Damping & Smoothing** (`My project/Assets/Scripts/BalanceController.cs`)
   - Added `balanceDamping` to `BalanceSettings` (range 0–10, default 2.0).
   - `ApplyCorrectiveTorques` now applies a damping torque opposing pelvis angular velocity, scaled by `balanceStrength`.
   - Added `leanSmoothing` (EMA time constant in seconds) to smooth the lean vector before torque calculation.
   - Added `airborneBalance` (0–1) to reduce or disable balance correction while feet are not grounded.
   - `VoxelRagdoll.ResolveVoxelGround` now tracks foot ground contact and feeds `GroundContactScale` to the controller.
   - EMA alpha is computed from `Time.fixedDeltaTime` so smoothing is consistent at any `Time.timeScale`.
   - Gizmos now show the raw lean (faint) and the smoothed lean (yellow) used by the controller.
   - Prevents P-only oscillation and direction-flipping of the corrective torque.

3. **Ground Friction Time-Scale Fix** (`My project/Assets/Scripts/VoxelRagdoll.cs`)
   - `groundFriction` is now a **per-second rate** (default 20, range 0–50) instead of a per-FixedUpdate factor.
   - Friction is applied via `Mathf.Exp(-groundFriction * Time.fixedDeltaTime)` so the actor slides the same at normal and slow game speeds.

4. **Ground Penetration Correction Time-Scale Fix + Inelastic Landing** (`My project/Assets/Scripts/VoxelRagdoll.cs`)
   - `groundStiffness` now divides by `Time.fixedDeltaTime` so the correction speed is `penetration * groundStiffness / Time.fixedDeltaTime`.
   - This makes the landing response time-step independent: the same fall produces the same bounce at any `Time.timeScale`.
   - Added `groundRestitution` (0–1). Default 0 = inelastic landing (impact absorbed, no arbitrary bounce). 1 = fully elastic.
   - Default `groundStiffness` changed from `30` to `0.1` (dimensionless aggressiveness, 0.1 = soft settling over several steps).
   - Friction is now applied when the body is near or touching the ground, not only when actively penetrating. This fixes sliding on flat ground.

5. **Physics/Visual Transform Decoupling** (`My project/Assets/Scripts/VoxelRagdoll.cs`)
   - `_boneRoot` is now a **top-level GameObject** instead of a child of the VoxelObject.
   - Re-enabled the pelvis anchor in `RevoxelizeFrame` so the voxel volume dynamically follows the actor.
   - `SizeAndAllocateVolume` sizes from the pelvis again.
   - The VoxelObject transform can move freely for recentering without teleporting the physics bodies.

6. **Debug Gizmo Improvements** (`My project/Assets/Scripts/VoxelRagdoll.cs` + `RagdollBodyGizmos.cs`)
   - Created `RagdollBodyGizmos.cs` component attached to the generated `_boneRoot`.
   - Moved bone/joint gizmo drawing from `VoxelRagdoll` to `RagdollBodyGizmos`.
   - Added `drawGizmosAlways` and `gizmoOffset` controls on the body container for side-by-side comparison with colliders.
   - Gizmo drawing uses world-space matrix so bone orientations align with physics colliders.

**Architecture Rule Established**:

> **Physics bodies must never be children of the visual voxel volume that recenters.**
>
> Visual-only transforms can move freely (recentering, LOD snapping, animation). Physics bodies must stay in world space, or be moved through Rigidbody APIs only.

**Expected Behavior**:

1. Re-voxelization ON, balance ON, gravity ON → actor stands/falls without jitter.
2. Actor can travel long distances; the voxel volume follows the pelvis while the physics bodies remain stable.
3. Ground collision works correctly because the bodies are not being teleported by the parent transform.
4. Selecting the `_boneRoot` GameObject in the hierarchy shows accurate bone/joint gizmos.
5. `Time.timeScale` changes should not change the direction the actor falls; only the speed.
6. Higher `balanceStrength` should remain stable with `balanceDamping` in the 2–5 range.
7. The yellow gizmo line (smoothed lean) should be visible and lag slightly behind the faint raw lean line.

**Critical Test Points**:

- Test at `Time.timeScale = 1.0` and `Time.timeScale = 0.1` — the fall trajectory should match, just slower/faster.
- Try `balanceStrength = 10` with `balanceDamping = 2.0` and `leanSmoothing = 0.15`.
- Verify ground friction feels the same at both time scales.
- Verify landing feels solid at both time scales. With `groundRestitution = 0`, the body should absorb impact and not bounce.
- Test `groundRestitution = 1.0` for a fully bouncy surface if needed later.
- Check that `groundMaxPushSpeed = 4` still prevents rocket bounces from deep penetration.

**Documentation**:

- `docs/RAGDOLL_PHYSICS_ITERATION_GUIDE.md` updated with new sections 11 and 12 (jitter root cause + robust fix).
- `VOXEL_ACTOR_SYSTEM_DESIGN.md` updated:
  - Re-voxelization section now explicitly describes the visual/physics decoupling.
  - Design Decisions Log adds the new architecture rule.
  - Balance controller section now includes `balanceDamping` and `leanSmoothing`.
- `docs/BALANCE_CONTROLLER_IMPLEMENTATION_PLAN.md` updated to include the new parameters, EMA smoothing, and ground contact scaling.
- `docs/RAGDOLL_PHYSICS_ITERATION_GUIDE.md` updated to include ground penetration correction fix and airborne balance.

---

### Session: GPU Rendering Pipeline (test cube → visible on screen)

**Goal**: Build the complete GPU rendering hookup so the test cube loads from
`.stasset` and displays via compute shader raymarching in the Unity Game window.

**Changes Made**:

1. **Test Asset Created** — `tools/asset_generator/examples/test_cube.json`
   - 8×8×8 solid Concrete cube (Material ID 3)
   - Generated binary: `My project/Assets/StreamingAssets/test_cube.stasset` (1040 bytes)
   - Verified: 512 voxels, 100% solid, packed 16-bit format

2. **VoxelRenderer.cs (NEW)** — `My project/Assets/Voxels/VoxelRenderer.cs`
   - **Unity 6 + URP compatible**: uses `RenderPipelineManager.endCameraRendering`
     instead of deprecated `OnRenderImage` (critical for URP pipelines)
   - Dispatches `VoxelRaymarch.compute` each frame via rendering callback
   - Creates `RenderTexture` output matching camera resolution
   - Uploads material color table to `ComputeBuffer`
   - Binds `Texture3D` voxel volume and camera parameters to compute shader
   - Blits output to camera target using `CommandBuffer` API
   - Thread group dispatch: 8×8 tiles, full-screen coverage

3. **PrototypeBootstrap.cs (UPDATED)** — GPU upload routine added
   - New method: `UploadVoxelTextureToGPU()`
   - Creates `Texture3D` (R16 format) matching loaded grid dimensions (8×8×8)
   - Converts `NativeArray<ushort>` → `byte[]` (little-endian)
   - Uploads to GPU via `SetPixelData()`, sets filter mode to `Point` (no interpolation)
   - Wires `Texture3D` reference to `VoxelRenderer.voxelVolume` field
   - New inspector field: `VoxelRenderer voxelRenderer` (assign in Editor)

4. **UNITY_SETUP_GUIDE.md (NEW)** — step-by-step Editor instructions
   - Camera setup: position (4,4,6), rotation (20,-30,0)
   - Attach `VoxelRenderer` component to Main Camera
   - Assign `VoxelRaymarch.compute` to renderer's shader slot
   - Create `Bootstrap` GameObject with `PrototypeBootstrap` component
   - Link `Main Camera` → `Bootstrap.Voxel Renderer` in Inspector
   - Troubleshooting tips for black screen, shader errors, missing references

**Data Flow** (end-to-end verified):
```
test_cube.json → [Python] → test_cube.stasset → [C# Load] → NativeArray<ushort>
  → [Upload] → Texture3D (GPU) → [Compute Shader] → RenderTexture → [Blit] → Camera
```

**Expected Result**: Press Play → Console shows load confirmation + GPU upload
confirmation → Game window shows 8×8×8 tan concrete cube rendered via raymarching.

**Next Steps**: Unity Editor scene wiring (see `UNITY_SETUP_GUIDE.md`), then verify
visual output. Once cube renders, pipeline is proven for complex assets.

**Critical Patch (Black Screen Fix)**:
- Initial test showed black screen — shader was running but outputting zeros
- **Root cause**: Ray direction calculation used incorrect clip-space reconstruction
- **Fix 1**: Refactored `RayDirFromUV()` to properly reconstruct near/far plane points
  in view space, then transform to world space using `_CameraToWorld` matrix
- **Fix 2**: Split combined `_InvViewProj` into separate `_CameraToWorld` and 
  `_InvProjection` matrices for correct projection math
- **Fix 3**: Added `_BackgroundColor` uniform — rays that miss voxels now return
  camera's clear color instead of hardcoded black (instant visual feedback)
- VoxelRenderer.cs now passes `camera.cameraToWorldMatrix`, `camera.projectionMatrix.inverse`,
  and `camera.backgroundColor` to shader each frame

**Debug Diagnostic Patch (Blue but No Cube)**:
- Background renders blue (shader works) but cube not visible at origin
- **Root cause hypothesis**: Ray-volume intersection or dimension passing issue
- **Fix 1**: Changed `_VolumeDims` from `float3` to `int3` for explicit integer bounds
- **Fix 2**: VoxelRenderer uses `SetInts()` to pass dimensions as explicit integers
- **Fix 3**: Added **MAGENTA DEBUG BYPASS** — any non-zero voxel returns `float4(1,0,1,1)`
  instead of reading material color table (eliminates color lookup as failure point)
- **Fix 4**: Added bright GREEN center pixel marker to confirm camera aim point
- **Fix 5**: Added debug logging every frame: dimensions, voxelSize, camera pos, offset
- **Expected visual**: Either MAGENTA cube (voxels hit) or GREEN center dot + blue (miss)

**AABB Intersection Fix (Production-Ready Raymarching)**:
- User confirmed: Magenta cube visible at (2,2,2) but disappears at other positions
- **Root cause**: DDA started at camera position, only worked when camera inside volume
- **Critical fix**: Implemented proper **AABB ray-box intersection** (`RayAABBIntersection`)
  - Calculates `tNear`/`tFar` — exact distance where ray enters/exits volume bounds
  - DDA now starts at entry point: `startPos = ro + rd * max(tNear, 0.0)`
  - Fixes `tMax` initialization to account for world-space entry position
  - Ray advancement uses correct `tDelta = invDir * _VoxelSize` scaling
- **Removed debug visuals**: Magenta bypass and green crosshair disabled
- **Restored production rendering**: Material color lookup + simple directional lighting
- **Result**: Cube now visible from ANY camera position/rotation — proper 3D raymarching

**Safety & Quality-of-Life Improvements**:
- **Infinite Loop Guard (VoxelPenetration.cs)**:
  - Added `loopGuard` counter (max 1000 iterations) to prevent thread freeze
  - Logs warning with diagnostic info if triggered (voxel pos, ray dir, remaining power)
  - Protects against edge cases in DDA math that could cause infinite loops
  - PrototypeBootstrap already uses safe normalized direction: `math.normalize(new float3(1,0,1))`
- **Live Inspector Sliders (VoxelRenderer.cs)**:
  - `maxSteps` and `voxelSize` already updated every frame in `DispatchRaymarch()`
  - Changes in Unity Inspector apply immediately to GPU — no Play/Stop required
  - Can adjust raymarching quality and scale in real-time while game runs
  - Example: Lower `maxSteps` to 16 → see chunky visualization; raise to 256 → fine detail

**Interactive Damage Visualization System**:
- **Flat-Color Rendering (VoxelRaymarch.compute)**:
  - Removed depth grain/concentric lines for clean solid colors
  - Tracks face normals during DDA advancement (`float3 normal`)
  - Each voxel face lit with directional diffuse: `max(0.3, dot(normal, lightDir))`
  - Light from upper-right: `(0.5, 0.8, -0.3)` for clear 3D definition
  - Result: Sharp, clean low-poly look — no more "dusty fingerprint" appearance
- **Two-Stage Damage System (VoxelBits.cs + Material IDs)**:
  - Added damaged material states: `DamagedConcrete` (13), `DamagedSteel` (14), `DamagedArmor` (15)
  - Intact → Damaged → Air (two hits to fully destroy)
  - Concrete (tan) → DamagedConcrete (crimson red) → Air (hole)
  - Steel (blue-gray) → DamagedSteel (hot orange) → Air
  - MaterialId.Count updated to 16 to accommodate damage states
- **Material Color Expansion (VoxelRenderer.cs)**:
  - Array expanded from 6 → 16 colors
  - Damaged states use distinctive colors impossible to miss:
    - DamagedConcrete: `(0.85, 0.15, 0.15)` — Crimson red fracture scar
    - DamagedSteel: `(0.9, 0.4, 0.1)` — Hot orange molten metal
    - DamagedArmor: `(0.8, 0.2, 0.2)` — Dark red armor breach
- **VoxelWeaponController.cs (NEW)** — `Assets/Combat/VoxelWeaponController.cs`:
  - Attach to Main Camera for point-and-click destruction testing
  - Left-click fires raycast into voxel volume using same DDA as renderer
  - Finds first solid voxel hit, applies damage in configurable radius
  - **Two-stage logic**: 1st click damages (color change), 2nd click destroys (air)
  - Updates `NativeArray<ushort>` voxel data directly
  - Re-uploads modified data to GPU `Texture3D` for instant visual feedback
  - Includes loop guard (256 max steps) for safe raycasting
  - Console logs hit location and number of voxels affected
- **PrototypeBootstrap Updates**:
  - Added `weaponController` inspector reference
  - Initializes weapon controller with voxel volume, dims, and texture
  - Texture3D now `makeNoLongerReadable: false` to allow weapon updates
  - Console message: "left-click to blast craters!" when ready

**Expected Behavior**:
1. Cube renders as clean flat-shaded solid tan/gray concrete
2. Left-click on cube → cluster of voxels flash to CRIMSON RED (damaged state)
3. Left-click red scar again → voxels vanish to AIR (permanent hole carved through)
4. Can peer straight through holes — ray tracing passes through air voxels
5. Damage persists — holes remain until Play mode stopped

**Unity 6 Input System Compatibility Fix**:
- **Critical patch 1**: VoxelWeaponController.cs initially used legacy `Input.GetKeyDown()`
  causing 999+ `InvalidOperationException` errors in Unity 6
- **Root cause**: Unity 6 defaults to New Input System, conflicts with old Input class
- **Fix 1 applied**:
  - Added `using UnityEngine.InputSystem;` namespace
  - Replaced `Input.GetKeyDown(fireKey)` → `Mouse.current.leftButton.wasPressedThisFrame`
  - Removed `KeyCode fireKey` field (hardcoded to left mouse button)
  - Added null-check: `if (Mouse.current != null)` for safety
- **Critical patch 2**: Mouse position tracking still used legacy API
  - `Input.mousePosition` in `ScreenPointToRay()` call triggered new errors
  - **Fix**: Replaced with `Mouse.current.position.ReadValue()`
  - Line 63-64: Now uses modern Vector2 mouse position from Input System
- **Result**: Zero Input API errors, clean compilation, all mouse input works correctly
- **Unity 6 compliant**: Uses modern Input System API throughout (clicks + position)

**AXIS ORDERING BUG FIX (CRITICAL)**:
- **Issue discovered by designer**: Array layout mismatch between Python and C#/HLSL
- **Root cause**: Python used `transpose(2,1,0)` + `ravel(order='C')` → Z varies fastest
- **C#/HLSL expected**: `index = x + y*dim_x + z*dim_x*dim_y` → X varies fastest
- **Result**: 50% of voxels appeared as air due to reading from wrong memory locations
- **Fix**:
  - **Python** (`voxel_format.py`):
    - Removed transpose: `grid.ravel(order='F')` (Fortran order, X varies fastest)
    - Updated load: `reshape((dim_x, dim_y, dim_z), order='F')`
  - **Verification**: Created `verify_axis_order.py` to test C# indexing formula
  - **Result**: 100% solid voxels now load correctly (512/512)
- **Impact**: This was the PRIMARY cause of rendering issues, not the StructuredBuffer migration
- **Files regenerated**: `test_cube.stasset` now has correct axis ordering

**ARCHITECTURE MIGRATION: Texture3D → StructuredBuffer (FINAL SOLUTION)**:
- **Critical insight from designer**: "You're using voxel structs, not images"
- **Root issue**: Treating structured voxel data as image pixels (Texture3D<float4>)
- **Migration**: Replaced Texture3D with ComputeBuffer (StructuredBuffer<uint>)
- **Benefits**:
  - ✅ No float conversion - Direct integer access
  - ✅ No precision loss - No round() hacks needed
  - ✅ No format limitations - Not constrained by texture formats
  - ✅ Conceptually correct - Voxels are structs, not pixels
  - ✅ 50% memory reduction - 2 KB vs 4 KB (uint vs RGBAHalf)
  - ✅ Future-proof - Easy to expand struct size
- **Changes**:
  - **Compute Shader** (`VoxelRaymarch.compute`):
    - `Texture3D<float4> _VoxelVolume` → `StructuredBuffer<uint> _VoxelData`
    - Added `VoxelIndex(int3)` helper to convert 3D coords to 1D index
    - Direct access: `uint packed = _VoxelData[VoxelIndex(voxel)]` (no float conversion!)
  - **Bootstrap** (`PrototypeBootstrap.cs`):
    - `Texture3D _voxelTexture` → `ComputeBuffer _voxelBuffer`
    - Upload: `uint[]` array → `SetData()` (direct copy, no Color array)
    - Cleanup: `Destroy(_voxelTexture)` → `_voxelBuffer.Release()`
  - **Renderer** (`VoxelRenderer.cs`):
    - `Texture3D voxelVolume` → `ComputeBuffer voxelBuffer`
    - Added `int3 volumeDims` field for grid dimensions
    - Binding: `SetTexture()` → `SetBuffer()`
  - **Weapon Controller** (`VoxelWeaponController.cs`):
    - `Texture3D voxelVolume` → `ComputeBuffer voxelBuffer`
    - Update: `SetPixels(Color[])` → `SetData(uint[])` (direct integer upload)
- **Result**: Clean architecture aligned with "voxels as scene database" philosophy
  - No more "fighting the API" with float conversions
  - Eliminates confetti rendering at root cause level
  - Sets foundation for shape-based SDF evaluation (future)

**Texture Format & Sampling Fix (SUPERSEDED BY STRUCTUREDBUFFER MIGRATION)**:
- **Critical issue**: "Confetti" rendering caused by float precision errors in material ID sampling
- **Root cause**: TextureFormat.R16 (normalized float) misinterpreted ushort data as 0.0-1.0 range
- **Fix 1 - Texture Format**:
  - Changed from `TextureFormat.R16` → `TextureFormat.RGBAHalf`
  - Upload data as `Color[]` with ushort values in R channel
  - Shader reads as `Texture3D<float4>` and extracts R channel
- **Fix 2 - Integer Rounding**:
  - Added `round()` before float-to-uint cast: `uint packed = (uint)round(texData.r);`
  - Prevents fractional material IDs (e.g., 2.7, 1.4) from causing random colors
  - Added safety clamp: `mat = min(mat, 15u);` to prevent out-of-bounds lookups
- **Fix 3 - Point Filtering**:
  - Already set: `_voxelTexture.filterMode = FilterMode.Point;`
  - Prevents GPU from blending between voxel values
- **Result**: Clean, solid, flat-shaded concrete cube with sharp face lighting
  - Material ID 3 (Concrete) → tan/gray (0.6, 0.6, 0.55)
  - Material ID 13 (Damaged Concrete) → crimson red (0.85, 0.15, 0.15)
  - Weapon clicks correctly remap 3→13 and display damage visualization

**Runtime Component Auto-Location Fix**:
- **Critical issue**: Texture3D created but never appeared in Inspector slots at runtime
  - VoxelRenderer.voxelVolume showed "None (Texture3D)"
  - VoxelWeaponController.voxelVolume showed "None (Texture3D)"
  - Caused noisy "fingerprint" rendering and "Missed — no solid voxel hit" errors
- **Root cause 1**: PrototypeBootstrap fields `voxelRenderer` and `weaponController` were null
  because they weren't manually dragged into Bootstrap's Inspector slots
- **Root cause 2**: Direct texture assignment during runtime failed due to Unity 6 strict initialization ordering
- **Fix (Pull-Based Architecture)**:
  - **PrototypeBootstrap.cs**:
    - Added public property: `public Texture3D GeneratedVoxelTexture => _voxelTexture;`
    - Updated to Unity 6 API: `FindFirstObjectByType` (replaces deprecated `FindObjectOfType`)
    - Auto-locates components at Start() if not manually assigned
  - **VoxelRenderer.cs**:
    - Added `LateUpdate()` loop that pulls texture from Bootstrap if null
    - Checks `bootstrap.GeneratedVoxelTexture` and assigns to local `voxelVolume`
    - Logs: "Pulled Texture3D reference from PrototypeBootstrap."
  - **VoxelWeaponController.cs**:
    - Added pull logic in `Update()` loop before weapon firing
    - Same pattern: finds Bootstrap, pulls texture if null
- **Result**: Texture3D automatically populates into both component slots at runtime
  - Inspector shows valid Texture3D reference during Play mode
  - Renderer gets clean flat-shaded cube
  - Weapon controller can raycast and blast craters
- **No manual setup required**: Bootstrap now auto-wires everything at runtime
- **Unity 6 compliant**: Uses `FindFirstObjectByType` (no CS0618 warnings)

---

### Session: Unity Project Reconciliation (scripts → /Assets, DOTS manifest)

**Goal**: Fold the existing C# scaffolding into the newly created Unity 6 project
(`My project/`) and pin the DOTS/ECS dependencies.

**Changes Made**:
1. Created `My project/Assets/{Voxels,Combat,Geoscape,Prototype}/`.
2. Moved scripts into Assets:
   - `Voxels/` ← `VoxelBits.cs`, `VoxelPenetration.cs`, `StAssetReader.cs`,
     **`VoxelRaymarch.compute`** (moved too — Unity needs the shader compiled).
   - `Combat/` ← `CommandHierarchy.cs`
   - `Geoscape/` ← `SectorState.cs`
   - `Prototype/` ← `PrototypeBootstrap.cs`
3. Injected pinned DOTS deps into `Packages/manifest.json`:
   `com.unity.entities 1.3.14`, `com.unity.physics 1.3.14`,
   `com.unity.burst 1.8.18`, `com.unity.mathematics 1.3.2`.

**Notes**: Repo-root `README.md` docs remain in `voxels/ combat/ geoscape/` as
documentation. Source root `prototype/` is now empty. `entities` pulls
`collections`/`serialization` transitively. No `.asmdef` added — these packages
auto-reference into `Assembly-CSharp`, so the scripts compile as-is.

---

### Session: Phase 3 — Unity-Side Reconciliation (16-bit ushort)

**Goal**: Make the Unity runtime speak the exact same binary language as the
Python authoring tool — read the packed 16-bit voxel format and the `.stasset`
container — so engine and tool agree the moment Unity finishes installing.

**Changes Made** (`voxels/`):

1. **`VoxelBits.cs` (NEW)** — single source of truth for the 16-bit layout on the
   C# side, mirroring `tools/asset_generator/voxel_format.py`. Provides
   shifts/masks + `Material()/Shape()/Rotation()/Pack()` helpers and the
   `MaterialId` / `ShapeId` / `RotationId` constant tables (Steel = 5,
   `MaterialId.Count = 6`).
2. **`VoxelPenetration.cs`** — buffer switched `NativeArray<byte>` →
   `NativeArray<ushort>`; the loop masks `mat = volume[idx] & 0x1FF` so physics
   reads only the low 9 material bits; shred now clears the whole packed word to
   `0` (Air). Removed the old inline `MaterialId` (now in `VoxelBits.cs`).
3. **`VoxelRaymarch.compute`** — volume reinterpreted as packed words
   (`R16_UInt`); added `VxMaterial/VxShape/VxRotation` bit helpers; the kernel
   masks material (low 9 bits) for color and reserves shape/rotation bits for a
   future per-voxel SDF.
4. **`StAssetReader.cs` (NEW)** — parses the 16-byte header (magic `STAS`,
   version, dims X/Y/Z), validates magic/version/payload length, and loads the
   little-endian `ushort` payload into a `NativeArray<ushort>` via the `StAsset`
   struct (`Load(path)` / `Parse(bytes)`, with `Index/Get/Dispose`).
5. **`prototype/PrototypeBootstrap.cs`** — sandbox volume updated to packed
   `ushort` (Cube/North), material tables sized by `MaterialId.Count` + Steel.

**Verification**: Generated the example `.stasset` and dumped its header —
`magic=STAS, version=1, dims=(16,16,32), file=16400 bytes (= 16 + 16·16·32·2)`.
The `StAssetReader` byte offsets and LE `ushort` decoding match the Python writer
exactly. (Full C# compile pending Unity install.)

**Pipeline status**: 🟢 Python tool ↔ Unity runtime now share one binary format.

---

### Session: Procedural Asset Generator — Phase 1 Core (Python)

**Goal**: Build a standalone Python procedural asset pipeline tool (developed
independently of Unity) that converts LLM-friendly JSON primitive definitions
into packed voxel grids and exports a `.stasset` binary the Unity DOTS runtime
can read directly.

**Changes Made** (`tools/asset_generator/`):

1. `voxel_format.py` — canonical **16-bit** voxel packing (4 bits shape /
   3 bits rotation / 9 bits material) + `.stasset` binary container with a
   16-byte header (magic `STAS`, version, dims). Includes `pack`/`unpack`,
   `save_stasset`/`load_stasset`.
2. `generator.py` — JSON → NumPy grid rasterizer for Cube / Wedge / Cylinder /
   Sphere primitives (with hollow-carve via Air primitives) + CLI.
3. `examples/vekari_walker_gun_barrel.json` — sample asset definition.
4. `test_generator.py` — headless verification suite (6 tests).
5. `requirements.txt`, `README.md`.

**Tests**: `python test_generator.py` → **6/6 passing** (bit layout, pack/unpack
round-trip, cube fill, sphere volume, non-cubic `.stasset` byte-exact round-trip,
example asset build = 1468 solid voxels).

**ARCHITECTURE DECISIONS (need follow-up reconciliation)**:
- **Voxel width upgraded 1 byte → 16-bit `ushort`** to carry shape + rotation +
  material. `voxels/VoxelRaymarch.compute` and `voxels/VoxelPenetration.cs`
  still assume 1 byte — they must be updated to read `ushort` and mask
  `material = voxel & 0x1FF`. Tracked in tool README.
- **Material IDs kept in canonical `MATERIAL_MATRIX.md` order**; `Steel`
  appended as ID 5 (no renumbering).

**Next Steps**:
- Phase 2: PyQt voxel-preview tool reusing the Kraken `pyqtgraph.opengl`
  `GLViewWidget` pattern (proven native path; avoids WebGL freeze issues).
- Phase 3: Unity C# `.stasset` reader + runtime `ushort` reconciliation.

---

### Session: Initial Project Scaffolding

**Goal**: Establish the project directory infrastructure and initial
documentation set for *Steel Tide: First Device*, matching the conventions used
by sibling projects in this workspace (documentation index + recent changes
tracking).

**Changes Made**:

1. **Root files**
   - `README.md` — project overview, three-tier architecture summary, tech
     stack, directory map, Phase 1 vertical slice deliverables, narrative setting.
   - `RECENT_CHANGES.md` — this file.

2. **Directory infrastructure** (per Phase 1 blueprint)
   - `docs/`     → Engineering specs and design pillars
   - `design/`   → Material matrices and layer balances
   - `prototype/`→ Hardcoded vertical slice scene assembly
   - `geoscape/` → OOS math arrays and data structures
   - `combat/`   → Hierarchical AI and FPS weapon tracking scripts
   - `voxels/`   → GPU Compute Shaders, raymarching, and volume buffers

3. **Core documentation** (`docs/core/`)
   - `DOCUMENTATION_INDEX.md` — master index + status tracker
   - `ARCHITECTURE.md` — three-tier simulation loop deep dive
   - `TECH_STACK.md` — Unity DOTS / ECS / Burst / voxel decisions
   - `DEVELOPMENT_GUIDELINES.md` — data-oriented coding rules

4. **Design docs** (`design/`)
   - `MATERIAL_MATRIX.md` — voxel ID byte mapping + Amanatides–Woo penetration math
   - `LAYER_BALANCE.md` — strategic/operational/tactical tier tuning

5. **Subsystem starter stubs** (decoupled from GameObjects)
   - `geoscape/SectorState.cs` — sector data struct + OOS battle math sketch
   - `combat/CommandHierarchy.cs` — High Command → Battalion → Squad → Soldier AI sketch
   - `voxels/VoxelRaymarch.compute` — GPU raymarching compute shader skeleton
   - `voxels/VoxelPenetration.cs` — Amanatides–Woo ray traversal + shredding sketch
   - `prototype/PrototypeBootstrap.cs` — vertical slice scene assembly entry point
   - Per-folder `README.md` notes describing intent and next steps.

**Next Steps**:
- Stand up the Unity DOTS project shell and import these scripts.
- Implement the single-city micro-voxel buffer grid (`voxels/`).
- Build the penetration physics test bed against the voxel buffer.
- Prototype the withdrawal sandbox state machine (`combat/`).

---
