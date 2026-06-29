# 🏗️ Voxel Scene Setup Guide - Steel Tide
**Dynamic voxel destruction system with raymarching physics**

---

## 🎯 **Core Design Philosophy**

Steel Tide uses **dynamic voxel destruction** with **low-poly raymarching physics**:
- ✅ Voxels can be destroyed/modified at runtime
- ✅ Collision uses raymarching against actual voxel data (not Unity colliders)
- ✅ Performance-optimized for real-time destruction
- ✅ No static Box Colliders - all collision is voxel-based

### **🚀 Development Roadmap**

```
Phase 1: HYBRID SETUP ✅ COMPLETE
├─ Mesh plane for ground (temporary) ✅
├─ Mesh player for movement (temporary) ✅
├─ Voxel buildings as targets ✅
└─ Test rendering & shooting ✅

Phase 2: VOXEL PHYSICS ← You are here!
├─ Build voxel ground plane (PRIORITY #1)
├─ Implement VoxelWorld data structure
├─ Add raymarching collision
├─ Create voxel player character
└─ Test voxel player on voxel ground

Phase 3: FULL VOXEL
├─ Replace mesh plane with voxel terrain
├─ Replace mesh player with voxel character
├─ Add destruction system
└─ Polish & optimize
```

**Current Status:** You have mesh ground + mesh player + voxel buildings working. Now you need to get the **voxel ground plane** working so the **voxel player** can stand on it with raymarching physics.

**Priority Order:**
1. **Voxel Ground Plane** (foundation for everything)
2. **VoxelWorld + Raymarching** (physics system)
3. **Voxel Player** (character that uses physics)
4. Buildings/destruction (already rendering, can wait)

---

## 📋 **Table of Contents**
1. [Phase 1: Hybrid Setup (Mesh + Voxel)](#1-phase-1-hybrid-setup)
2. [Building Assets (Voxel Targets)](#2-building-assets)
3. [Phase 2: Voxel Physics System](#3-voxel-physics-system)
4. [Phase 3: Full Voxel Integration](#4-full-voxel-integration)
5. [Examples & Visual Reference](#5-examples--visual-reference)
6. [Material System Synchronization](#-material-system-synchronization)

---

## 1️⃣ **Phase 1: Hybrid Setup (Mesh + Voxel)**

> **Goal**: Get a playable scene FAST with mesh ground/player + voxel buildings for testing

### **A. Temporary Mesh Ground Plane**

**Why mesh first?** You need something to stand on while building the voxel physics system!

1. **Hierarchy** → **3D Object** → **Plane**
2. **Name**: `GroundPlane_TEMP`
3. **Transform**:
   - Position: `(0, 0, 0)`
   - Rotation: `(0, 0, 0)`
   - Scale: `(10, 1, 10)` ← Large enough for testing
4. **Add Material** (optional):
   - Create material: `Assets/Materials/Ground_TEMP.mat`
   - Set color to gray/brown for visibility

```
     ╔════════════════════════════════╗
     ║   TEMPORARY MESH PLANE         ║
     ║   (Will replace with voxels)   ║
     ╚════════════════════════════════╝
```

### **B. Temporary Mesh Player**

**Why mesh first?** Unity's built-in FPS controller works out-of-the-box!

1. **Hierarchy** → **3D Object** → **Capsule**
2. **Name**: `Player_TEMP`
3. **Transform**:
   - Position: `(0, 1, 0)` ← Standing on plane
   - Rotation: `(0, 0, 0)`
   - Scale: `(1, 1, 1)`
4. **Add Components**:
   - **Character Controller** (built-in Unity)
   - **First Person Controller** script (from Standard Assets)
5. **Add Camera**:
   - Right-click `Player_TEMP` → **Camera**
   - Position: `(0, 0.6, 0)` ← Eye level
   - Tag: `MainCamera`

```
        👤 ← Capsule player
        │
    ════════ ← Mesh plane
```

### **C. Voxel Buildings (Your Targets!)**

**This is where voxels START!** Buildings are voxel objects you can see/shoot.

1. **Create GameObject**:
   - Hierarchy → **Create Empty**
   - Name: `Building_House01`
   - Position: `(5, 1, 5)` ← On the plane

2. **Add VoxelObject Component**:
   - **Asset File Name**: `Building_House01.stasset`
   - **Voxel Size**: `0.125`
   - **Show Gizmo**: ✓

3. **Repeat for 3-5 buildings** around the scene

```
    🏠        🏠     ← Voxel buildings
      \      /
       \    /
        👤          ← Mesh player (temp)
        │
    ════════════    ← Mesh plane (temp)
```

### **D. Test Your Hybrid Scene**

**Press Play!** You should be able to:
- ✅ Walk around on mesh plane
- ✅ Look with mouse
- ✅ See voxel buildings rendered
- ✅ Approach buildings and inspect them

**Volume Dims Behavior:**
- **Edit Mode (Play OFF)**: Shows default dims (32×32×32)
- **Play Mode (Play ON)**: Auto-updates to actual dims from `.stasset` file
- **This is normal!** The VoxelObject script reads the `.stasset` file at runtime and updates the dimensions

```
Edit Mode:   Volume Dims: 32 × 32 × 32   (default placeholder)
              ↓ Press Play ↓
Play Mode:   Volume Dims: 128 × 2 × 128  (actual data from .stasset)
                      or   16 × 16 × 16   (depends on your asset)
```

---

## 2️⃣ **Building Assets (Voxel Targets)**

> **Goal**: Create destructible voxel buildings to populate your test scene

### **A. Generate Building in Voxel Studio**

**Recommended Building Sizes:**

```
┌─────────────────┬──────────────┬────────────────┐
│ Building Type   │ Voxel Dims   │ World Units    │
├─────────────────┼──────────────┼────────────────┤
│ Small House     │ 16×16×16     │ 2×2×2          │
│ Medium House    │ 24×24×24     │ 3×3×3          │
│ Large Building  │ 32×32×48     │ 4×4×6          │
│ Skyscraper      │ 32×64×32     │ 4×8×4          │
└─────────────────┴──────────────┴────────────────┘
```

**Workflow:**
1. Open **Voxel Asset Studio**
2. **Generate** → **Armored Cube** (or custom shape)
3. Set dimensions (e.g., 16×16×16 for small house)
4. **Paint/Edit**:
   - Add windows (hollow out voxels)
   - Add door (hollow out entrance)
   - Paint walls, roof, details
5. **File** → **Save As** → `Building_House01.stasset`
6. **Repeat** for 3-5 different buildings

**Pro Tip:** Use different colors for each building to make them visually distinct!

### **B. Place Buildings in Unity**

**Y-Position Calculation** (IMPORTANT!):

```
Y = ground_level + (voxel_height × 0.125 / 2)

Examples:
- 16-voxel tall building: Y = 0 + (16 × 0.125 / 2) = 1.0
- 24-voxel tall building: Y = 0 + (24 × 0.125 / 2) = 1.5
- 32-voxel tall building: Y = 0 + (32 × 0.125 / 2) = 2.0
```

**Example Scene Layout:**

```
        [🏠]              [🏢]     ← Buildings (voxel)
          │                │
    ══════════════════════════     ← Ground (mesh temp)
          │
         👤                        ← Player (mesh temp)

Top-down view:

    🏠 (-5,5)      🏠 (5,5)
         \          /
          \        /
           \      /
            \    /
             👤 (0,0)
            /    \
           /      \
          /        \
    🏠 (-5,-5)    🏢 (5,-5)
```

### **C. Quick Placement Guide**

**For 5 buildings around spawn:**

```csharp
// Copy these positions into Unity Inspector:
Building_House01:  Position (5, 1, 5)    // NE corner
Building_House02:  Position (-5, 1, 5)   // NW corner
Building_House03:  Position (-5, 1, -5)  // SW corner
Building_Tower01:  Position (5, 2, -5)   // SE corner (taller)
Building_Shop01:   Position (0, 1, 8)    // North (on street)
```

**Grid Snapping** (for perfect alignment):
1. **Edit** → **Snap Settings**
2. Set **Move X/Y/Z**: `0.125` ← Matches voxel size
3. Hold **Ctrl** while dragging objects in Scene view
4. Buildings snap to voxel grid perfectly!

### **D. Visual Verification**

**What you should see:**

```
Scene View (Edit Mode):
├─ Green wireframe boxes (gizmos) around buildings
├─ Volume Dims showing placeholder values
└─ Buildings positioned on mesh plane

Game View (Play Mode):
├─ Voxel buildings fully rendered
├─ Volume Dims updated to real values
├─ Player can walk around and inspect
└─ Buildings have proper scale/position
```

---

## 3️⃣ **Phase 2: Voxel Physics System**

> **Goal**: Replace mesh ground/player with voxel physics (raymarching collision)

---

## ✅ **Scripts Status - READY!**

### **All Scripts Available in Assets/Scripts:**
✅ `VoxelObject.cs` - Renders `.stasset` files  
✅ `VoxelRenderer.cs` - Generates meshes from voxel data  
✅ `VoxelWorld.cs` - Manages all voxel data for physics/collision  
✅ `VoxelPhysics.cs` - Player movement with raymarching gravity  
✅ `VoxelModifier.cs` - Voxel destruction system (shooting/explosions)

**Status**: All scripts are in `My project/Assets/Scripts/` and compiling successfully! ✅

**What was fixed:**
- Scripts moved from root to `Assets/Scripts/` folder
- Fixed `Vector3Int.normalized` compilation error
- Updated deprecated `FindObjectOfType` to `FindFirstObjectByType`
- All scripts now visible in Unity's "Add Component" menu

**You can now use these scripts immediately!**

---

### **Why Raymarching Physics?**

**Traditional Unity Physics:**
```
Player → Box Collider → Unity Physics → Static Mesh Collider
                ❌ Can't destroy voxels dynamically
```

**Voxel Raymarching Physics:**
```
Player → Raycast DOWN → Voxel Grid → Hit Detection
                ✅ Works with dynamic voxel data
                ✅ Voxels can be destroyed/modified
                ✅ No collider mesh generation needed
```

### **Physics Independence from Camera**

**IMPORTANT:** Physics rays ≠ Camera rays!

```
        Camera Ray (for aiming/shooting)
           ↗
          /
         /
        👤 ← Player
        │
        ↓  ← Gravity ray (ALWAYS DOWN)
        │
    ════════ ← Ground voxels
```

**Player can look at sky, spin 360° - physics still works!**

Gravity rays cast from player's **feet position**, not camera direction.

### **A. VoxelWorld Script** ✅ Ready!

**Location:** `My project/Assets/Scripts/VoxelWorld.cs`

**What it does:**
- Manages all voxel data in a Dictionary (memory-efficient)
- Provides `RaymarchChunk()` for collision detection
- Handles voxel get/set operations
- Converts between world coords and voxel grid coords

**Key Features:**
```csharp
// Check if voxel exists at position
byte material = voxelWorld.GetVoxel(worldPosition);

// Destroy voxel (set to air)
voxelWorld.SetVoxel(worldPosition, 0);

// Raycast for collision
VoxelHit hit = voxelWorld.RaymarchChunk(origin, direction, maxDistance);
if (hit.hit) {
    // Collision detected!
}
```

### **B. VoxelPhysics Script** ✅ Ready!

**Location:** `My project/Assets/Scripts/VoxelPhysics.cs`

**What it does:**
- Applies gravity (raycasts DOWN from player)
- Handles WASD movement with collision
- Jump mechanics
- Mouse look (independent of physics)
- Wall sliding

**Ground Detection:**
```csharp
void CheckGrounded() {
    Vector3 rayOrigin = transform.position;  // Player position
    Vector3 rayDirection = Vector3.down;     // ALWAYS DOWN (not camera!)
    
    VoxelHit hit = voxelWorld.RaymarchChunk(rayOrigin, rayDirection, 0.2f);
    isGrounded = hit.hit;
}
```

### **C. VoxelModifier Script** ✅ Ready!

**Location:** `My project/Assets/Scripts/VoxelModifier.cs`

**What it does:**
- Left-click to destroy voxels
- Right-click to place voxels
- Crosshair + target highlight
- Radius destruction support

**Usage:**
```csharp
// Destroy single voxel
Left Click → Raycast from camera → Hit voxel → SetVoxel(pos, 0)

// Destroy radius (explosion)
destructionRadius = 3 → Destroys 7×7×7 sphere of voxels
```

### **D. Integration Steps** ✅ Scripts Ready!

**Now that all scripts are in Assets/Scripts and compiling:**

1. **Create VoxelWorld Manager:**
   - Hierarchy → Create Empty → Name: `VoxelWorldManager`
   - Add Component → Search "VoxelWorld" → Select `VoxelWorld` ✅
   - Inspector → Set `voxelSize = 0.125`

2. **Build Voxel Ground:**
   - In Voxel Studio: Create 128×128×4 ground plane
   - Save as `Ground_Voxel.stasset`
   - In Unity: Create Empty → Add Component → `VoxelObject` ✅
   - Inspector → Asset File Name: `Ground_Voxel.stasset`
   - Position: `(0, 0.25, 0)` ← Y = 4 voxels × 0.125 / 2

3. **Create Voxel Player:**
   - In Voxel Studio: Create 8×16×8 character (or use Character Generator)
   - Save as `Player_Voxel.stasset`
   - In Unity: Create Empty → Add Component → `VoxelObject` ✅
   - Add Component → Search "VoxelPhysics" → Select `VoxelPhysics` ✅
   - Add Component → Search "VoxelModifier" → Select `VoxelModifier` ✅
   - Inspector → Assign VoxelWorld reference
   - Position: `(0, 2, 0)` ← Above ground

4. **Delete Temporary Objects:**
   - Delete `GroundPlane_TEMP`
   - Delete `Player_TEMP`

5. **Test:**
   - Press Play
   - Player should fall and land on voxel ground
   - WASD to move, Space to jump
   - Left-click to destroy voxels
   - Right-click to place voxels

---

## 4️⃣ **Phase 3: Full Voxel Integration**

> **Goal**: Polish and optimize the complete voxel destruction system

### **Architecture Overview**

The voxel physics system uses **raymarching** for collision detection:

```
Player Position → Raycast → Voxel Grid → Collision Response
```

**Key Components:**
1. **VoxelWorld**: Manages all voxel data in the scene
2. **RaymarchCollider**: Performs raymarching collision detection  
3. **VoxelPhysics**: Handles physics simulation (gravity, velocity)
4. **VoxelModifier**: Handles dynamic voxel destruction

All scripts are ready to use via Unity's "Add Component" menu!

---

### **A. Performance Optimization**

**Voxel Data Structure:**
```csharp
// Memory-efficient: Only stores solid voxels
Dictionary<Vector3Int, byte> voxelData;

// Air voxels (material = 0) are NOT stored
// Saves memory for large worlds
```

**Raymarching Optimization:**
- DDA algorithm (fast voxel traversal)
- Early exit on first hit
- Configurable max distance

**Rendering Optimization:**
- Greedy meshing (future)
- Chunk-based updates (future)
- Occlusion culling (future)

### **B. Advanced Features**

**Destruction Radius:**
```csharp
// Single voxel
destructionRadius = 1;  // 1×1×1

// Small explosion
destructionRadius = 2;  // 3×3×3 sphere

// Large explosion
destructionRadius = 5;  // 11×11×11 sphere
```

**Material System:**
```csharp
byte materialID = voxelWorld.GetVoxel(position);

// Material IDs:
// 0 = Air (empty)
// 1 = Concrete
// 2 = Metal
// 3 = Wood
// 4 = Glass
// etc.
```

**Weapon Integration:**
```csharp
// Attach VoxelModifier to weapon GameObject
public class VoxelWeapon : MonoBehaviour {
    VoxelModifier modifier;
    
    void Shoot() {
        // Raycast from weapon muzzle
        VoxelHit hit = voxelWorld.RaymarchChunk(muzzle.position, muzzle.forward, 100f);
        
        if (hit.hit) {
            // Destroy voxels in radius
            modifier.SetDestructionRadius(3);
            modifier.DestroyVoxel(hit.worldPosition);
        }
    }
}
```

### **C. Debugging Tools**

**Visual Debug Rays:**
```csharp
// In VoxelWorld.cs
showDebugRays = true;  // Show raycast lines in Scene view

// In VoxelPhysics.cs
showDebugRays = true;  // Show ground check rays + debug overlay
```

**Debug Overlay (VoxelPhysics):**
```
┌─────────────────────────┐
│ Grounded: True          │
│ Velocity: (0, -2, 0)    │
│ Position: (5.2, 1.0, 3) │
│ Speed: 5.23 u/s         │
└─────────────────────────┘
```

**Console Logging:**
```csharp
// VoxelWorld logs:
"Registered 1024 voxels from Building_House01.stasset at (5, 1, 5)"

// VoxelModifier logs:
"Destroyed voxel at (12, 5, 8)"
"Placed voxel (material 1) at (13, 5, 8)"
```

## 5️⃣ **Examples & Visual Reference**

### **Example 1: Small Test Scene (Phase 1)**

**Scene Hierarchy:**
```
SampleScene
├─ GroundPlane_TEMP (Mesh Plane)        ← Phase 1
├─ Player_TEMP (Capsule + Camera)       ← Phase 1
├─ Building_House01 (VoxelObject)       ← Voxel target
├─ Building_House02 (VoxelObject)       ← Voxel target
├─ Building_Tower01 (VoxelObject)       ← Voxel target
└─ Directional Light
```

**What you can do:**
- ✅ Walk around on mesh plane
- ✅ Look at voxel buildings
- ✅ Test rendering performance
- ✅ Plan building placement

### **Example 2: Voxel Physics Test**

**Scene Hierarchy:**
```
SampleScene
├─ VoxelWorldManager (VoxelWorld.cs)    ← Phase 2
├─ Ground_Voxel (VoxelObject)           ← Phase 2
├─ Player_Voxel (VoxelObject)           ← Phase 2
│  ├─ VoxelPhysics.cs
│  ├─ VoxelModifier.cs
│  └─ PlayerCamera
├─ Building_House01 (VoxelObject)
├─ Building_House02 (VoxelObject)
└─ Directional Light
```

**What you can do:**
- ✅ Walk on voxel ground (raymarching collision)
- ✅ Jump and test gravity
- ✅ Destroy voxels (left-click)
- ✅ Place voxels (right-click)
- ✅ Test wall collision

### **Example 3: Combat Test Scene**

**Scene Layout:**
```
     [🏢]    [🏠]    [🏢]     ← Destructible buildings
       \      |      /
        \     |     /
         \    |    /
          \   |   /
           \  |  /
            \ | /
             👤              ← Player with weapon
             │
    ═════════════════        ← Voxel ground
```

**Test Scenarios:**
1. **Precision Shooting**: Destroy single voxels (windows, doors)
2. **Explosions**: Destroy radius of voxels (walls, corners)
3. **Building Collapse**: Destroy support voxels, watch building fall (future)
4. **Cover System**: Hide behind buildings, shoot through windows

### **Visual Scale Reference**

**Voxel to World Units:**
```
1 voxel = 0.125 units
8 voxels = 1.0 unit
16 voxels = 2.0 units
32 voxels = 4.0 units
128 voxels = 16.0 units
```

**Common Object Sizes:**
```
┌──────────────────┬─────────────┬─────────────┐
│ Object           │ Voxel Dims  │ World Units │
├──────────────────┼─────────────┼─────────────┤
│ Player           │ 8×16×8      │ 1×2×1       │
│ Door             │ 8×16×2      │ 1×2×0.25    │
│ Window           │ 8×8×2       │ 1×1×0.25    │
│ Small House      │ 16×16×16    │ 2×2×2       │
│ Medium House     │ 24×24×24    │ 3×3×3       │
│ Large Building   │ 32×32×48    │ 4×4×6       │
│ Ground Plane     │ 128×4×128   │ 16×0.5×16   │
└──────────────────┴─────────────┴─────────────┘
```

### **Motivational Milestones**

**Phase 1 Complete ✅:**
```
🎉 You can walk around and see voxel buildings!
   Next: Build the physics system
```

**Phase 2 Complete ✅:**
```
🎉 You have working voxel physics!
   Next: Add destruction and polish
```

**Phase 3 Complete ✅:**
```
🎉 Full voxel destruction system working!
   Next: Add gameplay mechanics, weapons, enemies
```

---

## � **VoxelPlayer Hierarchy Setup (STANDARD WORKFLOW)**

### **✅ Recommended Structure:**

```
VoxelPlayer (GameObject)
├─ CharacterController (Height: 4.0, Center: 0,2,0, Radius: 0.5)
├─ VoxelPhysics (script)
├─ VoxelModel (child GameObject - visual offset)
│  ├─ Transform Position: (-0.6, -2.0, -0.4) ← Centers the model
│  └─ VoxelObject (Player_Voxel.stasset)
└─ Player Camera (child GameObject - camera offset)
   └─ Transform Position: (0, 3, -5) ← Behind and above player
```

### **🔧 Why This Structure?**

**Separation of Concerns:**
- **VoxelPlayer (parent)** = Physics, collision, gameplay logic
- **VoxelModel (child)** = Visual representation only
- **Player Camera (child)** = View/rendering only

**Benefits:**
1. ✅ Import `.stasset` files as-is from Voxel Studio (no pivot changes needed)
2. ✅ Adjust visual alignment per-character in Unity
3. ✅ No need to regenerate assets when tweaking alignment
4. ✅ Clean separation between gameplay and visuals
5. ✅ Standard Unity pattern (used in most games)

### **📋 Setup Steps:**

**1. Create VoxelPlayer:**
```
1. Create Empty GameObject → Name: "VoxelPlayer"
2. Add CharacterController component
   - Height: 4.0 (32 voxels × 0.125)
   - Center: (0, 2.0, 0) (half height)
   - Radius: 0.5
3. Add VoxelPhysics script
```

**2. Create VoxelModel Child:**
```
1. Right-click VoxelPlayer → Create Empty Child
2. Name: "VoxelModel"
3. Add VoxelObject component
   - Asset File Name: "Player_Voxel.stasset"
   - Voxel Size: 0.125
   - Click "Load Dimensions from .stasset File" button ← NEW!
   - Volume Dims updates to actual size (e.g., 10×32×6)
4. Add VoxelModelAligner component (AUTOMATIC CENTERING!)
   - Auto Align: ✅ Enabled
   - Align Bottom: ✅ Enabled (keeps feet at Y=0)
   - Automatically aligns when dimensions are loaded! ✅
5. Done! Model is centered in Edit Mode (no Play mode needed!) ✅
```

**Why This Works:**
- VoxelObjectEditor loads `.stasset` dimensions in Edit Mode
- VoxelModelAligner detects dimension change and auto-aligns
- No need to enter Play mode!
- Alignment is saved immediately

**Manual Method (if you prefer):**
```
Set Local Position to center the model:
   - For 10×32×6 character: (-0.625, 0, -0.375)
   - Formula: (-width/2, 0, -depth/2) in world units
```

**3. Add Camera Child:**
```
1. Right-click VoxelPlayer → Create Camera Child
2. Name: "Player Camera"
3. Set Local Position: (0, 3, -5) (behind and above)
4. Assign to VoxelPhysics → Camera Transform field
```

### **🎯 Workflow Summary:**

**Voxel Studio → Unity Pipeline:**
1. Create character in Voxel Studio
2. Export `.stasset` file (corner pivot is fine!)
3. Copy to Unity's StreamingAssets folder
4. Create VoxelPlayer with child structure above
5. Adjust VoxelModel's local position to center visuals
6. Done! ✅

**No need to change Voxel Studio output format!**

---

## �� **Material System Synchronization**

> **CRITICAL:** Unity's Material Colors array MUST match Voxel Studio's master material list!

**For complete material synchronization instructions, see:** `MATERIAL_SYNC_COMPLETE.md`

**Quick Summary:**
- Materials 0-16: ✅ Already synced (eyedropper method)
- Materials 17-20: ⚠️ Pending sync (5 minutes)
- Use Material Sampler generator in Voxel Studio for easy color matching
- Reference: `MATERIAL_SYSTEM.md` for full material list

---

## 📚 **Related Guides**

- `QUICK_START_GUIDE.md` - Voxel Studio tutorial
- `PLAYER_CHARACTER_SETUP.md` - Detailed player configuration
- `VOXEL_SCALE_REFERENCE.md` - Size guidelines for assets
- `MATERIAL_SYSTEM.md` - Material IDs and colors (MASTER REFERENCE)
- `UNITY_MATERIAL_COLORS_REFERENCE.md` - Quick copy-paste guide

---

---

## 💪 **You've Got This!**

**Remember:**
- Start with Phase 1 (hybrid) - it's fast and gives immediate results
- Phase 2 (physics) is the technical challenge - take your time
- Phase 3 (polish) is where it gets FUN

**The scripts are ready.** The guide is clear. The plan is solid.

**Now go build something awesome! 🚀**

---

**Last Updated**: June 28, 2026  
**Version**: 3.3.0 (Cleaned up - Removed duplicate sections, reorganized for clarity)
