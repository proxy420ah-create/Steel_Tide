// Steel Tide: Voxel Object
// VoxelObject.cs
//
// Unified component for voxel objects - handles loading, rendering, and damage.
// Replaces the VoxelRenderer + VoxelVolumeProxy combo with a single clean component.

using UnityEngine;
using Unity.Mathematics;

namespace SteelTide.Voxels
{
    public class VoxelObject : MonoBehaviour
    {
        [Header("Asset")]
        public string assetFileName = "Sphere_test.stasset";
        
        [Header("Voxel Grid")]
        public int3 volumeDims = new int3(32, 32, 32);
        public float voxelSize = 0.125f;
        
        [Header("Rendering")]
        public VoxelRenderer voxelRenderer;  // Reference to renderer component
        public bool showGizmo = true;

        [Header("World Registration")]
        [Tooltip("Register this volume with VoxelWorld for voxel-grid collision. Disable for dynamic render-only surfaces (e.g. the ragdoll re-voxelization volume).")]
        public bool registerWithVoxelWorld = true;
        
        // Voxel data
        private ushort[] voxelData;
        private ComputeBuffer voxelBuffer;

        // World registration context
        private VoxelWorld _voxelWorld;
        private Vector3Int _worldGridOrigin;

        // Optional rig metadata from .stasset v2 (null for v1 / un-rigged assets).
        public VoxelSkeleton Skeleton { get; private set; }
        public bool HasSkeleton => Skeleton != null && Skeleton.bones.Count > 0;
        
        void Start()
        {
            LoadAsset();
            CreateComputeBuffer();
            SetupRenderer();
            RegisterWithVoxelWorld();
        }
        
        void RegisterWithVoxelWorld()
        {
            if (!registerWithVoxelWorld)
            {
                Debug.Log($"[VoxelObject] {gameObject.name}: VoxelWorld registration disabled (render-only volume).");
                return;
            }

            // Register voxel data with VoxelWorld for collision detection
            _voxelWorld = VoxelWorld.Instance;
            
            if (_voxelWorld == null)
            {
                Debug.LogWarning($"[VoxelObject] {gameObject.name}: VoxelWorld not found. Collision detection will not work.");
                return;
            }
            
            if (voxelData == null || voxelData.Length == 0)
            {
                Debug.LogWarning($"[VoxelObject] {gameObject.name}: No voxel data to register.");
                return;
            }
            
            Debug.Log($"[VoxelObject] {gameObject.name}: Registering with VoxelWorld...");
            _voxelWorld.RegisterVoxelObject(transform.position, assetFileName, voxelSize);
            _worldGridOrigin = _voxelWorld.WorldToVoxelGrid(transform.position);
            Debug.Log($"[VoxelObject] {gameObject.name}: Successfully registered with VoxelWorld!");
        }
        
        void LoadAsset()
        {
            string path = System.IO.Path.Combine(Application.streamingAssetsPath, assetFileName);
            
            if (!System.IO.File.Exists(path))
            {
                Debug.LogError($"[VoxelObject] Asset not found: {path}");
                return;
            }
            
            // Use StAssetReader static method to load
            StAsset asset = StAssetReader.Load(path);
            volumeDims = asset.dims;
            
            // Copy NativeArray to managed array
            voxelData = new ushort[asset.VoxelCount];
            asset.volume.CopyTo(voxelData);

            // Capture optional rig metadata (v2). The skeleton is managed memory,
            // unaffected by the NativeArray Dispose() below.
            Skeleton = asset.skeleton;
            
            // Dispose the NativeArray
            asset.Dispose();
            
            Debug.Log($"[VoxelObject] Loaded {assetFileName}: {volumeDims.x}×{volumeDims.y}×{volumeDims.z} = {voxelData.Length:N0} voxels");
            if (HasSkeleton)
                Debug.Log($"[VoxelObject] Rig: {Skeleton.bones.Count} bones, {Skeleton.joints.Count} joints (root joint {Skeleton.rootJoint})");
            
            // DIAGNOSTIC: Sample some voxels to verify data integrity
            Debug.Log($"[VoxelObject] Sample voxels: [0,0,0]={GetVoxel(0,0,0)}, [0,5,0]={GetVoxel(0,5,0)}, [0,10,0]={GetVoxel(0,10,0)}, [0,15,0]={GetVoxel(0,15,0)}");
        }
        
        void CreateComputeBuffer()
        {
            if (voxelData == null || voxelData.Length == 0)
            {
                Debug.LogError("[VoxelObject] No voxel data loaded!");
                return;
            }
            
            // Create ComputeBuffer for GPU
            int totalVoxels = volumeDims.x * volumeDims.y * volumeDims.z;
            voxelBuffer = new ComputeBuffer(totalVoxels, sizeof(uint));
            
            // Convert ushort[] to uint[] for GPU
            uint[] gpuData = new uint[totalVoxels];
            for (int i = 0; i < totalVoxels; i++)
            {
                gpuData[i] = voxelData[i];
            }
            
            voxelBuffer.SetData(gpuData);
            
            Debug.Log($"[VoxelObject] ComputeBuffer created: {totalVoxels:N0} voxels");
        }
        
        void SetupRenderer()
        {
            // Auto-find VoxelRenderer if not assigned
            if (voxelRenderer == null)
            {
                // First try on this GameObject
                voxelRenderer = GetComponent<VoxelRenderer>();
                
                // If not found, find the main camera's renderer
                if (voxelRenderer == null)
                {
                    Camera mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        voxelRenderer = mainCam.GetComponent<VoxelRenderer>();
                    }
                }
            }
            
            if (voxelRenderer == null)
            {
                Debug.LogWarning("[VoxelObject] No VoxelRenderer found! Add VoxelRenderer component to Main Camera or this GameObject.");
                return;
            }
            
            // Register with the multi-volume system
            voxelRenderer.RegisterVolume(this);
            
            Debug.Log($"[VoxelObject] Registered with VoxelRenderer on {voxelRenderer.gameObject.name}");
        }
        
        void OnDestroy()
        {
            // Unregister from renderer
            if (voxelRenderer != null)
            {
                voxelRenderer.UnregisterVolume(this);
            }
            
            // Release ComputeBuffer
            if (voxelBuffer != null)
            {
                voxelBuffer.Release();
                voxelBuffer = null;
            }
        }
        
        // Public accessors for VoxelRenderer
        public ComputeBuffer GetVoxelBuffer() => voxelBuffer;
        public Unity.Mathematics.int3 GetVolumeDims() => volumeDims;
        public float GetVoxelSize() => voxelSize;
        public Vector3 GetVolumeOffset() => transform.position;
        
        // Public accessors for VoxelWeaponController (damage system)
        public ushort[] GetVoxelData() => voxelData;
        
        public ushort GetVoxel(int x, int y, int z)
        {
            // Safe to read during batch updates - we're only modifying CPU-side array
            // GPU upload happens atomically at EndBatchUpdate()
            int index = x + y * volumeDims.x + z * volumeDims.x * volumeDims.y;
            if (index >= 0 && index < voxelData.Length)
                return voxelData[index];
            return 0;
        }
        
        private int _batchUpdateDepth = 0;
        
        public void BeginBatchUpdate()
        {
            _batchUpdateDepth++;
        }
        
        public void EndBatchUpdate()
        {
            _batchUpdateDepth--;
            if (_batchUpdateDepth <= 0)
            {
                _batchUpdateDepth = 0;
                UploadToGPU();
            }
        }
        
        public void SetVoxel(int x, int y, int z, ushort value)
        {
            int index = x + y * volumeDims.x + z * volumeDims.x * volumeDims.y;
            if (index >= 0 && index < voxelData.Length)
            {
                voxelData[index] = value;
                
                // Sync to VoxelWorld for physics/collision detection
                SyncVoxelToWorld(x, y, z, value);
                
                if (_batchUpdateDepth == 0)
                {
                    UploadToGPU();
                }
            }
        }
        
        public void SetVoxel(Unity.Mathematics.int3 voxel, ushort value)
        {
            SetVoxel(voxel.x, voxel.y, voxel.z, value);
        }
        
        /// <summary>
        /// Sync a local voxel change to VoxelWorld's physics dictionary.
        /// Called after updating voxelData to keep collision detection in sync.
        /// </summary>
        private void SyncVoxelToWorld(int x, int y, int z, ushort value)
        {
            if (_voxelWorld == null) return;
            
            // Translate local voxel coords to world grid coords
            Vector3Int worldGridPos = _worldGridOrigin + new Vector3Int(x, y, z);
            
            // Extract material ID from packed voxel value
            byte material = (byte)(value & VoxelBits.MaterialMask);
            
            // Notify VoxelWorld (removes from dictionary if material is Air)
            _voxelWorld.SetVoxel(worldGridPos, material);
        }
        
        /// <summary>
        /// Bulk voxel update for large-scale destruction (mech footsteps, explosions).
        /// Automatically batches the upload.
        /// </summary>
        public void SetVoxelsBulk(System.Collections.Generic.List<(Unity.Mathematics.int3 pos, ushort mat)> updates)
        {
            if (updates == null || updates.Count == 0)
                return;
            
            BeginBatchUpdate();
            foreach (var (pos, mat) in updates)
            {
                SetVoxel(pos, mat);
            }
            EndBatchUpdate();
        }
        
        private void UploadToGPU()
        {
            if (voxelBuffer == null) return;
            
            // Convert ushort[] to uint[] for GPU
            int totalVoxels = volumeDims.x * volumeDims.y * volumeDims.z;
            uint[] gpuData = new uint[totalVoxels];
            for (int i = 0; i < totalVoxels; i++)
            {
                gpuData[i] = voxelData[i];
            }
            
            voxelBuffer.SetData(gpuData);
        }

        /// <summary>
        /// Resize the volume to <paramref name="newDims"/>, clearing it to Air and
        /// recreating the GPU buffer. Used by the ragdoll to allocate an oversized
        /// render cube that the bind-pose voxels are re-stamped into each frame.
        /// The VoxelRenderer re-fetches the buffer every frame, so no re-registration
        /// is needed.
        /// </summary>
        public void ReinitializeVolume(int3 newDims)
        {
            volumeDims = newDims;
            int totalVoxels = newDims.x * newDims.y * newDims.z;
            voxelData = new ushort[totalVoxels];  // zero-initialized (Air)

            if (voxelBuffer != null)
            {
                voxelBuffer.Release();
                voxelBuffer = null;
            }
            voxelBuffer = new ComputeBuffer(totalVoxels, sizeof(uint));
            voxelBuffer.SetData(new uint[totalVoxels]);

            Debug.Log($"[VoxelObject] {gameObject.name}: Volume reinitialized to {newDims.x}×{newDims.y}×{newDims.z}");
        }

        /// <summary>Zero the CPU voxel array (does not upload). Pair with ApplyVoxelData().</summary>
        public void ClearVoxelData()
        {
            if (voxelData != null)
                System.Array.Clear(voxelData, 0, voxelData.Length);
        }

        /// <summary>
        /// Write a material directly into the CPU array without VoxelWorld sync
        /// (fast path for per-frame re-voxelization). Call ApplyVoxelData() afterwards.
        /// </summary>
        public void StampVoxelDirect(int x, int y, int z, ushort value)
        {
            if (x < 0 || y < 0 || z < 0 ||
                x >= volumeDims.x || y >= volumeDims.y || z >= volumeDims.z)
                return;
            int index = x + y * volumeDims.x + z * volumeDims.x * volumeDims.y;
            voxelData[index] = value;
        }

        /// <summary>Upload the current CPU voxel array to the GPU buffer.</summary>
        public void ApplyVoxelData() => UploadToGPU();
        
        void OnDrawGizmos()
        {
            if (!showGizmo) return;
            
            Vector3 size = new Vector3(volumeDims.x, volumeDims.y, volumeDims.z) * voxelSize;
            Vector3 center = transform.position + size / 2f;
            
            // Draw wireframe cube
            Gizmos.color = new Color(0, 1, 1, 0.5f); // Cyan
            Gizmos.DrawWireCube(center, size);
            
            // Draw corner markers
            Gizmos.color = Color.cyan;
            float markerSize = voxelSize * 2f;
            Gizmos.DrawWireSphere(transform.position, markerSize);
            Gizmos.DrawWireSphere(transform.position + size, markerSize);
        }
    }
}
