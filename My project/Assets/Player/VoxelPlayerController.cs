// Steel Tide: Voxel Player Controller
// VoxelPlayerController.cs
//
// Complete first-person player controller with voxel-precise collision detection.
// Features: WASD movement, mouse look, jump, ground detection, wall collision.

using UnityEngine;
using Unity.Mathematics;
using SteelTide.Voxels;

namespace SteelTide.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class VoxelPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float jumpForce = 7f;
        public float gravity = 20f;
        
        [Header("Mouse Look")]
        public float mouseSensitivity = 2f;
        public float maxLookAngle = 80f;
        public Transform cameraTransform;
        
        [Header("Voxel Collision")]
        public float playerRadius = 0.4f;  // Collision cylinder radius
        public float playerHeight = 1.8f;  // Player height
        public LayerMask voxelLayer;       // Layer for voxel objects
        public int raycastSamples = 8;     // Number of rays for collision detection
        
        [Header("Ground Detection")]
        public float groundCheckDistance = 0.1f;
        public bool showDebugRays = true;
        
        // Components
        private CharacterController _controller;
        private Camera _camera;
        
        // State
        private Vector3 _velocity;
        private float _verticalRotation = 0f;
        private bool _isGrounded;
        private bool _wasGrounded;
        
        void Start()
        {
            _controller = GetComponent<CharacterController>();
            
            // Auto-find camera if not assigned
            if (cameraTransform == null)
            {
                _camera = GetComponentInChildren<Camera>();
                if (_camera != null)
                    cameraTransform = _camera.transform;
                else
                    Debug.LogError("[VoxelPlayerController] No camera found!");
            }
            else
            {
                _camera = cameraTransform.GetComponent<Camera>();
            }
            
            // Lock cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            Debug.Log("[VoxelPlayerController] Initialized - WASD to move, Space to jump, Mouse to look");
        }
        
        void Update()
        {
            HandleMouseLook();
            HandleMovement();
            HandleCursorToggle();
        }
        
        void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;
            
            // Horizontal rotation (Y-axis, body)
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);
            
            // Vertical rotation (X-axis, camera)
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            _verticalRotation -= mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -maxLookAngle, maxLookAngle);
            
            if (cameraTransform != null)
                cameraTransform.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
        }
        
        void HandleMovement()
        {
            // Ground check using voxel-precise detection
            _wasGrounded = _isGrounded;
            _isGrounded = CheckGroundVoxel();
            
            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            bool jump = Input.GetButtonDown("Jump");
            
            // Calculate movement direction (relative to camera)
            Vector3 forward = transform.forward;
            Vector3 right = transform.right;
            
            // Flatten to horizontal plane
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            
            Vector3 moveDirection = (forward * vertical + right * horizontal).normalized;
            
            // Apply speed
            float speed = isRunning ? runSpeed : walkSpeed;
            Vector3 move = moveDirection * speed;
            
            // Handle jumping
            if (_isGrounded)
            {
                _velocity.y = -2f; // Small downward force to stay grounded
                
                if (jump)
                {
                    _velocity.y = jumpForce;
                    Debug.Log("[VoxelPlayerController] Jump!");
                }
            }
            else
            {
                // Apply gravity
                _velocity.y -= gravity * Time.deltaTime;
            }
            
            // Combine horizontal movement with vertical velocity
            move.y = _velocity.y;
            
            // Move with CharacterController (handles basic collision)
            _controller.Move(move * Time.deltaTime);
            
            // Additional voxel-precise wall collision
            CheckWallCollision(ref move);
        }
        
        bool CheckGroundVoxel()
        {
            // Raycast downward from player center
            Vector3 origin = transform.position;
            Vector3 direction = Vector3.down;
            float distance = (_controller.height / 2f) + groundCheckDistance;
            
            // Sample multiple points around player cylinder
            for (int i = 0; i < raycastSamples; i++)
            {
                float angle = (i / (float)raycastSamples) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * playerRadius * 0.5f,
                    0f,
                    Mathf.Sin(angle) * playerRadius * 0.5f
                );
                
                Vector3 rayOrigin = origin + offset;
                
                if (Physics.Raycast(rayOrigin, direction, distance, voxelLayer))
                {
                    if (showDebugRays)
                        Debug.DrawRay(rayOrigin, direction * distance, Color.green);
                    return true;
                }
                
                if (showDebugRays)
                    Debug.DrawRay(rayOrigin, direction * distance, Color.red);
            }
            
            return false;
        }
        
        void CheckWallCollision(ref Vector3 move)
        {
            // Check horizontal movement for wall collisions
            Vector3 horizontalMove = new Vector3(move.x, 0f, move.z);
            if (horizontalMove.magnitude < 0.01f)
                return;
            
            Vector3 direction = horizontalMove.normalized;
            float distance = playerRadius + 0.1f;
            
            // Sample rays around player cylinder
            for (int i = 0; i < raycastSamples; i++)
            {
                float angle = (i / (float)raycastSamples) * Mathf.PI * 2f;
                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * playerRadius,
                    0f,
                    Mathf.Sin(angle) * playerRadius
                );
                
                Vector3 rayOrigin = transform.position + offset + Vector3.up * 0.5f;
                
                if (Physics.Raycast(rayOrigin, direction, distance, voxelLayer))
                {
                    // Hit a wall - stop horizontal movement
                    move.x = 0f;
                    move.z = 0f;
                    
                    if (showDebugRays)
                        Debug.DrawRay(rayOrigin, direction * distance, Color.yellow);
                    return;
                }
            }
        }
        
        void HandleCursorToggle()
        {
            // Press Escape to unlock cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        
        void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            
            // Draw player cylinder
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, playerRadius);
            Gizmos.DrawLine(
                transform.position - Vector3.up * (_controller.height / 2f),
                transform.position + Vector3.up * (_controller.height / 2f)
            );
        }
    }
}
