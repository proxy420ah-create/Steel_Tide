using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Custom voxel-based physics controller for player character.
/// Uses raymarching for collision detection instead of Unity's built-in physics.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class VoxelPhysics : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Movement speed in units/second")]
    public float moveSpeed = 5f;
    
    [Tooltip("Jump velocity")]
    public float jumpForce = 8f;
    
    [Tooltip("Sprint multiplier")]
    public float sprintMultiplier = 1.5f;
    
    [Header("Physics")]
    [Tooltip("Gravity acceleration")]
    public float gravity = -20f;
    
    [Tooltip("Maximum fall speed")]
    public float terminalVelocity = -50f;
    
    [Tooltip("Ground check distance (slightly larger than voxel size)")]
    public float groundCheckDistance = 0.2f;
    
    [Tooltip("Player collision radius (in voxels)")]
    public float collisionRadius = 0.5f;
    
    [Header("Camera")]
    [Tooltip("Camera transform for look direction")]
    public Transform cameraTransform;
    
    [Tooltip("Base mouse sensitivity")]
    public float mouseSensitivity = 2f;
    
    [Tooltip("Mouse sensitivity multiplier (0.1 = very slow, 1.0 = normal, 2.0 = very fast)")]
    [Range(0.1f, 2.0f)]
    public float mouseSensitivityMultiplier = 1.0f;
    
    [Tooltip("Keyboard movement sensitivity multiplier (0.1 = very slow, 1.0 = normal, 2.0 = very fast)")]
    [Range(0.1f, 2.0f)]
    public float keyboardSensitivityMultiplier = 1.0f;
    
    [Tooltip("Vertical look limits")]
    public float maxLookAngle = 80f;
    
    [Header("Debug")]
    public bool showDebugRays = false;
    
    // Internal state
    private Vector3 velocity;
    private bool isGrounded;
    private float cameraPitch = 0f;
    private CharacterController controller;
    private VoxelWorld voxelWorld;
    
    // Input cache
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool jumpInput;
    private bool jumpInputConsumed; // Track if jump was processed
    private bool sprintInput;
    
    private void Start()
    {
        controller = GetComponent<CharacterController>();
        voxelWorld = VoxelWorld.Instance;
        
        if (cameraTransform == null)
        {
            cameraTransform = Camera.main.transform;
        }
        
        // Lock cursor for FPS controls
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    private void Update()
    {
        // Gather input
        GatherInput();
        
        // Update camera rotation
        UpdateCameraRotation();
    }
    
    private void FixedUpdate()
    {
        // Physics runs at fixed timestep
        CheckGrounded();
        ApplyGravity();
        ApplyMovement();
    }
    
    #region Input
    
    private void GatherInput()
    {
        // Movement input (WASD)
        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float horizontal = 0f;
            float vertical = 0f;
            
            if (keyboard.aKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.wKey.isPressed) vertical += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;
            
            moveInput = new Vector2(horizontal, vertical);
            
            // Jump input (Space) - latch until consumed by FixedUpdate
            if (keyboard.spaceKey.wasPressedThisFrame)
            {
                jumpInput = true;
                jumpInputConsumed = false;
                Debug.Log($"[VoxelPhysics] SPACE PRESSED! jumpInput latched, isGrounded={isGrounded}");
            }
            
            // Sprint input (Shift)
            sprintInput = keyboard.leftShiftKey.isPressed;
            
            // Unlock cursor (Escape)
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        
        // Look input (Mouse)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            lookInput = mouse.delta.ReadValue();
            
            // Re-lock cursor (Left Click)
            if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    #endregion
    
    #region Camera
    
    private void UpdateCameraRotation()
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;
        
        // Apply sensitivity multiplier for fine control
        float effectiveMouseSens = mouseSensitivity * mouseSensitivityMultiplier;
        
        // Horizontal rotation (Y-axis) - rotate player body
        float yaw = lookInput.x * effectiveMouseSens;
        transform.Rotate(Vector3.up * yaw);
        
        // Vertical rotation (X-axis) - rotate camera only
        cameraPitch -= lookInput.y * effectiveMouseSens;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        cameraTransform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }
    
    #endregion
    
    #region Physics
    
    private void CheckGrounded()
    {
        // Cast ray downward from player center
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = Vector3.down;
        
        VoxelHit hit = voxelWorld.RaymarchChunk(rayOrigin, rayDirection, groundCheckDistance);
        
        isGrounded = hit.hit;
        
        if (showDebugRays)
        {
            Color rayColor = isGrounded ? Color.green : Color.red;
            Debug.DrawRay(rayOrigin, rayDirection * groundCheckDistance, rayColor);
            
            // Debug log every 60 frames (~1 second)
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"Ground Check: Origin={rayOrigin}, Grounded={isGrounded}, Hit={hit.hit}, Distance={hit.distance}");
            }
        }
        
        // Reset vertical velocity if grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
    }
    
    private void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y += gravity * Time.fixedDeltaTime;
            velocity.y = Mathf.Max(velocity.y, terminalVelocity);
        }
        
        // Handle jump
        if (jumpInput && !jumpInputConsumed && isGrounded)
        {
            velocity.y = jumpForce;
            jumpInput = false; // Reset for next jump
            jumpInputConsumed = true;
            Debug.Log($"[VoxelPhysics] ✅ JUMP! velocity.y = {jumpForce}");
        }
        // Jump blocked silently if not grounded (no spam)
    }
    
    private void ApplyMovement()
    {
        // Calculate movement direction relative to camera
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        // Flatten to horizontal plane
        forward.y = 0;
        right.y = 0;
        forward.Normalize();
        right.Normalize();
        
        // Calculate desired movement
        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        
        // Apply speed with keyboard sensitivity multiplier for fine control
        float baseSpeed = sprintInput ? moveSpeed * sprintMultiplier : moveSpeed;
        float currentSpeed = baseSpeed * keyboardSensitivityMultiplier;
        Vector3 horizontalVelocity = moveDirection * currentSpeed;
        
        // Combine horizontal and vertical velocity
        Vector3 finalVelocity = new Vector3(
            horizontalVelocity.x,
            velocity.y,
            horizontalVelocity.z
        );
        
        // Check for collisions before moving
        Vector3 desiredMove = finalVelocity * Time.fixedDeltaTime;
        
        if (CheckCollision(desiredMove))
        {
            // Collision detected - try sliding along walls
            desiredMove = HandleCollisionSliding(desiredMove);
        }
        
        // Apply movement
        controller.Move(desiredMove);
    }
    
    private bool CheckCollision(Vector3 moveVector)
    {
        // Cast ray in movement direction
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = moveVector.normalized;
        float rayDistance = moveVector.magnitude + collisionRadius;
        
        VoxelHit hit = voxelWorld.RaymarchChunk(rayOrigin, rayDirection, rayDistance);
        
        if (showDebugRays && hit.hit)
        {
            Debug.DrawLine(rayOrigin, hit.worldPosition, Color.yellow);
        }
        
        return hit.hit;
    }
    
    private Vector3 HandleCollisionSliding(Vector3 moveVector)
    {
        // Simple wall sliding: project movement onto wall surface
        Vector3 rayOrigin = transform.position;
        Vector3 rayDirection = moveVector.normalized;
        float rayDistance = moveVector.magnitude + collisionRadius;
        
        VoxelHit hit = voxelWorld.RaymarchChunk(rayOrigin, rayDirection, rayDistance);
        
        if (hit.hit)
        {
            // Project movement vector onto wall surface
            Vector3 wallNormal = new Vector3(hit.normal.x, hit.normal.y, hit.normal.z).normalized;
            Vector3 slideVector = Vector3.ProjectOnPlane(moveVector, wallNormal);
            
            // Check if slide movement also collides
            if (!CheckCollision(slideVector))
            {
                return slideVector;
            }
        }
        
        // Can't move - return zero
        return Vector3.zero;
    }
    
    #endregion
    
    #region Debug
    
    private void OnGUI()
    {
        if (!showDebugRays) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label($"Grounded: {isGrounded}");
        GUILayout.Label($"Velocity: {velocity}");
        GUILayout.Label($"Position: {transform.position}");
        GUILayout.Label($"Speed: {controller.velocity.magnitude:F2} u/s");
        GUILayout.EndArea();
    }
    
    #endregion
}
