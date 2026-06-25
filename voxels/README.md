# voxels/ — GPU Compute Shaders, Raymarching & Volume Buffers

The Teardown-style simulation core. Buildings, vehicles, and armor are **3D
Texture Volumes** (1 byte = 1 voxel material ID) rendered on the GPU via
raymarching — **no per-block triangles, fully decoupled from GameObjects**.

## Files
- `VoxelRaymarch.compute` — GPU raymarching kernel (Amanatides–Woo DDA).
- `VoxelPenetration.cs` — CPU/Burst ray traversal + voxel shredding.

## Pipeline
1. World stored as a flat/3D byte buffer on a rigid global grid.
2. GPU raymarch renders the volume each frame.
3. Beams/projectiles traverse the array (Amanatides–Woo), subtracting penetration
   per voxel density and **shredding** (set to Air) when power remains.
4. Shredded voxels are marked dirty → global collision-mesh patch + flood-fill
   **Dynamic Simulation Island** detection for organic collapse.
5. Micro-debris spawns as non-colliding GPU particles (visual only).

## Next Steps
- Chunked volume binding for large maps.
- Flood-fill islanding job + Unity Physics rigid-body handoff.
- Energy-shield HP pool (ID 1) instead of single-hit shred.
- Surface normals + depth output for compositing with ECS entities.

See: `docs/core/TECH_STACK.md` (§2–3), `design/MATERIAL_MATRIX.md`.
