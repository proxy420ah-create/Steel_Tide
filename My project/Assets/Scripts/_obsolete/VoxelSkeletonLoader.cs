using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Loads voxel skeleton from .stasset file and creates ragdoll physics hierarchy.
/// Detects bone voxels (material 12) and joint voxels (material 21).
/// </summary>
public class VoxelSkeletonLoader : MonoBehaviour
{
    [Header("Skeleton Asset")]
    [Tooltip("Name of .stasset file in StreamingAssets (e.g., 'Tpose.stasset')")]
    public string skeletonAssetName = "Tpose.stasset";
    
    [Tooltip("World position offset for skeleton")]
    public Vector3 positionOffset = Vector3.zero;
    
    [Header("Physics Settings")]
    [Tooltip("Mass per bone segment")]
    public float boneSegmentMass = 1.0f;
    
    [Tooltip("Drag for physics stability")]
    public float drag = 0.5f;
    
    [Tooltip("Angular drag for rotation damping")]
    public float angularDrag = 0.5f;
    
    [Header("Joint Settings")]
    [Tooltip("Spring force for joints")]
    public float jointSpring = 100f;
    
    [Tooltip("Damper for joint springs")]
    public float jointDamper = 10f;
    
    [Header("Visualization")]
    [Tooltip("Show bone GameObjects as spheres")]
    public bool showBoneGizmos = true;
    
    [Tooltip("Show joint connections")]
    public bool showJointGizmos = true;
    
    // Material IDs
    private const byte BONE_MATERIAL = 12;
    private const byte JOINT_MATERIAL = 21;
    
    // Voxel size (matches VoxelWorld)
    private float voxelSize = 0.125f;
    
    // Loaded skeleton data
    private Dictionary<Vector3Int, byte> voxelData = new Dictionary<Vector3Int, byte>();
    private List<Vector3Int> boneVoxels = new List<Vector3Int>();
    private List<Vector3Int> jointVoxels = new List<Vector3Int>();
    
    // Created GameObjects
    private List<GameObject> boneObjects = new List<GameObject>();
    private Dictionary<Vector3Int, GameObject> jointObjects = new Dictionary<Vector3Int, GameObject>();
    
    void Start()
    {
        LoadSkeletonAsset();
        CreateSkeletonHierarchy();
        AddPhysicsComponents();
    }
    
    /// <summary>
    /// Load .stasset file and extract voxel data
    /// </summary>
    void LoadSkeletonAsset()
    {
        string path = Path.Combine(Application.streamingAssetsPath, skeletonAssetName);
        
        if (!File.Exists(path))
        {
            Debug.LogError($"[VoxelSkeletonLoader] File not found: {path}");
            return;
        }
        
        using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
        {
            // Read header
            string magic = new string(reader.ReadChars(4));
            if (magic != "STAS")
            {
                Debug.LogError($"[VoxelSkeletonLoader] Invalid file format: {magic}");
                return;
            }
            
            byte version = reader.ReadByte();
            byte flags = reader.ReadByte();
            ushort width = reader.ReadUInt16();
            ushort height = reader.ReadUInt16();
            ushort depth = reader.ReadUInt16();
            uint reserved = reader.ReadUInt32();
            
            Debug.Log($"[VoxelSkeletonLoader] Loading {skeletonAssetName}: {width}×{height}×{depth}");
            
            // Read voxel data (X-major order, little-endian uint16)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        ushort materialID = reader.ReadUInt16();
                        
                        if (materialID > 0)
                        {
                            Vector3Int pos = new Vector3Int(x, y, z);
                            voxelData[pos] = (byte)materialID;
                            
                            // Categorize voxels
                            if (materialID == BONE_MATERIAL)
                            {
                                boneVoxels.Add(pos);
                            }
                            else if (materialID == JOINT_MATERIAL)
                            {
                                jointVoxels.Add(pos);
                            }
                        }
                    }
                }
            }
        }
        
        Debug.Log($"[VoxelSkeletonLoader] Loaded {boneVoxels.Count} bone voxels, {jointVoxels.Count} joint voxels");
    }
    
    /// <summary>
    /// Create GameObject hierarchy for skeleton
    /// </summary>
    void CreateSkeletonHierarchy()
    {
        // Create root object
        GameObject root = new GameObject("SkeletonRoot");
        root.transform.parent = transform;
        root.transform.localPosition = positionOffset;
        
        // Create bone segment GameObjects
        // For now, create one GameObject per bone voxel (simple approach)
        foreach (Vector3Int bonePos in boneVoxels)
        {
            GameObject boneObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            boneObj.name = $"Bone_{bonePos.x}_{bonePos.y}_{bonePos.z}";
            boneObj.transform.parent = root.transform;
            boneObj.transform.localPosition = VoxelToWorld(bonePos);
            boneObj.transform.localScale = Vector3.one * voxelSize;
            
            // Visual: Make bones white
            Renderer renderer = boneObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.9f, 0.86f, 0.78f); // Bone color
            }
            
            boneObjects.Add(boneObj);
        }
        
        // Create joint GameObjects
        foreach (Vector3Int jointPos in jointVoxels)
        {
            GameObject jointObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            jointObj.name = $"Joint_{jointPos.x}_{jointPos.y}_{jointPos.z}";
            jointObj.transform.parent = root.transform;
            jointObj.transform.localPosition = VoxelToWorld(jointPos);
            jointObj.transform.localScale = Vector3.one * voxelSize * 1.2f; // Slightly larger
            
            // Visual: Make joints red
            Renderer renderer = jointObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(1.0f, 0.25f, 0.25f); // Joint color
            }
            
            jointObjects[jointPos] = jointObj;
        }
        
        Debug.Log($"[VoxelSkeletonLoader] Created {boneObjects.Count} bone objects, {jointObjects.Count} joint objects");
    }
    
    /// <summary>
    /// Add Rigidbody and joints for ragdoll physics
    /// </summary>
    void AddPhysicsComponents()
    {
        // Add Rigidbody to each bone
        foreach (GameObject boneObj in boneObjects)
        {
            Rigidbody rb = boneObj.AddComponent<Rigidbody>();
            rb.mass = boneSegmentMass;
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.useGravity = true;
        }
        
        // Add Rigidbody to each joint
        foreach (GameObject jointObj in jointObjects.Values)
        {
            Rigidbody rb = jointObj.AddComponent<Rigidbody>();
            rb.mass = boneSegmentMass * 0.5f; // Joints are lighter
            rb.linearDamping = drag;
            rb.angularDamping = angularDrag;
            rb.useGravity = true;
        }
        
        // Connect bones with joints using ConfigurableJoint
        // Simple approach: Connect each bone to nearest joint
        foreach (GameObject boneObj in boneObjects)
        {
            GameObject nearestJoint = FindNearestJoint(boneObj.transform.position);
            
            if (nearestJoint != null)
            {
                ConfigurableJoint joint = boneObj.AddComponent<ConfigurableJoint>();
                joint.connectedBody = nearestJoint.GetComponent<Rigidbody>();
                
                // Configure joint limits (ball joint behavior)
                joint.xMotion = ConfigurableJointMotion.Locked;
                joint.yMotion = ConfigurableJointMotion.Locked;
                joint.zMotion = ConfigurableJointMotion.Locked;
                
                joint.angularXMotion = ConfigurableJointMotion.Limited;
                joint.angularYMotion = ConfigurableJointMotion.Limited;
                joint.angularZMotion = ConfigurableJointMotion.Limited;
                
                // Set angular limits
                SoftJointLimit limit = new SoftJointLimit();
                limit.limit = 45f; // Max rotation angle
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
            }
        }
        
        Debug.Log($"[VoxelSkeletonLoader] Added physics components. Press Play to see ragdoll!");
    }
    
    /// <summary>
    /// Find nearest joint to a position
    /// </summary>
    GameObject FindNearestJoint(Vector3 position)
    {
        GameObject nearest = null;
        float minDist = float.MaxValue;
        
        foreach (GameObject jointObj in jointObjects.Values)
        {
            float dist = Vector3.Distance(position, jointObj.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = jointObj;
            }
        }
        
        return nearest;
    }
    
    /// <summary>
    /// Convert voxel grid position to world position
    /// </summary>
    Vector3 VoxelToWorld(Vector3Int voxelPos)
    {
        return new Vector3(
            voxelPos.x * voxelSize,
            voxelPos.y * voxelSize,
            voxelPos.z * voxelSize
        );
    }
    
    void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;
        
        // Draw bone connections
        if (showBoneGizmos)
        {
            Gizmos.color = Color.white;
            foreach (GameObject boneObj in boneObjects)
            {
                if (boneObj != null)
                {
                    Gizmos.DrawWireSphere(boneObj.transform.position, voxelSize * 0.5f);
                }
            }
        }
        
        // Draw joint connections
        if (showJointGizmos)
        {
            Gizmos.color = Color.red;
            foreach (GameObject jointObj in jointObjects.Values)
            {
                if (jointObj != null)
                {
                    Gizmos.DrawWireSphere(jointObj.transform.position, voxelSize * 0.6f);
                }
            }
        }
    }
}
