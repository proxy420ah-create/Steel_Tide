using UnityEngine;
using SteelTide.Voxels;

[RequireComponent(typeof(VoxelActor))]
[DisallowMultipleComponent]
public class VoxelActorGizmos : MonoBehaviour
{
    [Header("Debug")]
    public bool drawBoneGizmos = true;
    [Tooltip("Draw colored wire spheres at every joint position.")]
    public bool drawJointGizmos = true;
    [Tooltip("Joint gizmo radius as a multiple of voxelSize.")]
    public float jointGizmoRadiusVoxels = 0.8f;
    [Tooltip("Draw the bone/joint/balance gizmos even when this object is not selected.")]
    public bool drawGizmosAlways = false;
    [Tooltip("World-space offset applied to the bone/joint gizmos.")]
    public Vector3 gizmoOffset = Vector3.zero;

    private VoxelActor _actor;

    void Awake() => _actor = GetComponent<VoxelActor>();

    void OnDrawGizmosSelected()
    {
        DrawAllGizmos();
    }

    void OnDrawGizmos()
    {
        if (!drawGizmosAlways) return;
        DrawAllGizmos();
    }

    private void DrawAllGizmos()
    {
        if (_actor == null || !_actor.IsInitialized) return;

        var balanceComp = _actor.GetComponent<VoxelBalance>();
        if (balanceComp != null && balanceComp.balanceSettings != null && balanceComp.balanceSettings.drawBalanceGizmos && _actor.Balance != null)
            _actor.Balance.DrawGizmos();
    }

    public void DrawBodyGizmos(bool drawBones, bool drawJoints, float jointRadius, Vector3 offset)
    {
        if (_actor == null || !_actor.IsInitialized) return;

        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.Translate(offset);

        if (drawBones)
        {
            foreach (var t in _actor.BoneTransforms.Values)
            {
                var cap = t.GetComponent<CapsuleCollider>();
                if (cap != null)
                {
                    Gizmos.color = Color.green;
                    DrawWireCapsule(t, cap);
                    continue;
                }
                var sphere = t.GetComponent<SphereCollider>();
                if (sphere != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawWireSphere(t.TransformPoint(sphere.center), sphere.radius);
                    continue;
                }
                var box = t.GetComponent<BoxCollider>();
                if (box != null)
                {
                    Gizmos.color = Color.cyan;
                    Matrix4x4 boxMatrix = Gizmos.matrix;
                    Gizmos.matrix = boxMatrix * t.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                    Gizmos.matrix = boxMatrix;
                    continue;
                }
            }
            if (_actor.RootBody != null)
            {
                Gizmos.color = Color.red;
                var rootCol = _actor.RootBody.GetComponent<BoxCollider>();
                if (rootCol != null)
                {
                    Matrix4x4 boxMatrix = Gizmos.matrix;
                    Gizmos.matrix = boxMatrix * _actor.RootBody.localToWorldMatrix;
                    Gizmos.DrawWireCube(rootCol.center, rootCol.size);
                    Gizmos.matrix = boxMatrix;
                }
                else
                {
                    Gizmos.DrawWireSphere(_actor.RootBody.position, _actor.VoxelSize);
                }
            }
        }

        if (drawJoints && _actor.Skeleton != null)
        {
            foreach (var joint in _actor.Skeleton.joints)
            {
                Vector3 pos = GetJointWorldPosition(joint);
                Gizmos.color = joint.type == JointType.Root ? Color.red
                             : joint.type == JointType.Ball ? Color.yellow
                             : joint.type == JointType.Hinge ? new Color(1f, 0.55f, 0f)
                             : Color.white;
                float r;
                if (joint.hasVoxelBounds)
                {
                    Vector3 bMin = VoxelActor.V(joint.voxelBoundsMin);
                    Vector3 bMax = VoxelActor.V(joint.voxelBoundsMax);
                    Vector3 sizeVox = bMax - bMin + Vector3.one;
                    r = Mathf.Max(sizeVox.x, sizeVox.y, sizeVox.z) * 0.5f * _actor.VoxelSize;
                }
                else
                {
                    r = Mathf.Max(0.01f, jointRadius * _actor.VoxelSize);
                }
                Gizmos.DrawWireSphere(pos, r);
            }
        }

        Gizmos.matrix = oldMatrix;
    }

    private Vector3 GetJointWorldPosition(SkeletonJoint joint)
    {
        if (_actor == null || _actor.Skeleton == null || _actor.BoneTransforms == null)
            return _actor.BindVoxelToWorld(VoxelActor.V(joint.position));

        foreach (var bone in _actor.Skeleton.bones)
        {
            if (bone.parentJoint != joint.id) continue;
            if (!_actor.BoneTransforms.TryGetValue(bone.id, out var boneT)) continue;
            var cj = boneT.GetComponent<ConfigurableJoint>();
            if (cj == null) continue;
            return boneT.TransformPoint(cj.anchor);
        }

        if (joint.id == _actor.Skeleton.rootJoint && _actor.RootBody != null)
            return _actor.RootBody.TransformPoint(Vector3.zero);

        if (joint.hasVoxelBounds)
        {
            Vector3 boundsCenter = (VoxelActor.V(joint.voxelBoundsMin) + VoxelActor.V(joint.voxelBoundsMax)) * 0.5f;
            return _actor.BindVoxelToWorld(boundsCenter);
        }
        return _actor.BindVoxelToWorld(VoxelActor.V(joint.position));
    }

    private static void DrawWireCapsule(Transform t, CapsuleCollider cap)
    {
        Vector3 center = t.TransformPoint(cap.center);
        float r = cap.radius;
        float halfHeight = Mathf.Max(0f, (cap.height * 0.5f) - r);
        Vector3 localAxis = cap.direction == 0 ? Vector3.right
                         : cap.direction == 1 ? Vector3.up
                         : Vector3.forward;
        Vector3 worldAxis = t.TransformDirection(localAxis).normalized;
        Vector3 top = center + worldAxis * halfHeight;
        Vector3 bot = center - worldAxis * halfHeight;

        Gizmos.DrawWireSphere(top, r);
        Gizmos.DrawWireSphere(bot, r);

        Vector3 perp1, perp2;
        if (Mathf.Abs(worldAxis.x) < 0.9f) perp1 = Vector3.right;
        else perp1 = Vector3.up;
        perp1 = (perp1 - Vector3.Project(perp1, worldAxis)).normalized * r;
        perp2 = Vector3.Cross(worldAxis, perp1).normalized * r;

        Gizmos.DrawLine(top + perp1, bot + perp1);
        Gizmos.DrawLine(top - perp1, bot - perp1);
        Gizmos.DrawLine(top + perp2, bot + perp2);
        Gizmos.DrawLine(top - perp2, bot - perp2);
    }
}
