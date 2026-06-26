# Architecture Migration: Texture3D → StructuredBuffer

**Date**: June 25, 2026, 11:48 PM  
**Issue**: Designer correctly identified we're treating voxel structs as image data  
**Impact**: Medium (requires shader + upload code changes, but cleaner architecture)

---

## 🎯 The Core Problem

### What We're Doing Wrong

```
Voxel Struct (ushort)
  ↓ Convert to float
Color(voxel, 0, 0, 0)
  ↓ Upload as image
Texture3D<float4>
  ↓ Read as float
float4 texData
  ↓ Round and cast
uint packed = (uint)round(texData.r)  ← Fighting the API
```

### What We Should Be Doing

```
Voxel Struct (ushort)
  ↓ Upload as array
StructuredBuffer<uint>
  ↓ Direct integer read
uint packed = _VoxelData[index]  ← Clean, direct access
```

---

## ✅ Benefits of StructuredBuffer

1. **No precision loss** - Direct integer storage, no float conversion
2. **No format hacks** - Not limited by Unity's Texture3D format support
3. **Conceptually correct** - Voxels are structs, not pixels
4. **Better performance** - No round() calls, direct memory access
5. **Future-proof** - Easy to expand to larger structs (32-bit, 64-bit, etc.)
6. **Cleaner code** - No workarounds for texture format limitations

---

## 📋 Migration Steps

### Step 1: Change Shader Binding

**File**: `VoxelRaymarch.compute`

**Before**:
```hlsl
Texture3D<float4> _VoxelVolume;

// In raymarch loop:
float4 texData = _VoxelVolume.Load(int4(voxel, 0));
uint packed = (uint)round(texData.r);
```

**After**:
```hlsl
StructuredBuffer<uint> _VoxelData;
int3 _VolumeDims;  // Already exists

// Helper function to convert 3D coords to 1D index
int VoxelIndex(int3 v)
{
    return v.x + v.y * _VolumeDims.x + v.z * _VolumeDims.x * _VolumeDims.y;
}

// In raymarch loop:
uint packed = _VoxelData[VoxelIndex(voxel)];  // Direct access!
```

---

### Step 2: Change CPU Upload Code

**File**: `PrototypeBootstrap.cs`

**Before**:
```csharp
// Create Texture3D
_voxelTexture = new Texture3D(volumeDims.x, volumeDims.y, volumeDims.z,
                              TextureFormat.RGBAHalf, mipChain: false);

// Pack into Color array
Color[] colors = new Color[_volume.Length];
for (int i = 0; i < _volume.Length; i++)
{
    colors[i] = new Color(_volume[i], 0, 0, 0);
}
_voxelTexture.SetPixels(colors);
_voxelTexture.Apply();
```

**After**:
```csharp
// Create ComputeBuffer
int totalVoxels = volumeDims.x * volumeDims.y * volumeDims.z;
_voxelBuffer = new ComputeBuffer(totalVoxels, sizeof(uint));

// Upload directly (NativeArray<ushort> → uint array)
uint[] voxelData = new uint[_volume.Length];
for (int i = 0; i < _volume.Length; i++)
{
    voxelData[i] = _volume[i];  // Direct copy, no conversion!
}
_voxelBuffer.SetData(voxelData);
```

---

### Step 3: Update Renderer Binding

**File**: `VoxelRenderer.cs`

**Before**:
```csharp
raymarchShader.SetTexture(_kernelIndex, "_VoxelVolume", voxelVolume);
```

**After**:
```csharp
raymarchShader.SetBuffer(_kernelIndex, "_VoxelData", voxelBuffer);
raymarchShader.SetInts("_VolumeDims", volumeDims.x, volumeDims.y, volumeDims.z);
```

---

### Step 4: Update Weapon Controller

**File**: `VoxelWeaponController.cs`

**Before**:
```csharp
private Texture3D voxelVolume;

void UpdateGPUTexture()
{
    Color[] colors = new Color[totalVoxels];
    for (int i = 0; i < totalVoxels; i++)
    {
        colors[i] = new Color(voxelData[i], 0, 0, 0);
    }
    voxelVolume.SetPixels(colors);
    voxelVolume.Apply();
}
```

**After**:
```csharp
private ComputeBuffer voxelBuffer;

void UpdateGPUBuffer()
{
    uint[] data = new uint[totalVoxels];
    for (int i = 0; i < totalVoxels; i++)
    {
        data[i] = voxelData[i];  // Direct copy
    }
    voxelBuffer.SetData(data);
}
```

---

## 🎨 Designer's Additional Suggestions

### 1. Expand Material System (Future)

**Current**:
```csharp
Material ID → Color (float4)
```

**Future**:
```csharp
struct MaterialProperties
{
    float4 baseColor;
    float4 emissive;
    float roughness;
    float metalness;
    float density;
    float penetrationResistance;
    float destructionResistance;
    int sparkEffectID;
    int debrisTypeID;
    int audioProfileID;
}

StructuredBuffer<MaterialProperties> _Materials;
```

---

### 2. Add Flag Bits (Future)

**Current Bit Layout**:
```
15..12 (4) Shape
11..9  (3) Rotation
8..0   (9) Material
```

**Suggested Layout**:
```
15     (1) Destructible flag
14     (1) Physics flag
13     (1) Light source flag
12     (1) Reserved
11..9  (3) Rotation
8..0   (9) Material
```

Or keep shape bits and use a separate flags field in a larger struct.

---

### 3. Shape-Based SDF Evaluation (Future)

**Designer's Vision**:
```hlsl
uint shape = VxShape(packed);

float sdf;
switch(shape)
{
    case 0: sdf = SDFCube(localPos); break;
    case 1: sdf = SDFWedge(localPos); break;
    case 2: sdf = SDFCylinder(localPos); break;
    case 3: sdf = SDFSphere(localPos); break;
    // ... more shapes
}

if (sdf < 0.0) // Inside shape
{
    // Hit! Shade this voxel
}
```

This turns each voxel into a **procedural primitive** rather than a cube.

---

## 📊 Memory Comparison

### Current (Texture3D + RGBAHalf)
```
8×8×8 voxels = 512 voxels
RGBAHalf = 8 bytes per voxel
Total: 512 × 8 = 4,096 bytes (4 KB)
```

### Proposed (StructuredBuffer<uint>)
```
8×8×8 voxels = 512 voxels
uint = 4 bytes per voxel (can store full 16-bit packed + padding)
Total: 512 × 4 = 2,048 bytes (2 KB)
```

**50% memory reduction!** (Or use ushort for 1 KB, even better)

---

## ⚠️ Potential Issues

### 1. Random Access Pattern
StructuredBuffer doesn't have 3D spatial caching like Texture3D.  
**Solution**: Modern GPUs handle this fine. If performance issues arise, we can add spatial sorting or use a hybrid approach.

### 2. Filtering/Interpolation
Texture3D has built-in filtering. StructuredBuffer doesn't.  
**Solution**: We're using `FilterMode.Point` anyway (no interpolation), so this doesn't matter.

### 3. Mipmap Support
Texture3D supports mipmaps. StructuredBuffer doesn't.  
**Solution**: We're not using mipmaps (`mipChain: false`), so this doesn't matter.

---

## 🎯 Recommendation

**YES, migrate to StructuredBuffer.**

### Why?
1. Designer is correct - we're treating structs as images
2. Eliminates all float conversion hacks
3. Cleaner, more maintainable code
4. Better memory efficiency
5. More flexible for future expansion
6. Conceptually aligned with what voxels actually are

### When?
**Now, while the codebase is small.** The longer we wait, the harder it becomes.

### Effort?
**Low-Medium**. Changes needed in 4 files:
- `VoxelRaymarch.compute` (shader binding)
- `PrototypeBootstrap.cs` (upload code)
- `VoxelRenderer.cs` (binding code)
- `VoxelWeaponController.cs` (update code)

---

## 🚀 Next Steps

1. **Backup current working state** (commit to git)
2. **Implement Step 1** (shader changes)
3. **Implement Step 2** (CPU upload)
4. **Implement Step 3** (renderer binding)
5. **Implement Step 4** (weapon controller)
6. **Test** (should see identical visual result, but cleaner code)
7. **Remove all float conversion code** (cleanup)
8. **Update documentation** (RECENT_CHANGES.md, DESIGNER_RESPONSE.md)

---

## 💡 Designer's Philosophy

> "You're treating the voxel grid as the **scene database**, and the renderer asks the world: 'What's at this location?' rather than 'Which triangles should I draw?'"

This is the key insight. Our architecture should reflect this:
- Voxels = **data structures** (not pixels)
- Renderer = **query engine** (not mesh generator)
- Materials = **property database** (not per-voxel duplication)
- Shapes = **procedural primitives** (not geometry)

**StructuredBuffer aligns perfectly with this philosophy.**

---

**Decision**: Migrate to StructuredBuffer?  
**Recommendation**: ✅ YES  
**Confidence**: HIGH (designer's reasoning is sound, benefits are clear)
