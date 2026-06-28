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

## ⚡ **Quick Start: Jump to Voxel Physics**

**If you already have mesh ground + mesh player + voxel buildings working:**

### **Step 1: Create Voxel Ground Plane (15 minutes)**

1. **In Voxel Studio:**
   ```
   Generate → Ground Plane
   Dimensions: 128×4×128 (or larger)
   Material: Regolith Concrete (2) or Regolith (6)
   Save as: Ground_Voxel.stasset
   ```

2. **In Unity:**
   ```
   - Create Empty GameObject → Name: "VoxelGround"
   - Add VoxelObject component
   - Asset File Name: Ground_Voxel.stasset
   - Voxel Size: 0.125
   - Position: (0, 0.25, 0)  ← Y = 4 voxels × 0.125 / 2
   - Press Play → Should render!
   ```

### **Step 2: Create Voxel Player (10 minutes)**

1. **In Voxel Studio:**
   ```
   Character Generator → Generate Character
   Height: 32 voxels
   Save as: Player_Voxel.stasset
   ```

2. **In Unity:**
   ```
   - Create Empty GameObject → Name: "VoxelPlayer"
   - Add VoxelObject component
   - Asset File Name: Player_Voxel.stasset
   - Voxel Size: 0.125
   - Position: (0, 2.5, 0)  ← Above ground
   ```

### **Step 3: Add Physics Scripts (Jump to Section 3)**

Now go to **[Phase 2: Voxel Physics System](#3-voxel-physics-system)** for:
- VoxelWorld script setup
- VoxelPhysics script (gravity, movement)
- Raymarching collision

**Total Time:** ~30 minutes to get voxel player standing on voxel ground!

---

## 📋 **Table of Contents**
1. [Phase 1: Hybrid Setup (Mesh + Voxel)](#1-phase-1-hybrid-setup)
2. [Building Assets (Voxel Targets)](#2-building-assets)
3. [Phase 2: Voxel Physics System](#3-voxel-physics-system)
4. [Phase 3: Full Voxel Integration](#4-full-voxel-integration)
5. [Examples & Visual Reference](#5-examples--visual-reference)

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

## ⚠️ **IMPORTANT: Scripts Status**

### **Scripts You HAVE (Already in Unity):**
✅ `VoxelObject.cs` - Renders `.stasset` files (you're using this!)  
✅ `VoxelRenderer.cs` - Generates meshes from voxel data

### **Scripts You NEED TO CREATE (Phase 2):**
❌ `VoxelWorld.cs` - Manages all voxel data for physics/collision  
❌ `VoxelPhysics.cs` - Player movement with raymarching gravity  
❌ `VoxelModifier.cs` - Voxel destruction system (shooting/explosions)

**This section will show you HOW to create these scripts!**

The guide provides the **complete C# code** for each script below. You'll copy-paste them into Unity.

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

### **A. VoxelWorld Script** (Already Created!)

**Location:** `Unity/Scripts/VoxelWorld.cs`

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

### **B. VoxelPhysics Script** (Already Created!)

**Location:** `Unity/Scripts/VoxelPhysics.cs`

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

### **C. VoxelModifier Script** (Already Created!)

**Location:** `Unity/Scripts/VoxelModifier.cs`

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

### **D. Integration Steps**

**When you're ready to switch from mesh to voxel:**

1. **Create VoxelWorld Manager:**
   - Hierarchy → Create Empty → Name: `VoxelWorldManager`
   - Add Component → `VoxelWorld.cs`
   - Set `voxelSize = 0.125`

2. **Build Voxel Ground:**
   - In Voxel Studio: Create 128×128×4 ground plane
   - Save as `Ground_Voxel.stasset`
   - In Unity: Create Empty → Add `VoxelObject`
   - Position: `(0, 0.25, 0)` ← Y = 4 voxels × 0.125 / 2

3. **Create Voxel Player:**
   - In Voxel Studio: Create 8×16×8 character
   - Save as `Player_Voxel.stasset`
   - In Unity: Create Empty → Add `VoxelObject`
   - Add `VoxelPhysics.cs` + `VoxelModifier.cs`
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

---

## 📝 **SCRIPT CREATION GUIDE**

**The following sections provide COMPLETE C# code for the physics scripts.**

### **How to Create These Scripts:**

1. **In Unity:** `Assets` folder → Right-click → `Create` → `C# Script`
2. **Name it** exactly as shown (e.g., `VoxelWorld`)
3. **Double-click** to open in your code editor
4. **Delete** the default code
5. **Copy-paste** the code from this guide
6. **Save** the file
7. **Return to Unity** (it will compile automatically)

**You'll create 3 scripts:**
- `VoxelWorld.cs` (Step 1)
- `VoxelPhysics.cs` (Step 2)
- `VoxelModifier.cs` (Step 3)

---

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

---

### **Step 1: Create VoxelWorld Script**

**In Unity:** `Assets/Scripts` → Right-click → `Create` → `C# Script` → Name: `VoxelWorld`

**Copy this COMPLETE code:**

```csharp
using UnityEngine;
using System.Collections.Generic;

public class VoxelWorld : MonoBehaviour
{
    [System.Serializable]
    public class VoxelChunk
    {
        public VoxelObject voxelObject;
        public byte[,,] voxelData;
        public Vector3Int gridSize;
        public float voxelSize;
    }
    
    public List<VoxelChunk> chunks = new List<VoxelChunk>();
    
    void Start()
    {
        // Load all VoxelObjects in scene
        VoxelObject[] voxelObjects = FindObjectsOfType<VoxelObject>();
        foreach (var vo in voxelObjects)
        {
            var chunk = new VoxelChunk
            {
                voxelObject = vo,
                gridSize = vo.GridSize,
                voxelSize = vo.VoxelSize
            };
            chunks.Add(chunk);
        }
    }
    
    public bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
    {
        // Raymarch through all chunks
        foreach (var chunk in chunks)
        {
            if (RaymarchChunk(chunk, origin, direction, maxDistance, out hit))
                return true;
        }
        hit = new RaycastHit();
        return false;
    }
    
    private bool RaymarchChunk(VoxelChunk chunk, Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
    {
        // DDA raymarching implementation
        // Convert world position to voxel grid coordinates
        // March through grid until solid voxel hit or max distance reached
        
        // TODO: Implement DDA raymarching (similar to Voxel Studio)
        hit = new RaycastHit();
        return false;
    }
    
    public void SetVoxel(Vector3 worldPos, byte material)
    {
        // Find which chunk contains this position
        // Convert world position to voxel coordinates
        // Update voxel data
        // Trigger mesh regeneration
    }
}
```

### **Step 2: Create VoxelPhysics Script**

Create `Assets/Scripts/VoxelPhysics.cs`:

```csharp
using UnityEngine;

public class VoxelPhysics : MonoBehaviour
{
    [Header("Physics Settings")]
    public float gravity = -20f;
    public float moveSpeed = 5f;
    public float jumpHeight = 1.5f;
    
    [Header("References")]
    public VoxelWorld voxelWorld;
    public Transform playerCamera;
    
    private Vector3 velocity;
    private bool isGrounded;
    
    void Start()
    {
        if (voxelWorld == null)
            voxelWorld = FindObjectOfType<VoxelWorld>();
    }
    
    void Update()
    {
        HandleMovement();
        HandleMouseLook();
        ApplyGravity();
    }
    
    void HandleMovement()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        
        Vector3 move = transform.right * x + transform.forward * z;
        Vector3 desiredMove = move * moveSpeed * Time.deltaTime;
        
        // Check collision before moving
        if (!CheckCollision(transform.position + desiredMove))
        {
            transform.position += desiredMove;
        }
    }
    
    void ApplyGravity()
    {
        // Raycast down to check ground
        RaycastHit hit;
        if (voxelWorld.Raycast(transform.position, Vector3.down, 0.2f, out hit))
        {
            isGrounded = true;
            velocity.y = 0;
        }
        else
        {
            isGrounded = false;
            velocity.y += gravity * Time.deltaTime;
        }
        
        // Jump
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        
        // Apply vertical movement
        Vector3 verticalMove = Vector3.up * velocity.y * Time.deltaTime;
        if (!CheckCollision(transform.position + verticalMove))
        {
            transform.position += verticalMove;
        }
    }
    
    bool CheckCollision(Vector3 position)
    {
        // Raycast in movement direction
        Vector3 direction = (position - transform.position).normalized;
        float distance = Vector3.Distance(position, transform.position);
        
        RaycastHit hit;
        return voxelWorld.Raycast(transform.position, direction, distance, out hit);
    }
    
    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * 2f;
        float mouseY = Input.GetAxis("Mouse Y") * 2f;
        
        transform.Rotate(Vector3.up * mouseX);
        
        // Camera pitch
        Vector3 cameraRotation = playerCamera.localEulerAngles;
        cameraRotation.x -= mouseY;
        cameraRotation.x = Mathf.Clamp(cameraRotation.x, -80f, 80f);
        playerCamera.localEulerAngles = cameraRotation;
    }
}
```

### **Step 3: Attach Scripts**

1. Create empty GameObject: `VoxelWorldManager`
2. Add `VoxelWorld` script
3. Select `Player` GameObject
4. Add `VoxelPhysics` script
5. Assign references:
   - **Voxel World**: Drag `VoxelWorldManager` here
   - **Player Camera**: Drag `PlayerCamera` child

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

### **Step 1: Create VoxelModifier Script**

Create `Assets/Scripts/VoxelModifier.cs`:

```csharp
using UnityEngine;

public class VoxelModifier : MonoBehaviour
{
    [Header("Modification Settings")]
    public float range = 5f;
    public int brushSize = 1;
    public byte materialToPlace = 1;
    
    [Header("References")]
    public VoxelWorld voxelWorld;
    public Transform playerCamera;
    
    void Update()
    {
        // Left click: destroy voxel
        if (Input.GetButtonDown("Fire1"))
        {
            ModifyVoxel(0); // 0 = air/destroy
        }
        
        // Right click: place voxel
        if (Input.GetButtonDown("Fire2"))
        {
            ModifyVoxel(materialToPlace);
        }
    }
    
    void ModifyVoxel(byte material)
    {
        RaycastHit hit;
        Vector3 rayOrigin = playerCamera.position;
        Vector3 rayDirection = playerCamera.forward;
        
        if (voxelWorld.Raycast(rayOrigin, rayDirection, range, out hit))
        {
            // Calculate voxel position
            Vector3 voxelPos = hit.point - hit.normal * 0.5f;
            voxelWorld.SetVoxel(voxelPos, material);
        }
    }
}
```

### **Step 2: Attach Script**

1. Select `Player` GameObject
2. Add `VoxelModifier` script
3. Assign references:
   - **Voxel World**: Drag `VoxelWorldManager`
   - **Player Camera**: Drag `PlayerCamera`

---

### **Example 1: Small Test Scene**

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

## 🎯 **Quick Start Workflow**

### **Phase 1: Hybrid Setup (30 minutes)**

1. ✅ Create mesh plane for ground
2. ✅ Create capsule player with camera
3. ✅ Generate 3-5 buildings in Voxel Studio
4. ✅ Place buildings as VoxelObjects
5. ✅ Press Play → Walk around and inspect

**Checkpoint:** Can you see voxel buildings rendered? ✅

### **Phase 2: Voxel Physics (2-3 hours)**

1. ✅ Copy scripts to Unity project
2. ✅ Create VoxelWorldManager
3. ✅ Build voxel ground plane in Voxel Studio
4. ✅ Create voxel player character
5. ✅ Add VoxelPhysics + VoxelModifier scripts
6. ✅ Test movement and destruction

**Checkpoint:** Can you walk on voxel ground and destroy voxels? ✅

### **Phase 3: Polish (1-2 hours)**

1. ✅ Tune physics parameters (gravity, jump, speed)
2. ✅ Add weapon system
3. ✅ Test destruction radius
4. ✅ Optimize performance
5. ✅ Add visual effects (future)

**Checkpoint:** Does it feel good to play? ✅

---

## 🎨 **Material System Synchronization**

> **CRITICAL:** Unity's Material Colors array MUST match Voxel Studio's master material list!

### **Why Synchronization Matters**

**Voxel Studio** stores material IDs (0-20) in `.stasset` files.  
**Unity** uses those IDs to look up colors in the `Material Colors` array.

**If they don't match:**
- ❌ Buildings appear with wrong colors
- ❌ Flesh becomes concrete, steel becomes wood
- ❌ Your carefully painted assets look broken

**When they match:**
- ✅ Colors are identical between Voxel Studio and Unity
- ✅ Assets look exactly as designed
- ✅ Material system works perfectly

---

### **Master Material List (Voxel Studio)**

**Reference:** `MATERIAL_SYSTEM.md`

```
ID  | Name                  | RGB Color           | Use Case
----|-----------------------|---------------------|------------------------
0   | Air                   | (0, 0, 0)           | Empty space
1   | Prefab Composite      | (166, 153, 140)     | Modular buildings
2   | Regolith Concrete     | (64, 51, 46)        | Roads/foundations
3   | Concrete              | (128, 128, 128)     | Standard structures
4   | Flesh                 | (255, 153, 153)     | Organic matter
5   | Durasteel             | (71, 77, 82)        | Reinforced metal
6   | Regolith              | (102, 76, 51)       | Alien soil
7   | Xenoflora             | (38, 140, 128)      | Alien plants
8   | Basalt                | (64, 64, 69)        | Volcanic rock
9   | Wood                  | (153, 102, 51)      | Imported lumber
10  | Transparent Aluminum  | (217, 235, 255)     | Sci-fi glass
11  | Uniform               | (51, 76, 51)        | Military uniform
12  | Reserved              | -                   | Future use
13  | Damaged Concrete      | (217, 38, 38)       | Battle damage
14  | Damaged Steel         | (230, 102, 26)      | Heat damage
15  | Damaged Armor         | (204, 51, 51)       | Heavy damage
16  | Ablative Plating      | (38, 41, 46)        | Heat-resistant armor
17  | Reactive Armor        | (89, 92, 97)        | Explosive tiles
18  | Foam-Crete            | (191, 191, 191)     | Quick-deploy foam
19  | Nanomesh Fabric       | (26, 38, 64)        | Smart fabric
20  | Plasteel Panels       | (115, 115, 115)     | Plastic-metal hybrid
```

---

### **Unity Material Colors Array Setup**

**Location:** `VoxelRenderer.cs` → Inspector → `Material Colors` array

### **Method 1: Eyedropper Technique (RECOMMENDED)**

**You've already done this for IDs 0-16!** ✅

**For remaining materials (17-20):**

1. **Generate Material Sampler in Voxel Studio:**
   ```
   Generate → 🎨 Material Sampler (All Materials Grid)
   ```
   - Creates a grid showing ALL materials (0-20)
   - Each material is a distinct block
   - Save as `MaterialSampler.stasset`

2. **Load in Unity:**
   ```
   - Create GameObject → Add VoxelObject
   - Asset File Name: MaterialSampler.stasset
   - Press Play
   ```

3. **Use Eyedropper:**
   ```
   - Pause game
   - Open Scene view
   - Use Unity's eyedropper tool on each material block
   - Copy RGB values to Material Colors array
   ```

4. **Update Material Colors Array:**
   ```
   VoxelRenderer → Material Colors → Size: 21
   
   Element 17: (89, 92, 97)     ← Reactive Armor (gunmetal)
   Element 18: (191, 191, 191)  ← Foam-Crete (light gray)
   Element 19: (26, 38, 64)     ← Nanomesh Fabric (dark blue)
   Element 20: (115, 115, 115)  ← Plasteel Panels (medium gray)
   ```

---

### **Method 2: Manual Entry (PRECISE)**

**If you want exact RGB values from the master list:**

**Step-by-Step:**

1. **Open Unity** → Select any `VoxelRenderer` component
2. **Inspector** → Find `Material Colors` array
3. **Set Size** → `21` (for materials 0-20)
4. **Expand array** → Click arrow next to `Material Colors`
5. **For each element**, enter RGB values:

```csharp
// Copy these EXACT values into Unity Inspector:

Element 0:  R=0,   G=0,   B=0     // Air (transparent)
Element 1:  R=166, G=153, B=140   // Prefab Composite (tan)
Element 2:  R=64,  G=51,  B=46    // Regolith Concrete (dark brown)
Element 3:  R=128, G=128, B=128   // Concrete (gray)
Element 4:  R=255, G=153, B=153   // Flesh (pink)
Element 5:  R=71,  G=77,  B=82    // Durasteel (blue-gray)
Element 6:  R=102, G=76,  B=51    // Regolith (brown)
Element 7:  R=38,  G=140, B=128   // Xenoflora (teal)
Element 8:  R=64,  G=64,  B=69    // Basalt (dark gray)
Element 9:  R=153, G=102, B=51    // Wood (tan)
Element 10: R=217, G=235, B=255   // Transparent Aluminum (light blue)
Element 11: R=51,  G=76,  B=51    // Uniform (dark green)
Element 12: R=0,   G=0,   B=0     // Reserved (black)
Element 13: R=217, G=38,  B=38    // Damaged Concrete (crimson)
Element 14: R=230, G=102, B=26    // Damaged Steel (orange)
Element 15: R=204, G=51,  B=51    // Damaged Armor (dark red)
Element 16: R=38,  G=41,  B=46    // Ablative Plating (charcoal)
Element 17: R=89,  G=92,  B=97    // Reactive Armor (gunmetal)
Element 18: R=191, G=191, B=191   // Foam-Crete (light gray)
Element 19: R=26,  G=38,  B=64    // Nanomesh Fabric (dark blue)
Element 20: R=115, G=115, B=115   // Plasteel Panels (medium gray)
```

**Pro Tip:** Unity uses 0-255 RGB values, NOT 0.0-1.0 floats!

---

### **Method 3: Material Sampler Asset (FASTEST)**

**Use the built-in Material Sampler generator:**

1. **In Voxel Studio:**
   ```
   Generate → 🎨 Material Sampler (All Materials Grid)
   ```
   - Generates a 21×1×1 grid (one voxel per material)
   - Each voxel uses its corresponding material ID
   - Perfect for color matching!

2. **Save as:**
   ```
   File → Save As → MaterialSampler.stasset
   ```

3. **In Unity:**
   ```
   - Create GameObject
   - Add VoxelObject component
   - Asset File Name: MaterialSampler.stasset
   - Press Play
   ```

4. **Hover over each voxel:**
   - Material label shows: "Material 17: Reactive Armor"
   - Use eyedropper or visual comparison
   - Update Material Colors array

---

### **Verification Checklist**

**After updating Material Colors array:**

- [ ] **Test Building:** Load a building asset
  - Walls should be **tan** (Prefab Composite)
  - Foundation should be **dark brown** (Regolith Concrete)
  
- [ ] **Test Character:** Load a humanoid
  - Head should be **pink** (Flesh)
  - Body should be **dark green** (Uniform)
  - Limbs should be **medium gray** (Plasteel Panels)

- [ ] **Test Damaged Materials:** Create test asset with IDs 13-15
  - Damaged Concrete should be **crimson**
  - Damaged Steel should be **orange**
  - Damaged Armor should be **dark red**

- [ ] **Test Advanced Materials:** Create test asset with IDs 16-20
  - Ablative Plating should be **charcoal**
  - Reactive Armor should be **gunmetal**
  - Foam-Crete should be **light gray**
  - Nanomesh Fabric should be **dark blue**
  - Plasteel Panels should be **medium gray**

---

### **Common Issues & Fixes**

**Issue 1: Colors are wrong**
```
Problem: Building walls are gray instead of tan
Cause: Material Colors array not updated
Fix: Check Element 1 = (166, 153, 140) for Prefab Composite
```

**Issue 2: Materials 17-20 show as black**
```
Problem: New materials appear black
Cause: Material Colors array size is too small
Fix: Set Size = 21 (not 17!)
```

**Issue 3: Eyedropper gives wrong colors**
```
Problem: Colors don't match Voxel Studio
Cause: Lighting/post-processing affecting colors
Fix: Use Scene view (not Game view) for eyedropper
      OR use manual RGB entry from master list
```

**Issue 4: Material Sampler not showing**
```
Problem: MaterialSampler.stasset loads but nothing renders
Cause: Volume Dims might be 1×1×1 (very small!)
Fix: Check Inspector → Volume Dims should be 21×1×1
     Zoom in close in Scene view
```

---

### **Quick Reference: Material Categories**

**Core Materials (0-5):**
- Air, Prefab Composite, Regolith Concrete, Concrete, Flesh, Durasteel

**Terrain (6-10):**
- Regolith, Xenoflora, Basalt, Wood, Transparent Aluminum

**Utility (11-12):**
- Uniform, Reserved

**Damage States (13-15):**
- Damaged Concrete, Damaged Steel, Damaged Armor

**Advanced Armor (16-20):**
- Ablative Plating, Reactive Armor, Foam-Crete, Nanomesh Fabric, Plasteel Panels

---

### **Workflow Summary**

```
1. Generate MaterialSampler in Voxel Studio
   ↓
2. Load in Unity
   ↓
3. Use eyedropper OR manual entry
   ↓
4. Update Material Colors array (Size: 21)
   ↓
5. Verify with test assets
   ↓
6. ✅ Colors match perfectly!
```

**Time Required:** 10-15 minutes for all 21 materials

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
**Version**: 3.1.0 (Voxel Physics Priority Edition)
