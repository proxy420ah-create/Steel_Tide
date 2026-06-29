# Steel Tide Voxel Metrics & Units

## Core Scale Assumptions
- **Unity baseline**: 1 Unity unit = 1 meter (default scene scale). All voxel math builds on this assumption.
- **Voxel step size**: Every `VoxelObject` defaults to `voxelSize = 0.125f`, so a single voxel measures **0.125 m (12.5 cm)** along each axis. Eight voxels stack to exactly one meter. @SteelTide/My project/Assets/Voxels/VoxelObject.cs#18-61
- **World registration**: When a voxel asset registers with `VoxelWorld`, it passes this same `voxelSize`, keeping physics, destruction, and rendering in lockstep. @SteelTide/My project/Assets/Voxels/VoxelObject.cs#37-57

### Conversion Table
| Voxels | Meters | Notes |
| ---: | ---: | --- |
| 1 | 0.125 m | Single voxel “pixel”
| 4 | 0.5 m | Half-meter trim or curb
| 8 | 1.0 m | Standard human stride / door thickness
| 16 | 2.0 m | Typical floor-to-ceiling height
| 32 | 4.0 m | Two-story facade or soldier block height
| 128 | 16.0 m | Ground plane tile span (see below)

Formulae:
- **Meters = Voxels × 0.125**
- **Voxels = Meters ÷ 0.125 (×8)**

## Reference Assets & Dimensions
- **Ground Plane (Generate → Terrain: Ground Plane)**: 128×2×128 voxels → 16 m × 0.25 m × 16 m tile for streets/terrain. Ideal for tiling a city block grid. @SteelTide/VoxelAssetStudio/voxel_editor.py#539-565
- **Armored Character Block**: 12×24×12 voxels, created with the same armored-cube workflow used to validate rendering. Handy placeholder for collision and cover testing. @SteelTide/VoxelAssetStudio/voxel_editor.py#566-595
- **Procedural Soldier Block**: 12×32×8 voxels (1.5 m × 4.0 m × 1.0 m) with layered materials for armor, limbs, visor, and joint markers. Export via Generate → "🪖 Soldier Block" to keep rig-friendly proportions. @SteelTide/VoxelAssetStudio/voxel_editor.py#597-627 @SteelTide/VoxelAssetStudio/simple_character_generator.py#61-170

## Sources of Truth
1. **Voxel Asset Studio generators** – Define the raw voxel grids and intended world dimensions (see `simple_character_generator.py`, `shape_generator.py`, `procedural_buildings.py`). Measurements listed in comments/status messages are already converted to meters for quick reference. @SteelTide/VoxelAssetStudio/simple_character_generator.py#61-199
2. **`.stasset` metadata (`volumeDims`)** – Each asset stores integer voxel counts for X/Y/Z. Treat these as authoritative when sizing colliders, proxies, or destruction volumes.
3. **`VoxelObject` component** – Holds both `volumeDims` and `voxelSize`, pushes the buffer to the GPU, and registers with `VoxelRenderer`/`VoxelWorld`. Changing scale anywhere else will cause mismatches; adjust `voxelSize` here if you ever redefine the global unit. @SteelTide/My project/Assets/Voxels/VoxelObject.cs#18-161
4. **`VoxelRenderer`** – Uses the provided `voxelSize` and dimensions when drawing proxy boxes and raymarch bounds, so keeping the above values in sync ensures renders, physics, and hit registration all agree. @SteelTide/My project/Assets/Voxels/VoxelRenderer.cs#314-370

## Usage Guidelines
- **Level design**: Multiply desired meter lengths by 8 to get voxel counts before generating assets. Example: a 3 m doorway opening should be ~24 voxels tall.
- **Character pipeline**: Keep humanoids between 28–40 voxels tall (3.5–5 m) for readability, then scale the prefab uniformly if you need exact hero sizes.
- **Vehicle/architecture handoff**: Record voxel counts in `.stasset` filenames or commit messages so engineering can verify volume sizes without re-opening the studio.
- **Changing global scale**: Update `voxelSize` on every `VoxelObject` prefab **and** regenerate affected assets to avoid stretching. Re-run this document’s table after any change.

Keep this document updated whenever we introduce new canonical assets (vehicles, props, mega-structures) or change the base voxel size.
