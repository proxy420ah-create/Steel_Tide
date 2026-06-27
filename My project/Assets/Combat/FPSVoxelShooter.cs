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
            // Use VoxelWeaponController's DDA raymarching directly
            // This bypasses Physics.Raycast and can hit interior voxels!
            if (weaponController != null)
            {
                weaponController.FireWeapon();
                Debug.Log($"[{gameObject.name}/FPSVoxelShooter] FIRE ► | Using DDA raymarching (can penetrate interior voxels)");
            }
            else
            {
                Debug.LogWarning($"[{gameObject.name}/FPSVoxelShooter] No weapon controller assigned!");
            }
        }
    }
}
