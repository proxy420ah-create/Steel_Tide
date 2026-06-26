"""
Visualize the test_cube.stasset in 3D using matplotlib
This shows what Unity SHOULD be rendering
"""

import numpy as np
import matplotlib.pyplot as plt
from mpl_toolkits.mplot3d import Axes3D
from voxel_format import load_stasset
import os

# Load the cube
script_dir = os.path.dirname(os.path.abspath(__file__))
cube_path = os.path.join(script_dir, "../../My project/Assets/StreamingAssets/test_cube.stasset")
cube = load_stasset(cube_path)

print(f"Loaded cube: shape={cube.shape}, solid voxels={np.count_nonzero(cube)}/{cube.size}")

# Extract material IDs (low 9 bits)
materials = cube & 0x1FF

# Create figure
fig = plt.figure(figsize=(12, 10))
ax = fig.add_subplot(111, projection='3d')

# Get coordinates of all solid voxels
solid_mask = materials != 0
x_coords, y_coords, z_coords = np.where(solid_mask)

# Material colors (matching Unity's VoxelRenderer.cs)
material_colors = {
    0: (0.02, 0.03, 0.08, 0.0),   # Air (transparent)
    1: (0.0, 1.0, 0.97, 1.0),     # Energy Shield (cyan)
    2: (0.5, 0.5, 0.5, 1.0),      # Chobham Armor (gray)
    3: (0.6, 0.6, 0.55, 1.0),     # Concrete (tan/gray)
    4: (1.0, 0.27, 0.27, 1.0),    # Flesh (red)
    5: (0.7, 0.7, 0.8, 1.0),      # Steel (blue-gray)
    13: (0.85, 0.15, 0.15, 1.0),  # Damaged Concrete (crimson)
    14: (0.9, 0.4, 0.1, 1.0),     # Damaged Steel (orange)
    15: (0.8, 0.2, 0.2, 1.0),     # Damaged Armor (dark red)
}

# Get colors for each voxel
colors = []
for x, y, z in zip(x_coords, y_coords, z_coords):
    mat_id = materials[x, y, z]
    color = material_colors.get(mat_id, (0.5, 0.5, 0.5, 1.0))
    colors.append(color[:3])  # RGB only for scatter

# Plot voxels as cubes
voxel_size = 0.5  # Unity's voxel size
for i, (x, y, z) in enumerate(zip(x_coords, y_coords, z_coords)):
    # Draw a cube at this position
    # Cube corners
    r = [0, voxel_size]
    X, Y = np.meshgrid(r, r)
    
    # Draw 6 faces of the cube
    color = colors[i]
    alpha = 0.8
    
    # Bottom face (z)
    ax.plot_surface(X + x*voxel_size, Y + y*voxel_size, 
                    np.zeros_like(X) + z*voxel_size,
                    color=color, alpha=alpha, shade=True)
    # Top face (z + voxel_size)
    ax.plot_surface(X + x*voxel_size, Y + y*voxel_size,
                    np.ones_like(X) * (z+1)*voxel_size,
                    color=color, alpha=alpha, shade=True)
    
    # Front face (y)
    ax.plot_surface(X + x*voxel_size, 
                    np.zeros_like(X) + y*voxel_size,
                    Y + z*voxel_size,
                    color=color, alpha=alpha, shade=True)
    # Back face (y + voxel_size)
    ax.plot_surface(X + x*voxel_size,
                    np.ones_like(X) * (y+1)*voxel_size,
                    Y + z*voxel_size,
                    color=color, alpha=alpha, shade=True)
    
    # Left face (x)
    ax.plot_surface(np.zeros_like(X) + x*voxel_size,
                    X + y*voxel_size,
                    Y + z*voxel_size,
                    color=color, alpha=alpha, shade=True)
    # Right face (x + voxel_size)
    ax.plot_surface(np.ones_like(X) * (x+1)*voxel_size,
                    X + y*voxel_size,
                    Y + z*voxel_size,
                    color=color, alpha=alpha, shade=True)

# Set labels and limits
ax.set_xlabel('X (world units)')
ax.set_ylabel('Y (world units)')
ax.set_zlabel('Z (world units)')

# Set equal aspect ratio
max_range = cube.shape[0] * voxel_size
ax.set_xlim([0, max_range])
ax.set_ylim([0, max_range])
ax.set_zlim([0, max_range])

# Add grid
ax.grid(True, alpha=0.3)

# Set viewing angle (similar to Unity camera at (4, 4, -6))
ax.view_init(elev=20, azim=-45)

# Title
unique_mats = np.unique(materials[solid_mask])
mat_names = {3: "Concrete", 13: "Damaged Concrete"}
mat_list = ", ".join([mat_names.get(m, f"Material {m}") for m in unique_mats])
ax.set_title(f'Voxel Cube Visualization\n{cube.shape[0]}×{cube.shape[1]}×{cube.shape[2]} grid, {np.count_nonzero(solid_mask)} solid voxels\nMaterials: {mat_list}',
             fontsize=14, fontweight='bold')

plt.tight_layout()
plt.show()

print("\n✅ This is what Unity SHOULD be rendering!")
print(f"   - Solid {cube.shape[0]}×{cube.shape[1]}×{cube.shape[2]} cube")
print(f"   - All voxels are material ID 3 (tan/gray concrete)")
print(f"   - World bounds: (0,0,0) to ({max_range},{max_range},{max_range})")
