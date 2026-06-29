using UnityEngine;
using Unity.Mathematics;

/// <summary>
/// Automatically centers a VoxelObject on its parent GameObject.
/// Accounts for corner-pivot .stasset files by calculating the offset needed
/// to center the voxel model on the parent's origin.
/// </summary>
[ExecuteInEditMode]
public class VoxelModelAligner : MonoBehaviour
{
    [Header("Auto-Alignment")]
    [Tooltip("Automatically align on Start and when values change in editor")]
    public bool autoAlign = true;
    
    [Tooltip("Align the bottom of the model to Y=0 (feet on ground)")]
    public bool alignBottom = true;
    
    [Header("Manual Control")]
    [Tooltip("Click to manually trigger alignment")]
    public bool alignNow = false;
    
    [Header("Debug")]
    public bool showDebug = false;
    
    [Header("Saved Alignment (Persists After Play Mode)")]
    [Tooltip("Last calculated alignment offset - use this in Edit mode")]
    public Vector3 savedAlignment = Vector3.zero;
    
    [Tooltip("Apply the saved alignment now")]
    public bool applySavedAlignment = false;
    
    private SteelTide.Voxels.VoxelObject voxelObject;
    private Vector3 lastVolumeDims;
    private float lastVoxelSize;
    
    private void Start()
    {
        if (autoAlign)
        {
            AlignModel();
        }
    }
    
    private void Update()
    {
        // Only run in editor
        if (!Application.isPlaying)
        {
            // Check if alignment is needed
            if (alignNow)
            {
                AlignModel();
                alignNow = false;
            }
            
            // Auto-align if dimensions changed
            if (autoAlign && HasDimensionsChanged())
            {
                AlignModel();
            }
        }
    }
    
    private bool HasDimensionsChanged()
    {
        if (voxelObject == null)
        {
            voxelObject = GetComponent<SteelTide.Voxels.VoxelObject>();
            if (voxelObject == null) return false;
        }
        
        Vector3 currentDims = new Vector3(voxelObject.volumeDims.x, voxelObject.volumeDims.y, voxelObject.volumeDims.z);
        float currentSize = voxelObject.voxelSize;
        
        if (currentDims != lastVolumeDims || currentSize != lastVoxelSize)
        {
            lastVolumeDims = currentDims;
            lastVoxelSize = currentSize;
            return true;
        }
        
        return false;
    }
    
    public void AlignModel()
    {
        voxelObject = GetComponent<SteelTide.Voxels.VoxelObject>();
        
        if (voxelObject == null)
        {
            Debug.LogWarning("[VoxelModelAligner] No VoxelObject component found!");
            return;
        }
        
        // Get ACTUAL dimensions from the loaded asset
        // In edit mode, volumeDims gets updated when the asset is loaded
        int3 dims = voxelObject.volumeDims;
        float voxelSize = voxelObject.voxelSize;
        
        // Validate dimensions
        if (dims.x == 0 || dims.y == 0 || dims.z == 0)
        {
            Debug.LogWarning($"[VoxelModelAligner] Invalid dimensions: {dims.x}×{dims.y}×{dims.z}. " +
                           $"Make sure the .stasset file is loaded. Try entering Play mode first.");
            return;
        }
        
        // Calculate world-space dimensions
        float width = dims.x * voxelSize;   // X dimension
        float height = dims.y * voxelSize;  // Y dimension
        float depth = dims.z * voxelSize;   // Z dimension
        
        // Calculate centering offset
        // Since .stasset files have corner pivot, we need to shift by half the dimensions
        float offsetX = -width / 2f;
        float offsetY = alignBottom ? 0f : -height / 2f;  // If alignBottom, keep Y=0 at feet
        float offsetZ = -depth / 2f;
        
        // Calculate final position
        Vector3 alignmentOffset = new Vector3(offsetX, offsetY, offsetZ);
        
        // Save the alignment for use after exiting Play mode
        savedAlignment = alignmentOffset;
        
        // Apply offset
        transform.localPosition = alignmentOffset;
        
        if (showDebug || !Application.isPlaying)
        {
            Debug.Log($"[VoxelModelAligner] Aligned model:\n" +
                     $"  Asset: {voxelObject.assetFileName}\n" +
                     $"  Voxel Dims: {dims.x}×{dims.y}×{dims.z}\n" +
                     $"  Voxel Size: {voxelSize}\n" +
                     $"  World Dims: {width:F3}×{height:F3}×{depth:F3}\n" +
                     $"  Offset: ({offsetX:F3}, {offsetY:F3}, {offsetZ:F3})\n" +
                     $"  Saved Alignment: {savedAlignment}\n" +
                     $"  Local Position: {transform.localPosition}");
        }
    }
    
    public void ApplySavedAlignment()
    {
        if (savedAlignment == Vector3.zero)
        {
            Debug.LogWarning("[VoxelModelAligner] No saved alignment! Press Play first to calculate alignment.");
            return;
        }
        
        transform.localPosition = savedAlignment;
        Debug.Log($"[VoxelModelAligner] Applied saved alignment: {savedAlignment}");
    }
    
    // Editor helper - show alignment button in inspector
    private void OnValidate()
    {
        if (alignNow && !Application.isPlaying)
        {
            AlignModel();
            alignNow = false;
        }
        
        // Auto-align when dimensions change in edit mode
        if (autoAlign && !Application.isPlaying && HasDimensionsChanged())
        {
            AlignModel();
        }
        
        // Apply saved alignment if requested
        if (applySavedAlignment && !Application.isPlaying)
        {
            ApplySavedAlignment();
            applySavedAlignment = false;
        }
    }
}
