// Steel Tide: First Device — Geoscape / Strategic Layer
// SectorState.cs
//
// Data-oriented representation of a single planetary sector for Out-of-Sight
// (OOS) simulation. Sectors are stored as a flat NativeArray<SectorState> and
// stepped by a Burst job in the background. NO GameObjects.
//
// See: docs/core/ARCHITECTURE.md (§2), design/LAYER_BALANCE.md (§2)

using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;

namespace SteelTide.Geoscape
{
    public enum SectorOwner : byte
    {
        Human = 0,
        Vekari = 1,
        Contested = 2
    }

    /// <summary>
    /// One sector's abstracted strategic state. Pure value type for cache-locality.
    /// </summary>
    public struct SectorState
    {
        public int          sectorId;
        public SectorOwner  owner;
        public float        humanDefensePower;
        public float        vekariAssaultForce;
        public float        terrainModifier;   // 0.8 - 1.5 (defender bonus)
        public float        infrastructure;    // 0..1 (power / industry)
        public byte         bombardmentTier;   // 0..3 -> damage overlay severity
    }

    /// <summary>
    /// Background OOS attrition step. Resolves abstracted battles for every
    /// contested sector. Designed to be Burst-compiled and jobified.
    /// </summary>
    [BurstCompile]
    public struct OosBattleStepJob : IJobParallelFor
    {
        public NativeArray<SectorState> sectors;

        public float kAttrition;   // fraction of effective power traded per tick
        public uint  randomSeed;    // per-step seed (vary each tick)

        public void Execute(int index)
        {
            SectorState s = sectors[index];
            if (s.owner != SectorOwner.Contested)
                return;

            var rng = new Random(randomSeed + (uint)(index + 1));

            float defenseEffective = s.humanDefensePower * s.terrainModifier;
            float assaultEffective = s.vekariAssaultForce;

            float defenseLoss = assaultEffective * kAttrition * rng.NextFloat(0.8f, 1.2f);
            float assaultLoss = defenseEffective * kAttrition * rng.NextFloat(0.8f, 1.2f);

            s.humanDefensePower  = math.max(0f, s.humanDefensePower  - defenseLoss);
            s.vekariAssaultForce = math.max(0f, s.vekariAssaultForce - assaultLoss);

            if (s.humanDefensePower <= 0f)
                s.owner = SectorOwner.Vekari;
            else if (s.vekariAssaultForce <= 0f)
                s.owner = SectorOwner.Human;

            sectors[index] = s;
        }
    }

    // TODO(Phase 3):
    //  - Sector adjacency graph + frontline propagation (reinforcement flow).
    //  - Bombardment tier accrual from sustained assault -> damage overlay input.
    //  - Persistence / save-load of the sector array.
}
