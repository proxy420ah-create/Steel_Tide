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

using System.IO;
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
        public string assetFileName = "test_cube.stasset";  // loaded from StreamingAssets
        public float  voxelSize  = 0.5f;
        public VoxelRenderer voxelRenderer;  // assign in Inspector to wire GPU rendering
        public VoxelWeaponController weaponController;  // assign in Inspector for interactive destruction

        private int3 volumeDims;  // set by loaded asset
        private ComputeBuffer _voxelBuffer;  // GPU-side voxel data (StructuredBuffer)
        
        // Public property for components to pull buffer reference
        public ComputeBuffer GeneratedVoxelBuffer => _voxelBuffer;

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
            // Auto-locate components if not manually assigned in Inspector
            if (voxelRenderer == null)
            {
                voxelRenderer = FindFirstObjectByType<VoxelRenderer>();
                if (voxelRenderer != null)
                    Debug.Log("[SteelTide] Auto-located VoxelRenderer component.");
            }
            
            if (weaponController == null)
            {
                weaponController = FindFirstObjectByType<VoxelWeaponController>();
                if (weaponController != null)
                    Debug.Log("[SteelTide] Auto-located VoxelWeaponController component.");
            }

            BuildMaterialTables();
            LoadVoxelAsset();        // Deliverable 1: load from .stasset
            UploadVoxelTextureToGPU();  // upload to GPU for rendering
            RunPenetrationTest();    // Deliverable 2
        }

        void Update()
        {
            StepWithdrawalSandbox(Time.deltaTime); // Deliverable 3
        }

        // ---- Deliverable 1: load voxel asset from .stasset file -------------
        private void LoadVoxelAsset()
        {
            string path = Path.Combine(Application.streamingAssetsPath, assetFileName);
            if (!File.Exists(path))
            {
                Debug.LogError($"[SteelTide] Asset not found: {path}");
                return;
            }

            StAsset asset = StAssetReader.Load(path, Allocator.Persistent);
            volumeDims = asset.dims;
            _volume = asset.volume;  // take ownership of the NativeArray

            int solid = 0;
            for (int i = 0; i < _volume.Length; i++)
                if (VoxelBits.Material(_volume[i]) != MaterialId.Air)
                    solid++;

            Debug.Log($"[SteelTide] Loaded '{assetFileName}': {volumeDims} grid, " +
                      $"{solid}/{_volume.Length} solid voxels ({100f * solid / _volume.Length:F1}%)");
        }

        // ---- GPU Upload: NativeArray -> Texture3D ---------------------------
        private void UploadVoxelTextureToGPU()
        {
            if (_volume.Length == 0)
            {
                Debug.LogWarning("[SteelTide] No voxel data to upload.");
                return;
            }

            // Create ComputeBuffer for direct integer storage (no float conversion!)
            int totalVoxels = volumeDims.x * volumeDims.y * volumeDims.z;
            _voxelBuffer = new ComputeBuffer(totalVoxels, sizeof(uint));

            // Upload voxel data directly (ushort → uint, no precision loss)
            uint[] voxelData = new uint[_volume.Length];
            int nonZeroCount = 0;
            for (int i = 0; i < _volume.Length; i++)
            {
                voxelData[i] = _volume[i];  // Direct copy, no float conversion!
                if (voxelData[i] != 0) nonZeroCount++;
            }

            _voxelBuffer.SetData(voxelData);
            
            Debug.Log($"[SteelTide] Created ComputeBuffer: {totalVoxels} voxels ({totalVoxels * sizeof(uint)} bytes)");
            Debug.Log($"[SteelTide] Buffer contains {nonZeroCount}/{totalVoxels} non-zero values");
            Debug.Log($"[SteelTide] Sample values: [0]={voxelData[0]}, [256]={voxelData[256]}, [511]={voxelData[511]}");
            Debug.Log($"[SteelTide] GeneratedVoxelBuffer property returns: {(GeneratedVoxelBuffer != null ? "VALID" : "NULL")}");

            // Wire the buffer to the renderer (if assigned).
            if (voxelRenderer != null)
            {
                Debug.Log($"[SteelTide] VoxelRenderer found, assigning buffer directly...");
                voxelRenderer.voxelBuffer = _voxelBuffer;
                voxelRenderer.volumeDims = volumeDims;
                voxelRenderer.voxelSize = voxelSize;
                Debug.Log($"[SteelTide] Uploaded {totalVoxels * sizeof(uint)} bytes to GPU ComputeBuffer.");
                Debug.Log($"[SteelTide] Verification: voxelRenderer.voxelBuffer is now {(voxelRenderer.voxelBuffer != null ? "ASSIGNED" : "NULL")}");
            }
            else
            {
                Debug.LogWarning("[SteelTide] VoxelRenderer not assigned — cube won't render!");
            }

            // Wire the weapon controller for interactive destruction (if assigned).
            if (weaponController != null)
            {
                weaponController.InitializeVolume(_volume, volumeDims, _voxelBuffer);
                weaponController.voxelSize = voxelSize;
                weaponController.volumeOffset = Vector3.zero;
                Debug.Log("[SteelTide] VoxelWeaponController initialized — left-click to blast craters!");
            }
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
            if (_voxelBuffer != null)      _voxelBuffer.Release();
        }
    }
}
