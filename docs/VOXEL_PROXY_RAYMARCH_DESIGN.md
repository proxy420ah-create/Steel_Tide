# Voxel Rendering Migration: Proxy-Box Raymarch + SV_Depth

**Status:** DESIGN (pre-implementation)
**Author:** Cascade (pair session)
**Date:** 2026-06-28
**Supersedes (render path only):** compute-dispatch-per-volume in `VoxelRenderer.cs` + `VoxelRaymarch.compute`
**Leaves intact:** voxel data model (`VoxelObject`), `.stasset` pipeline, `VoxelWeaponController` damage logic, `VoxelWorld` collision registration.

---

## 1. Problem Statement

### 1.1 Symptom
Generated volumes (e.g. the high-density sphere) **bleed through walls** of other volumes — geometry that is physically behind renders in front.

### 1.2 Confirmed Root Cause (depth value bug)
In `VoxelRaymarch.compute` the per-pixel depth is recorded as **distance from each volume's bounding-box entry point**, not distance from the camera.

- DDA `tMax` (`sideDist`) is seeded relative to the volume entry point.
- After the first DDA step, `currentT` is overwritten with that from-entry value.
- Stored depth collapses to `depth = currentT` (since `worldEntry == tStart`, the expression `worldEntry + (currentT - tStart)` reduces to `currentT`).

Consequence: a volume whose first solid surface is deep inside its AABB (a thick sphere) reports an artificially **small** depth and wins every depth comparison. Thin shells (buildings) are hit on the first step where `currentT == tStart`, so they coincidentally look correct. This asymmetry is exactly the observed bug.

> The minimal hotfix (`tMax = tStart + sideDist; depth = currentT;`) corrects depth values within the current architecture. This document instead specifies the **target architecture** because the project vision requires mass-scale destructible volumes that must also interleave with normal Unity geometry — neither of which the compute-composite path handles well.

### 1.3 Architectural Limits of the Current Path
- **Ordering fragility:** correctness depends on a CPU back-to-front `Sort()` plus read-modify-write of shared `_Output`/`_DepthBuffer` between N dispatches.
- **No scene integration:** voxel output is blitted over the camera target; it cannot correctly occlude or be occluded by normal Unity meshes (players, props, particles, VFX).
- **Cost scales with screen, not coverage:** every dispatch is effectively full-screen regardless of how few pixels the volume covers.

---

## 2. Goals & Non-Goals

### 2.1 Goals
1. **Correct occlusion** among all voxel volumes with no CPU sort and no inter-dispatch state. *(Primary — this is the bleed-through fix. The hardware Z-buffer resolves nearest-voxel per pixel automatically; it does not depend on using Unity meshes.)*
2. **Scale to mass volumes** (hundreds–thousands of destructible buildings/props/units/interiors) with **per-volume cost proportional to screen coverage** via free frustum + early-Z culling from proxy-box rasterization.
3. **Preserve** the existing data model, `.stasset` loading, damage, and collision systems.

> **On "scene integration":** This is an all-voxel/raymarched game; Unity 3D meshes are **not** used for game content. Therefore "occlude/be-occluded by normal Unity meshes" is **not** a primary goal. The hardware Z-buffer is justified by Goals 1–2 alone. It remains a *bonus* for any **rasterized effects you do use** — particles/VFX (soft particles, depth-fade), decals, world-space UI, skybox, and debug gizmos — which composite correctly against voxel depth for free.

### 2.2 Non-Goals (this phase)
- Per-voxel shape SDFs (wedge/cylinder/sphere bits) — remain cubes for now.
- Transparency/alpha blending between volumes (Transparent Aluminum, etc.) — opaque-only first; transparency is a later pass.
- Rotated volumes — volumes remain axis-aligned (matches current `GetVolumeOffset() == transform.position`). Rotation support is a documented future extension.
- GI/shadows/ambient occlusion.

---

## 3. Target Architecture: Proxy-Box Raymarch + SV_Depth

### 3.1 Concept
For each volume, rasterize its **bounding box** (a unit cube scaled to `dims * voxelSize`, positioned at `transform.position`). In the **fragment shader**, reconstruct the world-space ray for that pixel, run the DDA through the volume's voxel buffer, and:
- on a solid hit: output the material-shaded color and write `SV_Depth` = clip-space depth of the world hit point;
- on no hit (ray passes through only air): `discard`.

The GPU's depth test then resolves all ordering — between volumes and against the rest of the scene — with no CPU sort.

### 3.2 Why this satisfies the goals
- **Correctness/integration:** `SV_Depth` participates in the hardware Z-buffer (Goal 1, 2).
- **Scalability:** a volume only shades the fragments its box covers; off-screen boxes are frustum-culled; occluded boxes are skipped by early-Z before the expensive raymarch (Goal 3, 4).
- **Mass submission:** boxes are drawn via instanced/indirect draws (Goal 3).

### 3.3 Pipeline diagram (per camera, per frame)
```
URP opaque queue
  └─ For each visible VoxelObject proxy box (instanced/indirect draw):
       Vertex:   place unit cube at (position, dims*voxelSize); pass world ray data
       Fragment: build ray (ro, rd) from camera through this pixel
                 intersect box (entry tStart)
                 DDA through _VoxelData buffer for THIS volume
                 if solid hit:
                     color  = shade(material, faceNormal)
                     SV_Depth = ClipDepth(worldHit)   // reversed-Z aware
                 else:
                     discard
  └─ Hardware early-Z + depth test resolves all occlusion automatically
```

---

## 4. Coordinate & Depth Math (critical correctness section)

### 4.1 Volume world bounds
A volume occupies the **axis-aligned** world AABB:
```
volumeMinWorld = transform.position
volumeMaxWorld = transform.position + float3(dims) * voxelSize
```
This matches the current `GetVolumeOffset()` returning `transform.position` and corner-anchored gizmos.

### 4.2 Ray construction (per fragment)
```
ro = _WorldSpaceCameraPos
rd = normalize(worldPosFromVertex - ro)   // interpolated world pos on the box, or reconstruct from screen UV + inverse VP
```
Either interpolate the box's world-space position from the vertex stage, or reconstruct from depth-uv via inverse view-projection. Interpolated box world position is simpler and avoids matrix edge cases.

### 4.3 Camera-space distance (fix the original bug here)
Seed the DDA so the running parameter is **distance from the camera**, not from the entry point:
```
tStart = max(tNear, 0)                 // ray-box entry, camera-relative
tMax   = tStart + sideDist             // <-- the fix: include tStart
tDelta = deltaDist
currentT = tStart
...
// on hit:
worldHit = ro + rd * currentT          // currentT is true camera distance
```

### 4.4 World hit → clip depth (reversed-Z aware)
```
float4 clip = mul(UNITY_MATRIX_VP, float4(worldHit, 1.0));
float  ndcZ = clip.z / clip.w;         // already reversed-Z on modern Unity/D3D/Metal/Vulkan
// SV_Depth expects the platform depth value directly:
outDepth = ndcZ;                       // URP: do NOT manually re-invert; UNITY_MATRIX_VP encodes reversed-Z
```
Notes:
- On platforms with reversed-Z (default on modern Unity), near = 1, far = 0; `clip.z/clip.w` is already in that convention.
- If validation shows inverted occlusion, the single corrective lever is whether to use `clip.z/clip.w` vs `(clip.z/clip.w)` passed through `UNITY_REVERSED_Z` handling — to be locked during Phase 1 on the actual target (Windows/D3D11/D3D12).

### 4.5 DDA traversal (unchanged logic, corrected seeding)
Voxel indexing remains C-order, identical to CPU/`VoxelObject.GetVoxel` and `StAssetReader`:
```
index = x + y * dims.x + z * dims.x * dims.y
```
Face normal tracked per axis-step for flat shading (as today).

---

## 5. Shader Interface Contract

### 5.1 Per-material (global, set once)
| Name | Type | Source |
|---|---|---|
| `_MaterialColors` | `StructuredBuffer<float4>` | `VoxelRenderer.materialColors` (21 entries) |
| `_MaterialCount` | `int` | `materialColors.Length` |

### 5.2 Per-volume (per draw, or per-instance buffer)
| Name | Type | Source (`VoxelObject`) |
|---|---|---|
| `_VoxelData` | `StructuredBuffer<uint>` | `GetVoxelBuffer()` |
| `_VolumeDims` | `int3` | `GetVolumeDims()` |
| `_VoxelSize` | `float` | `GetVoxelSize()` |
| `_VolumeOrigin` | `float3` | `transform.position` (`GetVolumeOffset()`) |

### 5.3 Per-instance struct (for indirect path, Phase 3)
```hlsl
struct VolumeInstance
{
    float3 origin;     // transform.position
    float  voxelSize;
    int3   dims;
    uint   bufferIndex; // index into a bindless table of voxel buffers
};
StructuredBuffer<VolumeInstance> _Instances;
```
> Per-instance binding of a *different* `StructuredBuffer<uint>` per volume is the core technical challenge (HLSL has no variable-length buffer arrays without bindless). See §7.

---

## 6. Render-Path Changes (`VoxelRenderer.cs`)

### 6.1 Removed
- `DispatchMultiVolume()` compute dispatch loop.
- CPU back-to-front `Sort()`.
- `_Output` / `_depth` RenderTextures and the `cmd.Blit(_output, …)` composite.
- Manual `_DepthBuffer` management in the compute kernel.

### 6.2 Added
- A `Material` using the new raymarch **fragment** shader.
- A shared unit-cube `Mesh` (proxy geometry).
- Per-volume draw issuance during the URP opaque pass.

### 6.3 Draw issuance options (staged)
- **Phase 2 (draw-per-volume):** for each registered volume, set per-volume material properties (`MaterialPropertyBlock`) and `Graphics.RenderMesh` the proxy cube. Simple, correct, validates the approach. ~1 draw/volume.
- **Phase 3 (indirect-instanced):** `Graphics.RenderMeshIndirect` with `_Instances` buffer + bindless voxel buffers. Thousands of volumes in a few draws.

### 6.4 URP integration point
Issue draws so they land in the **opaque** queue with depth write/test ON, before transparents. Two viable hooks:
- A `ScriptableRendererFeature` + `ScriptableRenderPass` (preferred, URP-native), or
- `RenderPipelineManager.beginCameraRendering` issuing `Graphics.RenderMesh*` with the proxy material (lighter touch; current code already uses these callbacks).

Decision: **start with `RenderMesh*` from the existing camera callback** (lowest restructuring), migrate to a `RendererFeature` if we need explicit pass ordering or multiple cameras.

---

## 7. The Hard Part: Per-Volume Buffer Binding at Scale

HLSL cannot index an array of distinct `StructuredBuffer`s directly. Options, in increasing capability:

1. **Draw-per-volume (Phase 2):** bind one `_VoxelData` per draw via `MaterialPropertyBlock`. Trivial, but draw-call count == volume count.
2. **Merged mega-buffer:** concatenate all volumes' voxels into one big `StructuredBuffer<uint>`; per-instance `bufferOffset` + `dims`. One bind, one indirect draw. Requires managing offsets and re-upload on destruction (or a free-list / chunk allocator).
3. **Bindless / `ResourceDescriptorHeap` (SM6.6) or `Texture2DArray`/3D atlas:** true per-instance buffer indexing. Most scalable, most platform-sensitive.

**Recommendation:** Phase 2 uses option 1 to validate visuals. Phase 3 adopts **option 2 (merged mega-buffer + per-instance offsets)** — it is the best fit for "mass destructible volumes" because:
- destruction only flips voxels to Air (no size change) → offsets stay stable;
- a single indirect draw covers the whole scene;
- avoids bindless platform fragmentation.

---

## 8. Interaction With Existing Systems

| System | Impact |
|---|---|
| `.stasset` load (`StAssetReader`) | None. |
| `VoxelObject` data model | Add nothing required for Phase 1–2; Phase 3 needs a hook to write into the merged buffer at the volume's offset. |
| `VoxelWeaponController` damage | None — CPU raymarch for hit detection is independent; `SetVoxel` still re-uploads. Phase 3: `SetVoxel` writes into the merged buffer slice instead of a private buffer. |
| `VoxelWorld` collision | None. |
| Material palette (21) | Reused as-is via `_MaterialColors`/`_MaterialCount`. |

---

## 9. Phased Migration Plan

### Phase 1 — Single volume, prove SV_Depth (lowest risk)
- New fragment raymarch shader on one proxy box.
- Apply camera-distance depth fix (§4.3) and clip-depth output (§4.4).
- Validate: one volume correctly occludes AND is occluded by a test Unity cube/plane; no bleed-through; depth correct from all angles.
- **Exit criteria:** a Unity primitive mesh can pass in front of and behind the voxel sphere with correct occlusion.

### Phase 2 — Proxy box per volume (draw-per-volume)
- Renderer issues one proxy draw per registered `VoxelObject` via `MaterialPropertyBlock`.
- Remove compute path, RTs, blit, and CPU sort.
- Validate: sphere + multiple buildings render with correct mutual occlusion; original bleed-through scene is fixed.
- **Exit criteria:** the reported failing scene renders correctly with zero sort logic.

### Phase 3 — Indirect instancing + merged buffer (scale)
- Merge voxel data into a mega-buffer with per-instance offsets.
- `Graphics.RenderMeshIndirect` with `_Instances`.
- Route `SetVoxel`/destruction writes into the merged buffer slice.
- Validate: stress test with N=500–2000 volumes; measure draw calls and GPU frame time.
- **Exit criteria:** target volume count renders in a small, bounded number of draw calls at interactive frame rate.

---

## 10. Test Plan

### 10.1 Correctness assets
- `Test_Occlusion_Pair.stasset`: two solid blocks placed so one is clearly behind the other from the test camera.
- Reuse `Sphere_test.stasset` (the thick volume that exposed the bug).
- A standard Unity cube mesh (non-voxel) for scene-integration occlusion.

### 10.2 Checks
1. **No bleed-through:** sphere never draws in front of a nearer wall from any orbit angle.
2. **Scene integration:** Unity cube correctly occludes/occluded by voxels (Phase 1+).
3. **Depth from all axes:** rotate camera 360°/over-under; occlusion stable.
4. **Damage unaffected:** shooting still carves correct voxels (regression vs current behavior).
5. **Reversed-Z sanity:** near objects test as nearer on the actual D3D target.
6. **Scale (Phase 3):** draw-call count and frame time within budget at target N.

### 10.3 Diagnostics retained
- Material-ID clamp (`min(mat, _MaterialCount-1)`) preserved.
- Optional debug: visualize linear depth as grayscale to confirm monotonic camera distance.

### 10.4 Performance Instrumentation (Coder Clarification #1)
Phase 2 issues one proxy draw per volume; early-Z helps but cost is dominated by **shaded fragments x DDA steps**. Watch these during validation:

| Metric | Where to read it | Healthy signal | Red flag |
|---|---|---|---|
| **Draw calls / SetPass** | Unity Frame Debugger; Profiler `Rendering` module | ~1 draw per visible volume (Phase 2) | growth uncorrelated with visible volume count |
| **GPU frame time** | Profiler `GPU` track (or RenderDoc) | stable as off-screen volumes are added (frustum cull working) | rises when off-screen volumes added |
| **Overdraw / fragments shaded** | RenderDoc overlay; or a debug heatmap writing DDA-step-count to color | overlapping boxes do **not** all run full DDA (early-Z culls occluded) | bright heatmap behind near walls = early-Z not engaging |
| **DDA steps per pixel** | shader debug mode: output `stepCount / _MaxSteps` as grayscale | bounded; thin shells exit fast | pixels routinely hitting `_MaxSteps` |
| **Early-Z effectiveness** | A/B: render front-to-back vs unsorted draw order | near-identical cost both ways | large delta = relying on draw order, not hardware Z |

Validation guidance:
- **Force early-Z:** ensure the shader does **not** write `SV_Depth` *and* `discard` in a way that disables early-Z. Outputting `SV_Depth` from a shader normally forces *late* depth test, which **defeats early-Z**. Mitigation: declare `[earlydepthstencil]` only when correct, OR keep depth conservative. **This is a known sharp edge — measure it explicitly in Phase 1.** (See Risks.)
- Add a runtime counter on the CPU: number of proxy boxes submitted vs number frustum-culled.
- Capture a baseline RenderDoc frame at the end of Phase 1 to compare against in regressions.

### 10.5 Automated Validation Harness (Coder Clarification #4)
Provide a repeatable scene-setup so regressions are one click:

- **`VoxelOcclusionTestHarness.cs`** (editor/play-mode helper): on `Start`, programmatically spawns the test layout:
  - `Sphere_test` volume + a thin wall volume positioned so the wall is nearer the camera.
  - `Test_Occlusion_Pair` blocks (one behind the other).
  - a fixed test camera at a known transform + a scripted 360° orbit coroutine.
- **Assertions (play-mode test, `tests/`):** read back the camera target / depth and assert:
  1. at orbit angle A, wall pixels occlude sphere pixels at the overlap region;
  2. no frame shows background `_BackgroundColor` where a volume is known to cover;
  3. depth is monotonic with camera distance for a sampled scanline.
- **Output:** pass/fail + a saved screenshot per orbit step into `test_logs/voxel_occlusion/` for visual diffing.
- Keep it asset-driven so new `.stasset` cases can be dropped in without code changes.

---

## 11. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Reversed-Z / clip-depth sign wrong → inverted occlusion | Lock in Phase 1 on the real D3D target with a 2-object A/B test before scaling. |
| Per-volume draw calls too many (Phase 2) | Expected; Phase 2 is validation-only. Phase 3 indirect solves it. |
| Bindless buffer indexing platform gaps | Choose merged mega-buffer (§7 option 2) instead of bindless. |
| Destruction re-upload cost | Voxel edits flip to Air in-place (no realloc); upload only the dirty slice. |
| URP pass ordering (voxels vs transparents) | Use opaque-queue draws; migrate to `ScriptableRendererFeature` if ordering control is needed. |
| Rotated volumes later | Document AABB-only assumption now; rotation needs OBB ray test + per-instance matrix. |
| **`SV_Depth` disables early-Z** | Writing `SV_Depth` normally forces late depth testing, killing the early-Z culling that Phase 2 scalability relies on. Measure overdraw in Phase 1; if confirmed, evaluate `[earlydepthstencil]` (only valid when depth output is conservative) or a depth-prepass. **Highest-impact perf risk.** |

---

## 12. Open Decisions (to confirm before Phase 1 code)
1. **Ray reconstruction:** interpolated box world-position (simple) vs inverse-VP from screen UV. *Proposed: interpolated world position.*
2. **Draw hook:** `RenderMesh*` from camera callback (light) vs `ScriptableRendererFeature` (URP-native). *Proposed: `RenderMesh*` first.*
3. **Phase 3 buffer strategy:** merged mega-buffer (proposed) vs bindless.
4. **Transparency:** defer (proposed) vs include Transparent Aluminum in Phase 1.

---

## 13. Phase 3 Voxel Mutation API (Coder Clarification #3)

Goal: destruction/damage must keep working when voxel data moves from per-volume `ComputeBuffer`s into the **merged mega-buffer**, without regressing `VoxelWeaponController`.

### 13.1 Current write path (preserve semantics)
- `VoxelWeaponController` computes hits on the CPU and calls `VoxelObject.SetVoxel(x,y,z,value)`.
- `SetVoxel` mutates the managed `ushort[] voxelData` then calls `UploadToGPU()` which re-uploads the **entire** per-volume `uint[]` via `ComputeBuffer.SetData`.

### 13.2 Phase 3 target: slice writes into the merged buffer
Introduce an allocation + mutation indirection so callers don't change:

```
VoxelBufferRegistry (new, owns the merged ComputeBuffer)
  RegisterVolume(VoxelObject) -> returns int bufferOffset (in voxel words)
  WriteVoxel(int bufferOffset, int localIndex, uint packed)   // single-voxel
  WriteRange(int bufferOffset, int localStart, uint[] data)   // batched edits
  UnregisterVolume(VoxelObject)
```

- **`VoxelObject.SetVoxel` is rewritten** to: update managed `voxelData[localIndex]`, then call `VoxelBufferRegistry.WriteVoxel(myOffset, localIndex, packed)`. **Public signature unchanged**, so `VoxelWeaponController` and all callers are untouched.
- **Upload mechanism:** `ComputeBuffer.SetData(data, managedStart, computeBufferStart, count)` writes only the dirty slice — no full re-upload, no realloc (destruction only flips to Air, dims are stable).
- **Batched damage:** weapon hits affect a radius of voxels in one frame → accumulate dirty `localIndex`es per volume per frame and flush as one or a few contiguous `WriteRange` calls in `LateUpdate` (dirty-rect / min-max index span) to avoid many tiny `SetData` calls.

### 13.3 Regression guarantees
- `SetVoxel`/`SetVoxel(int3)` keep identical signatures and visible behavior.
- `GetVoxel`/`GetVoxelData` continue to read the managed mirror (authoritative for CPU raymarch hit-detection).
- The merged buffer is a **GPU mirror**; the managed `ushort[]` remains source of truth for gameplay/collision. No dual-write divergence because every mutation goes through `SetVoxel`.
- Add a play-mode test: shoot a known voxel, assert `GetVoxel` returns Air **and** a GPU readback of the merged slice matches.

### 13.4 Allocation strategy
- Offsets assigned at registration; freed offsets returned to a free-list and reused by same-or-smaller volumes (or compacted offline). Since dims never change at runtime, no mid-life resizing is required.

---

## 14. Future Work

### 14.1 Semi-Transparent Materials (Coder Clarification #2)
Transparent Aluminum (id 10) and any future translucent materials **cannot** be resolved by opaque depth-test alone — they need **order-independent transparency (OIT)** or a manual back-to-front composite:

- **Pass split:** render opaque voxels first (this design), then a **separate transparent pass** for translucent voxels.
- **Options:** (a) per-pixel linked-list / weighted-blended OIT (McGuire) for true order independence; (b) depth-peeling (simpler, costlier); (c) for a *single* translucent layer, sort that layer back-to-front and alpha-blend after the opaque depth is laid down.
- **Interaction with merged buffer:** the DDA can early-out at the first opaque hit for the opaque pass, and accumulate translucent samples up to that depth in the transparent pass (front-to-back accumulation with the opaque depth as the stop distance).
- **Status:** out of scope for Phases 1–3; revisit once opaque mass-volume rendering is validated.

### 14.2 Rotated / Transformed Volumes
Current design assumes axis-aligned, corner-anchored AABBs. Rotation requires an OBB ray test in the fragment shader and a per-instance world matrix in `VolumeInstance`.

### 14.3 Per-Voxel Shape SDFs
Shape/rotation bits (`VxShape`/`VxRotation`) currently unused by the renderer; later select per-voxel SDFs for wedges/cylinders.

---

## 15. References
- Unity Discussions — *Rendering volumetric objects and maintaining correct depth* (write raymarch depth to Z-buffer; clip-space conversion; reversed-Z): https://discussions.unity.com/t/231113
- Matias Lavik — *Volume Rendering in Unity* (box-entry raymarch + depth output): https://matiaslavik.wordpress.com/2020/01/19/volume-rendering-in-unity/
- NVIDIA GPU Gems Ch.39 — *Volume Rendering Techniques* (compositing, early-out, proxy depth): https://developer.nvidia.com/gpugems/gpugems/part-vi-beyond-triangles/chapter-39-volume-rendering-techniques
- Will Usher — *Volume Rendering with WebGL* (front-to-back compositing, early termination): https://www.willusher.io/webgl/2019/01/13/volume-rendering-with-webgl/
- *Aokana: A GPU-Driven Voxel Rendering Framework for Open World Games* (tiled indirect dispatch, occlusion at scale): https://arxiv.org/html/2505.02017v1

---

## 16. Appendix: Current-Code Anchors
- Depth bug: `Assets/Voxels/VoxelRaymarch.compute` (`sideDist`/`tMax` seeding ~L153-165; depth write ~L214-218).
- Dispatch loop + CPU sort: `Assets/Voxels/VoxelRenderer.cs` `DispatchMultiVolume()` (~L188-258).
- Volume API: `Assets/Voxels/VoxelObject.cs` `GetVoxelBuffer/GetVolumeDims/GetVoxelSize/GetVolumeOffset` (~L157-161).
- Indexing parity: `VoxelObject.GetVoxel` (~L166), `StAssetReader`, compute `VoxelIndex` (~L57-60).
- Damage independence: `Assets/Combat/VoxelWeaponController.cs` (CPU raymarch + `SetVoxel`).
