// Steel Tide: First Device — Voxels / .stasset Writer
// StAssetWriter.cs
//
// Writes binary ".stasset" container compatible with StAssetReader
// Format matches Python authoring tool (tools/asset_generator/voxel_format.py)

using System.IO;
using Unity.Collections;
using Unity.Mathematics;

namespace SteelTide.Voxels
{
    public static class StAssetWriter
    {
        private const int HeaderSize = 16;
        private const byte Version = 1;
        private static readonly byte[] Magic = { 0x53, 0x54, 0x41, 0x53 }; // "STAS"

        /// <summary>Save a voxel volume to .stasset file format.</summary>
        public static void Save(string path, int3 dims, NativeArray<ushort> voxels)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (BinaryWriter writer = new BinaryWriter(fs))
            {
                // Header (16 bytes) - must match StAssetReader format!
                writer.Write(Magic);           // Bytes 0-3: "STAS"
                writer.Write(Version);         // Byte 4: version
                writer.Write((byte)0);         // Byte 5: flags (reserved)
                writer.Write((ushort)dims.x);  // Bytes 6-7: width
                writer.Write((ushort)dims.y);  // Bytes 8-9: height
                writer.Write((ushort)dims.z);  // Bytes 10-11: depth
                writer.Write((uint)0);         // Bytes 12-15: reserved (4 bytes)

                // Voxel data (2 bytes per voxel, little-endian)
                for (int i = 0; i < voxels.Length; i++)
                {
                    writer.Write(voxels[i]);
                }
            }
        }
    }
}
