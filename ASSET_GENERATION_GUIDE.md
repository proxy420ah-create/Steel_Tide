# 🏗️ Asset Generation Guide - Steel Tide

**Version**: 1.0  
**Last Updated**: June 27, 2026

Complete guide for generating all voxel assets with unified materials.

---

## 🎨 **Material Logic by Asset Type**

### **Buildings (Industrial/Commercial)**
- **2-Story Building**:
  - Walls: **Concrete** (gray) - Industrial/commercial
  - Floors: **Stone** (dark gray) - Structural foundation
  - Windows/Doors: **Air** (cut-outs)

### **Houses (Residential)**
- **L-Shape House**:
  - Walls: **Wood** (tan) - Residential warmth
  - Floor: **Stone** (dark gray) - Foundation
  - Windows/Doors: **Air** (cut-outs)

- **Simple House**:
  - Walls: **Concrete** (gray) - Small building
  - Floor: **Stone** (dark gray) - Foundation
  - Windows/Doors: **Air** (cut-outs)

### **Terrain**
- **Ground Plane** (default):
  - Surface: **Stone** (gray) - Hard street surface
  - Alternative: **Dirt** (brown), **Grass** (green), **Concrete** (gray)

### **Characters**
- **Humanoid**:
  - Body: **Uniform** (dark green) - Clothing/armor
  - Head: **Flesh** (pink) - Organic skin
  - Limbs: **Flesh** (pink) - Exposed skin on arms/legs

- **Combat Mech**:
  - Armor: **Chobham Armor** (brown) - Heavy protection
  - Frame: **Steel** (dark gray) - Structural
  - Weapons: **Damaged Steel** (orange) - Visual distinction

---

## 📋 **Complete Asset Checklist**

### **Step 1: Generate All Assets in Voxel Studio**

1. **Open Voxel Studio**
   ```bash
   cd "c:\Users\NADECC\ATSTradingDashboard Project\Cursor Workshop\SteelTide"
   .\VoxelStudio.bat
   ```

2. **Generate Buildings**:
   - Generate → 🏢 Building: 2-Story (32×64×32 + LOD)
   - File → Save As → `Building_2Story.stasset`
   
   - Generate → 🏠 Building: L-Shape House (48×32×48 + LOD)
   - File → Save As → `House_LShape.stasset`
   
   - Generate → 🏘️ Building: Simple House (32×24×24 + LOD)
   - File → Save As → `House_Simple.stasset`

3. **Generate Terrain**:
   - Generate → 🟫 Terrain: Ground Plane (128×2×128, 16m×0.25m)
   - File → Save As → `Ground_Stone.stasset`

4. **Generate Characters**:
   - Generate → 🧍 Character: Humanoid (6×16×4, 2m tall)
   - File → Save As → `Player_Humanoid.stasset`
   
   - Generate → 🤖 Character: Combat Mech (12×24×8, 3m tall)
   - File → Save As → `Enemy_Mech.stasset`

5. **Close Voxel Studio**

---

### **Step 2: Clear Unity StreamingAssets**

1. **In Windows Explorer**:
   - Navigate to: `My project\Assets\StreamingAssets\`
   - **Delete all `.stasset` files**
   - Keep the folder (don't delete it)

---

### **Step 3: Copy New Assets to Unity**

1. **Copy all generated `.stasset` files** from wherever you saved them
2. **Paste into**: `My project\Assets\StreamingAssets\`

**You should have:**
- `Building_2Story.stasset`
- `House_LShape.stasset`
- `House_Simple.stasset`
- `Ground_Stone.stasset`
- `Player_Humanoid.stasset`
- `Enemy_Mech.stasset`

---

### **Step 4: Setup in Unity**

#### **A. Ground Plane**
1. Create Empty GameObject → Name: `Ground`
2. Position: (0, 0, 0)
3. Add Component → VoxelObject
   - Asset File Name: `Ground_Stone.stasset`
   - Voxel Size: 0.125
4. Add Component → Box Collider
   - Size: (16, 0.25, 16)
   - Center: (8, 0.125, 8)

#### **B. Buildings (place on ground)**
For each building:
1. Create Empty GameObject → Name: `Building_2Story` (or appropriate name)
2. Position: **(X, 0.25, Z)** ← Elevated by street height!
   - Example positions:
     - 2-Story: (0, 0.25, 8)
     - L-House: (8, 0.25, 8)
     - Simple: (16, 0.25, 8)
3. Add Component → VoxelObject
   - Asset File Name: (appropriate `.stasset`)
   - Voxel Size: 0.125
4. Add Component → Box Collider
   - Adjust size to match voxel bounds (use Gizmo)

#### **C. Player Character**
1. Create Empty GameObject → Name: `Player`
2. Position: (0, 0.5, 0) ← On ground
3. Add Component → Character Controller
   - Height: 1.8
   - Radius: 0.4
   - Center: (0, 0.9, 0)
4. Create Child → Name: `PlayerCamera`
   - Position: (0, 1.6, 0)
   - Add Component → Camera (Tag: MainCamera)
   - Add Component → VoxelWeaponController
   - Add Component → PlayerWeapon
5. Create Child → Name: `PlayerModel`
   - Position: (0, 0, 0)
   - Add Component → VoxelObject
     - Asset File Name: `Player_Humanoid.stasset`
6. On Player GameObject:
   - Add Component → VoxelPlayerController
     - Camera Transform: Drag PlayerCamera here

---

## 🎨 **Material Color Reference**

### **What You'll See:**

| Asset | Primary Color | Secondary Color | Floor Color |
|-------|--------------|-----------------|-------------|
| **2-Story Building** | Gray (Concrete) | Dark Gray (Stone floors) | Dark Gray |
| **L-Shape House** | Tan (Wood) | Dark Gray (Stone floor) | Dark Gray |
| **Simple House** | Gray (Concrete) | Dark Gray (Stone floor) | Dark Gray |
| **Ground Plane** | Dark Gray (Stone) | - | - |
| **Humanoid** | Dark Green (Uniform body) | Pink (Flesh head) | Pink (Flesh limbs) |
| **Combat Mech** | Brown (Armor) | Dark Gray (Steel) | Orange (weapons) |

---

## ✅ **Testing Checklist**

### **In Voxel Studio:**
- [ ] All assets generate without errors
- [ ] Materials look correct (colors match expectations)
- [ ] Buildings have windows/doors cut out
- [ ] Characters have proper proportions
- [ ] Ground plane is flat and thin

### **In Unity:**
- [ ] All `.stasset` files load without errors
- [ ] Materials render with correct colors
- [ ] Buildings sit ON ground (not floating)
- [ ] No bleed-through between objects (proper spacing)
- [ ] Player can walk around
- [ ] Player collides with buildings
- [ ] Weapon can damage buildings
- [ ] Craters appear when shooting

---

## 📐 **Placement Grid (8m spacing)**

```
Ground at Y=0, Buildings at Y=0.25

   0    8   16   24   32
0  [2S] [ ] [LH] [ ] [ ]
8  [ ] [SH] [ ] [ ] [ ]
16 [ ] [ ] [ ] [ ] [ ]
24 [ ] [ ] [ ] [ ] [ ]

2S = 2-Story Building
LH = L-House
SH = Simple House
[ ] = Empty space
```

**Spacing rules:**
- Buildings on 8m grid (X and Z multiples of 8)
- All buildings at Y = 0.25 (street height)
- Minimum 8m between buildings (one grid cell)

---

## 🎯 **Expected Workflow Time**

| Step | Time |
|------|------|
| Generate all assets in Studio | 5-10 min |
| Clear Unity StreamingAssets | 1 min |
| Copy assets to Unity | 1 min |
| Setup ground plane | 2 min |
| Setup 3 buildings | 6 min |
| Setup player character | 5 min |
| Test and verify | 5 min |
| **Total** | **25-30 min** |

---

## 🚀 **You're Ready!**

After completing this guide, you'll have:
- ✅ Complete asset library with unified materials
- ✅ Proper material logic (wood houses, concrete buildings, stone ground)
- ✅ Working player character with collision
- ✅ Testable town layout
- ✅ End-to-end pipeline verified

**Now go build your town!** 🏘️✨
