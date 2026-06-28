# 🔧 Voxel Physics Scripts - Status & Creation Guide

**Date**: June 28, 2026  
**Purpose**: Clarify which scripts exist vs. need to be created for voxel physics

---

## ✅ **Scripts You Already Have**

### **VoxelObject.cs**
- **Location**: Already in Unity project
- **Purpose**: Renders `.stasset` files as voxel meshes
- **Status**: ✅ Working (you're using this!)
- **Usage**: Add to GameObject → Set Asset File Name → Press Play

### **VoxelRenderer.cs**
- **Location**: Already in Unity project
- **Purpose**: Generates mesh geometry from voxel data
- **Status**: ✅ Working (used by VoxelObject)
- **Usage**: Automatic (VoxelObject uses it internally)

---

## ❌ **Scripts You Need to Create (Phase 2)**

### **VoxelWorld.cs**
- **Status**: ❌ Does NOT exist yet
- **Purpose**: Manages all voxel data for physics/collision
- **When**: Create when ready for voxel physics
- **Code**: Provided in `VOXEL_SCENE_SETUP_GUIDE.md` Phase 3, Step 1

**What it does:**
- Stores all voxel data in Dictionary (memory-efficient)
- Provides `Raymarch()` function for collision detection
- Converts world coords ↔ voxel grid coords
- Handles voxel get/set operations

### **VoxelPhysics.cs**
- **Status**: ❌ Does NOT exist yet
- **Purpose**: Player movement with raymarching gravity
- **When**: Create after VoxelWorld.cs
- **Code**: Provided in `VOXEL_SCENE_SETUP_GUIDE.md` Phase 3, Step 2

**What it does:**
- Gravity (raycasts DOWN to find ground)
- WASD movement with collision
- Jump mechanics
- Mouse look (independent of physics)
- Wall sliding

### **VoxelModifier.cs**
- **Status**: ❌ Does NOT exist yet
- **Purpose**: Voxel destruction system (shooting/explosions)
- **When**: Create after VoxelPhysics.cs
- **Code**: Provided in `VOXEL_SCENE_SETUP_GUIDE.md` Phase 3, Step 3

**What it does:**
- Left-click to destroy voxels
- Right-click to place voxels
- Crosshair + target highlight
- Radius destruction (explosions)

---

## 📋 **Creation Workflow**

### **Step 1: Create the Scripts**

**For each script (VoxelWorld, VoxelPhysics, VoxelModifier):**

1. **In Unity Project panel:**
   ```
   Assets → Scripts → Right-click → Create → C# Script
   Name: VoxelWorld (or VoxelPhysics, or VoxelModifier)
   ```

2. **Open the script:**
   ```
   Double-click the new script file
   Opens in Visual Studio / VS Code / Rider
   ```

3. **Replace default code:**
   ```
   Delete everything in the file
   Copy code from VOXEL_SCENE_SETUP_GUIDE.md
   Paste into the script
   Save (Ctrl+S)
   ```

4. **Return to Unity:**
   ```
   Unity will auto-compile
   Check Console for errors
   ```

### **Step 2: Use the Scripts**

**After all 3 scripts are created:**

1. **Create VoxelWorld Manager:**
   ```
   Hierarchy → Create Empty → Name: VoxelWorldManager
   Add Component → VoxelWorld (now available!)
   Set voxelSize = 0.125
   ```

2. **Add to Voxel Player:**
   ```
   Select VoxelPlayer GameObject
   Add Component → VoxelPhysics
   Add Component → VoxelModifier
   Assign references in Inspector
   ```

3. **Test:**
   ```
   Press Play
   Player should fall and land on voxel ground
   WASD to move, Space to jump
   Left-click to destroy voxels
   ```

---

## 🎯 **Current Status Summary**

| Script | Exists? | Purpose | When to Create |
|--------|---------|---------|----------------|
| VoxelObject.cs | ✅ Yes | Render voxel models | Already done |
| VoxelRenderer.cs | ✅ Yes | Generate meshes | Already done |
| VoxelWorld.cs | ❌ No | Physics data manager | Phase 2 |
| VoxelPhysics.cs | ❌ No | Player physics | Phase 2 |
| VoxelModifier.cs | ❌ No | Destruction system | Phase 2 |

---

## 📖 **Where to Find the Code**

**All script code is in:** `VOXEL_SCENE_SETUP_GUIDE.md`

- **VoxelWorld.cs**: Phase 3 → Step 1
- **VoxelPhysics.cs**: Phase 3 → Step 2  
- **VoxelModifier.cs**: Phase 3 → Step 3

**Each section has:**
- ✅ Complete C# code (copy-paste ready)
- ✅ Explanation of what it does
- ✅ Instructions for setup

---

## ⚠️ **Important Notes**

### **Don't Create Scripts Yet If:**
- You haven't created voxel ground plane
- You haven't tested voxel rendering
- You're still in Phase 1 (mesh ground + mesh player)

### **Create Scripts When:**
- ✅ Voxel ground plane is rendering
- ✅ Voxel player model is rendering
- ✅ You're ready to add physics
- ✅ You want player to stand on voxel ground

### **Dependencies:**
```
VoxelWorld.cs (create first)
    ↓
VoxelPhysics.cs (needs VoxelWorld)
    ↓
VoxelModifier.cs (needs VoxelWorld)
```

Create in this order!

---

## 🚀 **Quick Reference**

**"I want to add voxel physics NOW!"**

1. Create voxel ground plane in Voxel Studio (128×4×128)
2. Load in Unity as VoxelObject
3. Create VoxelWorld.cs script (copy from guide)
4. Create VoxelPhysics.cs script (copy from guide)
5. Create VoxelModifier.cs script (copy from guide)
6. Add VoxelWorld to VoxelWorldManager GameObject
7. Add VoxelPhysics + VoxelModifier to VoxelPlayer GameObject
8. Press Play → Test!

**Total time:** ~30-45 minutes

---

**Last Updated**: June 28, 2026  
**Related**: `VOXEL_SCENE_SETUP_GUIDE.md` (complete code + instructions)
