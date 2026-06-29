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
using System.Collections.Generic;

namespace SteelTide.Combat
{
    [RequireComponent(typeof(Camera))]
    public class VoxelWeaponController : MonoBehaviour
    {
        [Header("Multi-Volume System")]
        public bool useMultiVolumeSystem = true;  // Use new VoxelObject system
        
        [Header("Legacy Single Volume (Bootstrap)")]
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
            // Multi-volume system: Find and shoot closest VoxelObject
            if (useMultiVolumeSystem)
            {
                FireWeaponMultiVolume();
                return;
            }
            
            // Legacy single-volume system
            FireWeaponLegacy();
        }
        
        private void FireWeaponMultiVolume()
        {
            // Find all VoxelObjects in scene
            var voxelObjects = FindObjectsByType<Voxels.VoxelObject>(FindObjectsSortMode.None);
            
            if (voxelObjects.Length == 0)
            {
                Debug.LogWarning("[VoxelWeaponController] No VoxelObjects found in scene!");
                return;
            }
            
            Ray ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            float3 rayOrigin = (float3)ray.origin;
            float3 rayDir = math.normalize((float3)ray.direction);
            
            // Find closest hit across all volumes
            float closestHit = float.MaxValue;
            Voxels.VoxelObject hitVolume = null;
            int3 hitVoxel = int3.zero;
            ushort hitMaterial = 0;
            
            foreach (var vol in voxelObjects)
            {
                if (vol.GetVoxelBuffer() == null)
                    continue;
                
                // Test ray against this volume
                Vector3 volumeOffset = vol.GetVolumeOffset();
                Unity.Mathematics.int3 dims = vol.GetVolumeDims();
                float voxSize = vol.GetVoxelSize();
                
                // Convert ray to volume-local space
                float3 localRayOrigin = rayOrigin - (float3)volumeOffset;
                
                // AABB test
                float3 volumeMin = float3.zero;
                float3 volumeMax = new float3(dims.x, dims.y, dims.z) * voxSize;
                
                if (!RayAABBIntersect(localRayOrigin, rayDir, volumeMin, volumeMax, out float tEnter, out float tExit))
                    continue;
                
                if (tEnter > closestHit)
                    continue;  // This volume is farther than current closest
                
                // DDA raymarch to find hit voxel
                if (RaymarchVolume(vol, localRayOrigin, rayDir, tEnter, out int3 voxel, out ushort mat, out float hitDist))
                {
                    if (hitDist < closestHit)
                    {
                        closestHit = hitDist;
                        hitVolume = vol;
                        hitVoxel = voxel;
                        hitMaterial = mat;
                    }
                }
            }
            
            // Apply damage to closest hit
            if (hitVolume != null)
            {
                Debug.Log($"[VoxelWeaponController] ✓ HIT {hitVolume.gameObject.name} at voxel {hitVoxel} | Material: {hitMaterial} | Distance: {closestHit:F2}m");
                ApplyDamageToVolume(hitVolume, hitVoxel, hitMaterial);
            }
            else
            {
                Debug.LogWarning($"[VoxelWeaponController] ❌ MISS — No voxel hit | Checked {voxelObjects.Length} volumes");
            }
        }
        
        private void FireWeaponLegacy()
        {
            // Original single-volume code
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
            
            // DEBUG: Log AABB intersection details
            Debug.Log($"[{gameObject.name}/VoxelWeaponController] AABB Test | tEnter: {tEnter:F3} | tExit: {tExit:F3} | VolumeMin: {volumeMin} | VolumeMax: {volumeMax}");
            
            // Check if ray intersects volume
            if (tExit < 0 || tEnter > tExit)
            {
                Debug.LogWarning($"[{gameObject.name}/VoxelWeaponController] ❌ MISS — Ray doesn't intersect volume | tExit: {tExit:F3} | tEnter: {tEnter:F3} | Ray Origin: {ray.origin:F2} | Direction: {ray.direction:F2}");
                return;
            }
            
            // Start DDA from volume entry point (or ray origin if already inside)
            // Add small epsilon to ensure we're inside the volume, not on the boundary
            float tStart = math.max(0, tEnter) + 0.001f;
            float3 startPoint = rayOrigin + rayDir * tStart;
            
            Debug.Log($"[{gameObject.name}/VoxelWeaponController] DDA Start | Entry Point: {startPoint:F2} | tStart: {tStart:F3} (epsilon applied)");
            
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
            int3 firstVoxel = voxel;
            int airVoxelsChecked = 0;

            while (loopGuard++ < maxSteps)
            {
                if (!InBounds(voxel))
                {
                    Debug.LogWarning($"[{gameObject.name}/VoxelWeaponController] ⚠️ Out of bounds at step {loopGuard} | Voxel: {voxel} | Checked {airVoxelsChecked} air voxels");
                    break;
                }

                int idx = Index(voxel);
                ushort packed = voxelData[idx];
                ushort mat = (ushort)(packed & Voxels.VoxelBits.MaterialMask);

                if (mat != Voxels.MaterialId.Air)
                {
                    // HIT! Apply two-stage damage
                    Debug.Log($"[{gameObject.name}/VoxelWeaponController] ✓ HIT at step {loopGuard} | Voxel: {voxel} | Material: {mat} | Air voxels traversed: {airVoxelsChecked}");
                    ApplyDamage(voxel, mat);
                    return;
                }
                
                airVoxelsChecked++;

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

            Debug.LogWarning($"[{gameObject.name}/VoxelWeaponController] ❌ MISS — No solid voxel hit after {loopGuard} steps | First Voxel: {firstVoxel} | Last Voxel: {voxel} | Air checked: {airVoxelsChecked} | Ray Origin: {ray.origin:F2} | Direction: {ray.direction:F2}");
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
                        else if (mat == Voxels.MaterialId.Durasteel)
                        {
                            // 1st hit: Durasteel → Damaged Steel (orange)
                            newMat = Voxels.MaterialId.DamagedSteel;
                        }
                        else if (mat == Voxels.MaterialId.DamagedSteel)
                        {
                            // 2nd hit: Damaged Steel → Air
                            newMat = Voxels.MaterialId.Air;
                        }
                        else if (mat == Voxels.MaterialId.ReactiveArmor)
                        {
                            // 1st hit: Reactive Armor → Damaged Armor
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
        
        // ===== MULTI-VOLUME HELPER METHODS =====
        
        private bool RayAABBIntersect(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax, out float tNear, out float tFar)
        {
            float3 invDir = 1.0f / rayDir;
            float3 t0 = (boxMin - rayOrigin) * invDir;
            float3 t1 = (boxMax - rayOrigin) * invDir;
            
            float3 tmin = math.min(t0, t1);
            float3 tmax = math.max(t0, t1);
            
            tNear = math.max(math.max(tmin.x, tmin.y), tmin.z);
            tFar = math.min(math.min(tmax.x, tmax.y), tmax.z);
            
            return tNear <= tFar && tFar >= 0.0f;
        }
        
        private bool RaymarchVolume(Voxels.VoxelObject vol, float3 rayOrigin, float3 rayDir, float tEnter, out int3 hitVoxel, out ushort hitMaterial, out float hitDistance)
        {
            hitVoxel = int3.zero;
            hitMaterial = 0;
            hitDistance = float.MaxValue;
            
            Unity.Mathematics.int3 dims = vol.GetVolumeDims();
            float voxSize = vol.GetVoxelSize();
            ushort[] data = vol.GetVoxelData();
            
            if (data == null) return false;
            
            // Start DDA from volume entry point
            float tStart = math.max(0, tEnter) + 0.001f;
            float3 startPoint = rayOrigin + rayDir * tStart;
            
            // DDA setup
            float3 p = startPoint / voxSize;
            int3 voxel = (int3)math.floor(p);
            int3 step = (int3)math.sign(rayDir);
            
            float3 inv = math.select(1f / math.abs(rayDir), float.PositiveInfinity, math.abs(rayDir) < 1e-6f);
            float3 tDelta = inv;
            float3 nextB = (math.floor(p) + math.max(step, 0));
            float3 tMax = (nextB - p) * inv;
            tMax = math.select(tMax, float.PositiveInfinity, math.abs(rayDir) < 1e-6f);
            
            int maxSteps = 256;
            for (int i = 0; i < maxSteps; i++)
            {
                // Check bounds
                if (voxel.x < 0 || voxel.x >= dims.x ||
                    voxel.y < 0 || voxel.y >= dims.y ||
                    voxel.z < 0 || voxel.z >= dims.z)
                    break;
                
                // Check voxel
                int idx = voxel.x + voxel.y * dims.x + voxel.z * dims.x * dims.y;
                ushort packed = data[idx];
                ushort mat = (ushort)(packed & Voxels.VoxelBits.MaterialMask);
                
                if (mat != Voxels.MaterialId.Air)
                {
                    hitVoxel = voxel;
                    hitMaterial = mat;
                    hitDistance = tStart + (i * voxSize);  // Approximate distance
                    return true;
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
            
            return false;
        }
        
        private void ApplyDamageToVolume(Voxels.VoxelObject vol, int3 centerVoxel, ushort currentMaterial)
        {
            Unity.Mathematics.int3 dims = vol.GetVolumeDims();
            
            // Use global damage queue for thread-safe multi-weapon scenarios
            var damageQueue = VoxelDamageQueue.Instance;
            
            // Apply damage in radius around hit point
            for (int x = -damageRadius; x <= damageRadius; x++)
            {
                for (int y = -damageRadius; y <= damageRadius; y++)
                {
                    for (int z = -damageRadius; z <= damageRadius; z++)
                    {
                        int3 voxel = centerVoxel + new int3(x, y, z);
                        
                        // Bounds check
                        if (voxel.x < 0 || voxel.x >= dims.x ||
                            voxel.y < 0 || voxel.y >= dims.y ||
                            voxel.z < 0 || voxel.z >= dims.z)
                            continue;
                        
                        ushort mat = vol.GetVoxel(voxel.x, voxel.y, voxel.z);
                        
                        if (mat == Voxels.MaterialId.Air)
                            continue;
                        
                        ushort newMat = mat;
                        
                        // TWO-STAGE DAMAGE SYSTEM
                        if (mat == Voxels.MaterialId.Concrete)
                        {
                            newMat = Voxels.MaterialId.DamagedConcrete;
                        }
                        else if (mat == Voxels.MaterialId.DamagedConcrete)
                        {
                            newMat = Voxels.MaterialId.Air;
                        }
                        else if (mat == Voxels.MaterialId.Durasteel)
                        {
                            newMat = Voxels.MaterialId.DamagedSteel;
                        }
                        else if (mat == Voxels.MaterialId.DamagedSteel)
                        {
                            newMat = Voxels.MaterialId.Air;
                        }
                        else
                        {
                            // Unknown material - destroy in one hit
                            newMat = Voxels.MaterialId.Air;
                        }
                        
                        // Queue damage (will be applied in LateUpdate with all other damage this frame)
                        damageQueue.QueueDamage(vol, voxel, newMat, VoxelDamageQueue.DamagePriority.Immediate);
                    }
                }
            }
            
            Debug.Log($"[VoxelWeaponController] Queued damage to {vol.gameObject.name} at {centerVoxel} (radius: {damageRadius})");
        }
    }
}
