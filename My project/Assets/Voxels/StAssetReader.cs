// Steel Tide: First Device — Voxels / .stasset Reader
// StAssetReader.cs
//
// Parses the binary ".stasset" container produced by the Python authoring tool
// (tools/asset_generator/voxel_format.py) and loads the packed 16-bit voxel
// payload into a NativeArray<ushort> ready for the penetration job / GPU upload.
//
// File format (little-endian):
//   Offset  Size  Field
//   0       4     magic   = "STAS"
//   4       1     version = 1
//   5       1     flags   = 0 (reserved)
//   6       2     dim_x   (ushort)
//   8       2     dim_y   (ushort)
//   10      2     dim_z   (ushort)
//   12      4     reserved (zero)
//   16      ..    payload = dim_x*dim_y*dim_z ushort voxels, X fastest
//                 index = x + y*dim_x + z*dim_x*dim_y
//
// Decoupled from GameObjects. Returns plain data the rest of the pipeline owns.
//
// See: tools/asset_generator/voxel_format.py, design/MATERIAL_MATRIX.md

using System;
using System.IO;
using Unity.Collections;
using Unity.Mathematics;

namespace SteelTide.Voxels
{
    /// <summary>A loaded voxel asset: dimensions + the packed 16-bit volume (+ optional rig).</summary>
    public struct StAsset : IDisposable
    {
        public int3 dims;                  // (x, y, z) voxel counts
        public NativeArray<ushort> volume;  // packed voxels, x + y*dimX + z*dimX*dimY
        public VoxelSkeleton skeleton;      // v2 rig metadata, or null for v1 / no rig

        public int VoxelCount => dims.x * dims.y * dims.z;

        public int Index(int x, int y, int z) => x + y * dims.x + z * dims.x * dims.y;

        public ushort Get(int x, int y, int z) => volume[Index(x, y, z)];

        public void Dispose()
        {
            if (volume.IsCreated)
                volume.Dispose();
        }
    }

    public static class StAssetReader
    {
        public const int HeaderSize = 16;
        public const byte MaxSupportedVersion = 2;  // v1 = voxels only, v2 = + appended skeleton block
        // "STAS" as little-endian bytes: 'S','T','A','S'.
        private static readonly byte[] Magic = { 0x53, 0x54, 0x41, 0x53 };
        // "SKEL" skeleton-block magic.
        private static readonly byte[] SkeletonMagic = { 0x53, 0x4B, 0x45, 0x4C };

        /// <summary>Load a .stasset file from disk into a NativeArray-backed StAsset.</summary>
        public static StAsset Load(string path, Allocator allocator = Allocator.Persistent)
        {
            byte[] bytes = File.ReadAllBytes(path);
            return Parse(bytes, allocator);
        }

        /// <summary>Parse a .stasset byte buffer (e.g. from a TextAsset.bytes).</summary>
        public static StAsset Parse(byte[] bytes, Allocator allocator = Allocator.Persistent)
        {
            if (bytes == null || bytes.Length < HeaderSize)
                throw new InvalidDataException("stasset: buffer smaller than 16-byte header");

            // --- Magic ---
            for (int i = 0; i < 4; i++)
            {
                if (bytes[i] != Magic[i])
                    throw new InvalidDataException("stasset: bad magic (not a SteelTide asset)");
            }

            byte version = bytes[4];
            if (version < 1 || version > MaxSupportedVersion)
                throw new InvalidDataException($"stasset: unsupported version {version}");
            // bytes[5] = flags (reserved), bytes[12..15] = reserved.

            int dimX = ReadU16(bytes, 6);
            int dimY = ReadU16(bytes, 8);
            int dimZ = ReadU16(bytes, 10);
            int count = dimX * dimY * dimZ;

            long expected = (long)HeaderSize + (long)count * 2L;
            if (bytes.Length < expected)
                throw new InvalidDataException(
                    $"stasset: payload truncated (need {expected} bytes, have {bytes.Length})");

            var volume = new NativeArray<ushort>(count, allocator, NativeArrayOptions.UninitializedMemory);
            int offset = HeaderSize;
            for (int i = 0; i < count; i++)
            {
                // Little-endian ushort: low byte first.
                volume[i] = (ushort)(bytes[offset] | (bytes[offset + 1] << 8));
                offset += 2;
            }

            VoxelSkeleton skeleton = null;
            if (version >= 2)
                skeleton = TryParseSkeleton(bytes, offset);

            return new StAsset
            {
                dims = new int3(dimX, dimY, dimZ),
                volume = volume,
                skeleton = skeleton,
            };
        }

        /// <summary>Parse the optional "SKEL" block that follows the voxel payload (v2).</summary>
        private static VoxelSkeleton TryParseSkeleton(byte[] bytes, int offset)
        {
            // Need at least the 4-byte magic + 4-byte length.
            if (offset + 8 > bytes.Length)
                return null;

            for (int i = 0; i < 4; i++)
            {
                if (bytes[offset + i] != SkeletonMagic[i])
                    return null; // no skeleton block present
            }

            int jsonLen = bytes[offset + 4]
                          | (bytes[offset + 5] << 8)
                          | (bytes[offset + 6] << 16)
                          | (bytes[offset + 7] << 24);
            int jsonStart = offset + 8;
            if (jsonLen <= 0 || jsonStart + jsonLen > bytes.Length)
                return null;

            string json = System.Text.Encoding.UTF8.GetString(bytes, jsonStart, jsonLen);
            return VoxelSkeleton.FromJson(json);
        }

        private static int ReadU16(byte[] b, int offset) => b[offset] | (b[offset + 1] << 8);
    }
}
