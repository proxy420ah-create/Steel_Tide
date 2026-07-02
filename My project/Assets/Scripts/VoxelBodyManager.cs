using System.Collections.Generic;
using UnityEngine;
using SteelTide.Voxels;
using SkeletonBone = SteelTide.Voxels.SkeletonBone;

[RequireComponent(typeof(VoxelActor))]
[DisallowMultipleComponent]
public class VoxelBodyManager : MonoBehaviour
{
    [Header("Bodies")]
    [Tooltip("Capsule collider radius as a multiple of voxelSize.")]
    public float boneRadiusVoxels = 1.0f;
    [Tooltip("Fallback mass per voxel-unit of bone length when bone.mass is 0 (legacy assets).")]
    public float massPerLength = 0.5f;
    [Tooltip("Minimum mass for any single body.")]
    public float minMass = 0.5f;
    [Tooltip("Scaling factor applied to bone.mass from the .stasset rig.")]
    public float massScale = 1.0f;
    [Tooltip("Allow ragdoll bones to collide with EACH OTHER. Off prevents overlapping bodies from exploding apart.")]
    public bool selfCollision = false;
    [Tooltip("PhysX solver iterations per body (higher = stiffer, more stable joints).")]
    public int solverIterations = 16;

    [Header("Joints")]
    [Tooltip("Slack (degrees) added to each joint's metadata angle limits.")]
    public float jointLimitSlack = 5f;
    [Tooltip("Reduce joint drift by projecting bodies back to their constraints.")]
    public bool useProjection = true;
    [Tooltip("Force all joints to be locked. Use this for rigid-plank balance testing.")]
    public bool lockAllJoints = false;

    private VoxelActor _actor;

    void Awake() => _actor = GetComponent<VoxelActor>();

    public void Build()
    {
        if (_actor == null) return;
        Clear();

        Vector3 pelvisWorld = _actor.BindVoxelToWorld(_actor.RootJointPosition());
        BuildBodies(pelvisWorld);
        _actor.CaptureBindPose();
        BuildJoints(pelvisWorld);
    }

    public void Clear()
    {
        if (_actor.BoneRoot != null)
            Destroy(_actor.BoneRoot);
        _actor.BoneRoot = null;
        _actor.RootBody = null;
        _actor.BoneTransforms.Clear();
        _actor.Bodies.Clear();
        _actor.Colliders.Clear();
        _actor.FootBoneIds.Clear();
    }

    private void BuildBodies(Vector3 pelvisWorld)
    {
        _actor.BoneRoot = new GameObject($"{_actor.name}_ActorBodies");
        _actor.BoneRoot.transform.position = Vector3.zero;

        // Add the body gizmo drawer to the generated container.
        var bodyGizmos = _actor.BoneRoot.AddComponent<RagdollBodyGizmos>();
        var actorGizmos = _actor.GetComponent<VoxelActorGizmos>();
        if (actorGizmos != null)
        {
            bodyGizmos.parentActor = _actor;
            bodyGizmos.drawBoneGizmos = actorGizmos.drawBoneGizmos;
            bodyGizmos.drawJointGizmos = actorGizmos.drawJointGizmos;
            bodyGizmos.jointGizmoRadiusVoxels = actorGizmos.jointGizmoRadiusVoxels;
        }

        // Synthetic pelvis root body.
        var rootGo = new GameObject("body_pelvis_root");
        rootGo.transform.SetParent(_actor.BoneRoot.transform, true);
        rootGo.transform.position = pelvisWorld;
        rootGo.transform.rotation = Quaternion.identity;
        var rootRb = rootGo.AddComponent<Rigidbody>();
        rootRb.mass = Mathf.Max(minMass, 2f);
        rootRb.interpolation = RigidbodyInterpolation.Interpolate;
        rootRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rootRb.solverIterations = solverIterations;
        rootRb.solverVelocityIterations = solverIterations;
        var rootCol = rootGo.AddComponent<BoxCollider>();
        rootCol.size = Vector3.one * _actor.VoxelSize * 2f;
        _actor.RootBody = rootGo.transform;

        _actor.Bodies.Clear();
        _actor.Colliders.Clear();
        _actor.FootBoneIds.Clear();
        _actor.Bodies.Add((rootRb, _actor.VoxelSize, -1));
        _actor.Colliders.Add(rootCol);

        CacheFootBoneIds();

        foreach (var bone in _actor.Skeleton.bones)
        {
            Vector3 a = _actor.BindVoxelToWorld(VoxelActor.V(bone.start));
            Vector3 b = _actor.BindVoxelToWorld(VoxelActor.V(bone.end));
            Vector3 center = (a + b) * 0.5f;

            var go = new GameObject($"body_{bone.name}");
            go.transform.SetParent(_actor.BoneRoot.transform, true);
            go.transform.position = center;
            go.transform.rotation = Quaternion.identity;

            var rb = go.AddComponent<Rigidbody>();
            float boneMass = bone.mass > 0f ? bone.mass * massScale : bone.length * massPerLength;
            rb.mass = Mathf.Max(minMass, boneMass);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.solverIterations = solverIterations;
            rb.solverVelocityIterations = solverIterations;

            AddBoneCollider(go, bone, a, b);

            _actor.BoneTransforms[bone.id] = go.transform;
            float contactRadius = bone.hasVoxelBounds
                ? VoxelBoundsContactRadius(bone) * _actor.VoxelSize
                : Mathf.Max(0.001f, boneRadiusVoxels * _actor.VoxelSize);
            _actor.Bodies.Add((rb, contactRadius, bone.id));
            _actor.Colliders.Add(go.GetComponent<Collider>());
        }

        if (!selfCollision)
            IgnoreInternalCollisions();
    }

    public void BuildJoints(Vector3? pelvisWorldOverride = null)
    {
        if (!_actor.buildJoints) return;
        Vector3 pelvisWorld = pelvisWorldOverride ?? _actor.BindVoxelToWorld(_actor.RootJointPosition());
        foreach (var bone in _actor.Skeleton.bones)
        {
            Transform childT = _actor.BoneTransforms[bone.id];
            var childRb = childT.GetComponent<Rigidbody>();

            SkeletonBone parentBone = _actor.Skeleton.GetParentBone(bone);
            Rigidbody parentRb = parentBone != null
                ? _actor.BoneTransforms[parentBone.id].GetComponent<Rigidbody>()
                : _actor.RootBody.GetComponent<Rigidbody>();

            SkeletonJoint pivot = _actor.Skeleton.GetJoint(bone.parentJoint);
            Vector3 anchorWorld;
            if (pivot != null)
            {
                if (pivot.usePositionForAnchor)
                {
                    anchorWorld = _actor.BindVoxelToWorld(VoxelActor.V(pivot.position));
                }
                else if (pivot.hasVoxelBounds)
                {
                    Vector3 boundsCenter = (VoxelActor.V(pivot.voxelBoundsMin) + VoxelActor.V(pivot.voxelBoundsMax)) * 0.5f;
                    anchorWorld = _actor.BindVoxelToWorld(boundsCenter);
                }
                else
                {
                    anchorWorld = _actor.BindVoxelToWorld(VoxelActor.V(pivot.position));
                }
            }
            else
            {
                anchorWorld = _actor.BindVoxelToWorld(VoxelActor.V(bone.start));
            }

            var cj = childT.gameObject.AddComponent<ConfigurableJoint>();
            cj.connectedBody = parentRb;
            cj.autoConfigureConnectedAnchor = false;
            cj.anchor = childT.InverseTransformPoint(anchorWorld);
            cj.connectedAnchor = parentRb.transform.InverseTransformPoint(anchorWorld);
            cj.xMotion = cj.yMotion = cj.zMotion = ConfigurableJointMotion.Locked;
            if (useProjection)
            {
                cj.projectionMode = JointProjectionMode.PositionAndRotation;
                cj.projectionDistance = 0.01f;
                cj.projectionAngle = 1f;
            }
            ConfigureAngular(cj, pivot);
        }
    }

    private void IgnoreInternalCollisions()
    {
        for (int i = 0; i < _actor.Colliders.Count; i++)
        for (int j = i + 1; j < _actor.Colliders.Count; j++)
        {
            if (_actor.Colliders[i] != null && _actor.Colliders[j] != null)
                Physics.IgnoreCollision(_actor.Colliders[i], _actor.Colliders[j], true);
        }
    }

    public void CacheFootBoneIds()
    {
        _actor.FootBoneIds.Clear();
        if (_actor.Skeleton == null || _actor.Skeleton.bones == null) return;
        foreach (var bone in _actor.Skeleton.bones)
        {
            if (bone.role == null) continue;
            if (!bone.role.Equals("foot", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrEmpty(bone.side) ||
                bone.side.Equals("L", System.StringComparison.OrdinalIgnoreCase) ||
                bone.side.Equals("R", System.StringComparison.OrdinalIgnoreCase) ||
                bone.side.Equals("left", System.StringComparison.OrdinalIgnoreCase) ||
                bone.side.Equals("right", System.StringComparison.OrdinalIgnoreCase))
            {
                _actor.FootBoneIds.Add(bone.id);
            }
        }
    }

    private float VoxelBoundsContactRadius(SkeletonBone bone)
    {
        Vector3 bMin = VoxelActor.V(bone.voxelBoundsMin);
        Vector3 bMax = VoxelActor.V(bone.voxelBoundsMax);
        Vector3 size = (bMax - bMin + Vector3.one) * _actor.VoxelSize;
        return Mathf.Min(size.x, size.z) * 0.5f;
    }

    private void AddBoneCollider(GameObject go, SkeletonBone bone, Vector3 a, Vector3 b)
    {
        if (bone.hasVoxelBounds)
        {
            AddVoxelBoundsCollider(go, bone);
            return;
        }

        Vector3 delta = b - a;
        float len = delta.magnitude;
        var cap = go.AddComponent<CapsuleCollider>();
        cap.radius = Mathf.Max(0.001f, boneRadiusVoxels * _actor.VoxelSize);
        Vector3 abs = new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
        int dir = (abs.x >= abs.y && abs.x >= abs.z) ? 0 : (abs.y >= abs.z ? 1 : 2);
        cap.direction = dir;
        cap.height = Mathf.Max(cap.radius * 2f, len + 2f * cap.radius);
        cap.center = Vector3.zero;
    }

    private void AddVoxelBoundsCollider(GameObject go, SkeletonBone bone)
    {
        Vector3 bMin = VoxelActor.V(bone.voxelBoundsMin);
        Vector3 bMax = VoxelActor.V(bone.voxelBoundsMax);
        Vector3 sizeVox = bMax - bMin + Vector3.one;
        Vector3 size = sizeVox * _actor.VoxelSize;
        Vector3 centerVox = (bMin + bMax) * 0.5f;
        Vector3 centerWorld = _actor.BindVoxelToWorld(centerVox);
        Vector3 bodyPos = go.transform.position;
        Vector3 center = centerWorld - bodyPos;

        string role = bone.role;
        switch (role)
        {
            case "foot":
            case "hand":
            case "pelvis":
            {
                var box = go.AddComponent<BoxCollider>();
                box.size = size;
                box.center = center;
                break;
            }
            case "head":
            case "neck":
            {
                var sphere = go.AddComponent<SphereCollider>();
                sphere.radius = size.magnitude * 0.5f;
                sphere.center = center;
                break;
            }
            default:
            {
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = Mathf.Min(size.x, size.z) * 0.5f;
                cap.height = size.y;
                cap.center = center;
                Vector3 delta = VoxelActor.V(bone.end) - VoxelActor.V(bone.start);
                Vector3 absD = new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
                cap.direction = (absD.x >= absD.y && absD.x >= absD.z) ? 0 : (absD.y >= absD.z ? 1 : 2);
                break;
            }
        }
    }

    private void ConfigureAngular(ConfigurableJoint cj, SkeletonJoint pivot)
    {
        if (lockAllJoints)
        {
            cj.angularXMotion = cj.angularYMotion = cj.angularZMotion = ConfigurableJointMotion.Locked;
            return;
        }

        if (pivot == null || pivot.type == JointType.Root)
        {
            cj.angularXMotion = cj.angularYMotion = cj.angularZMotion = ConfigurableJointMotion.Locked;
            return;
        }

        if (pivot.type == JointType.Hinge)
        {
            cj.axis = AxisVec(pivot.axis);
            cj.secondaryAxis = PerpendicularTo(cj.axis);
            cj.angularXMotion = ConfigurableJointMotion.Limited;
            cj.angularYMotion = ConfigurableJointMotion.Locked;
            cj.angularZMotion = ConfigurableJointMotion.Locked;
            cj.lowAngularXLimit = new SoftJointLimit { limit = pivot.minAngle - jointLimitSlack };
            cj.highAngularXLimit = new SoftJointLimit { limit = pivot.maxAngle + jointLimitSlack };
            return;
        }

        cj.angularXMotion = ConfigurableJointMotion.Limited;
        cj.angularYMotion = ConfigurableJointMotion.Limited;
        cj.angularZMotion = ConfigurableJointMotion.Limited;
        cj.lowAngularXLimit = new SoftJointLimit { limit = -(pivot.maxAngleX + jointLimitSlack) };
        cj.highAngularXLimit = new SoftJointLimit { limit = (pivot.maxAngleX + jointLimitSlack) };
        cj.angularYLimit = new SoftJointLimit { limit = (pivot.maxAngleY + jointLimitSlack) };
        cj.angularZLimit = new SoftJointLimit { limit = (pivot.maxAngleZ + jointLimitSlack) };
    }

    private static Vector3 AxisVec(string axis)
    {
        if (string.IsNullOrEmpty(axis)) return Vector3.right;
        switch (axis.ToUpperInvariant())
        {
            case "Y": return Vector3.up;
            case "Z": return Vector3.forward;
            default: return Vector3.right;
        }
    }

    private static Vector3 PerpendicularTo(Vector3 axis)
        => Mathf.Abs(axis.x) > 0.5f ? Vector3.up : Vector3.right;
}
