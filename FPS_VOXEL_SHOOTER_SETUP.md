# 🎮 FPS Voxel Shooter Setup Guide

## What You're Building

A first-person shooter where you can:
- ✅ Walk around with WASD
- ✅ Look with mouse
- ✅ Shoot voxels with left-click
- ✅ See gradient coloring (gray shell → orange core)
- ✅ Destroy one voxel per shot

---

## 🚀 Quick Setup (5 Steps)

### **Step 1: Create FPS Player**

1. In Unity Hierarchy, **Right-click** → **3D Object** → **Capsule**
2. Rename it to **"FPS Player"**
3. Set Position: `(0, 1, -10)` (10 units back from cube)
4. Add Component: **Character Controller**
5. Add Component: **Simple FPS Controller** (script)
6. Add Component: **FPS Voxel Shooter** (script)

### **Step 2: Create Player Camera**

1. **Right-click** on "FPS Player" → **Camera**
2. Rename to **"Player Camera"**
3. Set Local Position: `(0, 0.6, 0)` (eye height)
4. Set Local Rotation: `(0, 0, 0)`
5. Tag it as **"MainCamera"**
6. Add Component: **Voxel Renderer** (copy from old Main Camera - see Step 3)
7. Add Component: **Create Crosshair UI** (for proper crosshair rendering)

### **Step 3: Move VoxelRenderer to Player Camera**

1. Select the old **"Main Camera"** in Hierarchy
2. In Inspector, find **Voxel Renderer** component
3. Right-click **Voxel Renderer** → **Copy Component**
4. Select **"Player Camera"** in Hierarchy
5. Right-click in Inspector → **Paste Component As New**
6. Go back to old **"Main Camera"**
7. Right-click **Voxel Renderer** → **Remove Component**

### **Step 4: Disable Old Camera**

1. Find the old **"Main Camera"** in Hierarchy
2. **Uncheck** it to disable (or delete it)
   - Keep it disabled - it still has VoxelWeaponController which the Voxel Proxy needs

### **Step 5: Setup Voxel Proxy Collider**

1. In Hierarchy, **Right-click** → **3D Object** → **Cube**
2. Rename to **"Voxel Proxy"**
3. Set Position: `(0, 0, 0)` (same as voxel volume offset)
4. **Remove** the Mesh Renderer (we don't want it visible!)
5. Keep the **Box Collider**
6. Add Component: **Voxel Volume Proxy** (script)
7. In Inspector:
   - **Volume Dims**: `(8, 8, 8)`
   - **Voxel Size**: `112.4` (match VoxelRenderer settings)
   - **Weapon Controller**: Drag the old (disabled) Main Camera's VoxelWeaponController here

### **Step 6: Wire Up FPS Shooter**

1. Select **"FPS Player"** in Hierarchy
2. Find **FPS Voxel Shooter** component
3. Set:
   - **FPS Camera**: Drag "Player Camera" here
   - **Weapon Controller**: Drag the old (disabled) Main Camera's VoxelWeaponController here
   - **Fire Rate**: `0.2` (5 shots per second)

---

## 🎯 Testing

1. **Press Play** ▶
2. **WASD** to move around
3. **Mouse** to look
4. **Left-click** to shoot voxels
5. **Watch** the cube reveal its orange core as you destroy the gray shell!

---

## 🎨 Gradient Visualization

The shader now automatically applies a gradient to concrete voxels:

```
Edge voxels (shell):  Gray   (0.6, 0.6, 0.55)
                       ↓
Center voxels (core): Orange (1.0, 0.5, 0.0)
```

As you shoot away the outer gray shell, you'll see the orange core inside!

---

## ⚙️ Settings You Can Tweak

### **FPS Controller (SimpleFPSController):**
- **Walk Speed**: `5` (units/sec)
- **Run Speed**: `10` (hold Shift to run)
- **Jump Force**: `7`
- **Mouse Sensitivity**: `2`

### **Shooter (FPSVoxelShooter):**
- **Fire Rate**: `0.2` (seconds between shots)
- **Range**: `100` (max shooting distance)
- **Crosshair Size**: `20` (pixels)

### **Voxel Volume:**
- **Voxel Size**: Smaller = more detail, larger = easier to see
- **Volume Dims**: Size of the voxel grid

---

## 🐛 Troubleshooting

### **"Can't shoot voxels!"**
- Check that Voxel Proxy has a **Box Collider**
- Verify **Weapon Controller** is assigned in both scripts
- Make sure **Player Camera** is tagged as MainCamera

### **"Cursor won't lock!"**
- Press **left-click** to re-lock cursor
- Press **ESC** to unlock cursor

### **"No gradient visible!"**
- Make sure you're using **concrete material** (ID 3)
- Shoot away outer voxels to see the orange core
- Check that shader recompiled (look for errors in Console)

### **"Player falls through floor!"**
- Add a **Plane** or **Cube** below the player with a collider
- Or set Player Y position higher

---

## 🚀 Next Steps

Once this works, you can:
1. **Add explosion radius** (destroy multiple voxels per shot)
2. **Add particle effects** on impact
3. **Add sound effects** for shooting
4. **Create different voxel materials** with different gradients
5. **Build actual game levels** with voxel structures

---

## 📝 Technical Details

### **How It Works:**

```
Player Left-Click
    ↓
FPSVoxelShooter.Fire()
    ↓
Physics.Raycast (hits Voxel Proxy collider)
    ↓
VoxelVolumeProxy.DestroyVoxel(coords)
    ↓
VoxelWeaponController.DestroyVoxelAt(index)
    ↓
Set voxelData[index] = 0 (air)
    ↓
Upload to GPU ComputeBuffer
    ↓
Shader sees air, skips that voxel
    ↓
Ray continues to next voxel behind it!
```

### **Coordinate Conversion:**

```csharp
// World space → Local space
Vector3 localHit = proxy.transform.InverseTransformPoint(hit.point);

// Local space → Voxel grid coordinates
int3 voxelCoords = new int3(
    Mathf.FloorToInt(localHit.x / voxelSize),
    Mathf.FloorToInt(localHit.y / voxelSize),
    Mathf.FloorToInt(localHit.z / voxelSize)
);

// 3D coords → 1D buffer index
int index = x + y * dimX + z * dimX * dimY;
```

---

## ✅ Success Criteria

You'll know it's working when:
- ✅ You can walk around the cube
- ✅ Crosshair appears at screen center
- ✅ Left-click destroys individual voxels
- ✅ Destroyed voxels reveal the next layer behind them
- ✅ Gray outer shell gradually reveals orange core
- ✅ Console shows "Destroyed voxel at coords (x,y,z)"

**Have fun blasting voxels!** 🎯🔫
