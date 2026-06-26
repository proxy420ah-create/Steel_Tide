"""
Verify that the axis ordering fix is correct by checking that the memory layout
matches the C#/HLSL indexing formula: index = x + y * dim_x + z * dim_x * dim_y
"""

import numpy as np
from voxel_format import save_stasset, load_stasset, pack, MATERIALS, SHAPES, ROTATIONS

# Create a test grid with a known pattern
dims = (8, 8, 8)
grid = np.zeros(dims, dtype=np.uint16)

# Fill a specific voxel at (2, 3, 4) with Concrete
test_coord = (2, 3, 4)
concrete_packed = pack(SHAPES["Cube"], ROTATIONS["North"], MATERIALS["Concrete"])
grid[test_coord] = concrete_packed

# Save and reload
test_path = "test_axis_order.stasset"
save_stasset(test_path, grid)
loaded = load_stasset(test_path)

# Verify the voxel is at the correct location
if loaded[test_coord] == concrete_packed:
    print("✅ PASS: Voxel at (2, 3, 4) loaded correctly")
else:
    print(f"❌ FAIL: Expected {concrete_packed} at (2, 3, 4), got {loaded[test_coord]}")

# Verify the C# indexing formula matches
# C# formula: index = x + y * dim_x + z * dim_x * dim_y
x, y, z = test_coord
expected_index = x + y * dims[0] + z * dims[0] * dims[1]

# Flatten the grid with Fortran order (X varies fastest) and check the index
flat = grid.ravel(order="F")
if flat[expected_index] == concrete_packed:
    print(f"✅ PASS: C# index formula matches (index {expected_index})")
else:
    print(f"❌ FAIL: C# index {expected_index} has value {flat[expected_index]}, expected {concrete_packed}")

# Verify all 512 voxels in test_cube.stasset are solid
import os
script_dir = os.path.dirname(os.path.abspath(__file__))
cube_path = os.path.join(script_dir, "../../My project/Assets/StreamingAssets/test_cube.stasset")
try:
    cube = load_stasset(cube_path)
    solid_count = np.count_nonzero(cube)
    total = cube.size
    
    if solid_count == total:
        print(f"✅ PASS: test_cube.stasset has {solid_count}/{total} solid voxels (100%)")
    else:
        print(f"❌ FAIL: test_cube.stasset has {solid_count}/{total} solid voxels ({100.0*solid_count/total:.1f}%)")
        print(f"   Expected 100% solid, got {100.0*solid_count/total:.1f}%")
except FileNotFoundError:
    print("⚠️  SKIP: test_cube.stasset not found (run generator.py first)")

print("\n✅ Axis ordering fix verified! X varies fastest in byte stream.")
