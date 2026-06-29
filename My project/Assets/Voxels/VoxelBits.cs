// Steel Tide: First Device — Voxels / Bit Layout
// VoxelBits.cs
//
// Canonical 16-bit voxel packing — the C# mirror of the Python authoring tool
// (tools/asset_generator/voxel_format.py). This is the SINGLE SOURCE OF TRUTH on
// the runtime side; keep it in lock-step with the Python module.
//
//   Bit 15..12 (4)  SHAPE     0:Cube 1:Wedge 2:Cylinder 3:Sphere
//   Bit 11..9  (3)  ROTATION  0:North 1:South 2:East 3:West 4:Up 5:Down
//   Bit 8..0   (9)  MATERIAL  0:Air 1:EnergyShield 2:Chobham 3:Concrete 4:Flesh 5:Steel
//
//   packed = (shape << 12) | (rotation << 9) | material
//
// See: design/MATERIAL_MATRIX.md, docs/core/TECH_STACK.md (§3)

namespace SteelTide.Voxels
{
    /// <summary>Bit shifts and masks for the packed 16-bit voxel word.</summary>
    public static class VoxelBits
    {
        public const int ShapeShift    = 12;
        public const int RotationShift = 9;
        public const int MaterialShift = 0;

        public const ushort ShapeMask    = 0xF;    // 4 bits -> 16 shapes
        public const ushort RotationMask = 0x7;    // 3 bits -> 8 rotations
        public const ushort MaterialMask = 0x1FF;  // 9 bits -> 512 materials

        /// <summary>Low 9 bits — the only field the penetration/physics loop needs.</summary>
        public static ushort Material(ushort voxel) => (ushort)(voxel & MaterialMask);

        /// <summary>Upper 4 bits — used by the renderer to pick a mesh/SDF.</summary>
        public static ushort Shape(ushort voxel) => (ushort)((voxel >> ShapeShift) & ShapeMask);

        /// <summary>Middle 3 bits — orientation for the renderer.</summary>
        public static ushort Rotation(ushort voxel) => (ushort)((voxel >> RotationShift) & RotationMask);

        public static ushort Pack(ushort shape, ushort rotation, ushort material) =>
            (ushort)((shape << ShapeShift) | (rotation << RotationShift) | (material & MaterialMask));
    }

    /// <summary>Material IDs — Unified material system synchronized with Asset Studio.</summary>
    public static class MaterialId
    {
        // Primary Materials (0-5)
        public const ushort Air             = 0;
        public const ushort PrefabComposite = 1;
        public const ushort RegolithConcrete= 2;
        public const ushort Concrete        = 3;
        public const ushort Flesh           = 4;
        public const ushort Durasteel       = 5;
        
        // Terrain Materials (6-10)
        public const ushort Regolith        = 6;
        public const ushort Xenoflora       = 7;
        public const ushort Basalt          = 8;
        public const ushort Wood            = 9;
        public const ushort TransparentAluminum = 10;
        
        // Clothing/Organic (11-12)
        public const ushort Uniform         = 11;
        public const ushort Reserved        = 12;
        
        // Damaged states (13-15)
        public const ushort DamagedConcrete = 13;
        public const ushort DamagedSteel    = 14;
        public const ushort DamagedArmor    = 15;

        // Advanced materials (16-20)
        public const ushort AblativePlating = 16;
        public const ushort ReactiveArmor   = 17;
        public const ushort FoamCrete       = 18;
        public const ushort NanomeshFabric  = 19;
        public const ushort PlasteelPanels  = 20;

        public const int Count = 21;
    }

    /// <summary>Shape IDs (upper 4 bits).</summary>
    public static class ShapeId
    {
        public const ushort Cube     = 0;
        public const ushort Wedge    = 1;
        public const ushort Cylinder = 2;
        public const ushort Sphere   = 3;
    }

    /// <summary>Rotation IDs (middle 3 bits).</summary>
    public static class RotationId
    {
        public const ushort North = 0;
        public const ushort South = 1;
        public const ushort East  = 2;
        public const ushort West  = 3;
        public const ushort Up    = 4;
        public const ushort Down  = 5;
    }
}
