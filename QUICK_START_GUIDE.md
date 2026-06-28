# 🚀 Quick Start Guide - Steel Tide

**Get your voxel character running in Unity in 30 minutes!**

---

## 📋 **Prerequisites**

- ✅ Voxel Asset Studio working
- ✅ Unity project open
- ✅ VoxelRenderer on Main Camera
- ✅ Materials synchronized (just completed!)

---

## 🎯 **Part 1: Generate Assets** (10 minutes)

### **Step 1: Open Voxel Studio**
```bash
cd "c:\Users\NADECC\ATSTradingDashboard Project\Cursor Workshop\SteelTide"
.\VoxelStudio.bat
```

### **Step 2: Generate Ground Plane**
1. Menu → **Generate** → **🟫 Terrain: Ground Plane (128×2×128, 16m×0.25m)**
2. Wait for generation (instant)
3. Menu → **File** → **Save As**
   - Name: `Ground_Stone.stasset`
   - Location: `My project\Assets\StreamingAssets\`
   - Click **Save**

### **Step 3: Generate Player Character**
1. Menu → **Generate** → **🧍 Character: Humanoid (6×16×4, 2m tall)**
2. Wait for generation (instant)
3. You should see:
   - Dark green body (Uniform)
   - Pink head (Flesh)
   - Pink arms/legs (Flesh)
4. Menu → **File** → **Save As**
   - Name: `Player_Humanoid.stasset`
   - Location: `My project\Assets\StreamingAssets\`
   - Click **Save**

### **Step 4: Close Voxel Studio**
- Close the window
- Assets are now ready!

---

## 🎮 **Part 2: Setup Unity Scene** (20 minutes)

### **Step 5: Create Ground Plane**

1. **Create GameObject**:
   - Hierarchy → Right-click → **Create Empty**
   - Name: `Ground`
   - Position: `(0, 0, 0)`

2. **Add VoxelObject Component**:
   - Select `Ground`
   - Inspector → **Add Component** → Search: `VoxelObject`
   - Settings:
     - **Asset File Name**: `Ground_Stone.stasset`
     - **Voxel Size**: `0.125`
     - **Show Gizmo**: ✓ (checked)

3. **Add Box Collider**:
   - Inspector → **Add Component** → Search: `Box Collider`
   - Settings:
     - **Size**: X=`16`, Y=`0.25`, Z=`16`
     - **Center**: X=`8`, Y=`0.125`, Z=`8`

4. **Verify**:
   - You should see a cyan wireframe box (Gizmo)
   - Size should be 16m × 0.25m × 16m

---

### **Step 6: Create Player Character**

#### **A. Create Player Root**
1. **Create GameObject**:
   - Hierarchy → Right-click → **Create Empty**
   - Name: `Player`
   - Position: `(0, 0.5, 0)` ← On ground surface

2. **Add CharacterController**:
   - Select `Player`
   - Inspector → **Add Component** → Search: `Character Controller`
   - Settings:
     - **Height**: `1.8`
     - **Radius**: `0.4`
     - **Center**: X=`0`, Y=`0.9`, Z=`0`

#### **B. Create Player Camera**
1. **Create Child GameObject**:
   - Right-click `Player` → **Create Empty**
   - Name: `PlayerCamera`
   - Position: `(0, 1.6, 0)` ← Eye level

2. **Add Camera Component**:
   - Select `PlayerCamera`
   - Inspector → **Add Component** → Search: `Camera`
   - Settings:
     - **Tag**: `MainCamera` (important!)
     - **Clear Flags**: Skybox
     - **Field of View**: 60

3. **Add Weapon Scripts**:
   - Inspector → **Add Component** → Search: `VoxelWeaponController`
   - Settings:
     - **Volume Offset**: `(0, 0, 0)` (leave default)
     - **Volume Dims**: `(128, 128, 128)` (leave default)
     - **Voxel Size**: `0.125`
     - **Damage Radius**: `1`
   
   - Inspector → **Add Component** → Search: `PlayerWeapon`
   - Settings:
     - **Fire Rate**: `0.2`
     - **Damage Radius**: `1`
     - **Show Crosshair**: ✓ (checked)
     - **Crosshair Color**: White

#### **C. Create Player Model**
1. **Create Child GameObject**:
   - Right-click `Player` → **Create Empty**
   - Name: `PlayerModel`
   - Position: `(0, 0, 0)`

2. **Add VoxelObject Component**:
   - Select `PlayerModel`
   - Inspector → **Add Component** → Search: `VoxelObject`
   - Settings:
     - **Asset File Name**: `Player_Humanoid.stasset`
     - **Voxel Size**: `0.125`
     - **Show Gizmo**: ✓ (checked)

#### **D. Add Player Controller**
1. **Select Player Root**:
   - Click on `Player` (root object)

2. **Add VoxelPlayerController**:
   - Inspector → **Add Component** → Search: `VoxelPlayerController`
   - Settings:
     - **Walk Speed**: `5`
     - **Run Speed**: `8`
     - **Jump Force**: `7`
     - **Gravity**: `20`
     - **Mouse Sensitivity**: `2`
     - **Max Look Angle**: `80`
     - **Camera Transform**: Drag `PlayerCamera` here ← IMPORTANT!
     - **Player Radius**: `0.4`
     - **Player Height**: `1.8`
     - **Voxel Layer**: `Default` (or create "Voxel" layer)
     - **Raycast Samples**: `8`
     - **Ground Check Distance**: `0.1`
     - **Show Debug Rays**: ✓ (checked for testing)

---

### **Step 7: Setup Existing Buildings for Collision**

For each existing VoxelObject (Cube_test, Sphere_test, etc.):

1. **Select the GameObject**
2. **Move to proper height**:
   - Position Y: `0.25` ← Elevated by ground height
3. **Add Box Collider**:
   - Inspector → **Add Component** → Search: `Box Collider`
   - Adjust **Size** to match the cyan Gizmo wireframe
   - Example for 32×32×32 cube:
     - Size: `(4, 4, 4)` (32 voxels × 0.125m = 4m)
     - Center: `(2, 2, 2)` (half the size)

---

## ✅ **Part 3: Test!** (5 minutes)

### **Step 8: Enter Play Mode**

1. **Click Play button** (top center)
2. **You should see**:
   - Ground plane (gray stone)
   - Player model (dark green body, pink head/limbs)
   - Buildings rendered
   - Crosshair in center of screen

### **Step 9: Test Controls**

**Movement**:
- `W` - Move forward ✅
- `S` - Move backward ✅
- `A` - Strafe left ✅
- `D` - Strafe right ✅
- `Mouse` - Look around ✅
- `Space` - Jump ✅
- `Left Shift` - Run (hold) ✅

**Combat**:
- `Left Click` - Shoot ✅
- Aim at buildings and shoot - should see craters! ✅

**Collision**:
- Walk into buildings - should collide (can't pass through) ✅
- Walk on ground - should not fall through ✅
- Jump on buildings - should land on top ✅

### **Step 10: Verify Everything Works**

- [ ] Player spawns on ground (not floating or falling)
- [ ] WASD movement works smoothly
- [ ] Mouse look works (first-person view)
- [ ] Jump works (Space bar)
- [ ] Run works (Shift + WASD)
- [ ] Collides with buildings (can't walk through)
- [ ] Walks on ground (doesn't fall through)
- [ ] Crosshair visible
- [ ] Shooting works (Left Click)
- [ ] Buildings take damage (craters appear)
- [ ] Player model renders (dark green + pink)
- [ ] Ground renders (gray stone)

---

## 🐛 **Troubleshooting**

### **Problem: Player falls through ground**
**Solution**:
- Check Ground has Box Collider
- Verify Player position Y = 0.5 (above ground)
- Check CharacterController is on Player root

### **Problem: Can't see player model**
**Solution**:
- PlayerModel might be behind camera
- Try moving PlayerModel position to `(0, -0.9, 0.5)` to see feet
- Or add a 3rd person camera for testing

### **Problem: Mouse look not working**
**Solution**:
- Check Camera Transform is assigned in VoxelPlayerController
- Press Escape to lock/unlock cursor
- Verify Mouse Sensitivity isn't 0

### **Problem: Weapon not firing**
**Solution**:
- Check PlayerWeapon is on PlayerCamera (not Player root)
- Verify VoxelWeaponController is on same object
- Check console for errors

### **Problem: No collision with buildings**
**Solution**:
- Buildings need Box Colliders
- Verify Voxel Layer matches in VoxelPlayerController
- Check building positions (Y should be 0.25)

### **Problem: Buildings bleed through each other**
**Solution**:
- Space buildings at least 8m apart
- All buildings should be at Y = 0.25
- Ground should be at Y = 0

---

## 🎯 **Final Scene Hierarchy**

```
Scene
├── Main Camera (with VoxelRenderer)
├── Directional Light
├── Ground
│   ├── VoxelObject (Ground_Stone.stasset)
│   └── Box Collider
├── Player
│   ├── CharacterController
│   ├── VoxelPlayerController
│   ├── PlayerCamera
│   │   ├── Camera (MainCamera tag)
│   │   ├── VoxelWeaponController
│   │   └── PlayerWeapon
│   └── PlayerModel
│       └── VoxelObject (Player_Humanoid.stasset)
├── Cube_test (Y=0.25)
│   ├── VoxelObject
│   └── Box Collider
├── Sphere_test (Y=0.25)
│   ├── VoxelObject
│   └── Box Collider
└── (other buildings at Y=0.25)
```

---

## 🎮 **Controls Reference**

| Input | Action |
|-------|--------|
| **W** | Move forward |
| **S** | Move backward |
| **A** | Strafe left |
| **D** | Strafe right |
| **Mouse** | Look around |
| **Space** | Jump |
| **Left Shift** | Run (hold) |
| **Left Click** | Shoot |
| **Escape** | Toggle cursor lock |

---

## 🎉 **Success!**

If everything works, you now have:
- ✅ Playable voxel character
- ✅ First-person movement and controls
- ✅ Voxel-precise collision detection
- ✅ Working weapon system
- ✅ Ground plane for terrain
- ✅ Buildings you can shoot and destroy

**Time to build your town!** 🏘️✨

---

## 📝 **Next Steps**

1. **Generate more buildings** (L-House, Simple House, 2-Story)
2. **Layout a town** (8m grid spacing)
3. **Test gameplay** (walk around, shoot things)
4. **Generate enemy mech** (Combat Mech asset)
5. **Add basic AI** (next session)

**Total setup time: ~30 minutes** 🚀

---

**Need help?** Check the troubleshooting section or review:
- `PLAYER_CHARACTER_SETUP.md` - Detailed player setup
- `ASSET_GENERATION_GUIDE.md` - Complete asset workflow
- `MATERIAL_SYSTEM.md` - Material reference
