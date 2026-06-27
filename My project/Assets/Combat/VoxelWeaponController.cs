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
            Debug.Log($"[{gameObject.name}/VoxelWeaponController] START | Camera: {_camera.name} | Damage Radius: {damageRadius}");
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
                    Debug.Log($"[{gameObject.name}/VoxelWeaponController] ✓ Pulled ComputeBuffer from {bootstrap.gameObject.name}/PrototypeBootstrap");
                }
            }
            
            // NOTE: Input handling removed - this component is now a passive service
            // called by FPSVoxelShooter → VoxelVolumeProxy → DestroyVoxelAt()
            // If you want direct click-to-destroy testing, uncomment below:
            /*
            if (!_volumeReady || !voxelData.IsCreated)
                return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                FireWeapon();
            }
            */
        }

        public void InitializeVolume(NativeArray<ushort> data, int3 dims, ComputeBuffer buffer)
        {
            voxelData = data;
            volumeDims = dims;
            voxelBuffer = buffer;
            _volumeReady = true;
            int totalVoxels = dims.x * dims.y * dims.z;
            Debug.Log($"[{gameObject.name}/VoxelWeaponController] INIT ✓ | Dims: {dims} | Total Voxels: {totalVoxels:N0} | Voxel Size: {voxelSize}m | Offset: {volumeOffset:F2}");
        }

        public void FireWeapon()
        {
            // Shoot from screen center (crosshair position), not mouse cursor
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            // Convert to voxel space
            float3 rayOrigin = (float3)ray.origin - (float3)volumeOffset;
            float3 rayDir = math.normalize((float3)ray.direction);

            // AABB intersection: Find where ray enters the voxel volume
            float3 volumeMin = float3.zero;
            float3 volumeMax = new float3(volumeDims.x, volumeDims.y, volumeDims.z) * voxelSize;
            
            float3 invDir = 1.0f / rayDir;
            float3 t0 = (volumeMin - rayOrigin) * invDir;
            float3 t1 = (volumeMax - rayOrigin) * invDir;
            
            float3 tmin = math.min(t0, t1);
            float3 tmax = math.max(t0, t1);
            
            float tEnter = math.max(math.max(tmin.x, tmin.y), tmin.z);
            float tExit = math.min(math.min(tmax.x, tmax.y), tmax.z);
            
            // Check if ray intersects volume
            if (tExit < 0 || tEnter > tExit)
            {
                Debug.Log($"[{gameObject.name}/VoxelWeaponController] MISS — Ray doesn't intersect volume | Ray Origin: {ray.origin:F2} | Direction: {ray.direction:F2}");
                return;
            }
            
            // Start DDA from volume entry point (or ray origin if already inside)
            float tStart = math.max(0, tEnter);
            float3 startPoint = rayOrigin + rayDir * tStart;
            
            // Simple DDA raycast to find first solid voxel
            float3 p = startPoint / voxelSize;
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

            Debug.Log($"[{gameObject.name}/VoxelWeaponController] MISS — No solid voxel hit | Ray Origin: {ray.origin:F2} | Direction: {ray.direction:F2} | Steps: {loopGuard}");
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
                Vector3 worldPos = volumeOffset + new Vector3(centerVoxel.x, centerVoxel.y, centerVoxel.z) * voxelSize;
                Debug.Log($"[{gameObject.name}/VoxelWeaponController] HIT ✓ | Voxel: {centerVoxel} | World Pos: {worldPos:F2} | Changed: {voxelsChanged} voxels | Material: {currentMaterial} | Damage Radius: {damageRadius}");
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
        
        /// <summary>
        /// Destroy a single voxel at the given 1D index (called by FPS shooter)
        /// </summary>
        public void DestroyVoxelAt(int index)
        {
            if (!_volumeReady)
            {
                Debug.LogWarning("[VoxelWeaponController] Volume not ready for destruction!");
                return;
            }
            
            int totalVoxels = volumeDims.x * volumeDims.y * volumeDims.z;
            if (index < 0 || index >= totalVoxels)
            {
                Debug.LogWarning($"[VoxelWeaponController] Index {index} out of bounds!");
                return;
            }
            
            // Set voxel to air (material ID 0)
            voxelData[index] = 0;
            
            // Upload modified data to GPU
            UpdateGPUTexture();
            
            Debug.Log($"[VoxelWeaponController] Destroyed voxel at index {index}");
        }
    }
}
