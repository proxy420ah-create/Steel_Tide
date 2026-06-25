# geoscape/ — Strategic Layer (OOS Math & Data Structures)

Out-of-Sight (OOS) simulation of the planetary conflict. Sectors are abstracted
to **data tags** (owner, defense power, assault force, terrain, infrastructure)
and stepped by background Burst jobs — **no GameObjects, no live entities**.

## Files
- `SectorState.cs` — sector data struct + `OosBattleStepJob` (parallel attrition).

## Responsibilities
- Track planet-wide sector arrays in RAM (`NativeArray<SectorState>`).
- Resolve abstracted battles when the player is not deployed.
- Emit `bombardmentTier` flags that feed the Probabilistic Damage Overlay when a
  sector is later entered.

## Next Steps
- Sector adjacency graph + frontline propagation.
- Reinforcement / asset flow between adjacent sectors.
- Save/load persistence of the sector array.

See: `docs/core/ARCHITECTURE.md` (§2), `design/LAYER_BALANCE.md` (§2).
