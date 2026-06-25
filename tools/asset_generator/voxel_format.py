"""
Steel Tide: First Device — Procedural Asset Pipeline
voxel_format.py

Canonical 16-bit voxel packing + the ``.stasset`` binary container format.

Bit layout of a single voxel (uint16, little-endian):

    Bit 15..12 (4 bits)   Bit 11..9 (3 bits)   Bit 8..0 (9 bits)
  +--------------------+--------------------+----------------------+
  |      SHAPE ID      |    ROTATION ID     |     MATERIAL ID      |
  +--------------------+--------------------+----------------------+

  packed = (shape << 12) | (rotation << 9) | material

Material IDs are the canonical SteelTide matrix (see design/MATERIAL_MATRIX.md),
extended with Steel.  Air == 0 across the whole word, so an all-zero buffer is a
valid "empty" volume.

File format (.stasset):
    Offset  Size  Field
    0       4     magic   = b"STAS"
    4       1     version = 1
    5       1     flags   = 0 (reserved)
    6       2     dim_x   (uint16, little-endian)
    8       2     dim_y   (uint16)
    10      2     dim_z   (uint16)
    12      4     reserved (zero)
    16      ...   payload = dim_x*dim_y*dim_z uint16 voxels, C-order (x fastest)
"""

from __future__ import annotations

import struct
from typing import Dict, Tuple

import numpy as np

# ── Bit widths / shifts ────────────────────────────────────────────────
SHAPE_SHIFT = 12
ROTATION_SHIFT = 9
MATERIAL_SHIFT = 0

SHAPE_MASK = 0xF        # 4 bits  -> 16 shapes
ROTATION_MASK = 0x7     # 3 bits  -> 8 rotations
MATERIAL_MASK = 0x1FF   # 9 bits  -> 512 materials

# ── Shape IDs (4 bits) ─────────────────────────────────────────────────
SHAPES: Dict[str, int] = {
    "Cube": 0,
    "Wedge": 1,
    "Cylinder": 2,
    "Sphere": 3,
}

# ── Rotation IDs (3 bits) ──────────────────────────────────────────────
ROTATIONS: Dict[str, int] = {
    "North": 0,
    "South": 1,
    "East": 2,
    "West": 3,
    "Up": 4,
    "Down": 5,
}

# ── Material IDs (9 bits) — canonical matrix + Steel ───────────────────
MATERIALS: Dict[str, int] = {
    "Air": 0,
    "Energy_Shield": 1,
    "Chobham_Armor": 2,
    "Concrete": 3,
    "Flesh": 4,
    "Steel": 5,
}

# Reverse maps for unpacking / debugging.
SHAPES_INV = {v: k for k, v in SHAPES.items()}
ROTATIONS_INV = {v: k for k, v in ROTATIONS.items()}
MATERIALS_INV = {v: k for k, v in MATERIALS.items()}

MAGIC = b"STAS"
VERSION = 1
HEADER_SIZE = 16
VOXEL_DTYPE = np.dtype("<u2")  # little-endian uint16


# ── Packing helpers ────────────────────────────────────────────────────
def pack(shape: int, rotation: int, material: int) -> int:
    """Pack scalar shape/rotation/material IDs into a single uint16 voxel."""
    if not 0 <= shape <= SHAPE_MASK:
        raise ValueError(f"shape id {shape} out of range 0..{SHAPE_MASK}")
    if not 0 <= rotation <= ROTATION_MASK:
        raise ValueError(f"rotation id {rotation} out of range 0..{ROTATION_MASK}")
    if not 0 <= material <= MATERIAL_MASK:
        raise ValueError(f"material id {material} out of range 0..{MATERIAL_MASK}")
    return (shape << SHAPE_SHIFT) | (rotation << ROTATION_SHIFT) | material


def unpack(voxel: int) -> Tuple[int, int, int]:
    """Return (shape_id, rotation_id, material_id) from a packed uint16."""
    shape = (voxel >> SHAPE_SHIFT) & SHAPE_MASK
    rotation = (voxel >> ROTATION_SHIFT) & ROTATION_MASK
    material = (voxel >> MATERIAL_SHIFT) & MATERIAL_MASK
    return shape, rotation, material


def pack_name(shape: str, rotation: str, material: str) -> int:
    """Pack using human-readable names from the SHAPES/ROTATIONS/MATERIALS maps."""
    return pack(SHAPES[shape], ROTATIONS[rotation], MATERIALS[material])


# ── Binary container I/O ───────────────────────────────────────────────
def save_stasset(path: str, grid: np.ndarray) -> None:
    """
    Write a 3D uint16 voxel grid to a ``.stasset`` file (header + payload).

    ``grid`` must be shape (dim_x, dim_y, dim_z).  Stored C-order so that X is
    the fastest-varying axis, matching the Unity-side index:
        index = x + y*dim_x + z*dim_x*dim_y
    """
    if grid.ndim != 3:
        raise ValueError(f"grid must be 3D, got shape {grid.shape}")
    grid = np.ascontiguousarray(grid, dtype=VOXEL_DTYPE)
    dim_x, dim_y, dim_z = grid.shape
    for dim in (dim_x, dim_y, dim_z):
        if not 0 < dim <= 0xFFFF:
            raise ValueError(f"dimension {dim} out of uint16 range")

    header = struct.pack(
        "<4sBBHHHI",
        MAGIC,        # 4s
        VERSION,      # B
        0,            # B flags (reserved)
        dim_x,        # H
        dim_y,        # H
        dim_z,        # H
        0,            # I reserved
    )
    # Transpose to (z, y, x) then ravel so X varies fastest in the byte stream.
    payload = np.transpose(grid, (2, 1, 0)).ravel(order="C")
    with open(path, "wb") as fh:
        fh.write(header)
        fh.write(payload.astype(VOXEL_DTYPE).tobytes())


def load_stasset(path: str) -> np.ndarray:
    """Read a ``.stasset`` file back into a (dim_x, dim_y, dim_z) uint16 grid."""
    with open(path, "rb") as fh:
        header = fh.read(HEADER_SIZE)
        if len(header) != HEADER_SIZE:
            raise ValueError("file too small for header")
        magic, version, _flags, dim_x, dim_y, dim_z, _res = struct.unpack(
            "<4sBBHHHI", header
        )
        if magic != MAGIC:
            raise ValueError(f"bad magic {magic!r}, expected {MAGIC!r}")
        if version != VERSION:
            raise ValueError(f"unsupported version {version}")
        count = dim_x * dim_y * dim_z
        payload = np.frombuffer(fh.read(count * 2), dtype=VOXEL_DTYPE, count=count)
    # Inverse of the save transpose: (z, y, x) -> (x, y, z).
    return payload.reshape((dim_z, dim_y, dim_x)).transpose(2, 1, 0).copy()
