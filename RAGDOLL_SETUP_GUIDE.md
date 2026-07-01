# Voxel Skeleton Ragdoll - Setup Guide

**Goal**: Realistic ragdoll physics with voxel ground detection

---

## ⚠️ CRITICAL: How This Works

**Unity Physics CANNOT see VoxelWorld automatically!**

```
Unity Rigidbody → looks for Colliders → VoxelObject has NONE → falls forever
```

**VoxelSkeletonRagdoll script is the bridge**:

```
1. Unity physics makes bones fall (Rigidbody + gravity)
2. VoxelSkeletonRagdoll queries VoxelWorld.GetVoxel() each frame
3. Script manually corrects bone positions if below voxels
4. Result: Bones appear to "collide" with voxel ground
```

**This is NOT automatic collision** - it's manual position correction every frame!

---

## Quick Setup

### 1. **Prepare Your Scene**

1. Make sure you have a voxel ground object (e.g., `Ground_Voxel`)
2. Ground must be a `VoxelObject` (registered with VoxelWorld)
3. **CRITICAL**: VoxelObject has NO Unity collider - Rigidbodies will fall through it!
4. **VoxelSkeletonRagdoll script** bridges Unity physics ↔ VoxelWorld by:
   - Querying `VoxelWorld.GetVoxel()` each frame
   - Manually correcting bone positions when below voxels
   - This is NOT automatic - the script does the work!

### 2. **Setup Skeleton GameObject**

1. Create empty GameObject: `SkeletonRagdoll`
2. Add `VoxelObject` component:
   - **Asset File Name**: `Tpose.stasset`
   - **Voxel Size**: `0.125`
   - Click **"Load Dimensions from .stasset File"**
3. **REQUIRED**: Add `VoxelSkeletonRagdoll` component (this is the bridge!):
   - **Bone Material ID**: `12`
   - **Joint Material ID**: `21`
   - **Ground Check Distance**: `0.5`
   - **Ground Correction Force**: `10`
   - **Show Debug**: ✓ (checked)
   
   **What this script does**:
   - Creates Rigidbody + Collider for each bone (Unity physics)
   - Queries VoxelWorld.GetVoxel() in FixedUpdate
   - Manually pushes bones up when below voxels
   - **Without this script, bones fall through ground!**

### 3. **Press Play**

**What should happen**:
1. Script scans voxel data for bone/joint voxels
2. Creates bone segment GameObjects with Rigidbody + Collider
3. Connects segments with ConfigurableJoint
4. Skeleton falls with Unity physics
5. Raycasts detect voxel ground
6. Bones pushed up when below ground
7. **Skeleton settles on ground realistically!**

---

## How It Works

### **Physics Stack**

```
VoxelSkeletonRagdoll (Master Controller)
├─ Scans VoxelObject for bone/joint voxels
├─ Creates bone segment GameObjects
└─ Runs voxel ground detection in FixedUpdate

Bone Segments (Created at runtime)
├─ Rigidbody (Unity physics - gravity, forces)
├─ CapsuleCollider (collision detection)
└─ ConfigurableJoint (connects to parent bone)
```

### **Ground Detection Logic**

```csharp
FixedUpdate() {
    foreach (bone in boneSegments) {
        // Sample VoxelWorld below bone
        byte material = VoxelWorld.Instance.GetVoxel(bone.position + Vector3.down);
        
        if (material != 0) {
            // Solid voxel detected!
            if (bone.y < voxelHeight) {
                // Push bone up
                bone.AddForce(Vector3.up * force);
                bone.position.y = voxelHeight;
            }
        }
        // else: Air voxel, bone falls naturally
    }
}
```

---

## Expected Behavior

### **Scenario 1: Spawn in Air**
```
Frame 0: Skeleton spawns at Y=5
├─ Unity gravity pulls bones down
├─ Joints keep bones connected
└─ Realistic tumbling/falling

Frame 30: Bones reach ground (Y=0)
├─ Raycasts detect voxel ground
├─ Bones below ground pushed up
└─ Skeleton settles in pile
```

### **Scenario 2: Shoot Floor**
```
Frame 0: Skeleton standing on floor
├─ Raycasts detect ground
└─ Bones stable

Frame 10: Floor voxels destroyed
├─ Raycasts miss (no ground)
├─ Gravity takes over
└─ Skeleton falls through hole!

Frame 50: Hits lower floor
├─ Raycasts detect new ground
└─ Skeleton settles
```

### **Scenario 3: Walk Off Edge**
```
Frame 0: On platform edge
├─ Some bones: raycasts HIT
├─ Some bones: raycasts MISS
└─ Skeleton tips over

Frame 20: Falls off
├─ All raycasts MISS
└─ Realistic tumbling fall
```

---

## Console Output

**Successful setup**:
```
[VoxelSkeletonRagdoll] Building ragdoll from voxel data...
[VoxelSkeletonRagdoll] Found 56 bone voxels, 10 joint voxels
[VoxelSkeletonRagdoll] Ragdoll created with 8 bone segments
```

**No bones found**:
```
[VoxelSkeletonRagdoll] No bone voxels found! Make sure skeleton uses material ID 12
```

---

## Debug Visualization

With **Show Debug** enabled:

- **Green rays** = Bone detected ground (stable)
- **Red rays** = Bone in air (falling)
- **White spheres** = Bone segment centers
- **Yellow lines** = Joint connections

---

## Troubleshooting

### **Skeleton falls through floor**

**Cause**: Ground not registered with VoxelWorld

**Fix**: 
1. Check ground GameObject has `VoxelObject` component
2. Verify VoxelWorld singleton exists in scene
3. Check console for "Registered with VoxelWorld" message

### **Skeleton explodes/flies apart**

**Cause**: Joint forces too high or mass too low

**Fix**:
- Reduce `Ground Correction Force` (try 5 instead of 10)
- Increase `Bone Segment Mass` (try 2.0 instead of 1.0)
- Increase `Joint Spring` (try 200)

### **Skeleton doesn't collapse realistically**

**Cause**: Joint limits too tight

**Fix**:
- Increase `Max Joint Angle` (try 90 instead of 45)
- Reduce `Joint Damper` (try 5 instead of 10)

### **No bone segments created**

**Cause**: Skeleton voxels not using correct material IDs

**Fix**:
1. Check `Tpose.stasset` in Voxel Studio
2. Verify bones use material 12 (white/beige)
3. Verify joints use material 21 (red)

---

## Performance

**Typical skeleton** (10 bone segments):
- 10 Rigidbodies (Unity physics)
- 10 CapsuleColliders
- 9 ConfigurableJoints
- 10 raycasts per FixedUpdate (50 Hz)

**Total**: ~500 raycasts/second (negligible CPU cost)

---

## Next Steps

Once ragdoll works:

1. **Add balance controller** - Make skeleton stand up
2. **Add IK solver** - Procedural leg placement
3. **Add animation blending** - Smooth transitions
4. **Add damage response** - Limbs detach when destroyed

---

## Comparison: Old vs New

### **Old Approach (VoxelSkeletonPhysics)**
- Manual gravity
- Manual ground detection
- No realistic collapse
- Falls as single rigid object

### **New Approach (VoxelSkeletonRagdoll)**
- Unity physics (gravity, forces)
- Voxel-aware ground detection
- Realistic ragdoll collapse
- Each bone independent

**Result**: Best of both worlds! 🎉
