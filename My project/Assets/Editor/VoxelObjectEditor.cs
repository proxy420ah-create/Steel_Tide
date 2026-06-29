using UnityEngine;
using UnityEditor;
using SteelTide.Voxels;
using System.IO;
using Unity.Mathematics;

/// <summary>
/// Custom editor for VoxelObject that loads .stasset dimensions in Edit Mode
/// </summary>
[CustomEditor(typeof(SteelTide.Voxels.VoxelObject))]
public class VoxelObjectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        VoxelObject voxelObj = (VoxelObject)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Asset Info", EditorStyles.boldLabel);
        
        // Show current dimensions
        EditorGUILayout.LabelField($"Volume Dims: {voxelObj.volumeDims.x} × {voxelObj.volumeDims.y} × {voxelObj.volumeDims.z}");
        
        EditorGUILayout.Space();
        
        // Button to load dimensions from .stasset file
        if (GUILayout.Button("Load Dimensions from .stasset File", GUILayout.Height(30)))
        {
            LoadAssetDimensions(voxelObj);
        }
        
        EditorGUILayout.HelpBox(
            "Click 'Load Dimensions' to read the actual voxel dimensions from the .stasset file.\n\n" +
            "This updates volumeDims BEFORE Play mode, so VoxelModelAligner can work in Edit mode!",
            MessageType.Info
        );
    }
    
    private void LoadAssetDimensions(VoxelObject voxelObj)
    {
        if (string.IsNullOrEmpty(voxelObj.assetFileName))
        {
            EditorUtility.DisplayDialog("Error", "Asset File Name is empty!", "OK");
            return;
        }
        
        string path = Path.Combine(Application.streamingAssetsPath, voxelObj.assetFileName);
        
        if (!File.Exists(path))
        {
            EditorUtility.DisplayDialog("Error", $"File not found:\n{path}", "OK");
            return;
        }
        
        try
        {
            Debug.Log($"[VoxelObjectEditor] Loading from path: {path}");
            Debug.Log($"[VoxelObjectEditor] File size: {new FileInfo(path).Length} bytes");
            
            // Read file header directly (faster than StAssetReader for just dimensions)
            byte[] header = new byte[16];
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                fs.Read(header, 0, 16);
            }
            
            // Parse dimensions (uint16 at offsets 6, 8, 10)
            int dimX = header[6] | (header[7] << 8);
            int dimY = header[8] | (header[9] << 8);
            int dimZ = header[10] | (header[11] << 8);
            
            Debug.Log($"[VoxelObjectEditor] Read from file: {dimX}×{dimY}×{dimZ}");
            Debug.Log($"[VoxelObjectEditor] Before update: {voxelObj.volumeDims}");
            
            // Use reflection to set int3 field directly (volumeDims is int3, not Vector3Int!)
            var field = typeof(VoxelObject).GetField("volumeDims", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                field.SetValue(voxelObj, new int3(dimX, dimY, dimZ));
            }
            else
            {
                Debug.LogError("[VoxelObjectEditor] Could not find volumeDims field!");
            }
            
            Debug.Log($"[VoxelObjectEditor] After update: {voxelObj.volumeDims}");
            
            EditorUtility.DisplayDialog("Success", 
                $"Loaded dimensions:\n{voxelObj.volumeDims.x} × {voxelObj.volumeDims.y} × {voxelObj.volumeDims.z}\n\n" +
                $"VoxelModelAligner will now use correct dimensions!",
                "OK");
            
            // Mark the object as dirty so Unity saves the change
            EditorUtility.SetDirty(voxelObj);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to load asset:\n{e.Message}", "OK");
            Debug.LogError($"[VoxelObjectEditor] Error loading {path}: {e}");
        }
    }
}
