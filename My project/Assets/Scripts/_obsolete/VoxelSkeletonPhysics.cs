using UnityEngine;
using System.Collections.Generic;
using SteelTide.Voxels;

/// <summary>
/// Add ragdoll physics to an existing VoxelObject that contains a skeleton.
/// Attach this to the same GameObject as VoxelObject.
/// </summary>
[RequireComponent(typeof(VoxelObject))]
public class VoxelSkeletonPhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    [Tooltip("Enable ragdoll physics on Start")]
    public bool enablePhysicsOnStart = true;
    
    [Tooltip("Mass of the entire skeleton")]
    public float totalMass = 10.0f;
    
    [Tooltip("Drag for physics stability")]
    public float drag = 0.5f;
    
    [Tooltip("Angular drag for rotation damping")]
    public float angularDrag = 0.5f;
    
    [Tooltip("Gravity force")]
    public float gravity = 9.8f;
    
    [Header("Voxel Collision")]
    [Tooltip("Layer mask for voxel collision detection (set to 'Default' or your voxel layer)")]
    public LayerMask voxelLayer = -1; // Default to all layers
    
    [Tooltip("Ground check distance")]
    public float groundCheckDistance = 0.5f;
    
    [Tooltip("Number of raycasts for ground detection")]
    public int raycastSamples = 8;
    
    [Tooltip("Collision radius for ground checks")]
    public float collisionRadius = 0.5f;
    
    [Tooltip("Show debug rays")]
    public bool showDebugRays = true;
    
    [Header("Joint Detection")]
    [Tooltip("Material ID for bone voxels")]
    public byte boneMaterialID = 12;
    
    [Tooltip("Material ID for joint voxels")]
    public byte jointMaterialID = 21;
    
    private VoxelObject voxelObject;
    private bool physicsEnabled = false;
    private Vector3 velocity = Vector3.zero;
    private bool isGrounded = false;
    
    void Start()
    {
        voxelObject = GetComponent<VoxelObject>();
        
        if (voxelObject == null)
        {
            Debug.LogError("[VoxelSkeletonPhysics] No VoxelObject found on this GameObject!");
            return;
        }
        
        if (enablePhysicsOnStart)
        {
            EnablePhysics();
        }
    }
    
    void Update()
    {
        if (!physicsEnabled) return;
        
        // Check if grounded using voxel raycasting
        isGrounded = CheckGroundVoxel();
        
        // Apply gravity
        if (!isGrounded)
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = 0f;
        }
        
        // Apply velocity
        transform.position += velocity * Time.deltaTime;
    }
    
    /// <summary>
    /// Enable ragdoll physics for this skeleton
    /// </summary>
    public void EnablePhysics()
    {
        if (physicsEnabled) return;
        
        physicsEnabled = true;
        velocity = Vector3.zero;
        
        Debug.Log("[VoxelSkeletonPhysics] Ragdoll physics enabled! Using voxel raycasting for collision.");
    }
    
    /// <summary>
    /// Disable ragdoll physics
    /// </summary>
    public void DisablePhysics()
    {
        if (!physicsEnabled) return;
        
        physicsEnabled = false;
        velocity = Vector3.zero;
        
        Debug.Log("[VoxelSkeletonPhysics] Ragdoll physics disabled");
    }
    
    /// <summary>
    /// Check if skeleton is standing on voxel ground using raycasting
    /// Matches VoxelPlayerController pattern: center + ring raycasts
    /// </summary>
    bool CheckGroundVoxel()
    {
        Vector3 origin = transform.position;
        Vector3 direction = Vector3.down;
        float distance = groundCheckDistance;
        
        // CRITICAL: Check center first (detects holes directly beneath skeleton)
        if (Physics.Raycast(origin, direction, distance, voxelLayer))
        {
            if (showDebugRays)
                Debug.DrawRay(origin, direction * distance, Color.green);
            return true;
        }
        
        if (showDebugRays)
            Debug.DrawRay(origin, direction * distance, Color.red);
        
        // Sample multiple points around skeleton cylinder edge
        for (int i = 0; i < raycastSamples; i++)
        {
            float angle = (i / (float)raycastSamples) * Mathf.PI * 2f;
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * collisionRadius,
                0f,
                Mathf.Sin(angle) * collisionRadius
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
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !physicsEnabled) return;
        
        // Draw ground check ray
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
}
