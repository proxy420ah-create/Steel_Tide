// Steel Tide: First Device — Combat / Voxel Weapon Controller
// VoxelWeaponController.cs
//
// Interactive weapon testing script that fires raycasts into the voxel volume
// on mouse click, demonstrating two-stage destruction:
//   1st hit: Intact material → Damaged state (visual feedback)
//   2nd hit: Damaged state → Air (hole carved through)
//
// Attach to Main Camera for point-and-click destruction testing.
// Unity 6 compatible: uses New Input System API.

using UnityEngine;
using UnityEngine.InputSystem;  // Unity 6 New Input System
using Unity.Collections;
using Unity.Mathematics;

namespace SteelTide.Combat
{
    [RequireComponent(typeof(Camera))]
    public class VoxelWeaponController : MonoBehaviour
    {
        [Header("Voxel Volume Reference")]
        public ComputeBuffer voxelBuffer;  // Set by PrototypeBootstrap
        public NativeArray<ushort> voxelData;  // CPU-side voxel data
        public int3 volumeDims;
        public float voxelSize = 0.5f;
        public Vector3 volumeOffset = Vector3.zero;

        [Header("Weapon Settings")]
        public int damageRadius = 1;  // Voxels affected per click

        private Camera _camera;
        private bool _volumeReady = false;

        void Start()
        {
            _camera = GetComponent<Camera>();
        }

        void Update()
        {
            // Pull buffer reference from Bootstrap if not yet assigned
            if (voxelBuffer == null)
            {
                var bootstrap = FindFirstObjectByType<SteelTide.Prototype.PrototypeBootstrap>();
                if (bootstrap != null && bootstrap.GeneratedVoxelBuffer != null)
                {
                    voxelBuffer = bootstrap.GeneratedVoxelBuffer;
                    Debug.Log("[VoxelWeaponController] Pulled ComputeBuffer reference from PrototypeBootstrap.");
                }
            }
            
            if (!_volumeReady || !voxelData.IsCreated)
                return;

            // Unity 6 New Input System: check for left mouse button press
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                FireWeapon();
            }
        }

        public void InitializeVolume(NativeArray<ushort> data, int3 dims, ComputeBuffer buffer)
        {
            voxelData = data;
            volumeDims = dims;
            voxelBuffer = buffer;
            _volumeReady = true;
            Debug.Log("[VoxelWeaponController] Initialized with volume dimensions: " + dims);
        }

        private void FireWeapon()
        {
            // Unity 6 New Input System: get mouse position from modern API
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = _camera.ScreenPointToRay(mousePos);
            
            // Convert to voxel space
            float3 rayOrigin = (float3)ray.origin - (float3)volumeOffset;
            float3 rayDir = math.normalize((float3)ray.direction);

            // Simple DDA raycast to find first solid voxel
            float3 p = rayOrigin / voxelSize;
            int3 voxel = (int3)math.floor(p);
            int3 step = (int3)math.sign(rayDir);

            float3 inv = math.select(1f / math.abs(rayDir), float.PositiveInfinity,
                                    math.abs(rayDir) < 1e-6f);
            float3 tDelta = inv;
            float3 nextB = (math.floor(p) + math.max(step, 0));
            float3 tMax = (nextB - p) * inv;
            tMax = math.select(tMax, float.PositiveInfinity, math.abs(rayDir) < 1e-6f);

            int maxSteps = 256;
            int loopGuard = 0;

            while (loopGuard++ < maxSteps)
            {
                if (!InBounds(voxel))
                    break;

                int idx = Index(voxel);
                ushort packed = voxelData[idx];
                ushort mat = (ushort)(packed & Voxels.VoxelBits.MaterialMask);

                if (mat != Voxels.MaterialId.Air)
                {
                    // HIT! Apply two-stage damage
                    ApplyDamage(voxel, mat);
                    return;
                }

                // Advance DDA
                if (tMax.x < tMax.y)
                {
                    if (tMax.x < tMax.z) { voxel.x += step.x; tMax.x += tDelta.x; }
                    else                 { voxel.z += step.z; tMax.z += tDelta.z; }
                }
                else
                {
                    if (tMax.y < tMax.z) { voxel.y += step.y; tMax.y += tDelta.y; }
                    else                 { voxel.z += step.z; tMax.z += tDelta.z; }
                }
            }

            Debug.Log("[VoxelWeaponController] Missed — no solid voxel hit");
        }

        private void ApplyDamage(int3 centerVoxel, ushort currentMaterial)
        {
            int voxelsChanged = 0;

            // Apply damage to voxels in radius
            for (int x = -damageRadius; x <= damageRadius; x++)
            {
                for (int y = -damageRadius; y <= damageRadius; y++)
                {
                    for (int z = -damageRadius; z <= damageRadius; z++)
                    {
                        int3 voxel = centerVoxel + new int3(x, y, z);
                        if (!InBounds(voxel))
                            continue;

                        int idx = Index(voxel);
                        ushort packed = voxelData[idx];
                        ushort mat = (ushort)(packed & Voxels.VoxelBits.MaterialMask);
                        ushort shape = Voxels.VoxelBits.Shape(packed);
                        ushort rotation = Voxels.VoxelBits.Rotation(packed);

                        ushort newMat = mat;

                        // TWO-STAGE DAMAGE SYSTEM
                        if (mat == Voxels.MaterialId.Concrete)
                        {
                            // 1st hit: Concrete → Damaged Concrete (crimson)
                            newMat = Voxels.MaterialId.DamagedConcrete;
                        }
                        else if (mat == Voxels.MaterialId.DamagedConcrete)
                        {
                            // 2nd hit: Damaged → Air (hole carved)
                            newMat = Voxels.MaterialId.Air;
                        }
                        else if (mat == Voxels.MaterialId.Steel)
                        {
                            // 1st hit: Steel → Damaged Steel (orange)
                            newMat = Voxels.MaterialId.DamagedSteel;
                        }
                        else if (mat == Voxels.MaterialId.DamagedSteel)
                        {
                            // 2nd hit: Damaged Steel → Air
                            newMat = Voxels.MaterialId.Air;
                        }
                        else if (mat == Voxels.MaterialId.ChobhamArmor)
                        {
                            // 1st hit: Armor → Damaged Armor
                            newMat = Voxels.MaterialId.DamagedArmor;
                        }
                        else if (mat == Voxels.MaterialId.DamagedArmor)
                        {
                            // 2nd hit: Damaged Armor → Air
                            newMat = Voxels.MaterialId.Air;
                        }

                        if (newMat != mat)
                        {
                            // Repack voxel with new material (preserve shape/rotation)
                            ushort newPacked = Voxels.VoxelBits.Pack(shape, rotation, newMat);
                            voxelData[idx] = newPacked;
                            voxelsChanged++;
                        }
                    }
                }
            }

            if (voxelsChanged > 0)
            {
                // Upload modified data to GPU texture
                UpdateGPUTexture();
                Debug.Log($"[VoxelWeaponController] Hit at {centerVoxel}: {voxelsChanged} voxels changed " +
                         $"(Material {currentMaterial})");
            }
        }

        private void UpdateGPUTexture()
        {
            if (voxelBuffer == null)
                return;

            // Upload voxel data directly to ComputeBuffer (ushort → uint, no float conversion!)
            int totalVoxels = volumeDims.x * volumeDims.y * volumeDims.z;
            uint[] data = new uint[totalVoxels];
            
            for (int i = 0; i < totalVoxels; i++)
            {
                data[i] = voxelData[i];  // Direct copy, no conversion!
            }

            voxelBuffer.SetData(data);
        }

        private bool InBounds(int3 v) =>
            v.x >= 0 && v.y >= 0 && v.z >= 0 &&
            v.x < volumeDims.x && v.y < volumeDims.y && v.z < volumeDims.z;

        private int Index(int3 v) => v.x + v.y * volumeDims.x + v.z * volumeDims.x * volumeDims.y;
    }
}
