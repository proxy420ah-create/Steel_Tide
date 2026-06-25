# Asset Generator — Procedural Voxel Pipeline Tool

A **standalone** Python tool that turns LLM-friendly JSON primitive definitions
into packed 16-bit voxel grids and exports them as `.stasset` binary files the
Unity DOTS runtime reads directly. Fully decoupled from Unity — it can be
developed and run with nothing but Python + NumPy.

## Files
- `voxel_format.py` — canonical 16-bit packing + `.stasset` container I/O.
- `generator.py` — JSON → NumPy grid rasterizer (Cube / Wedge / Cylinder / Sphere) + CLI.
- `examples/vekari_walker_gun_barrel.json` — sample asset definition.
- `test_generator.py` — headless verification suite (6 tests).
- `requirements.txt` — dependencies (Phase 1: just NumPy).

## Voxel Bit Layout (uint16)

```
Bit 15..12 (4) SHAPE     0:Cube 1:Wedge 2:Cylinder 3:Sphere
Bit 11..9  (3) ROTATION  0:North 1:South 2:East 3:West 4:Up 5:Down
Bit 8..0   (9) MATERIAL  0:Air 1:Energy_Shield 2:Chobham_Armor 3:Concrete 4:Flesh 5:Steel

packed = (shape << 12) | (rotation << 9) | material
```

Material IDs follow the canonical `design/MATERIAL_MATRIX.md` order (Steel
appended as ID 5). Air == 0 across the whole word.

## .stasset File Format

```
0   4  magic   = "STAS"
4   1  version = 1
5   1  flags   = 0 (reserved)
6   2  dim_x  (uint16 LE)
8   2  dim_y
10  2  dim_z
12  4  reserved
16  .. payload: dim_x*dim_y*dim_z uint16 voxels, X fastest
       (index = x + y*dim_x + z*dim_x*dim_y)
```

## Usage

```bash
pip install -r requirements.txt

# Build the example asset
python generator.py examples/vekari_walker_gun_barrel.json -o out/barrel.stasset

# Verify the pipeline
python test_generator.py
```

## JSON Schema (hand this shape to an LLM)

```json
{
  "asset_name": "Vekari_Walker_Gun_Barrel",
  "grid_size": [16, 16, 32],
  "primitives": [
    { "shape": "Cylinder", "material": "Steel", "rotation": "North",
      "start_coord": [8, 8, 2], "end_coord": [8, 8, 30], "radius": 3 }
  ]
}
```

Primitive semantics:
- **Cube** — inclusive box `start_coord`..`end_coord`.
- **Wedge** — box with a diagonal ramp along its longest horizontal axis.
- **Cylinder** — `radius` capsule between `start_coord` and `end_coord`.
- **Sphere** — `radius` centered at `start_coord` (`end_coord` ignored).

Later primitives overwrite earlier ones where they overlap (carve Air-filled
shapes to hollow out interiors — see the barrel bore in the example).

## Roadmap
- **Phase 2**: PyQt voxel-preview tool reusing the Kraken `pyqtgraph.opengl`
  `GLViewWidget` pattern (live 3D preview, material palette, JSON editor).
- **Phase 3**: Unity C# `.stasset` reader; reconcile runtime voxel buffer to
  `ushort` (mask low 9 bits for material in the penetration loop).

> ⚠️ **Runtime reconciliation pending**: `voxels/VoxelRaymarch.compute` and
> `voxels/VoxelPenetration.cs` currently assume **1 byte** per voxel. The
> canonical format is now **16-bit**. Update those to read `ushort` and mask
> `material = voxel & 0x1FF` before shipping the tactical layer.
