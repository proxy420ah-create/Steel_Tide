# Voxel Scaling Tool - Documentation

## Overview
The Voxel Scaling Tool allows you to dynamically resize voxel models after generation with independent per-axis control. This enables quick iteration on asset sizes without regenerating from scratch.

## Features

### 1. **Independent Axis Scaling**
- **X, Y, Z controls**: Each axis can be scaled independently (0.5×, 1×, 2×, 4×, 8×)
- **Aspect Lock**: Toggle to lock all axes together for uniform scaling
- **Live Preview**: Shows old → new dimensions in both voxels and meters

### 2. **Quick Presets**
- **2× All**: Uniform 2× scale on all axes (e.g., soldier → mech)
- **2× Area**: 2× on X/Z, 1× on Y (e.g., expand road tile footprint)
- **2× Height**: 2× on Y, 1× on X/Z (e.g., taller building variant)

### 3. **Smart Viewport**
- **Auto-framing**: Camera automatically adjusts distance based on model size
- **Large grid**: 256m × 256m grid (2048 voxels) to accommodate scaled models
- **Centered rendering**: Models are centered at origin for consistent viewing

## Usage Examples

### Example 1: Road Tile (4× Area, Keep Thickness)
**Goal**: Expand a 128×2×128 road tile to 512×2×512 (4× area, same 2-voxel thickness)

**Steps**:
1. Generate → 🟫 Terrain: Ground Plane (128×2×128)
2. Scale Panel:
   - ☐ Uncheck "Lock Aspect Ratio"
   - X: **4×** (512 voxels)
   - Y: **1×** (2 voxels) ← Keeps road thickness
   - Z: **4×** (512 voxels)
3. Preview: `128×2×128 → 512×2×512 (16m × 0.25m × 16m → 64m × 0.25m × 64m)`
4. Click **Apply Scale**
5. File → Save As → `Road_Tile_Large.stasset`

**Result**: 64m × 64m road tile, still only 2 voxels (0.25m) thick

---

### Example 2: Soldier (2× Uniform → Mech Size)
**Goal**: Scale a 12×32×8 soldier to 24×64×16 (double size, proportional)

**Steps**:
1. Generate → 🪖 Soldier Block (12×32×8)
2. Scale Panel:
   - ☑ Keep "Lock Aspect Ratio" checked
   - X: **2×** (all axes sync to 2×)
3. Preview: `12×32×8 → 24×64×16 (1.5m × 4m × 1m → 3m × 8m × 2m)`
4. Click **Apply Scale**
5. File → Save As → `Soldier_Mech.stasset`

**Result**: Mech-sized soldier, 2× larger in all dimensions

---

### Example 3: Building (2× Height Only)
**Goal**: Create a taller variant of a building without changing footprint

**Steps**:
1. Generate → 🏘️ Building: Simple House
2. Click preset **"2× Height"**
3. Preview shows X/Z unchanged, Y doubled
4. Click **Apply Scale**
5. File → Save As → `House_Tall.stasset`

**Result**: Building with same footprint, double the height

---

## Technical Details

### Resampling Algorithm
- **Method**: Nearest-neighbor sampling (preserves sharp material boundaries)
- **Performance**: Optimized for large grids (>32k voxels use numpy indexing)
- **Material Preservation**: All voxel IDs copied exactly, no interpolation or blurring

### Viewport Enhancements
- **Grid Size**: 256m × 256m (2048 voxels) with 16m spacing
- **Grid Centering**: Grid centered at origin for symmetric viewing
- **Model Centering**: Voxel models automatically centered at origin
- **Auto-framing**: Camera distance = `max_dimension × 1.5` (clamped 30-300m)

### Scale Factors
| Factor | Effect | Example |
|--------|--------|---------|
| 0.5× | Downscale to half size | 128 voxels → 64 voxels |
| 1× | No change | 128 voxels → 128 voxels |
| 2× | Double size | 128 voxels → 256 voxels |
| 4× | Quadruple size | 128 voxels → 512 voxels |
| 8× | 8× larger | 128 voxels → 1024 voxels |

### Voxel-to-Meter Conversion
- **Voxel Size**: 0.125m (12.5cm) per voxel
- **8 voxels** = 1 meter
- **128 voxels** = 16 meters
- **512 voxels** = 64 meters
- **1024 voxels** = 128 meters

## Limitations

### 1. **Downscaling Loses Detail**
- **0.5× scale** drops 87.5% of voxels (nearest-neighbor keeps only corner samples)
- **Example**: 128×128×128 → 64×64×64 loses fine details
- **Recommendation**: Only downscale if detail loss is acceptable

### 2. **Upscaling Creates Blocky Look**
- Each voxel becomes a 2×2×2 or 4×4×4 cube (no smoothing)
- **Example**: 32×32×32 → 128×128×128 looks pixelated
- **Recommendation**: Use for quick prototyping, regenerate at target size for final assets

### 3. **Performance on Large Grids**
- **512×512×512** grid (2M+ voxels) takes ~15-20 seconds to scale
- **1024×1024×1024** grid (1B+ voxels) may cause memory issues
- **Recommendation**: Scale in steps (2× → 2× → 2×) rather than 8× all at once

## UI Location
**Left Sidebar** → Below Brush Panel → **"Scale Voxel Grid"** group box

## Files Modified
- `scale_panel.py` - UI widget with X/Y/Z controls and presets
- `voxel_scaler.py` - Resampling engine (loop + optimized versions)
- `voxel_editor.py` - Integration and apply handler
- `viewport_widget.py` - Grid expansion, centering, auto-framing

## See Also
- `VOXEL_METRICS_AND_UNITS.md` - Voxel size and conversion reference
- `VOXEL_SCALE_PLAN.md` - Original scale normalization plan
