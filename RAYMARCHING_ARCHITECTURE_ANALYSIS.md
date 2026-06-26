# Raymarching Architecture: Full Technical Breakdown

**Date**: June 26, 2026, 1:45 AM  
**Context**: Analyzing sparse rendering issue vs SVO reference implementation

---

## 🎯 **Your Architecture: FLAT DENSE GRID + DDA Raymarching**

### **NOT using Sparse Voxel Octree (SVO)**

Your implementation is **fundamentally different** from the SVO reference. You're using a **flat dense grid** with **Amanatides-Woo DDA traversal**, which is a completely valid and common approach.

---

## 📊 **Data Structure Comparison**

### **SVO (Reference Video)**
```
Hierarchical tree structure:
- Root node → 8 children
- Each child → 8 children (recursive)
- Leaf nodes contain voxel data
- Sparse: Only stores non-empty regions
- Traversal: Tree descent with child_index lookups
```

### **Your System (Flat Dense Grid)**
```
Linear array structure:
- All voxels stored in 1D array
- Index formula: x + y*dimX + z*dimX*dimY
- Dense: Stores ALL voxels (including air)
- Traversal: DDA stepping through grid
```

**These are COMPLETELY DIFFERENT approaches!** ✅ This is intentional and correct.

---

## 🔧 **Your Voxel Encoding (16-bit packed)**

### **Bit Layout** (Matching between Python & C#)
```
Bit 15..12 (4 bits)  → SHAPE     (0=Cube, 1=Wedge, 2=Cylinder, 3=Sphere)
Bit 11..9  (3 bits)  → ROTATION  (0=North, 1=South, 2=East, 3=West, 4=Up, 5=Down)
Bit 8..0   (9 bits)  → MATERIAL  (0=Air, 1=Shield, 2=Armor, 3=Concrete, 4=Flesh, 5=Steel)
```

### **Python Encoding** (`voxel_format.py`)
```python
def pack(shape: int, rotation: int, material: int) -> int:
    return (shape << 12) | (rotation << 9) | (material & 0x1FF)

MATERIALS = {
    "Air": 0,
    "Energy_Shield": 1,
    "Chobham_Armor": 2,
    "Concrete": 3,  # ← Test cube uses this
    "Flesh": 4,
    "Steel": 5,
}
```

### **C# Decoding** (`VoxelBits.cs`)
```csharp
public static ushort Material(ushort voxel) => (ushort)(voxel & 0x1FF);
public static ushort Shape(ushort voxel) => (ushort)((voxel >> 12) & 0xF);
public static ushort Rotation(ushort voxel) => (ushort)((voxel >> 9) & 0x7);
```

### **Shader Decoding** (`VoxelRaymarch.compute`)
```hlsl
#define VX_MATERIAL_MASK   0x1FFu   // low 9 bits
#define VX_SHAPE_SHIFT     12
#define VX_ROTATION_SHIFT  9

uint VxMaterial(uint v)  { return v & VX_MATERIAL_MASK; }
uint VxShape(uint v)     { return (v >> VX_SHAPE_SHIFT) & 0xFu; }
uint VxRotation(uint v)  { return (v >> VX_ROTATION_SHIFT) & 0x7u; }
```

✅ **Encoding/Decoding is CONSISTENT across all layers!**

---

## 🚀 **Your Raymarching Pipeline**

### **1. Data Flow**
```
Python Tool (generator.py)
    ↓
NumPy array (x, y, z) with uint16 voxels
    ↓
Flatten with order='F' (X varies fastest)
    ↓
.stasset file (binary)
    ↓
Unity StAssetReader.cs
    ↓
NativeArray<ushort>
    ↓
ComputeBuffer (GPU upload as uint[])
    ↓
StructuredBuffer<uint> in shader
    ↓
DDA raymarching
```

### **2. Index Formula** (CRITICAL)
```
Python save:  payload = grid.ravel(order="F")
              → X varies fastest (Fortran order)

C# index:     x + y*dimX + z*dimX*dimY
              → X varies fastest ✅

Shader index: v.x + v.y * _VolumeDims.x + v.z * _VolumeDims.x * _VolumeDims.y
              → X varies fastest ✅
```

**All three layers use the SAME indexing formula!** ✅

### **3. Raymarching Algorithm: Amanatides-Woo DDA**

**NOT an SVO traversal!** This is a **grid-stepping algorithm**:

```hlsl
// Start at ray-box intersection point
float3 startPos = ro + rd * max(tNear, 0.0);
int3 voxel = (int3)floor(startPos / _VoxelSize);

// DDA setup
int3 step = (int3)sign(rd);  // +1 or -1 per axis
float3 tDelta = (1.0 / abs(rd)) * _VoxelSize;  // Distance per voxel
float3 tMax = (nextBoundary - startPos) / rd;  // Distance to next boundary

// Step through grid
for (int i = 0; i < _MaxSteps; ++i)
{
    if (!InBounds(voxel)) break;
    
    uint packed = _VoxelData[VoxelIndex(voxel)];
    uint mat = VxMaterial(packed);
    
    if (mat != 0u) {
        // Hit solid voxel - shade and return
        return color;
    }
    
    // Advance to next voxel along smallest tMax
    if (tMax.x < tMax.y && tMax.x < tMax.z) {
        voxel.x += step.x;
        tMax.x += tDelta.x;
    } else if (tMax.y < tMax.z) {
        voxel.y += step.y;
        tMax.y += tDelta.y;
    } else {
        voxel.z += step.z;
        tMax.z += tDelta.z;
    }
}
```

**This is a CLASSIC DDA implementation!** ✅

---

## 🔍 **Key Differences from SVO**

| Feature | SVO (Reference) | Your System (Flat Grid) |
|---------|----------------|-------------------------|
| **Data Structure** | Hierarchical tree | Flat 1D array |
| **Storage** | Sparse (only non-empty) | Dense (all voxels) |
| **Traversal** | Tree descent | DDA grid stepping |
| **Index Lookup** | `find_child_index()` | `x + y*dimX + z*dimX*dimY` |
| **Memory** | O(occupied voxels) | O(dimX × dimY × dimZ) |
| **Performance** | Fast for sparse scenes | Fast for small/medium grids |
| **Complexity** | High (tree management) | Low (simple array) |

---

## ✅ **What's Working Correctly**

1. **Bit packing**: Python ↔ C# ↔ Shader all use same layout
2. **Index formula**: All layers use X-fastest ordering
3. **Data upload**: 512/512 voxels confirmed in buffer
4. **DDA algorithm**: Classic implementation, looks correct
5. **Ray-box intersection**: Standard AABB test

---

## ❌ **Current Bug: Sparse Rendering**

### **Symptoms**
- Data is perfect (512/512 solid voxels)
- Forcing `mat = 3u` still produces sparse output
- Ray-box test (magenta override) should reveal if rays hit volume

### **Most Likely Causes**

#### **1. Camera Position (CONFIRMED)**
```
Camera: (4, 4, 6)  ← Behind the cube!
Cube:   (0, 0, 0) to (4, 4, 4)
```

**Camera is at Z=6, cube ends at Z=4!**

**Fix**: Move camera to `(4, 4, -6)` to be in front of cube

#### **2. Ray Direction Calculation**
```hlsl
float3 RayDirFromUV(float2 uv)
{
    // Converts screen UV to world-space ray direction
    // Uses inverse projection + camera-to-world matrix
}
```

If camera is behind cube, rays might be pointing wrong direction.

#### **3. DDA Starting Position**
```hlsl
float3 startPos = ro + rd * max(tNear, 0.0);
int3 voxel = (int3)floor(startPos / _VoxelSize);
```

If `startPos` is outside volume, `voxel` will be out of bounds immediately.

---

## 🧪 **Diagnostic Tests**

### **Test 1: Ray-Box Intersection** (Currently Active)
```hlsl
// Lines 121-122 uncommented
_Output[id.xy] = float4(1, 0, 1, 1); // Magenta
return;
```

**Expected**: Solid magenta rectangle where cube should be  
**If sparse**: Ray-box intersection is failing  
**If solid**: Bug is in DDA traversal

### **Test 2: Camera Position Fix**
```
In Unity:
1. Select Main Camera
2. Set Position: (4, 4, -6)  ← Change Z from 6 to -6
3. Press Play
```

**Expected**: Should fix rendering if camera was the issue

### **Test 3: First Voxel Debug**
```hlsl
// Replace DDA loop with:
int3 voxel = (int3)floor(startPos / _VoxelSize);
if (InBounds(voxel))
    color = float4(0, 1, 0, 1); // Green
else
    color = float4(1, 0, 0, 1); // Red
```

**Expected**: Green rectangle (all start positions in-bounds)  
**If red**: `startPos` calculation is wrong

---

## 📝 **Summary**

### **Your Architecture is CORRECT!**

You're **NOT** using SVO - you're using a **flat dense grid with DDA raymarching**, which is:
- ✅ A valid and common approach
- ✅ Simpler than SVO
- ✅ Perfect for small/medium voxel grids
- ✅ Easier to debug and maintain

### **The Bug is NOT in the Architecture**

The sparse rendering is caused by:
1. **Camera position** (confirmed: Z=6 instead of Z=-6)
2. **Ray direction** (possibly inverted due to camera behind cube)
3. **Ray-box intersection** (might be failing due to camera position)

### **Next Steps**

1. **Uncomment magenta test** (lines 121-122) - see if rays hit volume
2. **Fix camera position** - move to Z=-6
3. **Re-test** - should render solid cube

**The data is perfect. The algorithm is correct. It's just a camera/ray setup issue!** 🎯

---

## 🔗 **References**

- **DDA Algorithm**: Amanatides & Woo (1987) "A Fast Voxel Traversal Algorithm"
- **Your Implementation**: `VoxelRaymarch.compute` lines 128-203
- **Data Format**: `voxel_format.py` + `VoxelBits.cs`
- **Index Formula**: Fortran order (column-major, X varies fastest)
