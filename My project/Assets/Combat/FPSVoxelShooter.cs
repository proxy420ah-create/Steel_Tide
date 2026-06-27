// Steel Tide: First Device — Combat / FPS Voxel Shooter
// FPSVoxelShooter.cs
//
// Handles FPS-style voxel destruction:
// - Crosshair-centered raycasting
// - Single voxel destruction per shot
// - Proxy collider for targeting
// - Direct buffer modification

using UnityEngine;
using Unity.Mathematics;
using SteelTide.Voxels;  // For VoxelVolumeProxy
using UnityEngine.InputSystem;  // Unity 6 New Input System

namespace SteelTide.Combat
{
    public class FPSVoxelShooter : MonoBehaviour
    {
        [Header("References")]
        public Camera fpsCamera;
        public VoxelWeaponController weaponController;
        
        [Header("Shooting Settings")]
        public float fireRate = 0.2f; // Seconds between shots
        public float range = 100f;
        
        private float _nextFireTime = 0f;
        
        void Start()
        {
            if (fpsCamera == null)
                fpsCamera = Camera.main;
                
            if (weaponController == null)
                weaponController = FindFirstObjectByType<VoxelWeaponController>();
                
            // Lock and hide cursor for FPS gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        void Update()
        {
            // Handle shooting input (New Input System)
            if (Mouse.current.leftButton.isPressed && Time.time >= _nextFireTime)
            {
                Fire();
                _nextFireTime = Time.time + fireRate;
            }
            
            // ESC to unlock cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        void Fire()
        {
            // Shoot ray from center of screen (crosshair position)
            Ray cameraRay = fpsCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            RaycastHit hit;
            if (Physics.Raycast(cameraRay, out hit, range))
            {
                // Check if we hit a voxel volume (proxy collider)
                VoxelVolumeProxy proxy = hit.collider.GetComponent<VoxelVolumeProxy>();
                if (proxy != null)
                {
                    // Convert world hit point to voxel coordinates
                    Vector3 localHit = proxy.transform.InverseTransformPoint(hit.point);
                    
                    // Nudge slightly inward along ray direction to ensure we hit the voxel, not the edge
                    localHit -= cameraRay.direction.normalized * 0.01f;
                    
                    // Convert to voxel grid coordinates
                    int3 voxelCoords = new int3(
                        Mathf.FloorToInt(localHit.x / proxy.voxelSize),
                        Mathf.FloorToInt(localHit.y / proxy.voxelSize),
                        Mathf.FloorToInt(localHit.z / proxy.voxelSize)
                    );
                    
                    // Clamp to volume bounds
                    voxelCoords = math.clamp(voxelCoords, int3.zero, proxy.volumeDims - 1);
                    
                    // Destroy the voxel
                    proxy.DestroyVoxel(voxelCoords);
                    
                    Debug.Log($"[FPSVoxelShooter] Destroyed voxel at {voxelCoords}");
                }
            }
        }
    }
}
