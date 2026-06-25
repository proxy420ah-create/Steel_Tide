# Steel Tide: First Device

**Version**: 0.1.0-alpha
**Created**: June 25, 2026
**Status**: 🚧 PRE-PRODUCTION — Phase 1 Vertical Slice

---

## 🎯 Project Overview

**Steel Tide: First Device** is a high-performance, large-scale sci-fi war
simulation sandbox. The player operates not as a chosen savior or supreme
commander, but as a single, highly valuable combat asset (a soldier) within a
persistent, dynamically evolving planetary conflict that functions independently
around them.

The game fuses frantic macro-scale carnage with a simulation-first, fully
destructible micro-voxel world, all driven by a persistent strategic layer that
tracks an adaptive, intelligent enemy faction (the **Vekari**).

### Primary Inspirations

- **Earth Defense Force / Helldivers** — frantic macro-scale carnage, massive
  infantry formations, giant robotic walkers, heavy beam weapons.
- **Teardown** — simulation-first, micro-voxel destruction with dynamically
  calculated structural integrity.
- **BattleBit Remastered** — minimalist low-poly graphics to maximize
  performance, tick rates, and massive entity counts.
- **PlanetSide** — massive combined-arms battles with shifting frontlines across
  an interconnected theater of war.
- **XCOM / Phoenix Point** — a persistent strategic layer (Geoscape) tracking an
  adaptive enemy faction driving macro operations.

---

## 🏗️ Architecture (Three-Tier Loop)

```
[ Strategic Layer (Geoscape) ]  --> Background math updates Sector Arrays in RAM
               │
[ Operational Layer ]           --> Spawns Squad/Vehicle Entities & updates Paths via ECS
               │
[ Tactical Layer (FPS) ]        --> Instantiates World Voxel Grid via GPU Compute Shaders
```

| Layer | Responsibility | Cost Model |
|---|---|---|
| **Strategic (Geoscape)** | Planet-wide sector arrays (owner, power, infrastructure), OOS battle math | Background abstraction (cheap) |
| **Operational** | Squad movement vectors, regional objectives, asset distribution | ECS jobs (mid) |
| **Tactical (FPS)** | Real-time physics, AI tracking, voxel raymarching & destruction | GPU + Burst jobs (heavy) |

---

## 🧰 Technology Stack

- **Engine**: Unity **DOTS** (Data-Oriented Technology Stack)
- **Entities**: ECS — all troops, vehicles, walkers, projectiles stored as dense
  data structs in sequential cache-friendly memory.
- **Compute**: C# Job System + Burst Compiler for multi-threaded flocking AI,
  line-of-sight tracing, and structural flood-fills.
- **Physics**: Unity Physics / Havok for Dynamic Simulation Islands.
- **Voxels**: Custom HLSL **Compute Shaders** for GPU volume raymarching of 3D
  Texture Volumes (1 byte = 1 voxel).
- **Destruction**: Amanatides–Woo voxel ray traversal for penetration; flood-fill
  islanding for organic structural collapse.

> ⚠️ **Architectural mandate**: The standard Object-Oriented "GameObject"
> workflow is **not** used for simulation entities. The project commits fully to
> a data-oriented pipeline. Voxel buffering scripts are entirely decoupled from
> traditional GameObjects.

---

## 📁 Directory Structure

```
SteelTide/
├── docs/          → Engineering specs and design pillars
├── design/        → Material matrices and layer balances
├── prototype/     → Hardcoded vertical slice scene assembly
├── geoscape/      → OOS math arrays and data structures
├── combat/        → Hierarchical AI and FPS weapon tracking scripts
└── voxels/        → GPU Compute Shaders, raymarching, and volume buffers
```

---

## 🚀 Phase 1 Prototype Goal (Vertical Slice)

Construct a small-scale, fully decoupled framework testing the core loop before
expanding into macro systems.

### Required Deliverables

1. **Single City Sandbox Map** — built natively from a low-poly micro-voxel
   buffer grid.
2. **Penetration Physics Test Bed** — C# implementation using raw data array
   adjustments to simulate structural armor destruction via heavy linear beams.
3. **The Withdrawal Sandbox** — a script demonstrating the transition from an
   organized line of defense to an automated squad retreat sequence based on
   simulated enemy arrival rates.

---

## 🗺️ Narrative Setting

Humanity is a spacefaring civilization. The game takes place on a contested
**Frontier Colony** — an industrial/resource outpost that is a critical gateway
into deep human-controlled space. The **Vekari**, a disciplined near-peer alien
civilization, use newly engineered **Rift Technology** to deploy elite force
packages silently and establish an anchor footprint.

**Act I — "The False Confidence"**: A routine border skirmish becomes a scripted
military disaster when the **First Device** (the Vekari rift matrix) opens. The
mission dynamically shifts from *Defend the Colony* to *Preserve Combat Power*,
concluding with a desperate strategic withdrawal.

---

## 📚 Documentation

- [Documentation Index](docs/core/DOCUMENTATION_INDEX.md) — start here
- [Architecture](docs/core/ARCHITECTURE.md) — three-tier loop deep dive
- [Tech Stack](docs/core/TECH_STACK.md) — Unity DOTS decisions
- [Development Guidelines](docs/core/DEVELOPMENT_GUIDELINES.md)
- [Material Matrix](design/MATERIAL_MATRIX.md) — voxel IDs & penetration math
- [Layer Balance](design/LAYER_BALANCE.md) — simulation tier tuning
- [Recent Changes](RECENT_CHANGES.md)

---

**Status**: 🟢 Project scaffolding established — ready for prototype implementation.
