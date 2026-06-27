# High-Density Voxel Volume Setup

## 🎯 Goal
Test 32×32×32 high-density voxel volume for more precise destruction while maintaining good performance.

---

## 📋 Step-by-Step Instructions

### **Step 1: Wait for Unity to Compile**

1. **Wait for Unity to finish compiling** the new scripts (watch bottom-right corner)
2. You should see: "Compiling C# scripts..." then "Compilation finished"

### **Step 2: Generate the High-Density Volume**

1. In Unity, go to menu: **Tools → Steel Tide → Create High Density Volume (32x32x32)**
2. Check the Console - you should see:
   ```
   [SteelTide] Created high-density volume: [path]/HighDensity32.stasset
     Dimensions: int3(32, 32, 32)
     Total voxels: 32,768
     Solid voxels: ~28,000 (85%)
     File size: ~64 KB
     Voxel size should be: 0.125 units (4 / 32)
   ```

---

### **Step 2: Update PrototypeBootstrap Settings**

1. **Select "Bootstrap"** GameObject in Hierarchy
2. **Find "Prototype Bootstrap (Script)"** component
3. **Update these fields:**
   - **Asset File Name:** `HighDensity32.stasset`
   - **Voxel Size:** `0.125` (was 0.5)

---

### **Step 3: Update VoxelRenderer Settings**

1. **Select "Player Camera"** in Hierarchy
2. **Find "Voxel Renderer (Script)"** component
3. **Update these fields:**
   - **Voxel Size:** `0.125` (was 0.5)
   - **Volume Dims:** X: `32`, Y: `32`, Z: `32` (was 8×8×8)

---

### **Step 4: Update Voxel Proxy Collider**

1. **Select "Voxel Proxy"** in Hierarchy
2. **Find "Voxel Volume Proxy (Script)"** component
3. **Update these fields:**
   - **Volume Dims:** X: `32`, Y: `32`, Z: `32`
   - **Voxel Size:** `0.125`
4. **Find "Box Collider"** component
5. **Update these fields:**
   - **Size:** X: `4`, Y: `4`, Z: `4` (stays the same - world size)
   - **Center:** X: `2`, Y: `2`, Z: `2` (stays the same)

---

### **Step 5: Update VoxelWeaponController Settings**

1. **Select "Player Camera"** in Hierarchy
2. **Find "Voxel Weapon Controller (Script)"** component
3. **Update these fields:**
   - **Volume Dims:** X: `32`, Y: `32`, Z: `32`
   - **Voxel Size:** `0.125`
   - **Damage Radius:** `1` (keep at 1 for testing)

---

### **Step 6: Test!**

1. **Press Play**
2. **Aim at the voxel sphere**
3. **Left-click to shoot**

---

## 🎮 Expected Results

### **Visual Differences:**
- ✅ **Smoother surfaces** - 4x more voxels per unit
- ✅ **More precise destruction** - smaller individual voxels
- ✅ **Better detail** - layered armor more visible

### **Destruction with Damage Radius 1:**
- **Old (8×8×8):** 3×3×3 voxels = 1.5×1.5×1.5 units (chunky)
- **New (32×32×32):** 3×3×3 voxels = 0.375×0.375×0.375 units (precise!)

### **Performance:**
- Should still run at 60+ FPS
- Memory usage: ~64 KB (tiny!)
- GPU raymarching handles 32×32×32 easily

---

## 📊 Comparison Table

| Setting | Low Density (8×8×8) | High Density (32×32×32) |
|---------|---------------------|-------------------------|
| **Total Voxels** | 512 | 32,768 |
| **Voxel Size** | 0.5 units | 0.125 units |
| **World Size** | 4×4×4 units | 4×4×4 units |
| **Memory** | 1 KB | 64 KB |
| **Precision** | Chunky | Smooth |
| **Damage Radius 1** | 1.5 units | 0.375 units |

---

## 🔧 Troubleshooting

### **Black screen after switching:**
- Check Console for "Kernel at index (0) is invalid"
- Make sure ALL components have matching settings (dims + voxel size)
- Try: Assets → Refresh

### **Voxels appear too small/large:**
- Verify Voxel Size = 0.125 in ALL components
- Verify Volume Dims = 32×32×32 in ALL components

### **Player falls through voxels:**
- Check Voxel Proxy Box Collider size = 4×4×4
- Check Voxel Proxy Box Collider center = 2×2×2

---

## 🚀 Next Steps

Once high-density works:
1. Test different damage radius values (0, 1, 2)
2. Create custom voxel models (mechs, buildings)
3. Implement voxel physics for player
4. Add particle effects on destruction

---

## 📝 Technical Notes

**Why 32×32×32?**
- 4x density increase (2x per axis)
- Still small enough for real-time performance
- Sweet spot for detail vs. memory

**Why 0.125 voxel size?**
- Math: 4 units / 32 voxels = 0.125 units per voxel
- Keeps world size at 4×4×4 units
- Matches player scale

**Memory scaling:**
- 8×8×8 = 512 voxels × 2 bytes = 1 KB
- 16×16×16 = 4,096 voxels × 2 bytes = 8 KB
- 32×32×32 = 32,768 voxels × 2 bytes = 64 KB
- 64×64×64 = 262,144 voxels × 2 bytes = 512 KB (still feasible!)
