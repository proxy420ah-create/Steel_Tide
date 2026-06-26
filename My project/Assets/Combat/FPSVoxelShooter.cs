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
        public KeyCode fireKey = KeyCode.Mouse0;
        
        [Header("Crosshair")]
        public bool showCrosshair = true;
        public Color crosshairColor = Color.white;
        public float crosshairSize = 20f;
        
        private float _nextFireTime = 0f;
        
        void Start()
        {
            if (fpsCamera == null)
                fpsCamera = Camera.main;
                
            if (weaponController == null)
                weaponController = FindObjectOfType<VoxelWeaponController>();
                
            // Lock and hide cursor for FPS gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        void Update()
        {
            // Handle shooting input
            if (Input.GetKey(fireKey) && Time.time >= _nextFireTime)
            {
                Fire();
                _nextFireTime = Time.time + fireRate;
            }
            
            // ESC to unlock cursor
            if (Input.GetKeyDown(KeyCode.Escape))
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
        
        void OnGUI()
        {
            if (!showCrosshair) return;
            
            // Draw simple crosshair at screen center
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;
            
            // Horizontal line
            DrawLine(new Vector2(centerX - crosshairSize, centerY),
                    new Vector2(centerX + crosshairSize, centerY),
                    crosshairColor, 2f);
            
            // Vertical line
            DrawLine(new Vector2(centerX, centerY - crosshairSize),
                    new Vector2(centerX, centerY + crosshairSize),
                    crosshairColor, 2f);
        }
        
        void DrawLine(Vector2 start, Vector2 end, Color color, float width)
        {
            GUI.color = color;
            Vector2 diff = end - start;
            float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
            
            GUIUtility.RotateAroundPivot(angle, start);
            GUI.DrawTexture(new Rect(start.x, start.y, diff.magnitude, width), Texture2D.whiteTexture);
            GUIUtility.RotateAroundPivot(-angle, start);
        }
    }
}
