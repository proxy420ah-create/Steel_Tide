// Steel Tide: First Device — Voxel Ragdoll (Option B re-voxelization, Option A volume)
// VoxelRagdoll.cs
//
// Authoritative skeleton-physics component. Reads the .stasset v2 rig from a
// VoxelObject, builds a Unity rigidbody/joint ragdoll from the bone tree, and
// RE-VOXELIZES the body each frame into a single oversized volume that follows
// the pelvis (so the model can collapse/fall in any direction without clipping).
//
// Pipeline:
//   1. Snapshot the bind-pose voxels + rig from the VoxelObject.
//   2. Build a synthetic pelvis root Rigidbody + one Rigidbody/Collider per bone,
//      connected by ConfigurableJoints following the pelvis-rooted tree.
//      Each bone's Rigidbody.mass comes from the rig's per-bone mass field
//      (derived in Studio from material densities); legacy assets without mass
//      fall back to length * massPerLength.
//   3. Assign every solid bind-pose voxel to its nearest bone, recording the
//      voxel's position in that bone's local space.
//   4. Resize the VoxelObject volume to an oversized cube (sized to the model's
//      fully-splayed reach) and, each LateUpdate, re-stamp every voxel from its
//      bone's current world transform, then upload once.
//
// TESTING: leave 'Simulate Physics' OFF first. The bodies stay kinematic at the
// bind pose, so the re-voxelized model should look identical to the static asset
// (this validates the Option A volume + re-voxelization). Then enable it to drop.
//
// REQUIREMENTS:
//   - The VoxelObject on this GameObject must load a v2 (rigged) .stasset.
//   - Set VoxelObject.registerWithVoxelWorld = FALSE (this is a render-only volume).
//   - Ground: GroundMode.VoxelRaycast lands on VoxelWorld voxels (no collider needed);
//     GroundMode.UnityColliders instead relies on real Unity ground colliders.

using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using SteelTide.Voxels;
// Disambiguate from UnityEngine.SkeletonBone (avatar rigging type).
using SkeletonBone = SteelTide.Voxels.SkeletonBone;

[RequireComponent(typeof(VoxelObject))]
public class VoxelRagdoll : MonoBehaviour
{
    [Header("Simulation")]
    [Tooltip("OFF = bodies kinematic at bind pose (verify re-voxelization). ON = gravity ragdoll.")]
    public bool simulatePhysics = false;

    [Header("Volume Sizing")]
    [Tooltip("Extra voxels of padding added on each side beyond the model's max reach.")]
    public int volumePadding = 4;
    [Tooltip("Hard cap on the oversized cube side (voxels) to bound the GPU buffer.")]
    public int maxVolumeSide = 64;

    [Header("Bodies")]
    [Tooltip("Capsule collider radius as a multiple of voxelSize.")]
    public float boneRadiusVoxels = 1.0f;
    [Tooltip("Fallback mass per voxel-unit of bone length when bone.mass is 0 (legacy assets).")]
    public float massPerLength = 0.5f;
    [Tooltip("Minimum mass for any single body.")]
    public float minMass = 0.5f;
    [Tooltip("Scaling factor applied to bone.mass from the .stasset rig (1 = use Studio values as-is).")]
    public float massScale = 1.0f;
    [Tooltip("Allow ragdoll bones to collide with EACH OTHER. Off (recommended) prevents the overlapping pelvis/spine bodies from exploding apart.")]
    public bool selfCollision = false;
    [Tooltip("PhysX solver iterations per body (higher = stiffer, more stable joints).")]
    public int solverIterations = 16;

    [Header("Joints")]
    [Tooltip("Slack (degrees) added to each joint's metadata angle limits.")]
    public float jointLimitSlack = 5f;
    [Tooltip("Reduce joint drift by projecting bodies back to their constraints.")]
    public bool useProjection = true;

    public enum GroundMode
    {
        VoxelRaycast,   // resolve against VoxelWorld voxels via RaymarchChunk (no Unity collider needed)
        UnityColliders, // rely on real Unity ground colliders only
    }

    [Header("Ground Collision")]
    [Tooltip("VoxelRaycast = land on voxel-only ground using VoxelWorld (no collider). UnityColliders = use real colliders.")]
    public GroundMode groundMode = GroundMode.VoxelRaycast;
    [Tooltip("How far down (world units) each body probes for voxel ground.")]
    public float groundProbeDistance = 50f;
    [Tooltip("Horizontal velocity retained per FixedUpdate while touching ground (0 = full friction, 1 = frictionless).")]
    [Range(0f, 1f)] public float groundFriction = 0.4f;
    [Tooltip("Upward correction speed per unit of ground penetration (soft penalty, no teleport).")]
    public float groundStiffness = 30f;
    [Tooltip("Maximum upward correction speed (m/s) so deep penetration can't rocket bodies.")]
    public float groundMaxPushSpeed = 4f;

    [Header("Debug")]
    public bool drawBoneGizmos = true;

    // ---- runtime state ----
    private VoxelObject _vo;
    private VoxelSkeleton _skel;
    private bool _initialized;

    private float _voxelSize;
    private Vector3 _bindOrigin;     // VoxelObject.transform.position captured at bind
    private int3 _cubeDims;          // oversized render volume dims
    private Vector3 _cubeOriginOffset; // pelvis-to-origin offset (places pelvis at a cell center)

    private Transform _rootBody;     // synthetic pelvis body the volume follows
    private GameObject _boneRoot;    // container for all physics GameObjects
    private readonly Dictionary<int, Transform> _boneTransforms = new Dictionary<int, Transform>();

    // All physics bodies + their contact radius, for voxel-ground resolution.
    private readonly List<(Rigidbody rb, float radius)> _bodies = new List<(Rigidbody, float)>();
    private readonly List<Collider> _colliders = new List<Collider>();
    private VoxelWorld _voxelWorld;

    private struct VoxelRef
    {
        public int boneId;
        public Vector3 localOffset;  // position in bone-local space at bind
        public ushort material;
    }
    private readonly List<VoxelRef> _voxels = new List<VoxelRef>();

    void LateUpdate()
    {
        if (!_initialized)
        {
            TryInitialize();
            if (!_initialized) return;
        }

        RevoxelizeFrame();
    }

    // ============================================================== INIT

    private void TryInitialize()
    {
        _vo = GetComponent<VoxelObject>();
        if (_vo == null || !_vo.HasSkeleton) return;          // wait for VoxelObject.Start
        ushort[] srcData = _vo.GetVoxelData();
        if (srcData == null || srcData.Length == 0) return;

        _skel = _vo.Skeleton;
        _voxelSize = _vo.GetVoxelSize();
        _bindOrigin = _vo.transform.position;
        _voxelWorld = VoxelWorld.Instance;
        int3 srcDims = _vo.GetVolumeDims();

        Vector3 pelvisWorld = BindVoxelToWorld(RootJointPosition());

        BuildBodies(pelvisWorld);
        BuildJoints(pelvisWorld);
        AssignVoxels(srcData, srcDims, pelvisWorld);
        SizeAndAllocateVolume(srcData, srcDims, pelvisWorld);
        ApplyPhysicsState();

        _initialized = true;
        // Log per-bone mass breakdown for verification.
        var sb = new System.Text.StringBuilder();
        sb.Append($"[VoxelRagdoll] Initialized: {_boneTransforms.Count} bone bodies, " +
                  $"{_voxels.Count} mapped voxels, cube {_cubeDims.x}^3, simulate={simulatePhysics}\n");
        sb.Append("  Bone masses:\n");
        foreach (var bone in _skel.bones)
        {
            float m = bone.mass > 0f ? bone.mass * massScale : bone.length * massPerLength;
            m = Mathf.Max(minMass, m);
            sb.Append($"    {bone.name} [{bone.role}] mass={m:F2}\n");
        }
        Debug.Log(sb.ToString().TrimEnd());
    }

    private int RootJoint() => _skel.rootJoint;
    private Vector3Int RootJointPosition()
    {
        var j = _skel.GetJoint(_skel.rootJoint);
        return j != null ? j.position : Vector3Int.zero;
    }

    private Vector3 BindVoxelToWorld(Vector3 voxelCoord)
        => _bindOrigin + (voxelCoord + new Vector3(0.5f, 0.5f, 0.5f)) * _voxelSize;

    private static Vector3 V(Vector3Int v) => new Vector3(v.x, v.y, v.z);

    // -------------------------------------------------------------- bodies

    private void BuildBodies(Vector3 pelvisWorld)
    {
        _boneRoot = new GameObject($"{name}_RagdollBodies");
        _boneRoot.transform.position = Vector3.zero;

        // Synthetic pelvis root body (the bone tree's true root is a joint, not a bone).
        var rootGo = new GameObject("body_pelvis_root");
        rootGo.transform.SetParent(_boneRoot.transform, true);
        rootGo.transform.position = pelvisWorld;
        rootGo.transform.rotation = Quaternion.identity;
        var rootRb = rootGo.AddComponent<Rigidbody>();
        rootRb.mass = Mathf.Max(minMass, 2f);
        rootRb.interpolation = RigidbodyInterpolation.Interpolate;
        rootRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rootRb.solverIterations = solverIterations;
        rootRb.solverVelocityIterations = solverIterations;
        var rootCol = rootGo.AddComponent<BoxCollider>();
        rootCol.size = Vector3.one * _voxelSize * 2f;
        _rootBody = rootGo.transform;

        _bodies.Clear();
        _colliders.Clear();
        _bodies.Add((rootRb, _voxelSize));
        _colliders.Add(rootCol);

        foreach (var bone in _skel.bones)
        {
            Vector3 a = BindVoxelToWorld(V(bone.start));
            Vector3 b = BindVoxelToWorld(V(bone.end));
            Vector3 center = (a + b) * 0.5f;

            var go = new GameObject($"body_{bone.name}");
            go.transform.SetParent(_boneRoot.transform, true);
            go.transform.position = center;
            go.transform.rotation = Quaternion.identity;  // bind = identity; local axes == world axes

            var rb = go.AddComponent<Rigidbody>();
            // Use per-bone mass from the rig (material-density-derived). Fall back to
            // length-based estimate for legacy assets that have no mass field.
            float boneMass = bone.mass > 0f
                ? bone.mass * massScale
                : bone.length * massPerLength;
            rb.mass = Mathf.Max(minMass, boneMass);
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.solverIterations = solverIterations;
            rb.solverVelocityIterations = solverIterations;

            AddBoneCollider(go, a, b);

            _boneTransforms[bone.id] = go.transform;
            _bodies.Add((rb, Mathf.Max(0.001f, boneRadiusVoxels * _voxelSize)));
            _colliders.Add(go.GetComponent<CapsuleCollider>());
        }

        if (!selfCollision)
            IgnoreInternalCollisions();
    }

    /// <summary>
    /// Disable collisions between every pair of ragdoll bodies. Required because
    /// bones sharing a joint (pelvis/spine cluster) overlap at the bind pose and
    /// would otherwise be blasted apart by PhysX penetration recovery.
    /// </summary>
    private void IgnoreInternalCollisions()
    {
        for (int i = 0; i < _colliders.Count; i++)
        for (int j = i + 1; j < _colliders.Count; j++)
        {
            if (_colliders[i] != null && _colliders[j] != null)
                Physics.IgnoreCollision(_colliders[i], _colliders[j], true);
        }
    }

    private void AddBoneCollider(GameObject go, Vector3 a, Vector3 b)
    {
        Vector3 delta = b - a;
        float len = delta.magnitude;
        var cap = go.AddComponent<CapsuleCollider>();
        cap.radius = Mathf.Max(0.001f, boneRadiusVoxels * _voxelSize);

        // Bones are axis-aligned in the bind pose; pick the dominant world axis.
        Vector3 abs = new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
        int dir = (abs.x >= abs.y && abs.x >= abs.z) ? 0 : (abs.y >= abs.z ? 1 : 2);
        cap.direction = dir;                                  // 0=X,1=Y,2=Z (local == world at bind)
        cap.height = Mathf.Max(cap.radius * 2f, len + 2f * cap.radius);
        // Collider centered on the body (body is already at the bone midpoint).
        cap.center = Vector3.zero;
    }

    // -------------------------------------------------------------- joints

    private void BuildJoints(Vector3 pelvisWorld)
    {
        foreach (var bone in _skel.bones)
        {
            Transform childT = _boneTransforms[bone.id];
            var childRb = childT.GetComponent<Rigidbody>();

            SkeletonBone parentBone = _skel.GetParentBone(bone);
            Rigidbody parentRb = parentBone != null
                ? _boneTransforms[parentBone.id].GetComponent<Rigidbody>()
                : _rootBody.GetComponent<Rigidbody>();

            // Pivot joint = the joint nearer the root (bone.start side).
            SkeletonJoint pivot = _skel.GetJoint(bone.parentJoint);
            Vector3 anchorWorld = pivot != null
                ? BindVoxelToWorld(V(pivot.position))
                : BindVoxelToWorld(V(bone.start));

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

    private void ConfigureAngular(ConfigurableJoint cj, SkeletonJoint pivot)
    {
        // Root-attached / unknown pivots are rigid (keep the pelvis assembly together).
        if (pivot == null || pivot.type == JointType.Root)
        {
            cj.angularXMotion = cj.angularYMotion = cj.angularZMotion =
                ConfigurableJointMotion.Locked;
            return;
        }

        if (pivot.type == JointType.Hinge)
        {
            cj.axis = AxisVec(pivot.axis);
            cj.secondaryAxis = PerpendicularTo(cj.axis);
            cj.angularXMotion = ConfigurableJointMotion.Limited;
            cj.angularYMotion = ConfigurableJointMotion.Locked;
            cj.angularZMotion = ConfigurableJointMotion.Locked;
            cj.lowAngularXLimit  = new SoftJointLimit { limit = pivot.minAngle - jointLimitSlack };
            cj.highAngularXLimit = new SoftJointLimit { limit = pivot.maxAngle + jointLimitSlack };
            return;
        }

        // Ball: cone-limited on all three angular axes.
        cj.angularXMotion = ConfigurableJointMotion.Limited;
        cj.angularYMotion = ConfigurableJointMotion.Limited;
        cj.angularZMotion = ConfigurableJointMotion.Limited;
        cj.lowAngularXLimit  = new SoftJointLimit { limit = -(pivot.maxAngleX + jointLimitSlack) };
        cj.highAngularXLimit = new SoftJointLimit { limit =  (pivot.maxAngleX + jointLimitSlack) };
        cj.angularYLimit     = new SoftJointLimit { limit =  (pivot.maxAngleY + jointLimitSlack) };
        cj.angularZLimit     = new SoftJointLimit { limit =  (pivot.maxAngleZ + jointLimitSlack) };
    }

    private static Vector3 AxisVec(string axis)
    {
        if (string.IsNullOrEmpty(axis)) return Vector3.right;
        switch (axis.ToUpperInvariant())
        {
            case "Y": return Vector3.up;
            case "Z": return Vector3.forward;
            default:  return Vector3.right;
        }
    }

    private static Vector3 PerpendicularTo(Vector3 axis)
        => Mathf.Abs(axis.x) > 0.5f ? Vector3.up : Vector3.right;

    // -------------------------------------------------------- voxel mapping

    private void AssignVoxels(ushort[] src, int3 dims, Vector3 pelvisWorld)
    {
        _voxels.Clear();
        for (int z = 0; z < dims.z; z++)
        for (int y = 0; y < dims.y; y++)
        for (int x = 0; x < dims.x; x++)
        {
            ushort val = src[x + y * dims.x + z * dims.x * dims.y];
            if (val == 0) continue;  // Air

            Vector3 worldBind = BindVoxelToWorld(new Vector3(x, y, z));
            int boneId = NearestBone(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
            Transform bt = _boneTransforms[boneId];

            _voxels.Add(new VoxelRef
            {
                boneId = boneId,
                localOffset = bt.InverseTransformPoint(worldBind),
                material = val,
            });
        }
    }

    private int NearestBone(Vector3 voxelGridCenter)
    {
        int best = _skel.bones[0].id;
        float bestDist = float.MaxValue;
        foreach (var bone in _skel.bones)
        {
            float d = DistancePointSegment(voxelGridCenter, V(bone.start) + Half, V(bone.end) + Half);
            if (d < bestDist)
            {
                bestDist = d;
                best = bone.id;
            }
        }
        return best;
    }

    private static readonly Vector3 Half = new Vector3(0.5f, 0.5f, 0.5f);

    private static float DistancePointSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float len2 = ab.sqrMagnitude;
        if (len2 < 1e-6f) return (p - a).magnitude;
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
        return (p - (a + t * ab)).magnitude;
    }

    // ------------------------------------------------------- volume sizing

    private void SizeAndAllocateVolume(ushort[] src, int3 dims, Vector3 pelvisWorld)
    {
        // Max reach (in voxels) of any solid voxel from the pelvis, in the bind pose.
        // The bind T-pose is the fully-splayed configuration, so this bounds every
        // reachable pose (rigid bones can only fold inward).
        float maxReach = 1f;
        for (int z = 0; z < dims.z; z++)
        for (int y = 0; y < dims.y; y++)
        for (int x = 0; x < dims.x; x++)
        {
            if (src[x + y * dims.x + z * dims.x * dims.y] == 0) continue;
            float d = (BindVoxelToWorld(new Vector3(x, y, z)) - pelvisWorld).magnitude / _voxelSize;
            if (d > maxReach) maxReach = d;
        }

        int side = Mathf.CeilToInt(2f * maxReach) + 2 * volumePadding;
        side = Mathf.Clamp(side, 8, maxVolumeSide);
        if ((side & 1) == 1) side++;  // keep even so the pelvis sits near the center

        _cubeDims = new int3(side, side, side);
        // Origin is offset so the pelvis voxel center lands on cell-center (side/2):
        //   pelvisWorld = origin + (side/2 + 0.5) * voxelSize  ->  origin = pelvisWorld - offset
        float off = (side * 0.5f + 0.5f) * _voxelSize;
        _cubeOriginOffset = new Vector3(off, off, off);

        _vo.ReinitializeVolume(_cubeDims);
    }

    private void ApplyPhysicsState()
    {
        foreach (var t in _boneTransforms.Values)
            SetKinematic(t, !simulatePhysics);
        SetKinematic(_rootBody, !simulatePhysics);
    }

    private static void SetKinematic(Transform t, bool kinematic)
    {
        var rb = t.GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic = kinematic;
        rb.useGravity = !kinematic;
    }

    // ============================================================ PHYSICS

    void FixedUpdate()
    {
        if (!_initialized || !simulatePhysics) return;
        if (groundMode != GroundMode.VoxelRaycast || _voxelWorld == null) return;

        ResolveVoxelGround();
    }

    /// <summary>
    /// Per-body voxel-ground response (no Unity collider): probe straight down via
    /// VoxelWorld.RaymarchChunk and, when a body sinks below the voxel surface,
    /// push it out, cancel downward velocity, and apply ground friction.
    /// </summary>
    private void ResolveVoxelGround()
    {
        for (int i = 0; i < _bodies.Count; i++)
        {
            Rigidbody rb = _bodies[i].rb;
            float r = _bodies[i].radius;
            if (rb == null || rb.isKinematic) continue;

            Vector3 pos = rb.position;
            // Probe from just above the body center so we never start inside the ground.
            Vector3 origin = pos + Vector3.up * _voxelSize;
            VoxelHit hit = _voxelWorld.RaymarchChunk(origin, Vector3.down, groundProbeDistance);
            if (!hit.hit) continue;

            float surfaceY = hit.worldPosition.y + _voxelSize * 0.5f; // top face of the hit voxel
            float bottom = pos.y - r;
            if (bottom >= surfaceY) continue;                         // not penetrating

            // Soft penalty correction (NO teleport — teleporting jointed bodies
            // breaks the joint and injects energy). Drive the body upward with a
            // velocity proportional to penetration; gravity balances it at rest.
            float penetration = surfaceY - bottom;
            float push = Mathf.Clamp(penetration * groundStiffness, 0f, groundMaxPushSpeed);

            Vector3 v = rb.linearVelocity;
            if (v.y < push) v.y = push;          // resolve penetration, stop sinking
            v.x *= (1f - groundFriction);        // horizontal ground friction
            v.z *= (1f - groundFriction);
            rb.linearVelocity = v;
        }
    }

    // ============================================================ PER-FRAME

    private void RevoxelizeFrame()
    {
        // 1. Recenter the render cube on the pelvis.
        Vector3 pelvisWorld = _rootBody.position;
        Vector3 newOrigin = pelvisWorld - _cubeOriginOffset;
        _vo.transform.position = newOrigin;

        // 2. Clear + stamp every voxel from its bone's current world transform.
        //    Cell index of a world point = floor((world - origin) / voxelSize).
        _vo.ClearVoxelData();
        float inv = 1f / _voxelSize;
        for (int i = 0; i < _voxels.Count; i++)
        {
            VoxelRef vr = _voxels[i];
            if (!_boneTransforms.TryGetValue(vr.boneId, out var bt)) continue;

            Vector3 world = bt.TransformPoint(vr.localOffset);
            Vector3 rel = (world - newOrigin) * inv;
            int gx = Mathf.FloorToInt(rel.x);
            int gy = Mathf.FloorToInt(rel.y);
            int gz = Mathf.FloorToInt(rel.z);
            _vo.StampVoxelDirect(gx, gy, gz, vr.material);
        }

        // 3. Single GPU upload.
        _vo.ApplyVoxelData();
    }

    // Re-apply kinematic state when toggled in the Inspector at runtime.
    void OnValidate()
    {
        if (Application.isPlaying && _initialized)
            ApplyPhysicsState();
    }

    void OnDestroy()
    {
        if (_boneRoot != null)
            Destroy(_boneRoot);
    }

    void OnDrawGizmosSelected()
    {
        if (!drawBoneGizmos || !_initialized) return;
        Gizmos.color = Color.green;
        foreach (var t in _boneTransforms.Values)
        {
            var col = t.GetComponent<CapsuleCollider>();
            if (col != null)
                Gizmos.DrawWireSphere(t.position, col.radius);
        }
        if (_rootBody != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_rootBody.position, _voxelSize);
        }
    }
}
