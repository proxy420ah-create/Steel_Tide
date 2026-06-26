// Steel Tide: First Device — Combat / Simple FPS Controller
// SimpleFPSController.cs
//
// Basic WASD + Mouse Look FPS controller
// No external dependencies - just Unity built-ins

using UnityEngine;

namespace SteelTide.Combat
{
    [RequireComponent(typeof(CharacterController))]
    public class SimpleFPSController : MonoBehaviour
    {
        [Header("Movement")]
        public float walkSpeed = 5f;
        public float runSpeed = 10f;
        public float jumpForce = 7f;
        public float gravity = 20f;
        
        [Header("Mouse Look")]
        public float mouseSensitivity = 2f;
        public float maxLookAngle = 80f;
        
        [Header("References")]
        public Camera playerCamera;
        
        private CharacterController _controller;
        private Vector3 _moveDirection = Vector3.zero;
        private float _verticalRotation = 0f;
        
        void Start()
        {
            _controller = GetComponent<CharacterController>();
            
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();
                
            // Lock cursor for FPS gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        void Update()
        {
            HandleMovement();
            HandleMouseLook();
            
            // ESC to unlock cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            // Click to re-lock cursor
            if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        void HandleMovement()
        {
            // Get input
            float moveX = Input.GetAxis("Horizontal"); // A/D
            float moveZ = Input.GetAxis("Vertical");   // W/S
            
            // Check if running
            bool isRunning = Input.GetKey(KeyCode.LeftShift);
            float speed = isRunning ? runSpeed : walkSpeed;
            
            // Calculate movement direction (relative to where player is facing)
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            // Ground movement
            if (_controller.isGrounded)
            {
                _moveDirection = (forward * moveZ + right * moveX) * speed;
                
                // Jump
                if (Input.GetButtonDown("Jump"))
                {
                    _moveDirection.y = jumpForce;
                }
            }
            
            // Apply gravity
            _moveDirection.y -= gravity * Time.deltaTime;
            
            // Move the controller
            _controller.Move(_moveDirection * Time.deltaTime);
        }
        
        void HandleMouseLook()
        {
            // Get mouse input
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // Rotate player body left/right
            transform.Rotate(0, mouseX, 0);
            
            // Rotate camera up/down
            _verticalRotation -= mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -maxLookAngle, maxLookAngle);
            
            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(_verticalRotation, 0, 0);
            }
        }
    }
}
