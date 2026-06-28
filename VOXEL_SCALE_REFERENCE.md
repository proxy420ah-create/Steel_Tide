# Voxel Scale Reference - Steel Tide

**Last Updated:** June 27, 2026

---

## 📏 **Current Scale System**

### **Unity (In-Game):**
```
1 voxel = 0.125 Unity units (12.5 cm)
```

**Defined in:** `VoxelObject.cs` → `public float voxelSize = 0.125f;`

---

## 🎮 **What You See In-Game**

### **When You Shoot a Voxel Object:**

**Each small cube that gets destroyed = 1 voxel = 0.125 Unity units**

### **Example: 32×32×32 Voxel Grid**

```
Grid Size:     32 × 32 × 32 voxels
Voxel Size:    0.125 units per voxel
World Size:    4m × 4m × 4m

Total Voxels:  32,768 voxels
```

**Calculation:**
- Width:  32 voxels × 0.125 = 4.0 Unity units (4 meters)
- Height: 32 voxels × 0.125 = 4.0 Unity units (4 meters)
- Depth:  32 voxels × 0.125 = 4.0 Unity units (4 meters)

---

## 🏗️ **Real-World Scale Comparison**

### **If 1 Unity Unit = 1 Meter (Standard Unity Convention):**

| Voxels | Unity Units | Real-World Size | Example Object |
|--------|-------------|-----------------|----------------|
| 1      | 0.125m      | 12.5 cm         | Large fist |
| 8      | 1.0m        | 1 meter         | Doorway width |
| 16     | 2.0m        | 2 meters        | Tall person |
| 32     | 4.0m        | 4 meters        | Small room |
| 64     | 8.0m        | 8 meters        | Large vehicle |
| 128    | 16.0m       | 16 meters       | Small building |

### **Current Default Assets:**

**Test Cube (8×8×8):**
- Voxels: 8 × 8 × 8 = 512 voxels
- Size: 1m × 1m × 1m
- **Like a large crate or small vehicle**

**Armored Sphere (radius 15, in 32×32×32 grid):**
- Grid: 32 × 32 × 32 = 32,768 voxels
- Size: 4m × 4m × 4m
- Sphere diameter: ~3.75m
- **Like a small tank or bunker**

---

## 🎨 **Voxel Asset Studio Scale**

### **Studio Settings:**
```python
self.voxel_size = 0.125  # World units per voxel
self.grid_size = (32, 32, 32)  # Default grid
```

**Defined in:** `VoxelAssetStudio/viewport_widget.py`

### **Studio vs Unity:**

| Studio | Unity | Notes |
|--------|-------|-------|
| 1 voxel | 0.125 units | **Matches perfectly!** |
| 32×32×32 grid | 4m × 4m × 4m | Default canvas size |
| Brush size 1 | 0.125m cube | Single voxel |
| Brush size 10 | 1.25m cube | 10×10×10 voxels |

**✅ What you sculpt in the studio appears EXACTLY the same size in Unity!**

---

## 🔧 **Changing Voxel Size**

### **To Make Voxels Bigger (Lower Resolution):**

**Unity:** `VoxelObject.cs`
```csharp
public float voxelSize = 0.25f;  // 25cm per voxel (2x bigger)
```

**Studio:** `viewport_widget.py`
```python
self.voxel_size = 0.25  # Match Unity!
```

**Result:**
- 32×32×32 grid = 8m × 8m × 8m (twice as big)
- Fewer voxels needed for same object
- **Faster rendering, less detail**

---

### **To Make Voxels Smaller (Higher Resolution):**

**Unity:** `VoxelObject.cs`
```csharp
public float voxelSize = 0.0625f;  // 6.25cm per voxel (2x smaller)
```

**Studio:** `viewport_widget.py`
```python
self.voxel_size = 0.0625  # Match Unity!
```

**Result:**
- 32×32×32 grid = 2m × 2m × 2m (half as big)
- More voxels needed for same object
- **Slower rendering, more detail**

---

## 📐 **Recommended Scales for Different Objects**

### **Small Objects (Weapons, Props):**
```
Voxel Size: 0.0625m (6.25cm)
Grid Size:  16×16×16 to 32×32×32
World Size: 1m to 2m
Detail:     High (256 to 32K voxels)
```

### **Medium Objects (Vehicles, Furniture):**
```
Voxel Size: 0.125m (12.5cm) ← CURRENT DEFAULT
Grid Size:  32×32×32 to 64×64×64
World Size: 4m to 8m
Detail:     Medium (32K to 262K voxels)
```

### **Large Objects (Buildings, Terrain):**
```
Voxel Size: 0.25m to 0.5m (25-50cm)
Grid Size:  64×64×64 to 128×128×128
World Size: 16m to 64m
Detail:     Low (262K to 2M voxels)
```

---

## 🎯 **Performance Considerations**

### **Voxel Count vs Performance:**

| Grid Size | Total Voxels | GPU Memory | Recommended Use |
|-----------|--------------|------------|-----------------|
| 8×8×8     | 512          | ~2 KB      | Tiny props |
| 16×16×16  | 4,096        | ~16 KB     | Small objects |
| 32×32×32  | 32,768       | ~128 KB    | **Current default** |
| 64×64×64  | 262,144      | ~1 MB      | Large vehicles |
| 128×128×128 | 2,097,152  | ~8 MB      | Buildings |

**Current System:**
- Handles 5-10 objects @ 32K voxels each
- Target: 100+ objects with ECS optimization
- Goal: 1000+ objects with spatial partitioning

---

## 🧮 **Quick Conversion Formulas**

### **Voxels → Unity Units:**
```
World Size = Voxel Count × Voxel Size
Example: 32 voxels × 0.125 = 4.0 units
```

### **Unity Units → Voxels:**
```
Voxel Count = World Size ÷ Voxel Size
Example: 4.0 units ÷ 0.125 = 32 voxels
```

### **Grid Size → World Size:**
```
World Size = Grid Dimensions × Voxel Size
Example: (32, 32, 32) × 0.125 = (4m, 4m, 4m)
```

---

## 🎨 **Design Guidelines**

### **For Best Results:**

1. **Keep voxel size consistent** across all assets (0.125m recommended)
2. **Use larger grids** for bigger objects, not larger voxel size
3. **Optimize grid size** to object - don't use 128³ for a 2m object
4. **Test in Unity early** - what looks good in studio looks good in-game
5. **Consider performance** - more voxels = slower, but more detail

### **Common Mistakes:**

❌ **Using different voxel sizes** for different objects (scale inconsistency)  
✅ **Use same voxel size, different grid sizes**

❌ **Making grid too big** for object (wasted memory)  
✅ **Fit grid to object bounds**

❌ **Making voxels too small** for distant objects (unnecessary detail)  
✅ **Use LOD system or larger voxels for background objects**

---

## 🔍 **Debugging Scale Issues**

### **If Objects Appear Wrong Size in Unity:**

1. **Check VoxelObject.voxelSize** matches studio (0.125)
2. **Check grid dimensions** match .stasset file
3. **Check Transform.scale** is (1, 1, 1) in Unity
4. **Check asset was exported** from studio correctly

### **Gizmo Visualization:**

In Unity Scene view, VoxelObject draws:
- **Yellow wireframe** = voxel grid bounds
- **Cyan spheres** = corner markers
- **Size** = volumeDims × voxelSize

---

## 📝 **Summary**

**Current System:**
- ✅ **1 voxel = 0.125 Unity units (12.5cm)**
- ✅ **32×32×32 grid = 4m × 4m × 4m**
- ✅ **Studio and Unity scales match perfectly**
- ✅ **Each destroyed cube in-game = 1 voxel**

**To Answer Your Question:**
> "Is the smallest square I see in destruction 1 unit?"

**Answer:** No, it's **0.125 units (12.5cm)**. Each small cube you destroy is 1 voxel, which is 1/8th of a Unity meter. This gives you good detail while keeping performance reasonable.

---

**Next Steps:**
- Add procedural generation algorithms
- Create scale presets for different object types
- Add LOD system for distant objects
