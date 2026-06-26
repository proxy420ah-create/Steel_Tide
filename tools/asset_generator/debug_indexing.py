"""Debug the indexing to understand the memory layout"""

import numpy as np

# Create a small test grid
dims = (4, 3, 2)  # (x=4, y=3, z=2)
grid = np.zeros(dims, dtype=np.uint16)

# Fill with unique values based on coordinates
for x in range(dims[0]):
    for y in range(dims[1]):
        for z in range(dims[2]):
            # Pack coordinates into value for easy identification
            grid[x, y, z] = x * 100 + y * 10 + z

print("Grid shape (x, y, z):", dims)
print("\nGrid contents:")
print(grid)

print("\nFlattened with C order (row-major, rightmost index varies fastest):")
flat = grid.ravel(order="C")
print(flat)

print("\nC# indexing formula: index = x + y * dim_x + z * dim_x * dim_y")
print("Testing coordinate (2, 1, 1):")
x, y, z = 2, 1, 1
c_sharp_index = x + y * dims[0] + z * dims[0] * dims[1]
print(f"  C# index = {x} + {y}*{dims[0]} + {z}*{dims[0]}*{dims[1]} = {c_sharp_index}")
print(f"  Value at flat[{c_sharp_index}] = {flat[c_sharp_index]}")
print(f"  Value at grid[{x},{y},{z}] = {grid[x,y,z]}")
print(f"  Match: {flat[c_sharp_index] == grid[x,y,z]}")

print("\nNumPy ravel order:")
print("With order='C', the LAST axis (z) varies fastest")
print("So the order is: (0,0,0), (0,0,1), (0,1,0), (0,1,1), (0,2,0), ...")

print("\nC# expects X to vary fastest:")
print("So the order should be: (0,0,0), (1,0,0), (2,0,0), (3,0,0), (0,1,0), ...")

print("\nThis means we need order='F' (Fortran order) for X to vary fastest!")
flat_f = grid.ravel(order="F")
print("\nFlattened with F order (column-major, leftmost index varies fastest):")
print(flat_f)

print("\nTesting again with F order:")
print(f"  Value at flat_f[{c_sharp_index}] = {flat_f[c_sharp_index]}")
print(f"  Match: {flat_f[c_sharp_index] == grid[x,y,z]}")
