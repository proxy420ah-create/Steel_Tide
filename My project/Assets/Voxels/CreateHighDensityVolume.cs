// Steel Tide: First Device — Voxels / Create High Density Volume
// CreateHighDensityVolume.cs
//
// Generates a 32×32×32 high-density voxel volume for precision testing
// Run from Unity menu: Tools > Steel Tide > Create High Density Volume

using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using System.IO;
using SteelTide.Voxels;

namespace SteelTide.Tools
{
    public static class CreateHighDensityVolume
    {
        [UnityEditor.MenuItem("Tools/Steel Tide/Create High Density Volume (32x32x32)")]
        public static void CreateVolume()
        {
            int3 dims = new int3(32, 32, 32);
            int totalVoxels = dims.x * dims.y * dims.z;
            
            NativeArray<ushort> voxels = new NativeArray<ushort>(totalVoxels, Allocator.Temp);
            
            // Fill with layered structure
            for (int z = 0; z < dims.z; z++)
            {
                for (int y = 0; y < dims.y; y++)
                {
                    for (int x = 0; x < dims.x; x++)
                    {
                        int idx = x + y * dims.x + z * dims.x * dims.y;
                        
                        // Calculate distance from center
                        float3 center = new float3(dims.x / 2f, dims.y / 2f, dims.z / 2f);
                        float3 pos = new float3(x, y, z);
                        float dist = math.distance(pos, center);
                        
                        ushort material;
                        
                        // Layered sphere structure
                        if (dist > 15.5f)
                        {
                            // Outer shell: Concrete (gray)
                            material = MaterialId.Concrete;
                        }
                        else if (dist > 12f)
                        {
                            // Middle layer: Durasteel (blue-gray reinforced metal)
                            material = MaterialId.Durasteel;
                        }
                        else if (dist > 6f)
                        {
                            // Inner layer: Reactive Armor (advanced protection)
                            material = MaterialId.ReactiveArmor;
                        }
                        else
                        {
                            // Core: Concrete (represents critical systems)
                            material = MaterialId.Concrete;
                        }
                        
                        // Pack voxel data (material + full cube shape)
                        voxels[idx] = VoxelBits.Pack(ShapeId.Cube, 0, material);
                    }
                }
            }
            
            // Save to .stasset file
            string path = Path.Combine(Application.streamingAssetsPath, "HighDensity32.stasset");
            
            // Ensure StreamingAssets folder exists
            if (!Directory.Exists(Application.streamingAssetsPath))
            {
                Directory.CreateDirectory(Application.streamingAssetsPath);
            }
            
            StAssetWriter.Save(path, dims, voxels);
            
            int solidCount = 0;
            for (int i = 0; i < voxels.Length; i++)
            {
                if (VoxelBits.Material(voxels[i]) != MaterialId.Air)
                    solidCount++;
            }
            
            voxels.Dispose();
            
            Debug.Log($"[SteelTide] Created high-density volume: {path}");
            Debug.Log($"  Dimensions: {dims}");
            Debug.Log($"  Total voxels: {totalVoxels:N0}");
            Debug.Log($"  Solid voxels: {solidCount:N0} ({100f * solidCount / totalVoxels:F1}%)");
            Debug.Log($"  File size: {new FileInfo(path).Length / 1024f:F1} KB");
            Debug.Log($"  Voxel size should be: 0.125 units (4 / 32)");
            
            UnityEditor.AssetDatabase.Refresh();
        }
    }
}
