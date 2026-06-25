# Architecture — Steel Tide: First Device

**Version**: 0.1.0-alpha
**Last Updated**: June 25, 2026
**Location**: `docs/core/ARCHITECTURE.md`

---

## 1. The Three-Tier Simulation Loop

The game maintains a living planet without overtaxing hardware by abstracting the
simulation into three nested loops. Detail and cost increase as the loop narrows
toward the player.

```
+-------------------------------------------------------------+
| STRATEGIC LAYER (Geoscape)                                  |
| Tracks planet-wide Sector Arrays (Owner, Power, Infra)      |
+-------------------------------------------------------------+
                              │
                              ▼
+-------------------------------------------------------------+
| OPERATIONAL LAYER                                           |
| Controls Squad movement vectors & regional objectives       |
+-------------------------------------------------------------+
                              │
                              ▼
+-------------------------------------------------------------+
| TACTICAL LAYER (Active FPS Combat)                          |
| Computes real-time physics, AI tracking, voxel rendering    |
+-------------------------------------------------------------+
```

| Layer | Tick Cadence | Data Residence | Workload |
|---|---|---|---|
| Strategic | Seconds–minutes | `NativeArray<SectorState>` in RAM | Background math |
| Operational | ~1–10 Hz | ECS entities (squads/vehicles) | Burst jobs |
| Tactical | Per-frame | ECS + GPU voxel volumes | Physics + compute |

---

## 2. Strategic Layer (Geoscape)

### Out-of-Sight (OOS) Simulation
When the player is not deployed in a sector, battles resolve via background
mathematical abstraction. Each sector is a data tag set, not live entities:

- **Owner** (Human / Vekari / Contested)
- **Human Defense Power**
- **Vekari Assault Force**
- **Terrain Modifier**
- **Infrastructure / Power level**

OOS resolution steps a probabilistic exchange between Defense Power and Assault
Force, modified by terrain, mutating sector ownership over time.

### Probabilistic Damage Overlay Engine
When the player deploys into a sector flagged as bombarded/damaged by the OOS sim,
a **3D noise pass** (Perlin/Simplex) runs during loading to carve craters, holes,
and weapon scars directly into the pristine voxel map **before** runtime graphics
initialize. The world thus reflects its simulated war history with **zero**
real-time storage cost.

---

## 3. Operational Layer

Translates strategic intent into concrete entity motion:

- Spawns squad/vehicle **ECS entities** for the active and adjacent sectors.
- Computes squad **movement vectors** and regional objectives.
- Updates **paths** and asset distribution via Burst jobs.
- Feeds the tactical layer with the entities that must be rendered/simulated in
  full fidelity.

---

## 4. Tactical Layer (FPS Combat)

The only layer running full real-time fidelity:

- Instantiates the **world voxel grid** via GPU compute shaders (see `voxels/`).
- Runs real-time physics (Unity Physics / Havok) including **Dynamic Simulation
  Islands** for structural collapse.
- Executes per-soldier movement, target tracking, and firing via ECS system loops.
- Resolves **voxel penetration** for beams/projectiles (see `MATERIAL_MATRIX.md`).

---

## 5. Hierarchical AI Command Structure

Rather than thousands of independent smart brains, instructions trickle down a
top-down hierarchy into simple execution loops:

1. **High Command AI** — sets planetary objectives from sector status.
2. **Regional / Battalion AI** — breaks objectives into movement paths, asset
   distribution, and flanking maneuvers.
3. **Squad AI** — commands a localized entity set to take a point or suppress.
4. **Individual Soldier Entity** — executes raw movement vectors, basic target
   tracking, and firing scripts via high-speed ECS loops.

> Design intent: cognition lives at the top; the bottom tiers are cheap,
> data-driven executors that scale to tens of thousands of entities.

See `combat/CommandHierarchy.cs` for the structural sketch.

---

## 6. Cross-Layer Data Flow

```
Geoscape SectorState[]  ──(deploy)──►  Damage Overlay Noise Pass  ──►  Voxel Grid
        ▲                                                                  │
        │ (OOS results)                                                    ▼
Operational Squad Spawner  ◄──(objectives)──  High Command AI  ──►  Tactical ECS
```

- Strategic results define **where** combat matters and **how damaged** it is.
- Operational layer decides **who** deploys and their objectives.
- Tactical layer renders and resolves the **moment-to-moment** fight, returning
  outcomes back up to the strategic sector arrays.
