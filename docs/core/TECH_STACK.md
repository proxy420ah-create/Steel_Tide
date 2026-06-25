# Technology Stack — Steel Tide: First Device

**Version**: 0.1.0-alpha
**Last Updated**: June 25, 2026
**Location**: `docs/core/TECH_STACK.md`

---

## 0. Core Mandate

To simulate **thousands of entities** alongside **fully destructible
environments**, the standard Object-Oriented "GameObject" workflow is **not**
used for simulation entities. The project commits fully to a **data-oriented
pipeline**. Voxel buffering scripts are entirely decoupled from traditional
GameObjects.

---

## 1. Core Engine Framework: Unity DOTS

### Entity Component System (ECS)
All troops, vehicles, robotic walkers, and projectiles are stored as **dense data
structs arrayed sequentially in memory cache**. This enables simultaneous
processing of tens of thousands of units at high framerate.

- Components are `IComponentData` structs — no managed references in hot paths.
- Prefer **Struct of Arrays** access patterns for cache locality.
- Systems iterate archetypes; avoid per-entity virtual dispatch.

### C# Job System & Burst Compiler
High-performance, multi-threaded code. Heavy computation is distributed
automatically across all available CPU cores:

- **Enemy flocking AI**
- **Line-of-sight tracing**
- **Structural flood-fills** (island detection)

Burst-compile hot jobs; keep them allocation-free and branch-light.

---

## 2. Physics & Voxel Engine (Teardown-Style)

### GPU Volume Raymarching
Individual blocks have **no triangles**. Buildings, vehicles, and armor plates are
stored as **3D Texture Volumes (Voxel Grids)** where **1 byte = 1 voxel**. A
custom **Compute Shader** renders the bounding volumes on the GPU via raymarching.

→ Skeleton: `voxels/VoxelRaymarch.compute`

### Dynamic Simulation Islands
The voxel world is static on a rigid global grid until a multi-threaded
**flood-fill job** detects that a chunk has lost structural connection to the
foundation/ground. The system then separates those voxels into a **Dynamic Rigid
Body Entity** (Unity Physics / Havok) with mass, velocity, and angular momentum
to collapse organically.

### Debris Optimization
To protect CPU performance:
- Thousands of tiny shattered micro-blocks spawn as **purely visual, non-colliding
  GPU particle effects**.
- The **collision mesh** of the environment is instantly updated globally so units
  can pass through newly blasted openings.

---

## 3. Data-Driven Material & Penetration System

Every voxel maps back to a strict **material ID layout** (see
`design/MATERIAL_MATRIX.md`).

- **Voxel ID Byte Arrays**: each micro-block holds property parameters
  (e.g. `0=Air, 1=Energy Shield, 2=Chobham Armor, 3=Concrete, 4=Flesh`).
- **Raycast Penetration Loop**: beams/projectiles cast a ray through the voxel
  array using a 3D line-stepping algorithm (**Amanatides–Woo**).
- **Shreddable Armor Math**: each stepped block's density reduces penetration
  power. While power > 0, the voxel is **deleted (shredded)**, exposing interior
  layers or vital organs to subsequent shots.

→ Sketch: `voxels/VoxelPenetration.cs`

---

## 4. Recommended Unity Packages

| Package | Purpose |
|---|---|
| `com.unity.entities` | ECS core |
| `com.unity.burst` | Burst compiler |
| `com.unity.collections` | NativeArray / NativeList containers |
| `com.unity.mathematics` | SIMD-friendly math (`float3`, `int3`) |
| `com.unity.physics` | Stateless DOTS physics (Dynamic Islands) |
| `com.unity.rendering.hybrid` | Entities Graphics (if hybrid rendering needed) |

> Pin exact versions in the Unity project's `Packages/manifest.json` once the
> engine shell is created. Validate Burst + Physics compatibility against the
> chosen Unity LTS version.

---

## 5. Performance Principles

1. **Memory locality first** — design data layout before logic.
2. **Job everything heavy** — flocking, LOS, flood-fill, penetration sweeps.
3. **GPU owns the voxels** — keep the volume on the GPU; only island-separated
   chunks become CPU rigid bodies.
4. **Abstract the unseen** — OOS sectors are math, not entities.
5. **Visual ≠ physical** — debris is particles; collision is a global mesh update.
