using UnityEngine;

/// <summary>
/// Component for loading and rendering voxel assets from .stasset files.
/// Attach to GameObjects representing voxel buildings, props, etc.
/// </summary>
public class VoxelObject : MonoBehaviour
{
    [Header("Asset")]
    [Tooltip("Name of .stasset file in StreamingAssets folder")]
    public string assetFileName = "Building_House01.stasset";
    
    [Header("Voxel Grid")]
    [Tooltip("Size of each voxel in world units")]
    public float voxelSize = 0.125f;
    
    [Tooltip("Volume dimensions (auto-updated from asset at runtime)")]
    public Vector3Int volumeDims = new Vector3Int(32, 32, 32);
    
    [Header("Rendering")]
    [Tooltip("Camera to render voxels from (None = use VoxelRenderer component)")]
    public Camera voxelRenderer;
    
    [Header("Debug")]
    [Tooltip("Show gizmo wireframe in scene view")]
    public bool showGizmo = true;
    
    [Tooltip("Gizmo color")]
    public Color gizmoColor = Color.green;
    
    private VoxelWorld voxelWorld;
    private bool isRegistered = false;
    
    private void Start()
    {
        voxelWorld = VoxelWorld.Instance;
        RegisterWithWorld();
    }
    
    private void OnDestroy()
    {
        // TODO: Unregister voxels from world when object is destroyed
    }
    
    /// <summary>
    /// Register this voxel object's data with the VoxelWorld
    /// </summary>
    private void RegisterWithWorld()
    {
        if (isRegistered) return;
        
        if (voxelWorld == null)
        {
            Debug.LogError("VoxelWorld not found! Make sure VoxelWorld component exists in scene.");
            return;
        }
        
        // Register voxels at this object's world position
        voxelWorld.RegisterVoxelObject(transform.position, assetFileName, voxelSize);
        isRegistered = true;
        
        Debug.Log($"VoxelObject '{gameObject.name}' registered with VoxelWorld");
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmo) return;
        
        // Draw wireframe box showing voxel bounds
        Gizmos.color = gizmoColor;
        
        Vector3 size = new Vector3(
            volumeDims.x * voxelSize,
            volumeDims.y * voxelSize,
            volumeDims.z * voxelSize
        );
        
        Gizmos.DrawWireCube(transform.position, size);
        
        // Draw coordinate axes
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.right * voxelSize * 2);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * voxelSize * 2);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.forward * voxelSize * 2);
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        
        // Draw voxel grid when selected
        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.3f);
        
        Vector3 halfSize = new Vector3(
            volumeDims.x * voxelSize * 0.5f,
            volumeDims.y * voxelSize * 0.5f,
            volumeDims.z * voxelSize * 0.5f
        );
        
        Vector3 corner = transform.position - halfSize;
        
        // Draw grid lines (only draw every 4th line to avoid clutter)
        int step = Mathf.Max(1, volumeDims.x / 8);
        
        for (int x = 0; x <= volumeDims.x; x += step)
        {
            for (int y = 0; y <= volumeDims.y; y += step)
            {
                Vector3 start = corner + new Vector3(x * voxelSize, y * voxelSize, 0);
                Vector3 end = start + new Vector3(0, 0, volumeDims.z * voxelSize);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}
