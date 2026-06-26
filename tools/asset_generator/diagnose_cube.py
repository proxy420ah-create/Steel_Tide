"""Diagnose the test_cube.stasset to see what's actually stored"""

import numpy as np
from voxel_format import load_stasset, MATERIALS

import os
script_dir = os.path.dirname(os.path.abspath(__file__))
cube_path = os.path.join(script_dir, "../../My project/Assets/StreamingAssets/test_cube.stasset")
cube = load_stasset(cube_path)

print(f"Cube shape: {cube.shape}")
print(f"Total voxels: {cube.size}")
print(f"Solid voxels: {np.count_nonzero(cube)}")
print(f"Air voxels: {np.sum(cube == 0)}")

# Check what material IDs are present
unique_values = np.unique(cube)
print(f"\nUnique voxel values: {unique_values}")

# Decode the material IDs
for val in unique_values:
    mat_id = val & 0x1FF  # Extract low 9 bits (material)
    count = np.sum(cube == val)
    print(f"  Value {val}: material_id={mat_id}, count={count}")

# Check a few specific coordinates
print("\nSample voxel values:")
for x, y, z in [(0,0,0), (4,4,4), (7,7,7), (3,3,3)]:
    val = cube[x, y, z]
    mat_id = val & 0x1FF
    print(f"  cube[{x},{y},{z}] = {val} (material_id={mat_id})")

# Verify the data is actually solid concrete (material ID 3)
concrete_id = MATERIALS["Concrete"]
print(f"\nExpected material ID for Concrete: {concrete_id}")
print(f"Voxels with material ID {concrete_id}: {np.sum((cube & 0x1FF) == concrete_id)}")

# Check if the data is uniformly distributed or clustered
print("\nData distribution check:")
print(f"  X=0 plane: {np.count_nonzero(cube[0,:,:])} solid")
print(f"  X=4 plane: {np.count_nonzero(cube[4,:,:])} solid")
print(f"  X=7 plane: {np.count_nonzero(cube[7,:,:])} solid")
print(f"  Y=0 plane: {np.count_nonzero(cube[:,0,:])} solid")
print(f"  Y=4 plane: {np.count_nonzero(cube[:,4,:])} solid")
print(f"  Y=7 plane: {np.count_nonzero(cube[:,7,:])} solid")
print(f"  Z=0 plane: {np.count_nonzero(cube[:,:,0])} solid")
print(f"  Z=4 plane: {np.count_nonzero(cube[:,:,4])} solid")
print(f"  Z=7 plane: {np.count_nonzero(cube[:,:,7])} solid")
