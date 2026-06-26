// Steel Tide: First Device — Combat / Hierarchical AI
// CommandHierarchy.cs
//
// Top-down tactical hierarchy. Cognition lives at the top; the bottom tier is a
// cheap, data-driven executor that scales to tens of thousands of ECS entities.
//
//   1. High Command AI   -> planetary objectives from sector status
//   2. Battalion AI       -> movement paths, asset distribution, flanking
//   3. Squad AI           -> take a point / suppress, locally
//   4. Soldier Entity     -> raw movement vector + target tracking + fire
//
// See: docs/core/ARCHITECTURE.md (§5)

using Unity.Entities;
using Unity.Mathematics;

namespace SteelTide.Combat
{
    // ----- Tier 4: Individual Soldier (ECS component data) ---------------------

    public enum SquadOrder : byte
    {
        HoldLine = 0,
        Advance = 1,
        Suppress = 2,
        Retreat = 3,
        ProtectTransports = 4
    }

    /// <summary>Per-soldier execution state. Pure IComponentData struct.</summary>
    public struct SoldierState : IComponentData
    {
        public float3 position;
        public float3 moveVector;
        public int    targetEntityId;   // -1 = none
        public float  health;
        public int    squadId;
        public SquadOrder order;        // trickled down from squad
    }

    // ----- Tier 3: Squad ------------------------------------------------------

    public struct SquadState : IComponentData
    {
        public int        squadId;
        public int        battalionId;
        public float3      objectivePoint;
        public SquadOrder  order;
        public int         memberCount;
        public float       cohesion;     // 0..1 (formation integrity)
    }

    // ----- Tier 2: Battalion --------------------------------------------------

    public struct BattalionState : IComponentData
    {
        public int    battalionId;
        public int    sectorId;          // links back to Geoscape SectorState
        public float3 frontlineCenter;
        public float3 advanceVector;
    }

    // ----- Tier 1: High Command ----------------------------------------------

    public enum PlanetaryObjective : byte
    {
        Defend = 0,
        Counterattack = 1,
        Withdraw = 2
    }

    public struct HighCommandState : IComponentData
    {
        public PlanetaryObjective objective;
        public int   priorityTargetSectorId;
    }

    // -------------------------------------------------------------------------
    // TODO: Systems (ECS) that propagate orders downward each operational tick:
    //   HighCommandSystem  -> sets PlanetaryObjective from SectorState array
    //   BattalionSystem    -> derives advanceVector / asset distribution
    //   SquadSystem        -> assigns objectivePoint + SquadOrder to members
    //   SoldierSystem      -> Burst job: moveVector + target tracking + firing
    //
    // Withdrawal transition (Act I) is data-driven (see LAYER_BALANCE.md §3):
    //   if friendlyRatio < lineHoldThreshold -> order = Retreat / ProtectTransports
}
