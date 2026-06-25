"""
Steel Tide: First Device — Procedural Asset Pipeline
test_generator.py

Headless verification of the bit-packing, primitive rasterization, and the
.stasset binary round-trip. Run directly:

    python test_generator.py
"""

from __future__ import annotations

import os
import tempfile

import numpy as np

from voxel_format import (
    MATERIAL_MASK,
    MATERIALS,
    ROTATIONS,
    SHAPES,
    load_stasset,
    pack,
    pack_name,
    save_stasset,
    unpack,
)
from generator import build_grid, build_from_file


def test_pack_unpack_roundtrip():
    for shape in SHAPES.values():
        for rot in ROTATIONS.values():
            for mat in (0, 1, 5, MATERIAL_MASK):
                packed = pack(shape, rot, mat)
                assert unpack(packed) == (shape, rot, mat)
    print("  [ok] pack/unpack round-trip across all shape/rotation/material combos")


def test_known_bit_layout():
    # Steel cylinder facing North: shape=2, rot=0, material=5
    packed = pack_name("Cylinder", "North", "Steel")
    assert packed == (2 << 12) | (0 << 9) | 5, hex(packed)
    # Wedge / Up / Chobham: shape=1, rot=4, material=2
    packed2 = pack_name("Wedge", "Up", "Chobham_Armor")
    assert packed2 == (1 << 12) | (4 << 9) | 2, hex(packed2)
    print("  [ok] explicit bit layout matches the 4/3/9 spec")


def test_cube_fill():
    defn = {
        "asset_name": "unit_cube",
        "grid_size": [8, 8, 8],
        "primitives": [
            {"shape": "Cube", "material": "Concrete", "start_coord": [2, 2, 2],
             "end_coord": [5, 5, 5]},
        ],
    }
    grid = build_grid(defn)
    expected = pack(SHAPES["Cube"], ROTATIONS["North"], MATERIALS["Concrete"])
    # Interior cell is the packed concrete value.
    assert grid[3, 3, 3] == expected
    # Outside the box is Air (0).
    assert grid[0, 0, 0] == 0
    # Exactly 4x4x4 = 64 solid cells.
    assert int(np.count_nonzero(grid)) == 64
    print("  [ok] cube primitive fills the inclusive bounding box (64 cells)")


def test_sphere_is_round():
    defn = {
        "asset_name": "ball",
        "grid_size": [16, 16, 16],
        "primitives": [
            {"shape": "Sphere", "material": "Flesh", "start_coord": [8, 8, 8],
             "radius": 4},
        ],
    }
    grid = build_grid(defn)
    solid = np.count_nonzero(grid)
    ideal = (4.0 / 3.0) * np.pi * 4 ** 3
    # Voxelized sphere should be within ~25% of the analytic volume.
    assert abs(solid - ideal) / ideal < 0.25, (solid, ideal)
    print(f"  [ok] sphere volume ~ analytic ({solid} vs {ideal:.0f})")


def test_stasset_roundtrip():
    defn = {
        "asset_name": "rt",
        "grid_size": [10, 6, 14],  # deliberately non-cubic to catch axis bugs
        "primitives": [
            {"shape": "Cube", "material": "Steel", "start_coord": [1, 1, 1],
             "end_coord": [4, 3, 9]},
            {"shape": "Sphere", "material": "Energy_Shield", "start_coord": [7, 3, 11],
             "radius": 2},
        ],
    }
    grid = build_grid(defn)
    with tempfile.TemporaryDirectory() as tmp:
        path = os.path.join(tmp, "rt.stasset")
        save_stasset(path, grid)
        loaded = load_stasset(path)
    assert loaded.shape == grid.shape, (loaded.shape, grid.shape)
    assert np.array_equal(loaded, grid), "binary round-trip mismatch"
    print("  [ok] .stasset save/load is byte-exact for non-cubic grid")


def test_example_file():
    here = os.path.dirname(os.path.abspath(__file__))
    example = os.path.join(here, "examples", "vekari_walker_gun_barrel.json")
    grid = build_from_file(example)
    assert grid.shape == (16, 16, 32)
    assert np.count_nonzero(grid) > 0
    print(f"  [ok] example asset built ({np.count_nonzero(grid)} solid voxels)")


def main():
    tests = [
        test_pack_unpack_roundtrip,
        test_known_bit_layout,
        test_cube_fill,
        test_sphere_is_round,
        test_stasset_roundtrip,
        test_example_file,
    ]
    print(f"Running {len(tests)} verification tests...\n")
    for t in tests:
        print(f"- {t.__name__}")
        t()
    print("\nAll tests passed.")


if __name__ == "__main__":
    main()
