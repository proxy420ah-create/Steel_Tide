# Development Guidelines — Steel Tide: First Device

**Version**: 0.1.0-alpha
**Last Updated**: June 25, 2026
**Location**: `docs/core/DEVELOPMENT_GUIDELINES.md`

---

## 1. Documentation-First

Before implementing or searching the codebase, read
`docs/core/DOCUMENTATION_INDEX.md` and any relevant linked doc. Keep the index
current as the project grows.

---

## 2. Data-Oriented Discipline (DOTS)

- **No GameObjects for simulation entities.** Soldiers, vehicles, walkers, and
  projectiles are ECS `IComponentData` structs.
- **Design data layout before logic.** Favor Struct-of-Arrays and cache locality.
- **Keep hot paths allocation-free.** Use `NativeArray`/`NativeList` with explicit
  `Allocator` scopes; dispose deterministically.
- **Burst-safety.** Hot jobs must be Burst-compilable: no managed types, no
  exceptions in steady state, minimal branching.
- **Decouple the voxel layer.** Voxel buffers live in GPU volumes / native byte
  arrays — never as per-block GameObjects or components.

---

## 3. Layered Cost Awareness

Match work to the correct simulation tier (see `ARCHITECTURE.md`):
- Strategic → background math on sector arrays.
- Operational → ECS jobs at low Hz.
- Tactical → per-frame physics/compute only for the active theater.

Do not run tactical-fidelity logic for out-of-sight sectors.

---

## 4. Event-Driven / Data-Driven State

State should be driven by **data and events**, not wall-clock timers where a
data-count or simulation-step trigger is more deterministic (e.g. enemy arrival
based on simulated assault rate, not a fixed countdown).

---

## 5. Recent Changes Discipline

After each working session, append a dated entry to `RECENT_CHANGES.md` (root)
describing the goal, changes, and next steps. Document a subsystem in `docs/`
**only after it is functional** to avoid premature/fragmented docs.

---

## 6. Naming & Structure

| Folder | Contents |
|---|---|
| `geoscape/` | Sector arrays, OOS math, strategic data structures |
| `combat/` | AI command hierarchy, weapon/penetration tracking, FPS logic |
| `voxels/` | Compute shaders, raymarching, volume buffers, islanding |
| `prototype/` | Hardcoded vertical-slice scene assembly |
| `design/` | Material matrices, layer balance, tuning tables |
| `docs/` | Engineering specs and design pillars |

Keep C# files single-responsibility; keep compute shaders alongside their C#
dispatch wrappers in `voxels/`.
