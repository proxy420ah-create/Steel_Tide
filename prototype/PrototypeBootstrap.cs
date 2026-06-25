// Steel Tide: First Device — Prototype / Vertical Slice
// PrototypeBootstrap.cs
//
// Hardcoded entry point that assembles the Phase 1 vertical slice and exercises
// the three required deliverables in a fully decoupled, data-oriented way:
//
//   1. Single City Sandbox Map  -> low-poly micro-voxel buffer grid
//   2. Penetration Physics Test -> raw byte-array shredding via heavy beams
//   3. Withdrawal Sandbox        -> HOLD_LINE -> RETREAT transition by arrival rate
//
// See: README.md (Phase 1), design/LAYER_BALANCE.md (§3)

using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using SteelTide.Voxels;
using SteelTide.Combat;

namespace SteelTide.Prototype
{
    public class PrototypeBootstrap : MonoBehaviour
    {
        [Header("Voxel Sandbox")]
        public int3   volumeDims = new int3(128, 64, 128);
        public float  voxelSize  = 0.5f;

        [Header("Penetration Test")]
        public float  beamPower  = 200f;

        [Header("Withdrawal Sandbox")]
        public float  arrivalRatePerSec = 4f;
        public float  arrivalAccel      = 1.08f;
        public float  lineHoldThreshold = 0.35f;

        private NativeArray<ushort> _volume;        // packed 16-bit voxels
        private NativeArray<float>  _densityById;
        private NativeArray<bool>   _shreddableById;

        private SquadOrder _squadOrder = SquadOrder.HoldLine;
        private float _friendlyStrength = 1000f;
        private readonly float _initialFriendlyStrength = 1000f;
        private float _vekariArrived;

        void Start()
        {
            BuildMaterialTables();
            BuildCitySandbox();      // Deliverable 1
            RunPenetrationTest();    // Deliverable 2
        }

        void Update()
        {
            StepWithdrawalSandbox(Time.deltaTime); // Deliverable 3
        }

        // ---- Deliverable 1: micro-voxel city buffer -------------------------
        private void BuildCitySandbox()
        {
            int count = volumeDims.x * volumeDims.y * volumeDims.z;
            _volume = new NativeArray<ushort>(count, Allocator.Persistent);

            // Ground slab (concrete) + a few solid building boxes for testing.
            // Sandbox voxels are plain Cube/North, so the packed word == material id.
            for (int z = 0; z < volumeDims.z; z++)
            for (int y = 0; y < volumeDims.y; y++)
            for (int x = 0; x < volumeDims.x; x++)
            {
                ushort mat = MaterialId.Air;
                if (y < 2) mat = MaterialId.Concrete;                // ground
                else if ((x / 16 + z / 16) % 3 == 0 && y < 24)       // buildings
                    mat = MaterialId.Concrete;
                _volume[Index(x, y, z)] = VoxelBits.Pack(ShapeId.Cube, RotationId.North, mat);
            }

            Debug.Log($"[SteelTide] City sandbox built: {count} voxels ({volumeDims}).");
        }

        // ---- Deliverable 2: penetration / shredding -------------------------
        private void RunPenetrationTest()
        {
            var dirty = new NativeList<int3>(256, Allocator.TempJob);
            var job = new VoxelPenetrationJob
            {
                volume = _volume,
                densityById = _densityById,
                shreddableById = _shreddableById,
                dirtyVoxels = dirty,
                dims = volumeDims,
                voxelSize = voxelSize,
                rayOrigin = new float3(0, 8, 0),
                rayDir = math.normalize(new float3(1, 0, 1)),
                basePower = beamPower
            };

            PenetrationHit hit = job.Execute();
            Debug.Log($"[SteelTide] Penetration: absorbed={hit.absorbed} " +
                      $"shredded={hit.shreddedCount} remaining={hit.remainingPower:F1}");

            // TODO: hand dirty voxels to collision-mesh patch + flood-fill islanding.
            dirty.Dispose();
        }

        // ---- Deliverable 3: withdrawal transition ---------------------------
        private void StepWithdrawalSandbox(float dt)
        {
            arrivalRatePerSec *= math.pow(arrivalAccel, dt); // escalate
            _vekariArrived += arrivalRatePerSec * dt;

            // Simulated attrition: more Vekari on the field erodes friendly strength.
            _friendlyStrength = math.max(0f, _friendlyStrength - _vekariArrived * dt);

            float friendlyRatio = _friendlyStrength / _initialFriendlyStrength;
            if (_squadOrder == SquadOrder.HoldLine && friendlyRatio < lineHoldThreshold)
            {
                _squadOrder = SquadOrder.ProtectTransports; // RETREAT
                Debug.Log($"[SteelTide] WITHDRAWAL TRIGGERED at ratio={friendlyRatio:F2} " +
                          $"-> ProtectTransports (Vekari arrived ~{_vekariArrived:F0}).");
            }
        }

        // ---- Helpers --------------------------------------------------------
        private void BuildMaterialTables()
        {
            _densityById = new NativeArray<float>(MaterialId.Count, Allocator.Persistent);
            _densityById[MaterialId.Air]          = 0f;
            _densityById[MaterialId.EnergyShield] = 12f;
            _densityById[MaterialId.ChobhamArmor] = 40f;
            _densityById[MaterialId.Concrete]     = 18f;
            _densityById[MaterialId.Flesh]        = 6f;
            _densityById[MaterialId.Steel]        = 30f;

            _shreddableById = new NativeArray<bool>(MaterialId.Count, Allocator.Persistent);
            _shreddableById[MaterialId.Air]          = false;
            _shreddableById[MaterialId.EnergyShield] = true;
            _shreddableById[MaterialId.ChobhamArmor] = true;
            _shreddableById[MaterialId.Concrete]     = true;
            _shreddableById[MaterialId.Flesh]        = true;
            _shreddableById[MaterialId.Steel]        = true;
        }

        private int Index(int x, int y, int z) =>
            x + y * volumeDims.x + z * volumeDims.x * volumeDims.y;

        void OnDestroy()
        {
            if (_volume.IsCreated)         _volume.Dispose();
            if (_densityById.IsCreated)    _densityById.Dispose();
            if (_shreddableById.IsCreated) _shreddableById.Dispose();
        }
    }
}
