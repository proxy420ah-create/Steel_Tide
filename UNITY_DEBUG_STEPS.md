# Unity Debugging Steps - Sparse Rendering Issue

**Date**: June 26, 2026, 1:20 AM  
**Status**: Data confirmed perfect in Python, issue is in Unity rendering

---

## ✅ What We Know

1. **Python data is PERFECT**: 512/512 solid voxels, all material ID 3 (Concrete)
2. **Axis ordering is CORRECT**: Fortran order (X varies fastest)
3. **C# reads correctly**: Console shows `512/512 solid voxels (100.0%)`
4. **Unity renders SPARSE**: Only partial cube visible (see screenshot)

---

## 🔍 Debugging Steps (Follow in Order)

### Step 1: Force Unity to Reload Asset

1. **In Unity**: Assets → Reimport All
2. **Wait** for compilation to finish
3. **Press Play** ▶

### Step 2: Check New Diagnostic Logs

After pressing Play, look for these new logs in the console:

```
[SteelTide] Buffer contains 512/512 non-zero values
[SteelTide] Sample values: [0]=3, [256]=3, [511]=3
```

**Expected**: All values should be `3` (Concrete material ID)  
**If different**: Buffer upload is corrupted

### Step 3: Check Shader Parameters

Look for:
```
[VoxelRenderer] SHADER PARAMS:
  VolumeDims: 8x8x8
  VoxelSize: 0.5
  VolumeWorldBounds: (0,0,0) to (4,4,4)
  CameraPos: (4, 4, -6)
  Expected cube center: (2,2,2)
```

**Camera should be**: Outside the volume, looking at it  
**If camera is at (0,0,0)**: Camera is inside the cube!

### Step 4: Test Shader Override (If Still Broken)

1. **Open**: `VoxelRaymarch.compute`
2. **Find line 147**: `// mat = 3u; // Force concrete material for all voxels`
3. **Uncomment it**: `mat = 3u; // Force concrete material for all voxels`
4. **Save and Play**

**If this renders a solid cube**: The issue is in voxel sampling/indexing  
**If still sparse**: The issue is in ray-box intersection or DDA traversal

---

## 🎯 Most Likely Causes (Designer's Analysis)

### Cause 1: Buffer Stride Mismatch ❓
```csharp
// Current: sizeof(uint) = 4 bytes
ComputeBuffer buffer = new ComputeBuffer(totalVoxels, sizeof(uint));

// Voxel data: ushort = 2 bytes
// Stored as: uint = 4 bytes
```

**This is inefficient but shouldn't cause sparse rendering** unless there's padding/alignment issues.

### Cause 2: Index Formula Mismatch ❓
```hlsl
// Shader: int VoxelIndex(int3 v)
return v.x + v.y * _VolumeDims.x + v.z * _VolumeDims.x * _VolumeDims.y;
```

**Should match Python**: Fortran order (X varies fastest) ✅

### Cause 3: Ray-Box Intersection Bug ❓
```hlsl
// If rays are missing the volume or starting at wrong positions
if (!RayAABBIntersection(ro, rd, volumeMin, volumeMax, tNear, tFar))
```

**Test**: Shader override (Step 4) will isolate this

### Cause 4: DDA Stepping Bug ❓
```hlsl
// If DDA is skipping voxels during traversal
int3 step = (int3)sign(rd);
float3 tDelta = invDir * _VoxelSize;
```

**Test**: Shader override (Step 4) will isolate this

---

## 📊 Expected Console Output (After Fixes)

```
[SteelTide] Loaded 'test_cube.stasset': int3(8, 8, 8) grid, 512/512 solid voxels (100.0%)
[SteelTide] Created ComputeBuffer: 512 voxels (2048 bytes)
[SteelTide] Buffer contains 512/512 non-zero values
[SteelTide] Sample values: [0]=3, [256]=3, [511]=3
[SteelTide] VoxelRenderer found, assigning buffer directly...
[VoxelRenderer] Rendering frame with buffer: 8x8x8 voxels
[VoxelRenderer] SHADER PARAMS:
  VolumeDims: 8x8x8
  VoxelSize: 0.5
  VolumeWorldBounds: (0,0,0) to (4,4,4)
  CameraPos: (4, 4, -6)
  VolumeOffset: (0, 0, 0)
  CameraOrigin (shader): (4, 4, -6)
  Expected cube center: (2,2,2)
```

---

## 🎮 Expected Visual Result

**After all fixes**, you should see:
- ✅ Solid 8×8×8 cube (like the Python visualization)
- ✅ Tan/gray color (Concrete material)
- ✅ Clean flat-shaded faces
- ✅ No holes, no gaps, no noise
- ✅ 60+ FPS

---

## 📝 Next Steps

1. **Reimport All** in Unity
2. **Press Play**
3. **Copy console logs** and share them
4. **Try shader override** if still broken
5. **Report results**

The data is perfect - we just need to find where Unity's pipeline is dropping voxels! 🎯
