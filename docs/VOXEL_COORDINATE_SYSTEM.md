# Voxel Coordinate System & Common Pitfalls

**Date:** June 28, 2026  
**Status:** ✅ RESOLVED

---

## 🎯 **Coordinate System Standard**

All voxel operations in Steel Tide use the following coordinate system:

```
X = Width (left/right)
Y = Height (up/down)
Z = Depth (front/back)
```

### **Array Storage Order**

Voxels are stored in a 1D array using **row-major order (C-order)**:

```csharp
int index = x + y * width + z * width * height;
```

This is equivalent to:
```python
# Python numpy default
grid = np.zeros((width, height, depth), dtype=np.uint16)
flattened = grid.flatten()  # Uses C-order by default
```

---

## ⚠️ **Critical Bug: Loop Order Matters!**

### **The Problem**

When iterating over voxels in a 3D grid, **loop order MUST match the coordinate system**.

### **❌ WRONG - Causes Coordinate Mirroring**

```csharp
for (int z = -radius; z <= radius; z++)      // Z first
{
    for (int y = -radius; y <= radius; y++)  // Y second
    {
        for (int x = -radius; x <= radius; x++)  // X last
        {
            int3 voxel = center + new int3(x, y, z);
            // BUG: Voxels accessed in wrong order!
        }
    }
}
```

**Result:** Shooting one corner damages all 4 corners (coordinate mirroring/wrapping).

### **✅ CORRECT - Matches Coordinate System**

```csharp
for (int x = -radius; x <= radius; x++)      // X first
{
    for (int y = -radius; y <= radius; y++)  // Y second
    {
        for (int z = -radius; z <= radius; z++)  // Z last
        {
            int3 voxel = center + new int3(x, y, z);
            // Correct: Voxels accessed in proper order
        }
    }
}
```

---

## 🔍 **Why This Happens**

The loop order affects **cache locality** and **memory access patterns**. When loops don't match the storage order:

1. **Memory jumps** - Non-sequential access causes cache misses
2. **Coordinate confusion** - X/Y/Z offsets applied in wrong order
3. **Index calculation errors** - Stride multipliers misaligned

---

## 📋 **Checklist for New Voxel Code**

When writing any code that iterates over voxels:

- [ ] **Loop order is X → Y → Z** (outermost to innermost)
- [ ] **Index calculation:** `x + y * width + z * width * height`
- [ ] **Bounds checking** before array access
- [ ] **Test with hollow/sparse volumes** (not just solid blocks)
- [ ] **Test with multi-material volumes** (not just single material)

---

## 🧪 **Test Cases**

### **Minimum Test Suite**

1. **Solid single-material box** - Basic functionality
2. **Hollow box** (air interior) - Sparse volume handling
3. **Multi-material solid** - Material transitions
4. **Multi-material with air gaps** - Complex case (characters)

### **Validation**

- Shoot corner → Only that corner damaged ✅
- Shoot center → Symmetric damage pattern ✅
- Shoot edge → Damage doesn't wrap to opposite edge ✅

---

## 🐛 **Bug History**

### **June 28, 2026 - Loop Order Bug**

**File:** `VoxelWeaponController.cs`  
**Function:** `ApplyDamageToVolume()`  
**Symptom:** Shooting one corner damaged all 4 corners  
**Cause:** Loop order was Z, Y, X instead of X, Y, Z  
**Fix:** Reordered loops to match coordinate system  
**Commit:** [Link to commit when available]

---

## 📚 **Related Files**

- `VoxelObject.cs` - Voxel storage and index calculation
- `VoxelWeaponController.cs` - Voxel destruction system
- `stasset_io.py` - File format (uses numpy C-order flatten)
- `simple_character_generator.py` - Character generation

---

## 💡 **Best Practices**

1. **Always use the same loop order** across the codebase
2. **Add comments** when loop order matters for performance
3. **Test with diverse voxel patterns** (not just cubes)
4. **Document coordinate system** in file headers
5. **Use helper functions** for index calculation (don't inline)

---

**Last Updated:** June 28, 2026  
**Author:** Cascade AI + User
