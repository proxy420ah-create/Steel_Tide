// Steel Tide: First Device — Voxels / Volume Proxy
// VoxelVolumeProxy.cs
//
// Invisible proxy collider for voxel volumes.
// Allows Physics.Raycast to detect hits on voxel volumes,
// then translates world-space hits to voxel grid coordinates.

using UnityEngine;
using Unity.Mathematics;
using SteelTide.Combat;  // For VoxelWeaponController

namespace SteelTide.Voxels
{
    [RequireComponent(typeof(BoxCollider))]
    public class VoxelVolumeProxy : MonoBehaviour
    {
        [Header("Voxel Volume Settings")]
        public int3 volumeDims = new int3(8, 8, 8);
        public float voxelSize = 0.5f;
        
        [Header("References")]
        public VoxelWeaponController weaponController;
        
        private BoxCollider _collider;
        private ushort[] _voxelData; // Local copy for modifications
        
        void Start()
        {
            _collider = GetComponent<BoxCollider>();
            
            // Size collider to match voxel volume
            Vector3 size = new Vector3(volumeDims.x, volumeDims.y, volumeDims.z) * voxelSize;
            _collider.size = size;
            _collider.center = size / 2f;
            
            // Make collider invisible (no renderer needed)
            _collider.isTrigger = false;
            
            if (weaponController == null)
                weaponController = FindFirstObjectByType<VoxelWeaponController>();
                
            Debug.Log($"[{gameObject.name}/VoxelVolumeProxy] INIT ✓ | Dims: {volumeDims} | Voxel Size: {voxelSize}m | World Size: {size:F2} | Position: {transform.position:F2}");
        }
        
        public void DestroyVoxel(int3 voxelCoords)
        {
            // Calculate 1D index from 3D coordinates
            int index = voxelCoords.x + 
                       voxelCoords.y * volumeDims.x + 
                       voxelCoords.z * volumeDims.x * volumeDims.y;
            
            // Validate index
            int totalVoxels = volumeDims.x * volumeDims.y * volumeDims.z;
            if (index < 0 || index >= totalVoxels)
            {
                Debug.LogWarning($"[{gameObject.name}/VoxelVolumeProxy] ⚠ INDEX OUT OF BOUNDS | Coords: {voxelCoords} | Index: {index} | Valid Range: 0-{totalVoxels-1}");
                return;
            }
            
            // Set voxel to air (material ID 0)
            if (weaponController != null)
            {
                weaponController.DestroyVoxelAt(index);
                Vector3 worldPos = transform.position + new Vector3(voxelCoords.x, voxelCoords.y, voxelCoords.z) * voxelSize;
                Debug.Log($"[{gameObject.name}/VoxelVolumeProxy] DESTROY ✓ | Coords: {voxelCoords} | Index: {index} | World Pos: {worldPos:F2}");
            }
            else
            {
                Debug.LogError($"[{gameObject.name}/VoxelVolumeProxy] ✗ ERROR: No weapon controller assigned!");
            }
        }
        
        void OnDrawGizmos()
        {
            // Draw collider bounds in Scene view
            if (_collider == null)
                _collider = GetComponent<BoxCollider>();
                
            Gizmos.color = new Color(1, 0, 0, 0.3f); // Red semi-transparent
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(_collider.center, _collider.size);
        }
    }
}
