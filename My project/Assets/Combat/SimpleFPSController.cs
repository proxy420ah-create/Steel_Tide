// Steel Tide: First Device — Combat / Simple FPS Controller
// SimpleFPSController.cs
//
// Basic WASD + Mouse Look FPS controller
// Uses Unity 6 New Input System

using UnityEngine;
using UnityEngine.InputSystem;  // Unity 6 New Input System

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
            
            if (playerCamera == null)
                playerCamera = Camera.main;
                
            // Lock cursor for FPS gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        
        void Update()
        {
            HandleMovement();
            HandleMouseLook();
            
            // ESC to unlock cursor
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            
            // Click to re-lock cursor
            if (Mouse.current.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        
        void HandleMovement()
        {
            // Get input (New Input System)
            Vector2 moveInput = new Vector2(
                Keyboard.current.dKey.isPressed ? 1 : (Keyboard.current.aKey.isPressed ? -1 : 0),
                Keyboard.current.wKey.isPressed ? 1 : (Keyboard.current.sKey.isPressed ? -1 : 0)
            );
            
            // Check if running
            bool isRunning = Keyboard.current.leftShiftKey.isPressed;
            float speed = isRunning ? runSpeed : walkSpeed;
            
            // Calculate movement direction (relative to where player is facing)
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);
            
            // Ground movement
            if (_controller.isGrounded)
            {
                _moveDirection = (forward * moveInput.y + right * moveInput.x) * speed;
                
                // Jump
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
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
            // Get mouse input (New Input System)
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            float mouseX = mouseDelta.x * mouseSensitivity;
            float mouseY = mouseDelta.y * mouseSensitivity;
            
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
