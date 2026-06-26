# Recent Changes - Steel Tide: First Device

**Last Updated**: June 25, 2026
**Version**: 0.1.0-alpha

---

## Current Session Changes

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
