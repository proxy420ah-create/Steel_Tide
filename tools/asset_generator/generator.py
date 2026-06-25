"""
Steel Tide: First Device — Procedural Asset Pipeline
generator.py

Reads a JSON asset definition (a list of primitive shapes with bounds), rasterizes
each primitive into a 3D NumPy voxel grid using the canonical 16-bit packing, and
exports a ``.stasset`` binary the Unity DOTS side can read directly.

JSON schema (see examples/):

    {
      "asset_name": "Vekari_Walker_Gun_Barrel",
      "grid_size": [16, 16, 32],
      "primitives": [
        {
          "shape": "Cylinder",
          "material": "Steel",
          "rotation": "North",
          "start_coord": [8, 8, 0],
          "end_coord":   [8, 8, 28],
          "radius": 3
        },
        ...
      ]
    }

Primitive semantics:
    Cube      -> axis-aligned box from start_coord..end_coord (inclusive).
    Wedge     -> box with a diagonal cut (ramp) along the long axis.
    Cylinder  -> capsule/cylinder of `radius` between start_coord and end_coord.
    Sphere    -> sphere of `radius` centered at start_coord (end_coord ignored).

Usage:
    python generator.py examples/vekari_walker_gun_barrel.json -o out/barrel.stasset
"""

from __future__ import annotations

import argparse
import json
import os
from typing import Dict, List, Sequence

import numpy as np

from voxel_format import (
    MATERIALS,
    ROTATIONS,
    SHAPES,
    pack,
    save_stasset,
)


# ── Primitive rasterizers ──────────────────────────────────────────────
def _clamp_box(start: Sequence[int], end: Sequence[int], dims):
    lo = np.minimum(start, end)
    hi = np.maximum(start, end)
    lo = np.clip(lo, 0, np.array(dims) - 1)
    hi = np.clip(hi, 0, np.array(dims) - 1)
    return lo, hi


def _fill_cube(mask: np.ndarray, start, end) -> None:
    lo, hi = _clamp_box(start, end, mask.shape)
    mask[lo[0]:hi[0] + 1, lo[1]:hi[1] + 1, lo[2]:hi[2] + 1] = True


def _fill_wedge(mask: np.ndarray, start, end) -> None:
    """Box with a diagonal ramp removed along its longest axis vs height (Y)."""
    lo, hi = _clamp_box(start, end, mask.shape)
    span = hi - lo
    long_axis = int(np.argmax([span[0], span[2]])) * 2  # 0 (X) or 2 (Z)
    length = max(span[long_axis], 1)
    height = max(span[1], 1)
    for i, a in enumerate(range(lo[long_axis], hi[long_axis] + 1)):
        frac = i / length
        y_cut = lo[1] + int(round(frac * height))  # ramp rises along long axis
        if long_axis == 0:
            mask[a, lo[1]:y_cut + 1, lo[2]:hi[2] + 1] = True
        else:
            mask[lo[0]:hi[0] + 1, lo[1]:y_cut + 1, a] = True


def _fill_cylinder(mask: np.ndarray, start, end, radius: float) -> None:
    """Cylinder of `radius` between start and end (capsule-style endpoints)."""
    p0 = np.asarray(start, dtype=np.float64)
    p1 = np.asarray(end, dtype=np.float64)
    axis = p1 - p0
    length_sq = float(axis @ axis)

    lo, hi = _clamp_box(
        np.floor(np.minimum(p0, p1) - radius).astype(int),
        np.ceil(np.maximum(p0, p1) + radius).astype(int),
        mask.shape,
    )
    xs = np.arange(lo[0], hi[0] + 1)
    ys = np.arange(lo[1], hi[1] + 1)
    zs = np.arange(lo[2], hi[2] + 1)
    gx, gy, gz = np.meshgrid(xs, ys, zs, indexing="ij")
    pts = np.stack([gx, gy, gz], axis=-1).astype(np.float64)

    if length_sq < 1e-9:
        dist_sq = np.sum((pts - p0) ** 2, axis=-1)
    else:
        t = np.clip(((pts - p0) @ axis) / length_sq, 0.0, 1.0)
        proj = p0 + t[..., None] * axis
        dist_sq = np.sum((pts - proj) ** 2, axis=-1)

    sel = dist_sq <= radius * radius
    mask[lo[0]:hi[0] + 1, lo[1]:hi[1] + 1, lo[2]:hi[2] + 1] |= sel


def _fill_sphere(mask: np.ndarray, center, radius: float) -> None:
    c = np.asarray(center, dtype=np.float64)
    lo, hi = _clamp_box(
        np.floor(c - radius).astype(int),
        np.ceil(c + radius).astype(int),
        mask.shape,
    )
    xs = np.arange(lo[0], hi[0] + 1)
    ys = np.arange(lo[1], hi[1] + 1)
    zs = np.arange(lo[2], hi[2] + 1)
    gx, gy, gz = np.meshgrid(xs, ys, zs, indexing="ij")
    dist_sq = (gx - c[0]) ** 2 + (gy - c[1]) ** 2 + (gz - c[2]) ** 2
    mask[lo[0]:hi[0] + 1, lo[1]:hi[1] + 1, lo[2]:hi[2] + 1] |= dist_sq <= radius * radius


# ── Core build ─────────────────────────────────────────────────────────
def build_grid(definition: Dict) -> np.ndarray:
    """Rasterize a parsed JSON asset definition into a packed uint16 grid."""
    grid_size = definition.get("grid_size")
    if not grid_size or len(grid_size) != 3:
        raise ValueError("definition must include grid_size [x, y, z]")
    dims = tuple(int(v) for v in grid_size)
    grid = np.zeros(dims, dtype=np.uint16)  # all Air (packed 0)

    for idx, prim in enumerate(definition.get("primitives", [])):
        shape_name = prim["shape"]
        material_name = prim["material"]
        rotation_name = prim.get("rotation", "North")

        if shape_name not in SHAPES:
            raise ValueError(f"primitive {idx}: unknown shape '{shape_name}'")
        if material_name not in MATERIALS:
            raise ValueError(f"primitive {idx}: unknown material '{material_name}'")
        if rotation_name not in ROTATIONS:
            raise ValueError(f"primitive {idx}: unknown rotation '{rotation_name}'")

        packed = pack(
            SHAPES[shape_name], ROTATIONS[rotation_name], MATERIALS[material_name]
        )

        mask = np.zeros(dims, dtype=bool)
        start = prim.get("start_coord", [0, 0, 0])
        end = prim.get("end_coord", start)
        radius = float(prim.get("radius", 1))

        if shape_name == "Cube":
            _fill_cube(mask, start, end)
        elif shape_name == "Wedge":
            _fill_wedge(mask, start, end)
        elif shape_name == "Cylinder":
            _fill_cylinder(mask, start, end, radius)
        elif shape_name == "Sphere":
            _fill_sphere(mask, start, radius)

        # Later primitives overwrite earlier ones where they overlap.
        grid[mask] = packed

    return grid


def build_from_file(json_path: str) -> np.ndarray:
    with open(json_path, "r", encoding="utf-8") as fh:
        definition = json.load(fh)
    return build_grid(definition)


def _main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description="SteelTide procedural voxel asset generator")
    parser.add_argument("json", help="Path to the asset definition JSON")
    parser.add_argument("-o", "--output", help="Output .stasset path")
    args = parser.parse_args(argv)

    with open(args.json, "r", encoding="utf-8") as fh:
        definition = json.load(fh)

    grid = build_grid(definition)
    out = args.output
    if not out:
        name = definition.get("asset_name", os.path.splitext(os.path.basename(args.json))[0])
        out = os.path.join(os.path.dirname(args.json) or ".", f"{name}.stasset")

    os.makedirs(os.path.dirname(os.path.abspath(out)), exist_ok=True)
    save_stasset(out, grid)

    solid = int(np.count_nonzero(grid))
    total = int(grid.size)
    print(f"[SteelTide] '{definition.get('asset_name', '?')}' "
          f"grid={grid.shape} solid={solid}/{total} "
          f"({100.0 * solid / total:.1f}%) -> {out}")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main())
