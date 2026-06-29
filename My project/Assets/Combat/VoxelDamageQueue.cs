using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using SteelTide.Voxels;

namespace SteelTide.Combat
{
    /// <summary>
    /// Global damage queue that batches all voxel damage in a frame before uploading to GPU.
    /// Prevents race conditions when multiple weapons/physics events damage the same volume.
    /// </summary>
    public class VoxelDamageQueue : MonoBehaviour
    {
        private static VoxelDamageQueue _instance;
        public static VoxelDamageQueue Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject go = new GameObject("VoxelDamageQueue");
                    _instance = go.AddComponent<VoxelDamageQueue>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public enum DamagePriority
        {
            Immediate = 0,      // Weapon hits, mech footsteps
            Secondary = 1,      // Explosions triggered by damage
            Environmental = 2   // Falling debris, fire spread
        }

        private struct DamageEvent
        {
            public VoxelObject volume;
            public int3 voxel;
            public ushort newMaterial;
            public DamagePriority priority;
        }

        private List<DamageEvent> _damageQueue = new List<DamageEvent>(1024);
        private HashSet<VoxelObject> _dirtyVolumes = new HashSet<VoxelObject>();

        /// <summary>
        /// Queue a single voxel damage event. Will be applied at end of frame.
        /// </summary>
        public void QueueDamage(VoxelObject volume, int3 voxel, ushort newMaterial, DamagePriority priority = DamagePriority.Immediate)
        {
            _damageQueue.Add(new DamageEvent
            {
                volume = volume,
                voxel = voxel,
                newMaterial = newMaterial,
                priority = priority
            });
            _dirtyVolumes.Add(volume);
        }

        void LateUpdate()
        {
            if (_damageQueue.Count == 0)
                return;

            // Sort by priority (Immediate → Secondary → Environmental)
            _damageQueue.Sort((a, b) => a.priority.CompareTo(b.priority));

            // Begin batch update for all dirty volumes
            foreach (var vol in _dirtyVolumes)
            {
                if (vol != null)
                    vol.BeginBatchUpdate();
            }

            // Apply all queued damage in priority order
            foreach (var dmg in _damageQueue)
            {
                if (dmg.volume != null)
                {
                    dmg.volume.SetVoxel(dmg.voxel, dmg.newMaterial);
                }
            }

            // End batch update (triggers GPU upload)
            foreach (var vol in _dirtyVolumes)
            {
                if (vol != null)
                    vol.EndBatchUpdate();
            }

            // Clear for next frame
            _damageQueue.Clear();
            _dirtyVolumes.Clear();
        }
    }
}
