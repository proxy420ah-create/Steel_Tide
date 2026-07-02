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
using UnityEngine.InputSystem;
using SteelTide.Voxels;
using SteelTide.ActorPhysics;
// Disambiguate from UnityEngine.SkeletonBone (avatar rigging type).
using SkeletonBone = SteelTide.Voxels.SkeletonBone;

[RequireComponent(typeof(VoxelObject))]
public class VoxelRagdoll : MonoBehaviour
{
    [Header("Simulation")]
    [Tooltip("OFF = bodies kinematic at bind pose (verify re-voxelization). ON = gravity ragdoll.")]
    public bool simulatePhysics = false;
    [Tooltip("Enable the balance controller subsystem.")]
    public bool enableBalance = true;
    [Tooltip("Enable voxel/ground collision resolution.")]
    public bool enableGroundCollision = true;
    [Tooltip("Enable re-voxelization of the visual volume each frame.")]
    public bool enableRevoxelization = true;
    [Tooltip("Build joints. OFF = bones are independent bodies (use this to isolate joint jitter). Requires re-init to take effect.")]
    public bool buildJoints = true;

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
    [Tooltip("Force all joints to be locked. Use this for rigid-plank balance testing.")]
    public bool lockAllJoints = false;

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
    [Tooltip("Fraction of horizontal velocity lost per second while touching ground (0 = frictionless, 20 = full stop).")]
    [Range(0f, 50f)] public float groundFriction = 20f;
    [Tooltip("Ground penetration correction aggressiveness. 0.1 = soft, settles over several steps. 1 = close the full penetration in one physics step." +
             " Now time-step independent: it divides by Time.fixedDeltaTime so results match at any Time.timeScale." +
             " Keep this low (0.05–0.3) to avoid resting micro-bounce.")]
    [Range(0.01f, 2f)] public float groundStiffness = 0.1f;
    [Tooltip("Maximum upward correction speed (m/s) so deep penetration can't rocket bodies.")]
    public float groundMaxPushSpeed = 4f;
    [Tooltip("Bounciness of the voxel ground. 0 = perfectly inelastic (impact absorbed). 1 = fully elastic bounce." +
             " Default 0 gives solid, non-bouncy ground and opens the door for material-specific absorption.")]
    [Range(0f, 1f)] public float groundRestitution = 0f;

    [Header("Debug")]
    public bool drawBoneGizmos = true;
    [Tooltip("Draw colored wire spheres at every joint position. Root=red, Ball=yellow, Hinge=orange.")]
    public bool drawJointGizmos = true;
    [Tooltip("Joint gizmo radius as a multiple of voxelSize.")]
    public float jointGizmoRadiusVoxels = 0.8f;
    [Tooltip("Draw the bone/joint/balance gizmos even when this object is not selected.")]
    public bool drawGizmosAlways = false;
    [Tooltip("World-space offset applied to the bone/joint gizmos so you can silhouette them against the real colliders.")]
    public Vector3 gizmoOffset = Vector3.zero;
    [Tooltip("Log how many voxels are near cell boundaries each frame (diagnostic for scatter).")]
    public bool debugQuantization = false;

    [Header("Reset")]
    [Tooltip("Key to press in Play mode to reset the ragdoll to its bind pose. Avoids R (Unity Scale tool) and Space (player jump) so it does not conflict with existing controls.")]
    public Key resetKey = Key.F5;
    [Header("Balance")]
    [Tooltip("Active balance controller settings. Strength 0 = pure ragdoll. Higher values fight harder to stay upright.")]
    public BalanceSettings balanceSettings = new BalanceSettings();

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

    // All physics bodies + their contact radius and source bone id, for voxel-ground resolution.
    private readonly List<(Rigidbody rb, float radius, int boneId)> _bodies = new List<(Rigidbody, float, int)>();
    private readonly List<Collider> _colliders = new List<Collider>();
    private readonly HashSet<int> _footBoneIds = new HashSet<int>();
    private VoxelWorld _voxelWorld;

    private struct VoxelRef
    {
        public int boneId;
        public Vector3 localOffset;  // position in bone-local space at bind
        public ushort material;
    }
    private readonly List<VoxelRef> _voxels = new List<VoxelRef>();
    private int _prevScatterCount = -1;

    // Bind pose snapshot for ResetRagdoll().
    private Vector3 _bindRootBodyPosition;
    private Quaternion _bindRootBodyRotation;
    private readonly Dictionary<int, Pose> _bindBonePoses = new Dictionary<int, Pose>();
    private BalanceController _balance;

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current[resetKey].wasPressedThisFrame)
            ResetRagdoll();
    }

    void LateUpdate()
    {
        if (!_initialized)
        {
            TryInitialize();
            if (!_initialized) return;
        }

        if (enableRevoxelization)
            RevoxelizeFrame();
    }

    private void CaptureBindPose()
    {
        _bindRootBodyPosition = _rootBody.position;
        _bindRootBodyRotation = _rootBody.rotation;
        _bindBonePoses.Clear();
        foreach (var kvp in _boneTransforms)
            _bindBonePoses[kvp.Key] = new Pose(kvp.Value.position, kvp.Value.rotation);
    }

    [ContextMenu("Reset Ragdoll")]
    public void ResetRagdoll()
    {
        if (!_initialized) return;

        ResetBodyToBind(_rootBody.GetComponent<Rigidbody>(), _bindRootBodyPosition, _bindRootBodyRotation);
        foreach (var kvp in _boneTransforms)
        {
            if (!_bindBonePoses.TryGetValue(kvp.Key, out var bindPose)) continue;
            ResetBodyToBind(kvp.Value.GetComponent<Rigidbody>(), bindPose.position, bindPose.rotation);
        }

        // Re-stamp voxels immediately so the render cube snaps back this frame.
        RevoxelizeFrame();
    }

    private static void ResetBodyToBind(Rigidbody rb, Vector3 position, Quaternion rotation)
    {
        if (rb == null) return;
        rb.position = position;
        rb.rotation = rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
    }

    // ============================================================== INIT

    private void TryInitialize()
    {
        _vo = GetComponent<VoxelObject>();
        if (_vo == null || !_vo.HasSkeleton) return;          // wait for VoxelObject.Start

        // Initialize balance settings if the Inspector left them null (rare with [Serializable]).
        if (balanceSettings == null)
            balanceSettings = new BalanceSettings();

        ushort[] srcData = _vo.GetVoxelData();
        if (srcData == null || srcData.Length == 0) return;

        _skel = _vo.Skeleton;
        _voxelSize = _vo.GetVoxelSize();
        _bindOrigin = _vo.transform.position;
        _voxelWorld = VoxelWorld.Instance;
        int3 srcDims = _vo.GetVolumeDims();

        Vector3 pelvisWorld = BindVoxelToWorld(RootJointPosition());

        BuildBodies(pelvisWorld);
        CaptureBindPose();
        if (buildJoints) BuildJoints(pelvisWorld);
        AssignVoxels(srcData, srcDims, pelvisWorld);
        SizeAndAllocateVolume(srcData, srcDims, pelvisWorld);
        ApplyPhysicsState();

        _balance = new BalanceController(_skel, _boneTransforms, _rootBody, balanceSettings);

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
            string bounds = bone.hasVoxelBounds
                ? $" bounds=[{bone.voxelBoundsMin}-{bone.voxelBoundsMax}]"
                : "";
            sb.Append($"    {bone.name} [{bone.role}] mass={m:F2}{bounds}\n");
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
        // Keep the physics container at the scene root so the VoxelObject transform can
        // be recentered for the dynamic volume without teleporting the ragdoll bodies.

        // Add the body gizmo drawer to the generated container so selecting the real
        // physics bodies shows the authored/debug bone shapes alongside the colliders.
        var bodyGizmos = _boneRoot.AddComponent<RagdollBodyGizmos>();
        bodyGizmos.parentRagdoll = this;
        bodyGizmos.drawBoneGizmos = drawBoneGizmos;
        bodyGizmos.drawJointGizmos = drawJointGizmos;
        bodyGizmos.jointGizmoRadiusVoxels = jointGizmoRadiusVoxels;

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
        _footBoneIds.Clear();
        _bodies.Add((rootRb, _voxelSize, -1)); // synthetic pelvis has no bone id
        _colliders.Add(rootCol);

        CacheFootBoneIds();

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

            AddBoneCollider(go, bone, a, b);

            _boneTransforms[bone.id] = go.transform;
            float contactRadius = bone.hasVoxelBounds
                ? VoxelBoundsContactRadius(bone) * _voxelSize
                : Mathf.Max(0.001f, boneRadiusVoxels * _voxelSize);
            _bodies.Add((rb, contactRadius, bone.id));
            _colliders.Add(go.GetComponent<Collider>());
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

    private float VoxelBoundsContactRadius(SkeletonBone bone)
    {
        Vector3 bMin = V(bone.voxelBoundsMin);
        Vector3 bMax = V(bone.voxelBoundsMax);
        Vector3 size = (bMax - bMin + Vector3.one) * _voxelSize;
        return Mathf.Min(size.x, size.z) * 0.5f;
    }

    private void AddBoneCollider(GameObject go, SkeletonBone bone, Vector3 a, Vector3 b)
    {
        // If the bone has voxel bounds, derive collider shape + size from them.
        if (bone.hasVoxelBounds)
        {
            AddVoxelBoundsCollider(go, bone);
            return;
        }

        // Legacy fallback: hardcoded capsule as before.
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

    /// <summary>
    /// Derive collider type and size from the bone's voxel cluster bounding box.
    /// Shape is chosen by bone role; size always comes from the actual voxel geometry.
    /// </summary>
    private void AddVoxelBoundsCollider(GameObject go, SkeletonBone bone)
    {
        Vector3 bMin = V(bone.voxelBoundsMin);
        Vector3 bMax = V(bone.voxelBoundsMax);
        // +1 because bounds are inclusive voxel indices (e.g. 0..2 = 3 voxels).
        Vector3 sizeVox = bMax - bMin + Vector3.one;
        Vector3 size = sizeVox * _voxelSize;
        // Center of the voxel cluster in bind-world space.
        Vector3 centerVox = (bMin + bMax) * 0.5f;
        Vector3 centerWorld = BindVoxelToWorld(centerVox);
        // Body is at bone midpoint; collider center is relative to that.
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
                // Limb bones: capsule sized from voxel bounds.
                var cap = go.AddComponent<CapsuleCollider>();
                cap.radius = Mathf.Min(size.x, size.z) * 0.5f;
                cap.height = size.y;
                cap.center = center;
                // Pick dominant axis from bone direction (bind pose is axis-aligned).
                Vector3 delta = V(bone.end) - V(bone.start);
                Vector3 absD = new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z));
                cap.direction = (absD.x >= absD.y && absD.x >= absD.z) ? 0 : (absD.y >= absD.z ? 1 : 2);
                break;
            }
        }
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
            Vector3 anchorWorld;
            if (pivot != null)
            {
                // Check if this joint explicitly wants to use its position for anchoring
                // (useful for massive joints where position != bounds center)
                if (pivot.usePositionForAnchor)
                {
                    Debug.Log($"[VoxelRagdoll] Joint '{pivot.name}' using explicit position anchor: {pivot.position}");
                    anchorWorld = BindVoxelToWorld(V(pivot.position));
                }
                // Use voxel bounds center when available so multi-voxel joints
                // anchor at their true center, not a corner voxel.
                else if (pivot.hasVoxelBounds)
                {
                    Vector3 boundsCenter = (V(pivot.voxelBoundsMin) + V(pivot.voxelBoundsMax)) * 0.5f;
                    Debug.Log($"[VoxelRagdoll] Joint '{pivot.name}' using bounds center anchor: {boundsCenter}");
                    anchorWorld = BindVoxelToWorld(boundsCenter);
                }
                else
                {
                    Debug.Log($"[VoxelRagdoll] Joint '{pivot.name}' using position anchor (no bounds): {pivot.position}");
                    anchorWorld = BindVoxelToWorld(V(pivot.position));
                }
            }
            else
            {
                anchorWorld = BindVoxelToWorld(V(bone.start));
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

            // Diagnostic log for ankle joints so the user can verify lock state.
            if (bone.role != null && bone.role.Equals("foot", System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"[VoxelRagdoll] Ankle '{bone.name}' lock state: " +
                          $"X={cj.angularXMotion} Y={cj.angularYMotion} Z={cj.angularZMotion} " +
                          $"(lockAllJoints={lockAllJoints})");
            }
        }
    }

    private void ConfigureAngular(ConfigurableJoint cj, SkeletonJoint pivot)
    {
        // Rigid-plank test override: lock every joint regardless of asset type.
        if (lockAllJoints)
        {
            cj.angularXMotion = cj.angularYMotion = cj.angularZMotion =
                ConfigurableJointMotion.Locked;
            return;
        }

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
        // Max reach (in voxels) of any solid voxel from the pelvis. The bind T-pose
        // is the fully-splayed configuration, so this bounds every reachable pose.
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

    private void CacheFootBoneIds()
    {
        if (_skel == null || _skel.bones == null) return;
        foreach (var bone in _skel.bones)
        {
            if (bone.role == null) continue;
            if (!bone.role.Equals("foot", System.StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrEmpty(bone.side) ||
                bone.side.Equals("L", System.StringComparison.OrdinalIgnoreCase) ||
                bone.side.Equals("R", System.StringComparison.OrdinalIgnoreCase) ||
                bone.side.Equals("left", System.StringComparison.OrdinalIgnoreCase) ||
                bone.side.Equals("right", System.StringComparison.OrdinalIgnoreCase))
            {
                _footBoneIds.Add(bone.id);
            }
        }
    }

    void FixedUpdate()
    {
        if (!_initialized || !simulatePhysics) return;

        if (enableGroundCollision && groundMode == GroundMode.VoxelRaycast && _voxelWorld != null)
            ResolveVoxelGround();

        if (enableBalance && balanceSettings != null && balanceSettings.balanceStrength > 0f && _balance != null)
            _balance.UpdateBalance();
    }

    /// <summary>
    /// Per-body voxel-ground response (no Unity collider): probe straight down via
    /// VoxelWorld.RaymarchChunk and, when a body sinks below the voxel surface,
    /// push it out, cancel downward velocity, and apply ground friction.
    /// </summary>
    private void ResolveVoxelGround()
    {
        int feetGrounded = 0;
        int feetTotal = 0;

        for (int i = 0; i < _bodies.Count; i++)
        {
            Rigidbody rb = _bodies[i].rb;
            float r = _bodies[i].radius;
            int boneId = _bodies[i].boneId;
            bool isFoot = _footBoneIds.Contains(boneId);
            if (isFoot) feetTotal++;
            if (rb == null || rb.isKinematic) continue;

            Vector3 pos = rb.position;
            // Probe from just above the body center so we never start inside the ground.
            Vector3 origin = pos + Vector3.up * _voxelSize;
            VoxelHit hit = _voxelWorld.RaymarchChunk(origin, Vector3.down, groundProbeDistance);
            if (!hit.hit) continue;

            float surfaceY = hit.worldPosition.y + _voxelSize * 0.5f; // top face of the hit voxel
            float bottom = pos.y - r;
            float groundDistance = surfaceY - bottom;
            float contactMargin = Mathf.Max(_voxelSize * 0.05f, r * 0.1f);
            if (groundDistance < -contactMargin) continue;              // clearly above ground

            bool touching = groundDistance > -contactMargin * 0.5f;
            if (isFoot && touching) feetGrounded++;

            // Soft penalty correction (NO teleport — teleporting jointed bodies
            // breaks the joint and injects energy). Drive the body upward with a
            // velocity proportional to penetration; gravity balances it at rest.
            //
            // Time-step independence: penetration per FixedUpdate scales with dt,
            // so the required correction speed must be penetration / dt. Multiply
            // by groundStiffness for tuning. Without this, slow motion (smaller dt)
            // produces less penetration and a much softer landing than real time.
            if (groundDistance > 0f)
            {
                float push = Mathf.Clamp(groundDistance * groundStiffness / Time.fixedDeltaTime, 0f, groundMaxPushSpeed);

                Vector3 v = rb.linearVelocity;
                // Absorb or reflect the impact based on restitution. Default 0 = inelastic landing,
                // so the body doesn't get an arbitrary bounce from the penetration correction.
                // 1 = fully elastic bounce. This opens the door for material-specific absorption.
                if (v.y < 0f)
                    v.y = -v.y * groundRestitution;
                if (v.y < push) v.y = push;      // resolve penetration, stop sinking
                rb.linearVelocity = v;
            }

            // Friction must be a per-second decay, not a per-FixedUpdate factor.
            // At Time.timeScale 0.1 FixedUpdate runs 10x more often per game second,
            // so a per-step factor would make the ground 10x stickier and break balance.
            // Apply friction whenever the body is near or touching the ground — otherwise
            // a perfectly resting body can slide because the push keeps it exactly at surfaceY.
            if (touching)
            {
                Vector3 v = rb.linearVelocity;
                float frictionFactor = Mathf.Exp(-groundFriction * Time.fixedDeltaTime);
                v.x *= frictionFactor;           // horizontal ground friction
                v.z *= frictionFactor;
                rb.linearVelocity = v;
            }
        }

        // Pass ground contact to the balance controller so it can suppress correction while airborne.
        if (_balance != null)
        {
            if (feetTotal > 0)
                _balance.GroundContactScale = Mathf.Clamp01((float)feetGrounded / feetTotal);
            else
                _balance.GroundContactScale = 1f; // no feet defined: assume always grounded
        }
    }

    // ============================================================ PER-FRAME

    private void RevoxelizeFrame()
    {
        // 1. Recenter the render cube on the pelvis. The physics bodies are now
        //    a top-level GameObject, so moving this VoxelObject transform no longer
        //    teleports them. This gives the dynamic volume box you need for long travel.
        Vector3 pelvisWorld = _rootBody.position;
        Vector3 newOrigin = pelvisWorld - _cubeOriginOffset;

        // Stabilize origin: snap to nearest voxel boundary in world space.
        // This prevents sub-voxel origin drift from shifting all voxels.
        float inv = 1f / _voxelSize;
        newOrigin.x = Mathf.Round(newOrigin.x * inv) / inv;
        newOrigin.y = Mathf.Round(newOrigin.y * inv) / inv;
        newOrigin.z = Mathf.Round(newOrigin.z * inv) / inv;

        _vo.transform.position = newOrigin;

        // 2. Clear + stamp every voxel from its bone's current world transform.
        //    Voxel centers are at integer+0.5 in rel space, so we subtract 0.5
        //    and round to nearest integer for robust quantization.
        //    This prevents 1-voxel jitter from float precision errors at cell boundaries.
        _vo.ClearVoxelData();
        int scatterCount = 0;
        for (int i = 0; i < _voxels.Count; i++)
        {
            VoxelRef vr = _voxels[i];
            if (!_boneTransforms.TryGetValue(vr.boneId, out var bt)) continue;

            Vector3 world = bt.TransformPoint(vr.localOffset);
            Vector3 rel = (world - newOrigin) * inv;
            // Round-to-nearest-cell: more robust than FloorToInt at boundaries.
            int gx = Mathf.RoundToInt(rel.x - 0.5f);
            int gy = Mathf.RoundToInt(rel.y - 0.5f);
            int gz = Mathf.RoundToInt(rel.z - 0.5f);

            // Debug: count voxels that landed on a fractional boundary (potential scatter)
            if (debugQuantization)
            {
                float fx = Mathf.Abs((rel.x - 0.5f) - Mathf.Round(rel.x - 0.5f));
                float fy = Mathf.Abs((rel.y - 0.5f) - Mathf.Round(rel.y - 0.5f));
                float fz = Mathf.Abs((rel.z - 0.5f) - Mathf.Round(rel.z - 0.5f));
                if (fx > 0.4f || fy > 0.4f || fz > 0.4f)
                    scatterCount++;
            }

            _vo.StampVoxelDirect(gx, gy, gz, vr.material);
        }

        if (debugQuantization && scatterCount != _prevScatterCount)
        {
            Debug.Log($"[VoxelRagdoll] Voxel scatter (near-boundary): {scatterCount}/{_voxels.Count} voxels");
            _prevScatterCount = scatterCount;
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
        DrawAllGizmos();
    }

    void OnDrawGizmos()
    {
        if (!drawGizmosAlways) return;
        DrawAllGizmos();
    }

    private void DrawAllGizmos()
    {
        if (!_initialized) return;

        // --- Balance debug ---
        if (balanceSettings != null && balanceSettings.drawBalanceGizmos && _balance != null)
            _balance.DrawGizmos();
    }

    /// <summary>
    /// Draws the bone colliders and joint gizmos. Called by RagdollBodyGizmos on the generated
    /// RagdollBodies container so the debug shapes can be compared against the real colliders.
    /// </summary>
    public void DrawBodyGizmos(bool drawBones, bool drawJoints, float jointRadius, Vector3 offset)
    {
        if (!_initialized) return;

        // Bone/joint gizmos are authored in world space. OnDrawGizmos can inherit a local
        // transform matrix from the component's GameObject, so we reset to a pure offset
        // in world space. This stops the gizmos from "gyroscoping" to the world or jittering.
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.Translate(offset);

        if (drawBones)
        {
            foreach (var t in _boneTransforms.Values)
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
            if (_rootBody != null)
            {
                Gizmos.color = Color.red;
                var rootCol = _rootBody.GetComponent<BoxCollider>();
                if (rootCol != null)
                {
                    Matrix4x4 boxMatrix = Gizmos.matrix;
                    Gizmos.matrix = boxMatrix * _rootBody.localToWorldMatrix;
                    Gizmos.DrawWireCube(rootCol.center, rootCol.size);
                    Gizmos.matrix = boxMatrix;
                }
                else
                {
                    Gizmos.DrawWireSphere(_rootBody.position, _voxelSize);
                }
            }
        }

        // --- Joint gizmos ---
        if (drawJoints && _skel != null)
        {
            foreach (var joint in _skel.joints)
            {
                // Use the live joint anchor from the actual physics body. Fall back to bind pose
                // if the joint is not yet instantiated or is the root joint.
                Vector3 pos = GetJointWorldPosition(joint);
                Gizmos.color = joint.type == JointType.Root   ? Color.red
                             : joint.type == JointType.Ball   ? Color.yellow
                             : joint.type == JointType.Hinge  ? new Color(1f, 0.55f, 0f)
                             : Color.white;
                // Size gizmo from voxel bounds when available; fall back to default radius.
                float r;
                if (joint.hasVoxelBounds)
                {
                    Vector3 bMin = V(joint.voxelBoundsMin);
                    Vector3 bMax = V(joint.voxelBoundsMax);
                    Vector3 sizeVox = bMax - bMin + Vector3.one;
                    // Use the largest dimension so the sphere encompasses the joint's voxel area.
                    r = Mathf.Max(sizeVox.x, sizeVox.y, sizeVox.z) * 0.5f * _voxelSize;
                }
                else
                {
                    r = Mathf.Max(0.01f, jointRadius * _voxelSize);
                }
                Gizmos.DrawWireSphere(pos, r);
            }
        }

        Gizmos.matrix = oldMatrix;
    }

    /// <summary>
    /// Return the live world position of a joint anchor. Finds the bone whose parent joint is
    /// the given joint, reads its ConfigurableJoint anchor, and transforms it to world space.
    /// For the root joint or joints not found, falls back to the bind pose position.
    /// </summary>
    private Vector3 GetJointWorldPosition(SkeletonJoint joint)
    {
        if (_skel == null || _boneTransforms == null) return BindVoxelToWorld(V(joint.position));

        // Find the bone that uses this joint as its pivot (parent joint).
        foreach (var bone in _skel.bones)
        {
            if (bone.parentJoint != joint.id) continue;
            if (!_boneTransforms.TryGetValue(bone.id, out var boneT)) continue;
            var cj = boneT.GetComponent<ConfigurableJoint>();
            if (cj == null) continue;
            return boneT.TransformPoint(cj.anchor);
        }

        // Root joint: anchor at the synthetic root body.
        if (joint.id == _skel.rootJoint && _rootBody != null)
            return _rootBody.TransformPoint(Vector3.zero);

        // Fallback to bind pose.
        if (joint.hasVoxelBounds)
        {
            Vector3 boundsCenter = (V(joint.voxelBoundsMin) + V(joint.voxelBoundsMax)) * 0.5f;
            return BindVoxelToWorld(boundsCenter);
        }
        return BindVoxelToWorld(V(joint.position));
    }

    /// <summary>
    /// Draw a wire capsule: two spheres at the hemisphere centers plus
    /// 4 lines connecting them along the capsule axis.
    /// </summary>
    private static void DrawWireCapsule(Transform t, CapsuleCollider cap)
    {
        Vector3 center = t.TransformPoint(cap.center);
        float r = cap.radius;
        float halfHeight = Mathf.Max(0f, (cap.height * 0.5f) - r); // hemisphere offset along axis

        // Direction vector in local space (0=X, 1=Y, 2=Z)
        Vector3 localAxis = cap.direction == 0 ? Vector3.right
                         : cap.direction == 1 ? Vector3.up
                         : Vector3.forward;
        Vector3 worldAxis = t.TransformDirection(localAxis).normalized;

        Vector3 top = center + worldAxis * halfHeight;
        Vector3 bot = center - worldAxis * halfHeight;

        // Two end spheres
        Gizmos.DrawWireSphere(top, r);
        Gizmos.DrawWireSphere(bot, r);

        // 4 connecting lines (perpendicular offsets for cylinder silhouette)
        Vector3 perp1, perp2;
        if (Mathf.Abs(worldAxis.x) < 0.9f) { perp1 = Vector3.right; }
        else { perp1 = Vector3.up; }
        perp1 = (perp1 - Vector3.Project(perp1, worldAxis)).normalized * r;
        perp2 = Vector3.Cross(worldAxis, perp1).normalized * r;

        Gizmos.DrawLine(top + perp1, bot + perp1);
        Gizmos.DrawLine(top - perp1, bot - perp1);
        Gizmos.DrawLine(top + perp2, bot + perp2);
        Gizmos.DrawLine(top - perp2, bot - perp2);
    }

    // ============================================================ GAME VIEW UI

    void OnGUI()
    {
        if (!_initialized || _balance == null) return;
        if (balanceSettings == null || !balanceSettings.drawBalanceGizmos) return;

        float margin = 8f;
        float width = 200f;
        float lineHeight = 20f;
        float lines = 7f;
        float height = margin + lines * lineHeight;

        // Draw panel in the top-right, below the Time Scale overlay.
        Rect panel = new Rect(Screen.width - width - margin, margin + 90f, width, height);
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 14;
        headerStyle.normal.textColor = new Color(1f, 0.85f, 0.3f); // amber
        GUIStyle valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.fontSize = 14;
        valueStyle.normal.textColor = Color.white;

        float y = panel.y + margin;
        float x = panel.x + margin;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), "Balance Diagnostics", headerStyle);
        y += lineHeight;

        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Strength: {balanceSettings.balanceStrength:F0}", valueStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Lean: {_balance.LastLeanMagnitude:F2}", valueStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Applied Torque: {_balance.LastAppliedTorque.magnitude:F1}", valueStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Required Torque: {_balance.LastRequiredTorque:F1}", valueStyle);
        y += lineHeight;
        float ratio = _balance.LastRequiredTorque > 0f
            ? _balance.LastAppliedTorque.magnitude / _balance.LastRequiredTorque
            : 0f;
        GUIStyle ratioStyle = new GUIStyle(valueStyle);
        ratioStyle.normal.textColor = ratio < 1f ? new Color(1f, 0.4f, 0.4f)  // red = losing
                                  : ratio > 1.2f ? new Color(0.4f, 1f, 0.4f) // green = winning
                                  : new Color(1f, 0.85f, 0.3f);             // amber = marginal
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Torque Ratio: {ratio:F2}", ratioStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Mass: {_balance.TotalMass:F1}", valueStyle);
    }
}
