using UnityEngine;
using UnityEditor;
using SteelTide.Voxels;
using System.IO;
using Unity.Mathematics;

/// <summary>
/// Menu item to manually load voxel dimensions from .stasset files
/// </summary>
public class VoxelDimensionLoader
{
    [MenuItem("Tools/Load Voxel Dimensions from Selected Objects")]
    public static void LoadDimensionsFromSelection()
    {
        GameObject[] selected = Selection.gameObjects;
        
        if (selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Error", "No GameObjects selected!", "OK");
            return;
        }
        
        int updated = 0;
        
        foreach (GameObject obj in selected)
        {
            VoxelObject voxelObj = obj.GetComponent<VoxelObject>();
            
            if (voxelObj == null)
            {
                Debug.LogWarning($"[VoxelDimensionLoader] {obj.name} has no VoxelObject component");
                continue;
            }
            
            if (string.IsNullOrEmpty(voxelObj.assetFileName))
            {
                Debug.LogWarning($"[VoxelDimensionLoader] {obj.name} has no asset file name set");
                continue;
            }
            
            string path = Path.Combine(Application.streamingAssetsPath, voxelObj.assetFileName);
            
            if (!File.Exists(path))
            {
                Debug.LogError($"[VoxelDimensionLoader] File not found: {path}");
                continue;
            }
            
            try
            {
                // Read file header directly
                byte[] header = new byte[16];
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    fs.Read(header, 0, 16);
                }
                
                // Parse dimensions (uint16 at offsets 6, 8, 10)
                int dimX = header[6] | (header[7] << 8);
                int dimY = header[8] | (header[9] << 8);
                int dimZ = header[10] | (header[11] << 8);
                
                Debug.Log($"[VoxelDimensionLoader] {obj.name}: Read from file: {dimX}×{dimY}×{dimZ}");
                
                // Update volumeDims using SerializedObject
                SerializedObject so = new SerializedObject(voxelObj);
                SerializedProperty dimsProp = so.FindProperty("volumeDims");
                
                Debug.Log($"[VoxelDimensionLoader] {obj.name}: Before update: {voxelObj.volumeDims}");
                
                // Use reflection to set int3 field directly
                var field = typeof(VoxelObject).GetField("volumeDims", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                
                if (field != null)
                {
                    field.SetValue(voxelObj, new int3(dimX, dimY, dimZ));
                    EditorUtility.SetDirty(voxelObj);
                }
                
                Debug.Log($"[VoxelDimensionLoader] {obj.name}: After update: {voxelObj.volumeDims}");
                
                EditorUtility.SetDirty(voxelObj);
                updated++;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[VoxelDimensionLoader] Error loading {path}: {e.Message}");
            }
        }
        
        if (updated > 0)
        {
            EditorUtility.DisplayDialog("Success", 
                $"Updated {updated} VoxelObject(s)!\n\nCheck Console for details.",
                "OK");
        }
    }
}
