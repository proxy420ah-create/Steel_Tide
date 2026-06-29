using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VoxelModelAligner))]
public class VoxelModelAlignerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        VoxelModelAligner aligner = (VoxelModelAligner)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Align Model Now", GUILayout.Height(30)))
        {
            aligner.AlignModel();
            EditorUtility.SetDirty(aligner.gameObject);
        }
        
        EditorGUILayout.Space();
        
        // Show saved alignment info
        if (aligner.savedAlignment != Vector3.zero)
        {
            EditorGUILayout.LabelField("Saved Alignment:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Position: {aligner.savedAlignment}");
            
            if (GUILayout.Button("Apply Saved Alignment", GUILayout.Height(25)))
            {
                aligner.ApplySavedAlignment();
                EditorUtility.SetDirty(aligner.gameObject);
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "WORKFLOW:\n" +
            "1. Press Play → Auto-aligns with correct dimensions\n" +
            "2. Stop Play → Saved Alignment is preserved\n" +
            "3. Click 'Apply Saved Alignment' → Applies to Edit mode\n\n" +
            "OR use 'Align Model Now' if dimensions are already loaded.",
            MessageType.Info
        );
    }
}
