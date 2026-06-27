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
        
        // Voxel data
        private ushort[] voxelData;
        private ComputeBuffer voxelBuffer;
        
        void Start()
        {
            LoadAsset();
            CreateComputeBuffer();
            SetupRenderer();
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
            
            // Dispose the NativeArray
            asset.Dispose();
            
            Debug.Log($"[VoxelObject] Loaded {assetFileName}: {volumeDims.x}×{volumeDims.y}×{volumeDims.z} = {voxelData.Length:N0} voxels");
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
            int index = x + y * volumeDims.x + z * volumeDims.x * volumeDims.y;
            if (index >= 0 && index < voxelData.Length)
                return voxelData[index];
            return 0;
        }
        
        public void SetVoxel(int x, int y, int z, ushort value)
        {
            int index = x + y * volumeDims.x + z * volumeDims.x * volumeDims.y;
            if (index >= 0 && index < voxelData.Length)
            {
                voxelData[index] = value;
                UploadToGPU();
            }
        }
        
        public void SetVoxel(Unity.Mathematics.int3 voxel, ushort value)
        {
            SetVoxel(voxel.x, voxel.y, voxel.z, value);
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
