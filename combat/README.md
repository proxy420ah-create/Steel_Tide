# combat/ — Hierarchical AI & FPS Weapon Tracking

The tactical brain. Cognition lives at the top of a 4-tier command hierarchy;
the bottom tier is a cheap, data-driven ECS executor that scales to tens of
thousands of entities.

## Tiers
1. **High Command AI** — planetary objectives from sector status.
2. **Battalion AI** — movement paths, asset distribution, flanking.
3. **Squad AI** — take a point / suppress locally.
4. **Soldier Entity** — raw movement vector + target tracking + firing.

## Files
- `CommandHierarchy.cs` — ECS component structs for all four tiers + order enum.

## Next Steps
- ECS systems that propagate orders downward each operational tick.
- Burst flocking AI + line-of-sight tracing for soldiers.
- Wire the Act I withdrawal transition (data-driven, see `design/LAYER_BALANCE.md` §3).

See: `docs/core/ARCHITECTURE.md` (§5).
