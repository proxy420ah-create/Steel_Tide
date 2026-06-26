# Shader Diagnostic Tests

**Issue**: Even with `mat = 3u` forced, rendering is still sparse  
**This means**: The bug is in ray traversal, NOT voxel sampling

---

## Test 1: Ray-Box Intersection Test

**Uncomment lines 121-122** in `VoxelRaymarch.compute`:
```hlsl
_Output[id.xy] = float4(1, 0, 1, 1); // Magenta = ray hit volume
return;
```

**Expected**: Solid magenta rectangle where the cube should be  
**If sparse magenta**: Ray-box intersection is broken  
**If solid magenta**: Ray-box works, bug is in DDA traversal

---

## Test 2: First Voxel Test

**Replace the DDA loop** (lines 140-180) with:
```hlsl
// Just render the first voxel we enter
int3 voxel = (int3)floor(startPos / _VoxelSize);
if (InBounds(voxel))
{
    color = float4(0, 1, 0, 1); // Green = in bounds
}
else
{
    color = float4(1, 0, 0, 1); // Red = out of bounds
}
_Output[id.xy] = color;
return;
```

**Expected**: Green rectangle (all rays start in-bounds)  
**If red or sparse**: `startPos` calculation is wrong

---

## Test 3: Camera Position Fix

**The camera is at the WRONG position!**

Current: `(4, 4, 6)` - Behind the cube  
Should be: `(4, 4, -6)` - In front of the cube

**In Unity**:
1. Select **Main Camera** in Hierarchy
2. In Inspector, set **Transform → Position**:
   - X: `4`
   - Y: `4`
   - Z: `-6` ← Change this!
3. Press Play

---

## Most Likely Issue

Looking at the camera position `(4, 4, 6)` and cube bounds `(0,0,0)` to `(4,4,4)`:

```
Camera at Z=6 (behind cube)
    ↓
    ↓ Looking at -Z direction
    ↓
Cube at Z=0 to Z=4
```

**The camera is looking AWAY from the cube!**

The ray direction is probably pointing in the wrong direction, so rays are:
1. Starting behind the cube
2. Pointing away from it
3. Never intersecting the volume

**Fix**: Move camera to Z = -6 (in front of cube, looking at +Z direction)

---

## Quick Test

**Try this first** before any shader changes:

1. **In Unity**: Select Main Camera
2. **Set Position**: (4, 4, -6)
3. **Set Rotation**: (0, 0, 0)
4. **Press Play**

If this fixes it, the issue was just camera placement! 🎯
