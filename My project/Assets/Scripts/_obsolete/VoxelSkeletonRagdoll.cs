using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SteelTide.Voxels;

/// <summary>
/// Hybrid ragdoll + voxel physics system for voxel skeletons.
/// Auto-builds ragdoll from bone/joint voxels, uses Unity physics for realistic collapse,
/// and voxel raycasting to prevent tunneling through voxel ground.
/// </summary>
[RequireComponent(typeof(VoxelObject))]
public class VoxelSkeletonRagdoll : MonoBehaviour
{
    [Header("Material Detection")]
    [Tooltip("Material ID for bone voxels")]
    public byte boneMaterialID = 12;
    
    [Tooltip("Material ID for joint voxels")]
    public byte jointMaterialID = 21;
    
    [Header("Physics Settings")]
    [Tooltip("Mass per bone segment")]
    public float boneSegmentMass = 1.0f;
    
    [Tooltip("Drag for physics stability")]
    public float drag = 0.5f;
    
    [Tooltip("Angular drag for rotation damping")]
    public float angularDrag = 0.5f;
    
    [Header("Voxel Ground Detection")]
    [Tooltip("Ground check distance (in world units)")]
    public float groundCheckDistance = 0.5f;
    
    [Tooltip("Force applied when bone is below ground")]
    public float groundCorrectionForce = 10f;
    
    [Header("Joint Settings")]
    [Tooltip("Max rotation angle for joints (degrees)")]
    public float maxJointAngle = 45f;
    
    [Tooltip("Joint spring force")]
    public float jointSpring = 100f;
    
    [Tooltip("Joint damper")]
    public float jointDamper = 10f;
    
    [Header("Debug")]
    [Tooltip("Show debug rays and gizmos")]
    public bool showDebug = true;
    
    // Components
    private VoxelObject voxelObject;
    
    // Skeleton data
    private List<BoneSegment> boneSegments = new List<BoneSegment>();
    private List<Vector3Int> boneVoxels = new List<Vector3Int>();
    private List<Vector3Int> jointVoxels = new List<Vector3Int>();
    
    // Voxel size (from VoxelObject)
    private float voxelSize = 0.125f;
    
    /// <summary>
    /// Represents a bone segment with physics components
    /// </summary>
    private class BoneSegment
    {
        public GameObject gameObject;
        public Rigidbody rigidbody;
        public CapsuleCollider collider;
        public ConfigurableJoint joint;
        public string name;
        public Vector3 centerPosition;
        public float length;
        public BoneSegment parent;
        public List<Vector3Int> voxelPositions = new List<Vector3Int>();
    }
    
    void Start()
    {
        voxelObject = GetComponent<VoxelObject>();
        
        if (voxelObject == null)
        {
            Debug.LogError("[VoxelSkeletonRagdoll] No VoxelObject found!");
            return;
        }
        
        voxelSize = voxelObject.voxelSize;
        
        // Build ragdoll from voxel data
        BuildRagdollFromVoxels();
    }
    
    void FixedUpdate()
    {
        // Voxel ground detection for each bone
        foreach (var segment in boneSegments)
        {
            if (segment.rigidbody == null) continue;
            
            CheckVoxelGroundContact(segment);
        }
    }
    
    /// <summary>
    /// Scan VoxelObject for bone/joint voxels and build ragdoll
    /// </summary>
    void BuildRagdollFromVoxels()
    {
        Debug.Log("[VoxelSkeletonRagdoll] Building ragdoll from voxel data...");
        
        // Step 1: Find all bone and joint voxels
        ScanForSkeletonVoxels();
        
        if (boneVoxels.Count == 0)
        {
            Debug.LogWarning("[VoxelSkeletonRagdoll] No bone voxels found! Make sure skeleton uses material ID " + boneMaterialID);
            return;
        }
        
        Debug.Log($"[VoxelSkeletonRagdoll] Found {boneVoxels.Count} bone voxels, {jointVoxels.Count} joint voxels");
        
        // Step 2: Group bone voxels into segments
        GroupBonesIntoSegments();
        
        // Step 3: Create physics objects for each segment
        CreatePhysicsObjects();
        
        // Step 4: Connect segments with joints
        ConnectSegmentsWithJoints();
        
        Debug.Log($"[VoxelSkeletonRagdoll] Ragdoll created with {boneSegments.Count} bone segments");
    }
    
    /// <summary>
    /// Scan voxel data for bone and joint materials
    /// </summary>
    void ScanForSkeletonVoxels()
    {
        // Access voxel data from VoxelObject
        // Note: VoxelObject stores data as ushort[] in X-major order
        var volumeDims = voxelObject.volumeDims;
        
        // We need to access the voxel data - this may require making it public or adding a getter
        // For now, we'll scan by checking each position
        for (int x = 0; x < volumeDims.x; x++)
        {
            for (int y = 0; y < volumeDims.y; y++)
            {
                for (int z = 0; z < volumeDims.z; z++)
                {
                    Vector3Int pos = new Vector3Int(x, y, z);
                    byte material = GetVoxelMaterial(pos);
                    
                    if (material == boneMaterialID)
                    {
                        boneVoxels.Add(pos);
                    }
                    else if (material == jointMaterialID)
                    {
                        jointVoxels.Add(pos);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Get material ID at voxel position (helper method)
    /// </summary>
    byte GetVoxelMaterial(Vector3Int pos)
    {
        // Get voxel data from VoxelObject
        ushort voxelValue = voxelObject.GetVoxel(pos.x, pos.y, pos.z);
        
        // Extract material ID from voxel value (lower 8 bits)
        byte materialID = (byte)(voxelValue & 0xFF);
        
        return materialID;
    }
    
    /// <summary>
    /// Group connected bone voxels into segments
    /// </summary>
    void GroupBonesIntoSegments()
    {
        // Simple approach: Group bones by vertical columns (Y-axis)
        // More sophisticated: Use flood-fill to find connected components
        
        // For T-Pose skeleton, we can identify segments by Y-ranges
        var groupedByY = boneVoxels.GroupBy(v => v.y).OrderByDescending(g => g.Key);
        
        int segmentId = 0;
        foreach (var group in groupedByY)
        {
            if (group.Count() == 0) continue;
            
            BoneSegment segment = new BoneSegment();
            segment.name = $"Segment_{segmentId++}_Y{group.Key}";
            segment.voxelPositions = group.ToList();
            
            // Calculate center position
            Vector3 sum = Vector3.zero;
            foreach (var voxel in segment.voxelPositions)
            {
                sum += VoxelToWorld(voxel);
            }
            segment.centerPosition = sum / segment.voxelPositions.Count;
            
            // Calculate length (approximate)
            segment.length = segment.voxelPositions.Count * voxelSize;
            
            boneSegments.Add(segment);
        }
        
        // Assign parent relationships (bottom-up)
        for (int i = 0; i < boneSegments.Count - 1; i++)
        {
            boneSegments[i].parent = boneSegments[i + 1];
        }
    }
    
    /// <summary>
    /// Create Rigidbody + Collider for each bone segment
    /// </summary>
    void CreatePhysicsObjects()
    {
        foreach (var segment in boneSegments)
        {
            // Create GameObject
            GameObject boneObj = new GameObject($"Bone_{segment.name}");
            boneObj.transform.parent = transform;
            boneObj.transform.position = segment.centerPosition;
            
            // Add Rigidbody (Unity physics)
            Rigidbody rb = boneObj.AddComponent<Rigidbody>();
            rb.mass = boneSegmentMass;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.useGravity = true;  // Unity handles gravity
            
            // Add Collider
            CapsuleCollider collider = boneObj.AddComponent<CapsuleCollider>();
            collider.radius = voxelSize * 0.5f;
            collider.height = Mathf.Max(segment.length, voxelSize * 2f);
            collider.direction = 1; // Y-axis
            
            // Store references
            segment.gameObject = boneObj;
            segment.rigidbody = rb;
            segment.collider = collider;
        }
    }
    
    /// <summary>
    /// Connect bone segments with ConfigurableJoint
    /// </summary>
    void ConnectSegmentsWithJoints()
    {
        foreach (var segment in boneSegments)
        {
            if (segment.parent == null) continue; // Root bone
            
            // Add ConfigurableJoint
            ConfigurableJoint joint = segment.gameObject.AddComponent<ConfigurableJoint>();
            joint.connectedBody = segment.parent.rigidbody;
            
            // Lock position (bones don't separate)
            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;
            
            // Allow limited rotation (ragdoll bending)
            joint.angularXMotion = ConfigurableJointMotion.Limited;
            joint.angularYMotion = ConfigurableJointMotion.Limited;
            joint.angularZMotion = ConfigurableJointMotion.Limited;
            
            // Set rotation limits
            SoftJointLimit limit = new SoftJointLimit();
            limit.limit = maxJointAngle;
            joint.lowAngularXLimit = limit;
            joint.highAngularXLimit = limit;
            joint.angularYLimit = limit;
            joint.angularZLimit = limit;
            
            // Add spring for stability
            JointDrive drive = new JointDrive();
            drive.positionSpring = jointSpring;
            drive.positionDamper = jointDamper;
            drive.maximumForce = Mathf.Infinity;
            joint.slerpDrive = drive;
            
            segment.joint = joint;
        }
    }
    
    /// <summary>
    /// Check if bone is in contact with voxel ground and apply correction
    /// Uses VoxelWorld.GetVoxel() to detect solid voxels (your architecture)
    /// </summary>
    void CheckVoxelGroundContact(BoneSegment segment)
    {
        Vector3 bonePos = segment.rigidbody.position;
        
        // Sample voxel positions below bone
        bool groundDetected = false;
        float groundHeight = 0f;
        
        // Check multiple points below bone (center + ring)
        for (int i = 0; i < 5; i++)
        {
            Vector3 checkPos = bonePos;
            
            if (i > 0)
            {
                // Ring of points around center
                float angle = (i - 1) / 4f * Mathf.PI * 2f;
                checkPos += new Vector3(
                    Mathf.Cos(angle) * voxelSize * 0.5f,
                    0f,
                    Mathf.Sin(angle) * voxelSize * 0.5f
                );
            }
            
            // Check downward for solid voxels
            for (float y = 0; y < groundCheckDistance; y += voxelSize)
            {
                Vector3 samplePos = checkPos + Vector3.down * y;
                
                // Query VoxelWorld (your architecture!)
                byte material = VoxelWorld.Instance.GetVoxel(samplePos);
                
                if (material != 0)
                {
                    // Found solid voxel!
                    groundDetected = true;
                    groundHeight = Mathf.Max(groundHeight, samplePos.y);
                    
                    if (showDebug)
                        Debug.DrawLine(bonePos, samplePos, Color.green);
                    break;
                }
                else if (showDebug)
                {
                    Debug.DrawLine(bonePos, samplePos, Color.red);
                }
            }
        }
        
        // If bone is below detected ground, push it up
        if (groundDetected && segment.rigidbody.position.y < groundHeight)
        {
            // Apply upward impulse
            segment.rigidbody.AddForce(Vector3.up * groundCorrectionForce, ForceMode.Impulse);
            
            // Directly set position to ground level
            Vector3 correctedPos = segment.rigidbody.position;
            correctedPos.y = groundHeight + voxelSize * 0.5f; // Slightly above ground
            segment.rigidbody.position = correctedPos;
        }
    }
    
    /// <summary>
    /// Convert voxel grid position to world position
    /// </summary>
    Vector3 VoxelToWorld(Vector3Int voxelPos)
    {
        return transform.position + new Vector3(
            voxelPos.x * voxelSize,
            voxelPos.y * voxelSize,
            voxelPos.z * voxelSize
        );
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showDebug) return;
        
        // Draw bone segments
        Gizmos.color = Color.white;
        foreach (var segment in boneSegments)
        {
            if (segment.gameObject != null)
            {
                Gizmos.DrawWireSphere(segment.gameObject.transform.position, voxelSize * 0.5f);
            }
        }
        
        // Draw joint connections
        Gizmos.color = Color.yellow;
        foreach (var segment in boneSegments)
        {
            if (segment.parent != null && segment.gameObject != null && segment.parent.gameObject != null)
            {
                Gizmos.DrawLine(segment.gameObject.transform.position, segment.parent.gameObject.transform.position);
            }
        }
    }
}
