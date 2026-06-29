// Steel Tide: Player Weapon Controller
// PlayerWeapon.cs
//
// Integrates VoxelWeaponController with player character.
// Handles shooting, crosshair, and weapon feedback.

using UnityEngine;
using UnityEngine.InputSystem;
using SteelTide.Combat;

namespace SteelTide.Player
{
    [RequireComponent(typeof(VoxelWeaponController))]
    public class PlayerWeapon : MonoBehaviour
    {
        [Header("Weapon Settings")]
        public float fireRate = 0.2f;  // Time between shots (seconds)
        public int damageRadius = 1;   // Voxel damage radius
        
        [Header("Crosshair")]
        public bool showCrosshair = true;
        public Color crosshairColor = Color.white;
        public float crosshairSize = 20f;
        public float crosshairThickness = 2f;
        
        [Header("Visual Feedback")]
        public bool showMuzzleFlash = true;
        public float muzzleFlashDuration = 0.05f;
        public Light muzzleFlashLight;
        
        // Components
        private VoxelWeaponController _weaponController;
        private Camera _camera;
        
        // State
        private float _lastFireTime;
        private float _muzzleFlashTimer;
        
        void Start()
        {
            _weaponController = GetComponent<VoxelWeaponController>();
            _camera = GetComponent<Camera>();
            
            if (_camera == null)
            {
                Debug.LogError("[PlayerWeapon] No Camera component found!");
                enabled = false;
                return;
            }
            
            // Setup muzzle flash light
            if (muzzleFlashLight != null)
            {
                muzzleFlashLight.enabled = false;
            }
            
            Debug.Log("[PlayerWeapon] Ready - Left-click to shoot!");
        }
        
        void Update()
        {
            HandleShooting();
            UpdateMuzzleFlash();
        }
        
        void HandleShooting()
        {
            // Check if can fire (rate limit)
            if (Time.time - _lastFireTime < fireRate)
                return;
            
            // Check for fire input (new Input System)
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed)
            {
                Fire();
                _lastFireTime = Time.time;
            }
        }
        
        void Fire()
        {
            // Fire weapon (VoxelWeaponController handles the raycast and damage)
            _weaponController.FireWeapon();
            
            // Trigger muzzle flash
            if (showMuzzleFlash)
            {
                _muzzleFlashTimer = muzzleFlashDuration;
                if (muzzleFlashLight != null)
                    muzzleFlashLight.enabled = true;
            }
            
            // Optional: Add weapon recoil, sound, etc. here
        }
        
        void UpdateMuzzleFlash()
        {
            if (_muzzleFlashTimer > 0f)
            {
                _muzzleFlashTimer -= Time.deltaTime;
                
                if (_muzzleFlashTimer <= 0f && muzzleFlashLight != null)
                {
                    muzzleFlashLight.enabled = false;
                }
            }
        }
        
        void OnGUI()
        {
            if (!showCrosshair)
                return;
            
            // Draw crosshair at screen center
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            
            // Horizontal line
            DrawLine(
                new Vector2(centerX - crosshairSize, centerY),
                new Vector2(centerX + crosshairSize, centerY),
                crosshairThickness,
                crosshairColor
            );
            
            // Vertical line
            DrawLine(
                new Vector2(centerX, centerY - crosshairSize),
                new Vector2(centerX, centerY + crosshairSize),
                crosshairThickness,
                crosshairColor
            );
        }
        
        void DrawLine(Vector2 start, Vector2 end, float thickness, Color color)
        {
            // Create texture for line
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            
            // Calculate line rectangle
            Vector2 direction = end - start;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float length = direction.magnitude;
            
            // Draw rotated rectangle
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(
                new Rect(start.x, start.y - thickness / 2f, length, thickness),
                texture
            );
            GUIUtility.RotateAroundPivot(-angle, start);
        }
    }
}
