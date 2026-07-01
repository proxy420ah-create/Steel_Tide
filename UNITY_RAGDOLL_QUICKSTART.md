# Unity Ragdoll Physics - Quick Start

**Goal**: Make the T-Pose skeleton collapse with ragdoll physics

---

## Setup Steps

### 1. **Create VoxelObject with Skeleton**

1. In Unity Hierarchy, create: `GameObject` → `Create Empty`
2. Name it: `SkeletonRagdoll`
3. Add component: `VoxelObject` (your existing voxel renderer)
4. In VoxelObject Inspector, set:
   - **Asset File Name**: `Tpose.stasset`
   - **Voxel Size**: `0.125`
   - Click **"Load Dimensions from .stasset File"** button

### 2. **Add Physics Component**

1. With `SkeletonRagdoll` selected, add component: `VoxelSkeletonPhysics`
2. In Inspector, set:
   - **Enable Physics On Start**: ✓ (checked)
   - **Total Mass**: `10.0`
   - **Drag**: `0.5`
   - **Angular Drag**: `0.5`
   - **Bone Material ID**: `12`
   - **Joint Material ID**: `21`

### 3. **Add Ground Plane**

1. Create: `GameObject` → `3D Object` → `Plane`
2. Position it below skeleton: `Y = -1`
3. Scale it up: `Scale = (10, 1, 10)`

### 4. **Press Play**

**What should happen**:
- VoxelObject renders the T-Pose skeleton (white bones, red joints)
- VoxelSkeletonPhysics adds Rigidbody + MeshCollider
- Gravity is applied
- **Voxel skeleton collapses and falls to ground** 🎉

---

## What You'll See

```
Frame 0: T-Pose standing
Frame 1: Gravity applied
Frame 2-10: Skeleton starts collapsing
Frame 10-30: Ragdoll falls to ground
Frame 30+: Skeleton settles on ground
```

---

## Troubleshooting

### **Skeleton explodes/flies apart**
- **Cause**: Joint spring too high or mass too low
- **Fix**: Reduce `jointSpring` to 50, increase `boneSegmentMass` to 2.0

### **Skeleton doesn't fall**
- **Cause**: Gravity disabled
- **Fix**: Check Rigidbody `useGravity` is true

### **Skeleton falls through floor**
- **Cause**: No ground collider
- **Fix**: Add plane: `GameObject` → `3D Object` → `Plane`

### **Bones disconnect**
- **Cause**: Joint connections broken
- **Fix**: Check console for joint connection logs

---

## Next Steps

Once ragdoll works:

1. **Add ground plane** for skeleton to land on
2. **Adjust physics** (mass, spring, damper) for realistic collapse
3. **Add balance controller** to make it stand up
4. **Implement IK solver** for procedural animation

---

## Expected Console Output

```
[VoxelSkeletonLoader] Loading Tpose.stasset: 18×32×8
[VoxelSkeletonLoader] Loaded 56 bone voxels, 10 joint voxels
[VoxelSkeletonLoader] Created 56 bone objects, 10 joint objects
[VoxelSkeletonLoader] Added physics components. Press Play to see ragdoll!
```

---

## Physics Settings Explained

- **Mass**: Heavier = falls faster, harder to move
- **Drag**: Higher = slower falling (air resistance)
- **Angular Drag**: Higher = less spinning
- **Joint Spring**: Higher = stiffer joints (less floppy)
- **Joint Damper**: Higher = less oscillation (more stable)

**Realistic Settings**:
- Mass: 1.0-2.0 (human-like)
- Drag: 0.5-1.0
- Angular Drag: 0.5-1.0
- Joint Spring: 50-200
- Joint Damper: 10-50

---

## Ready to Test!

1. Save scene
2. Press Play
3. Watch skeleton collapse
4. Adjust settings in Inspector (while playing)
5. Find sweet spot for realistic ragdoll

**Goal**: Skeleton should collapse naturally, not explode or freeze.
