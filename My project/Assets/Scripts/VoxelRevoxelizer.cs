using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(VoxelActor))]
[DisallowMultipleComponent]
public class VoxelRevoxelizer : MonoBehaviour
{
    [Header("Volume Sizing")]
    [Tooltip("Extra voxels of padding added on each side beyond the model's max reach.")]
    public int volumePadding = 4;
    [Tooltip("Hard cap on the oversized cube side (voxels) to bound the GPU buffer.")]
    public int maxVolumeSide = 64;
    [Tooltip("Log how many voxels are near cell boundaries each frame (diagnostic for scatter).")]
    public bool debugQuantization = false;

    private VoxelActor _actor;

    void Awake() => _actor = GetComponent<VoxelActor>();

    public void AssignVoxels(ushort[] src)
    {
        int3 dims = _actor.VoxelObject.GetVolumeDims();
        _actor.Voxels.Clear();
        for (int z = 0; z < dims.z; z++)
        for (int y = 0; y < dims.y; y++)
        for (int x = 0; x < dims.x; x++)
        {
            ushort val = src[x + y * dims.x + z * dims.x * dims.y];
            if (val == 0) continue;

            Vector3 worldBind = _actor.BindVoxelToWorld(new Vector3(x, y, z));
            int boneId = NearestBone(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f));
            if (!_actor.BoneTransforms.TryGetValue(boneId, out var bt)) continue;

            _actor.Voxels.Add(new VoxelRef
            {
                boneId = boneId,
                localOffset = bt.InverseTransformPoint(worldBind),
                material = val,
            });
        }
    }

    public void SizeAndAllocateVolume(ushort[] src)
    {
        Vector3 pelvisWorld = _actor.RootBody.position;
        float maxReach = 1f;
        int3 dims = _actor.VoxelObject.GetVolumeDims();
        for (int z = 0; z < dims.z; z++)
        for (int y = 0; y < dims.y; y++)
        for (int x = 0; x < dims.x; x++)
        {
            if (src[x + y * dims.x + z * dims.x * dims.y] == 0) continue;
            float d = (_actor.BindVoxelToWorld(new Vector3(x, y, z)) - pelvisWorld).magnitude / _actor.VoxelSize;
            if (d > maxReach) maxReach = d;
        }

        int side = Mathf.CeilToInt(2f * maxReach) + 2 * volumePadding;
        side = Mathf.Clamp(side, 8, maxVolumeSide);
        if ((side & 1) == 1) side++;

        _actor.CubeDims = new int3(side, side, side);
        float off = (side * 0.5f + 0.5f) * _actor.VoxelSize;
        _actor.CubeOriginOffset = new Vector3(off, off, off);

        _actor.VoxelObject.ReinitializeVolume(_actor.CubeDims);
    }

    public void RevoxelizeFrame()
    {
        if (_actor == null || _actor.RootBody == null) return;

        Vector3 pelvisWorld = _actor.RootBody.position;
        Vector3 newOrigin = pelvisWorld - _actor.CubeOriginOffset;

        float inv = 1f / _actor.VoxelSize;
        newOrigin.x = Mathf.Round(newOrigin.x * inv) / inv;
        newOrigin.y = Mathf.Round(newOrigin.y * inv) / inv;
        newOrigin.z = Mathf.Round(newOrigin.z * inv) / inv;

        _actor.VoxelObject.transform.position = newOrigin;

        _actor.VoxelObject.ClearVoxelData();
        int scatterCount = 0;
        for (int i = 0; i < _actor.Voxels.Count; i++)
        {
            VoxelRef vr = _actor.Voxels[i];
            if (!_actor.BoneTransforms.TryGetValue(vr.boneId, out var bt)) continue;

            Vector3 world = bt.TransformPoint(vr.localOffset);
            Vector3 rel = (world - newOrigin) * inv;
            int gx = Mathf.RoundToInt(rel.x - 0.5f);
            int gy = Mathf.RoundToInt(rel.y - 0.5f);
            int gz = Mathf.RoundToInt(rel.z - 0.5f);

            if (debugQuantization)
            {
                float fx = Mathf.Abs((rel.x - 0.5f) - Mathf.Round(rel.x - 0.5f));
                float fy = Mathf.Abs((rel.y - 0.5f) - Mathf.Round(rel.y - 0.5f));
                float fz = Mathf.Abs((rel.z - 0.5f) - Mathf.Round(rel.z - 0.5f));
                if (fx > 0.4f || fy > 0.4f || fz > 0.4f)
                    scatterCount++;
            }

            _actor.VoxelObject.StampVoxelDirect(gx, gy, gz, vr.material);
        }

        if (debugQuantization && scatterCount != _actor.PrevScatterCount)
        {
            Debug.Log($"[VoxelActor] Voxel scatter (near-boundary): {scatterCount}/{_actor.Voxels.Count} voxels");
            _actor.PrevScatterCount = scatterCount;
        }

        _actor.VoxelObject.ApplyVoxelData();
    }

    private int NearestBone(Vector3 voxelGridCenter)
    {
        int best = _actor.Skeleton.bones[0].id;
        float bestDist = float.MaxValue;
        foreach (var bone in _actor.Skeleton.bones)
        {
            float d = DistancePointSegment(voxelGridCenter, VoxelActor.V(bone.start) + Half, VoxelActor.V(bone.end) + Half);
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
}
