// SteelTide VoxelActor — modular orchestrator for the physics/vis ragdoll pipeline.
// Replaces the monolithic VoxelRagdoll.cs by delegating to swappable subsystem components.

using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using SteelTide.Voxels;
using SteelTide.ActorPhysics;
using SkeletonBone = SteelTide.Voxels.SkeletonBone;

[RequireComponent(typeof(VoxelObject))]
[DisallowMultipleComponent]
public class VoxelActor : MonoBehaviour
{
    [Header("Simulation")]
    [Tooltip("OFF = bodies kinematic at bind pose (verify re-voxelization). ON = gravity ragdoll.")]
    public bool simulatePhysics = false;
    [Tooltip("Enable the body manager subsystem.")]
    public bool enableBodies = true;
    [Tooltip("Enable the balance controller subsystem.")]
    public bool enableBalance = true;
    [Tooltip("Enable voxel/ground collision resolution.")]
    public bool enableGroundCollision = true;
    [Tooltip("Enable re-voxelization of the visual volume each frame.")]
    public bool enableRevoxelization = true;
    [Tooltip("Build joints. OFF = bones are independent bodies (use this to isolate joint jitter). Requires re-init to take effect.")]
    public bool buildJoints = true;

    // ---- shared runtime state (read by subsystem components) ----
    public VoxelObject VoxelObject { get; private set; }
    public VoxelSkeleton Skeleton { get; private set; }
    public bool IsInitialized { get; private set; }
    public float VoxelSize { get; private set; }
    public Vector3 BindOrigin { get; private set; }
    public int3 CubeDims { get; set; }
    public Vector3 CubeOriginOffset { get; set; }

    public Transform RootBody { get; set; }
    public GameObject BoneRoot { get; set; }
    public Dictionary<int, Transform> BoneTransforms { get; } = new Dictionary<int, Transform>();
    public List<(Rigidbody rb, float radius, int boneId)> Bodies { get; } = new List<(Rigidbody, float, int)>();
    public List<Collider> Colliders { get; } = new List<Collider>();
    public HashSet<int> FootBoneIds { get; } = new HashSet<int>();
    public VoxelWorld VoxelWorld { get; private set; }

    public List<VoxelRef> Voxels { get; } = new List<VoxelRef>();
    public int PrevScatterCount { get; set; } = -1;

    public Vector3 BindRootBodyPosition { get; set; }
    public Quaternion BindRootBodyRotation { get; set; }
    public Dictionary<int, Pose> BindBonePoses { get; } = new Dictionary<int, Pose>();

    public BalanceController Balance { get; set; }

    // Cached subsystems
    private VoxelBodyManager _bodyManager;
    private VoxelGroundResolver _groundResolver;
    private VoxelBalance _balanceComp;
    private VoxelRevoxelizer _revoxelizer;
    private VoxelActorGizmos _gizmos;

    void LateUpdate()
    {
        if (!IsInitialized)
        {
            TryInitialize();
            if (!IsInitialized) return;
        }

        if (enableRevoxelization && _revoxelizer != null && _revoxelizer.enabled)
            _revoxelizer.RevoxelizeFrame();
    }

    void FixedUpdate()
    {
        if (!IsInitialized || !simulatePhysics) return;

        if (enableGroundCollision && _groundResolver != null && _groundResolver.enabled)
            _groundResolver.ResolveVoxelGround();

        if (enableBalance && _balanceComp != null && _balanceComp.enabled)
            _balanceComp.UpdateBalance();
    }

    void OnValidate()
    {
        if (Application.isPlaying && IsInitialized)
            ApplyPhysicsState();
    }

    void OnDestroy()
    {
        if (BoneRoot != null)
            Destroy(BoneRoot);
    }

    public void TryInitialize()
    {
        VoxelObject = GetComponent<VoxelObject>();
        if (VoxelObject == null || !VoxelObject.HasSkeleton) return;

        ushort[] srcData = VoxelObject.GetVoxelData();
        if (srcData == null || srcData.Length == 0) return;

        Skeleton = VoxelObject.Skeleton;
        VoxelSize = VoxelObject.GetVoxelSize();
        BindOrigin = VoxelObject.transform.position;
        VoxelWorld = VoxelWorld.Instance;

        CacheSubsystems();

        if (enableBodies && _bodyManager != null)
            _bodyManager.Build();
        else
            return; // cannot proceed without bodies

        CaptureBindPose();
        _bodyManager.BuildJoints();

        if (_revoxelizer != null)
        {
            _revoxelizer.AssignVoxels(srcData);
            _revoxelizer.SizeAndAllocateVolume(srcData);
        }

        ApplyPhysicsState();

        if (_balanceComp != null)
            _balanceComp.Initialize();

        IsInitialized = true;

        Debug.Log($"[VoxelActor] Initialized: {BoneTransforms.Count} bone bodies, {Voxels.Count} mapped voxels, simulate={simulatePhysics}");
    }

    private void CacheSubsystems()
    {
        _bodyManager = GetComponent<VoxelBodyManager>();
        _groundResolver = GetComponent<VoxelGroundResolver>();
        _balanceComp = GetComponent<VoxelBalance>();
        _revoxelizer = GetComponent<VoxelRevoxelizer>();
        _gizmos = GetComponent<VoxelActorGizmos>();
    }

    public void CaptureBindPose()
    {
        BindRootBodyPosition = RootBody.position;
        BindRootBodyRotation = RootBody.rotation;
        BindBonePoses.Clear();
        foreach (var kvp in BoneTransforms)
            BindBonePoses[kvp.Key] = new Pose(kvp.Value.position, kvp.Value.rotation);
    }

    [ContextMenu("Reset Actor")]
    public void ResetActor()
    {
        if (!IsInitialized) return;

        ResetBodyToBind(RootBody.GetComponent<Rigidbody>(), BindRootBodyPosition, BindRootBodyRotation);
        foreach (var kvp in BoneTransforms)
        {
            if (!BindBonePoses.TryGetValue(kvp.Key, out var bindPose)) continue;
            ResetBodyToBind(kvp.Value.GetComponent<Rigidbody>(), bindPose.position, bindPose.rotation);
        }

        if (_revoxelizer != null)
            _revoxelizer.RevoxelizeFrame();
    }

    public static void ResetBodyToBind(Rigidbody rb, Vector3 position, Quaternion rotation)
    {
        if (rb == null) return;
        rb.position = position;
        rb.rotation = rotation;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.WakeUp();
    }

    public void ApplyPhysicsState()
    {
        SetKinematic(!simulatePhysics);
    }

    public void SetKinematic(bool kinematic)
    {
        foreach (var t in BoneTransforms.Values)
            SetKinematic(t, kinematic);
        SetKinematic(RootBody, kinematic);
    }

    public static void SetKinematic(Transform t, bool kinematic)
    {
        var rb = t.GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic = kinematic;
        rb.useGravity = !kinematic;
    }

    // Utility helpers used by many subsystems.
    public int RootJoint() => Skeleton.rootJoint;
    public Vector3Int RootJointPosition()
    {
        var j = Skeleton.GetJoint(Skeleton.rootJoint);
        return j != null ? j.position : Vector3Int.zero;
    }

    public Vector3 BindVoxelToWorld(Vector3 voxelCoord)
        => BindOrigin + (voxelCoord + new Vector3(0.5f, 0.5f, 0.5f)) * VoxelSize;

    public static Vector3 V(Vector3Int v) => new Vector3(v.x, v.y, v.z);

    public void DrawBalanceGizmos()
    {
        if (Balance != null) Balance.DrawGizmos();
    }
}

public struct VoxelRef
{
    public int boneId;
    public Vector3 localOffset;
    public ushort material;
}
