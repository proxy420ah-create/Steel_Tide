// Steel Tide: First Device — Voxels / Penetration
// VoxelPenetration.cs
//
// Amanatides-Woo 3D DDA traversal of the voxel volume for beam/projectile
// penetration and "shredding". Operates directly on a flat 16-bit buffer
// (1 ushort = 1 packed voxel; see VoxelBits.cs). The penetration loop only cares
// about the low 9 material bits — shape/rotation bits are ignored here and used
// by the renderer. Decoupled from GameObjects; intended to run as a Burst job.
//
// See: design/MATERIAL_MATRIX.md (§2), docs/core/TECH_STACK.md (§3)

using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;

namespace SteelTide.Voxels
{
    /// <summary>Result of a penetration sweep.</summary>
    public struct PenetrationHit
    {
        public bool  absorbed;        // ray stopped inside the volume
        public int3  impactVoxel;     // last voxel touched
        public float remainingPower;  // penetration left when stopped / exited
        public int   shreddedCount;   // voxels deleted this sweep
    }

    [BurstCompile]
    public struct VoxelPenetrationJob
    {
        // Flat volume buffer of PACKED voxels, indexed as x + y*dimX + z*dimX*dimY.
        public NativeArray<ushort> volume;
        // Material density per material ID (penetration power subtracted per voxel).
        [ReadOnly] public NativeArray<float> densityById;
        // Whether a material is shreddable (deleted to Air when penetrated).
        [ReadOnly] public NativeArray<bool> shreddableById;
        // Voxels flipped to Air this sweep -> queued for collision/island recheck.
        public NativeList<int3> dirtyVoxels;

        public int3   dims;        // volume dimensions in voxels
        public float  voxelSize;   // world units per voxel
        public float3 rayOrigin;   // world space
        public float3 rayDir;      // normalized
        public float  basePower;   // weapon penetration power

        public PenetrationHit Execute()
        {
            var hit = new PenetrationHit { remainingPower = basePower };

            // Convert to voxel space.
            float3 p = rayOrigin / voxelSize;
            int3 voxel = (int3)math.floor(p);
            int3 step = (int3)math.sign(rayDir);

            // Per-axis tMax / tDelta for DDA. Guard against zero direction.
            float3 inv = math.select(1f / math.abs(rayDir), float.PositiveInfinity,
                                     math.abs(rayDir) < 1e-6f);
            float3 tDelta = inv; // distance (in t) to cross one voxel per axis
            float3 nextBoundary = (math.floor(p) + math.max(step, 0)) ;
            float3 tMax = (nextBoundary - p) * inv;
            tMax = math.select(tMax, float.PositiveInfinity, math.abs(rayDir) < 1e-6f);

            float power = basePower;

            while (power > 0f && InBounds(voxel))
            {
                int idx = Index(voxel);
                // Mask out shape/rotation — physics only reads the low 9 material bits.
                int mat = volume[idx] & VoxelBits.MaterialMask;
                if (mat != MaterialId.Air)
                {
                    power -= densityById[mat];
                    if (power <= 0f)
                    {
                        hit.absorbed = true;
                        hit.impactVoxel = voxel;
                        hit.remainingPower = 0f;
                        return hit;
                    }

                    if (shreddableById[mat])
                    {
                        // SHRED — clear the whole packed word back to Air (0).
                        volume[idx] = 0;
                        dirtyVoxels.Add(voxel);
                        hit.shreddedCount++;
                    }
                }

                AdvanceDda(ref voxel, ref tMax, tDelta, step);
            }

            hit.impactVoxel = voxel;
            hit.remainingPower = math.max(0f, power);
            return hit;
        }

        private static void AdvanceDda(ref int3 voxel, ref float3 tMax, float3 tDelta, int3 step)
        {
            // Step along the axis with the smallest tMax.
            if (tMax.x < tMax.y)
            {
                if (tMax.x < tMax.z) { voxel.x += step.x; tMax.x += tDelta.x; }
                else                 { voxel.z += step.z; tMax.z += tDelta.z; }
            }
            else
            {
                if (tMax.y < tMax.z) { voxel.y += step.y; tMax.y += tDelta.y; }
                else                 { voxel.z += step.z; tMax.z += tDelta.z; }
            }
        }

        private bool InBounds(int3 v) =>
            v.x >= 0 && v.y >= 0 && v.z >= 0 &&
            v.x < dims.x && v.y < dims.y && v.z < dims.z;

        private int Index(int3 v) => v.x + v.y * dims.x + v.z * dims.x * dims.y;
    }

    // TODO:
    //  - Energy shield HP pool (ID 1) instead of single-hit shred.
    //  - Batch many rays as IJobParallelFor for volley weapons.
    //  - After dirtyVoxels: patch global collision mesh + run flood-fill islanding.
}
