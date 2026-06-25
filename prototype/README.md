# prototype/ — Phase 1 Vertical Slice (Hardcoded Scene Assembly)

A small-scale, fully decoupled framework that exercises the core loop before
expanding into the macro systems. Everything here is intentionally hardcoded for
fast iteration.

## Files
- `PrototypeBootstrap.cs` — single entry point assembling all three deliverables.

## Required Deliverables
1. **Single City Sandbox Map** — low-poly micro-voxel buffer grid
   (`BuildCitySandbox`).
2. **Penetration Physics Test Bed** — raw byte-array shredding via heavy beams
   (`RunPenetrationTest` → `VoxelPenetrationJob`).
3. **The Withdrawal Sandbox** — HOLD_LINE → RETREAT transition driven by simulated
   enemy arrival rate (`StepWithdrawalSandbox`).

## How To Use (once the Unity DOTS shell exists)
- Drop `PrototypeBootstrap` on an empty GameObject in a scene.
- Press Play; watch the Console for sandbox build, penetration, and withdrawal logs.
- Tune the inspector fields (volume dims, beam power, arrival rate) to experiment.

## Next Steps
- Replace Console logging with on-screen voxel rendering via `VoxelRaymarch.compute`.
- Convert hardcoded soldiers into real ECS entities from `combat/`.
- Feed a `SectorState` (from `geoscape/`) to drive the damage overlay pre-pass.

See: `README.md` (Phase 1), `design/LAYER_BALANCE.md` (§3).
