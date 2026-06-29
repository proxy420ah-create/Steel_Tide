# Steel Tide: First Device - Documentation Index

**Last Updated**: June 29, 2026
**Version**: 0.2.0-alpha — VoxelWorld Architecture Established
**Location**: `docs/core/DOCUMENTATION_INDEX.md`

> 🚨 **READ FIRST**: This index is the single entry point to all project
> documentation. Consult it before searching the codebase or implementing work.

---

## ⚠️ CRITICAL: MANDATORY FIRST READ

### **🔒 docs/VOXELWORLD_ARCHITECTURE.md** ✅ FOUNDATIONAL (Jun 29, 2026)

**STATUS**: 🔴 **MANDATORY READING FOR ALL DEVELOPMENT**

This document defines the **VoxelWorld-centric architecture** that is the foundation
of Steel Tide's dynamic voxel world system. Every feature, system, and enhancement
MUST be compatible with this architecture.

**You MUST read this document BEFORE**:
- Implementing ANY voxel-related feature
- Working with collision or physics systems
- Modifying VoxelObject, VoxelWorld, or VoxelPhysics
- Adding AI pathfinding or gameplay systems
- Implementing multiplayer synchronization
- Creating save/load systems

**Key Concepts**:
- VoxelWorld is the single source of truth (sparse dictionary)
- VoxelObject syncs TO VoxelWorld (not the other way around)
- Visual and physics state are ALWAYS synchronized
- All systems query VoxelWorld for collision/physics/AI
- Destroyed voxels are removed from dictionary (performance optimization)

**Location**: `docs/VOXELWORLD_ARCHITECTURE.md`  
**Priority**: 🔴 **CRITICAL** — Read this FIRST, before any other documentation

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

## 🧊 Voxel Systems Documentation

### **docs/VOXELWORLD_ARCHITECTURE.md** 🔴 CRITICAL (Jun 29, 2026)
**MANDATORY FIRST READ** — VoxelWorld-centric architecture, synchronization patterns,
data flow, and critical rules for all voxel-related development.
- **Priority**: 🔴 **CRITICAL** — Foundation for all voxel systems

### **docs/VOXEL_METRICS_AND_UNITS.md** ✅ COMPLETE
Coordinate systems, voxel sizing, world-to-grid conversion, and scaling guidelines.
- **Priority**: HIGH — Required for spatial calculations

### **docs/VOXEL_COORDINATE_SYSTEM.md** ✅ COMPLETE
Detailed coordinate transformation math and examples.
- **Priority**: MEDIUM — Reference for coordinate conversions

### **docs/VOXEL_SCALING_TOOL.md** ✅ COMPLETE
Tools and utilities for voxel asset scaling and optimization.
- **Priority**: LOW — Asset pipeline utilities

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
