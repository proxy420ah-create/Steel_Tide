// Steel Tide: First Device — Voxel Rig Debug (U1 verifier)
// VoxelRigDebug.cs
//
// PURPOSE (stage U1): verify that a .stasset v2 rig parses correctly in Unity and
// that bone/joint positions line up with the rendered voxel volume — BEFORE any
// physics is attached. Purely additive: add this component next to a VoxelObject,
// enter Play mode, and inspect the Scene-view gizmos + Console hierarchy dump.
//
// It draws (in world space, using the same voxel->world mapping as the renderer):
//   - joints  : small spheres (red = root/pelvis, yellow = others)
//   - bones   : lines from start->end (cyan)
//   - parent links : thin magenta lines from a bone's pivot to its parent bone
//
// Voxel->world mapping (matches VoxelProxyRaymarch.shader):
//   worldCenter = volumeOffset + (voxelCoord + 0.5) * voxelSize
//
// Next stages (not in this file): U2 = build Rigidbody/Collider/ConfigurableJoint
// per bone from this metadata; U3 = re-voxelize the volume each frame (Option B).

using UnityEngine;
using SteelTide.Voxels;

[RequireComponent(typeof(VoxelObject))]
public class VoxelRigDebug : MonoBehaviour
{
    [Header("Gizmos")]
    public bool drawBones = true;
    public bool drawJoints = true;
    public bool drawParentLinks = true;
    [Tooltip("Joint sphere radius as a multiple of voxelSize.")]
    public float jointRadiusVoxels = 0.6f;

    [Header("Debug")]
    [Tooltip("Log the parsed hierarchy once when it first becomes available.")]
    public bool logHierarchyOnce = true;

    private VoxelObject _voxelObject;
    private bool _logged;

    private VoxelObject VoxelObj => _voxelObject != null
        ? _voxelObject
        : (_voxelObject = GetComponent<VoxelObject>());

    void Update()
    {
        if (_logged || !logHierarchyOnce) return;
        var vo = VoxelObj;
        if (vo == null || !vo.HasSkeleton) return;

        DumpHierarchy(vo.Skeleton);
        _logged = true;
    }

    private void DumpHierarchy(VoxelSkeleton skel)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"[VoxelRigDebug] Skeleton on '{name}': {skel.bones.Count} bones, " +
                      $"{skel.joints.Count} joints, root joint = {skel.rootJoint}");
        foreach (var bone in skel.bones)
        {
            var parent = skel.GetParentBone(bone);
            string parentName = parent != null ? parent.name : "(root)";
            string roleSide = string.IsNullOrEmpty(bone.side)
                ? bone.role
                : $"{bone.role} {bone.side}";
            sb.AppendLine($"  bone[{bone.id}] {bone.name,-16} [{roleSide}] " +
                          $"mass={bone.mass:F2} " +
                          $"pivot={bone.parentJoint,2} child={bone.childJoint,2} " +
                          $"parent={parentName}");
        }
        Debug.Log(sb.ToString());
    }

    private Vector3 VoxelToWorld(Vector3 voxelCoord)
    {
        var vo = VoxelObj;
        float s = vo.GetVoxelSize();
        Vector3 origin = vo.GetVolumeOffset();
        return origin + (voxelCoord + new Vector3(0.5f, 0.5f, 0.5f)) * s;
    }

    private static Vector3 ToVec(Vector3Int v) => new Vector3(v.x, v.y, v.z);

    void OnDrawGizmos()
    {
        var vo = VoxelObj;
        if (vo == null || !Application.isPlaying || !vo.HasSkeleton) return;

        var skel = vo.Skeleton;
        float s = vo.GetVoxelSize();

        if (drawBones)
        {
            Gizmos.color = Color.cyan;
            foreach (var bone in skel.bones)
            {
                Vector3 a = VoxelToWorld(ToVec(bone.start));
                Vector3 b = VoxelToWorld(ToVec(bone.end));
                Gizmos.DrawLine(a, b);
            }
        }

        if (drawParentLinks)
        {
            Gizmos.color = new Color(1f, 0f, 1f, 0.6f);
            foreach (var bone in skel.bones)
            {
                var parent = skel.GetParentBone(bone);
                if (parent == null) continue;
                Vector3 childPivot = VoxelToWorld(ToVec(bone.start));
                Vector3 parentMid = VoxelToWorld((ToVec(parent.start) + ToVec(parent.end)) * 0.5f);
                Gizmos.DrawLine(childPivot, parentMid);
            }
        }

        if (drawJoints)
        {
            float r = Mathf.Max(0.01f, jointRadiusVoxels * s);
            foreach (var joint in skel.joints)
            {
                Gizmos.color = joint.type == JointType.Root ? Color.red
                             : joint.type == JointType.Hinge ? new Color(1f, 0.55f, 0f)
                             : Color.yellow;
                Gizmos.DrawWireSphere(VoxelToWorld(ToVec(joint.position)), r);
            }
        }
    }
}
