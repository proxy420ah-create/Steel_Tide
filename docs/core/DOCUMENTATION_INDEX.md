# Steel Tide: First Device - Documentation Index

**Last Updated**: June 25, 2026
**Version**: 0.1.0-alpha — Initial Project Setup
**Location**: `docs/core/DOCUMENTATION_INDEX.md`

> 🚨 **READ FIRST**: This index is the single entry point to all project
> documentation. Consult it before searching the codebase or implementing work.

---

## 📁 Documentation Structure

```
SteelTide/
├── docs/
│   └── core/
│       ├── DOCUMENTATION_INDEX.md     → This file
│       ├── ARCHITECTURE.md            → Three-tier simulation loop
│       ├── TECH_STACK.md              → Unity DOTS / ECS / Burst / voxels
│       └── DEVELOPMENT_GUIDELINES.md  → Data-oriented coding rules
├── design/
│   ├── MATERIAL_MATRIX.md             → Voxel IDs + penetration math
│   └── LAYER_BALANCE.md               → Simulation tier tuning
├── geoscape/   → OOS math arrays and data structures
├── combat/     → Hierarchical AI and FPS weapon tracking
├── voxels/     → GPU compute shaders, raymarching, volume buffers
└── prototype/  → Hardcoded vertical slice scene assembly
```

---

## 🎯 Active Development Phase

> ### Project Status — Phase 1 (Vertical Slice)
>
> **Phase 0 (Scaffolding)**: ✅ **COMPLETE**
> **Phase 1 (Vertical Slice)**: 🚧 **IN PROGRESS**
> **Phase 2 (Operational Layer)**: ⏳ **PENDING**
> **Phase 3 (Geoscape Macro Sim)**: ⏳ **PENDING**
>
> | Component | Status | Priority |
> |---|---|---|
> | **Project Scaffolding** | ✅ Complete | HIGH |
> | **Core Documentation** | ✅ Complete | HIGH |
> | **Voxel Buffer Grid** | ⏳ Pending | HIGH |
> | **Penetration Test Bed** | ⏳ Pending | HIGH |
> | **Withdrawal Sandbox** | ⏳ Pending | HIGH |
> | **Operational ECS Spawning** | ⏳ Pending | MEDIUM |
> | **Geoscape OOS Math** | ⏳ Pending | MEDIUM |

---

## 📚 Core Documentation

### **docs/core/ARCHITECTURE.md** ✅ COMPLETE (Jun 25, 2026)
Three-tier simulation loop (Strategic → Operational → Tactical), OOS simulation,
probabilistic damage overlay, and the hierarchical AI command structure.
- **Priority**: HIGH — foundational system design.

### **docs/core/TECH_STACK.md** ✅ COMPLETE (Jun 25, 2026)
Unity DOTS commitment: ECS, C# Job System, Burst Compiler, Unity Physics/Havok,
GPU volume raymarching, and the GameObject-decoupling mandate.
- **Priority**: HIGH — engine and pipeline decisions.

### **docs/core/DEVELOPMENT_GUIDELINES.md** ✅ COMPLETE (Jun 25, 2026)
Data-oriented coding rules: memory locality, struct-of-arrays, Burst-safety,
documentation-first workflow, and recent-changes discipline.
- **Priority**: HIGH — coding standards.

---

## 🎨 Design Documentation

### **design/MATERIAL_MATRIX.md** ✅ COMPLETE (Jun 25, 2026)
Voxel ID byte mapping (0=Air … 4=Flesh), per-material density/penetration
parameters, and the Amanatides–Woo raycast shredding loop.

### **design/LAYER_BALANCE.md** ✅ COMPLETE (Jun 25, 2026)
Tuning parameters for OOS battle math, squad arrival rates, and tier update
cadences.

---

## 🧩 Subsystem Starters

| File | Purpose |
|---|---|
| `geoscape/SectorState.cs` | Sector data struct + OOS battle math sketch |
| `combat/CommandHierarchy.cs` | 4-tier AI command structure sketch |
| `voxels/VoxelRaymarch.compute` | GPU raymarching compute shader skeleton |
| `voxels/VoxelPenetration.cs` | Amanatides–Woo traversal + voxel shredding |
| `prototype/PrototypeBootstrap.cs` | Vertical slice scene assembly entry point |

---

## 🔄 Maintenance Protocol

1. Update this index whenever a doc or major subsystem is added.
2. Log every working session in `RECENT_CHANGES.md` (root).
3. Document subsystems only after they are functional (avoid premature docs).
