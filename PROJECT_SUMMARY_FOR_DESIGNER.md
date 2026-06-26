# Steel Tide: Voxel Destruction System - Project Summary

**Date**: June 25, 2026  
**Status**: Debugging texture reference assignment issue  
**Platform**: Unity 6 (Universal Render Pipeline)

---

## 🎯 Project Goal

Build an interactive voxel-based destruction system where players can:
1. See a clean, flat-shaded 3D voxel cube rendered via GPU compute shader
2. Left-click on the cube to damage voxels (turn them crimson red)
3. Click damaged voxels again to destroy them completely (create permanent holes)

Think Minecraft-style block destruction, but with:
- GPU raymarching (no mesh generation)
- Two-stage damage visualization (pristine → damaged → destroyed)
- Real-time voxel data modification

---

## 📊 Current Architecture

### Component Hierarchy
```
Scene
├─ Main Camera
│  ├─ VoxelRenderer (Script)           ← Renders voxels via compute shader
│  └─ VoxelWeaponController (Script)   ← Handles mouse clicks & damage
└─ Bootstrap (Empty GameObject)
   └─ PrototypeBootstrap (Script)      ← Loads .stasset file, creates Texture3D
```

### Data Flow (Intended)
```
1. PrototypeBootstrap.Start()
   ↓ Load test_cube.stasset from StreamingAssets (8×8×8 voxel grid)
   ↓ Create Texture3D (GPU-side voxel data storage)
   ↓ Upload voxel data to Texture3D
   ↓ 
2. Assign texture to components:
   ├─ voxelRenderer.voxelVolume = texture
   └─ weaponController.voxelVolume = texture (via InitializeVolume)
   ↓
3. VoxelRenderer.OnEndCameraRendering()
   ↓ Run VoxelRaymarch.compute shader
   ↓ Shader reads from _VoxelVolume (Texture3D)
   ↓ Raymarching finds solid voxels
   ↓ Renders flat-shaded cube to screen
   ↓
4. User clicks mouse
   ↓ VoxelWeaponController.FireWeapon()
   ↓ Raycast into voxel grid
   ↓ Change material ID (Concrete → DamagedConcrete → Air)
   ↓ Re-upload modified data to Texture3D
   ↓ Instant visual update (crimson → hole)
```

---

## 🐛 Current Issue: Texture Reference Not Persisting

### Symptoms
- **Console logs** show successful asset load and texture creation
- **Volume Dims** populate correctly in Inspector (X 8, Y 8, Z 8) ✅
- **Voxel Volume slots** remain empty ("None (Texture3D)") ❌
- **Screen** shows only blue sky (no cube rendering)
- **Mouse clicks** report "Missed — no solid voxel hit"

### Console Output (Actual)
```
[SteelTide] Loaded 'test_cube.stasset': int3(8, 8, 8) grid, 512/512 solid voxels (100.0%)
[SteelTide] Uploaded 1024 bytes to GPU Texture3D.
[VoxelWeaponController] Initialized with volume dimensions: int3(8, 8, 8)
[SteelTide] VoxelWeaponController initialized — left-click to blast craters!
[SteelTide] Penetration: absorbed=False shredded=0 remaining=200.0
```

### Console Output (Expected but Missing)
```
[SteelTide] Auto-located VoxelRenderer component.
[SteelTide] Auto-located VoxelWeaponController component.
[VoxelRenderer] Pulled Texture3D reference from PrototypeBootstrap.
[VoxelWeaponController] Pulled Texture3D reference from PrototypeBootstrap.
```

### Root Cause Hypothesis
**PrototypeBootstrap successfully creates the Texture3D and assigns it to components**, but Unity 6's strict component lifecycle is causing the assignment to fail or be overwritten. The texture exists in memory (`_voxelTexture` is valid), but the public fields on VoxelRenderer and VoxelWeaponController are not retaining the reference.

**Pull-based architecture was implemented** to work around this:
- Components should actively **pull** the texture from Bootstrap's public property
- Pull logic added to `VoxelRenderer.LateUpdate()` and `VoxelWeaponController.Update()`
- But pull logs aren't appearing → pull logic not executing

### Possible Reasons Pull Logic Isn't Running
1. **voxelVolume might not be null** from the code's perspective (even though Inspector shows "None")
2. **FindFirstObjectByType might be failing** to locate PrototypeBootstrap
3. **GeneratedVoxelTexture property might be returning null** despite `_voxelTexture` being valid
4. **Update loops might not be running** (unlikely but possible)

---

## 🔧 Technical Details

### Voxel Data Storage
- **Format**: NativeArray<ushort> (16-bit packed voxel data)
- **Packing**: Shape (6 bits) + Rotation (2 bits) + Material (8 bits)
- **GPU Format**: Texture3D with TextureFormat.R16 (16-bit unsigned integer)
- **Dimensions**: 8×8×8 voxels = 512 voxels = 1024 bytes

### Material IDs
```csharp
0:  Air (transparent)
1:  Energy Shield (cyan)
2:  Chobham Armor (gray)
3:  Concrete (tan) ← Default test cube material
4:  Flesh (red)
5:  Steel (blue-gray)
13: DamagedConcrete (crimson red) ← First damage stage
14: DamagedSteel (hot orange)
15: DamagedArmor (dark red)
```

### Compute Shader (VoxelRaymarch.compute)
- **Algorithm**: Amanatides-Woo DDA (Digital Differential Analyzer)
- **Ray-box intersection**: AABB test to find volume entry point
- **Face normal tracking**: For flat-color shading
- **Lighting**: Directional diffuse from (0.5, 0.8, -0.3)
- **Max steps**: 256 (configurable in Inspector)

### Input System
- **Unity 6 New Input System** (NOT legacy Input class)
- **Mouse clicks**: `Mouse.current.leftButton.wasPressedThisFrame`
- **Mouse position**: `Mouse.current.position.ReadValue()`

---

## 📁 Key Files

### Scripts
1. **PrototypeBootstrap.cs** (`Assets/Prototype/`)
   - Loads .stasset file from StreamingAssets
   - Creates and uploads Texture3D to GPU
   - Exposes `public Texture3D GeneratedVoxelTexture` property
   - Auto-locates VoxelRenderer and VoxelWeaponController via FindFirstObjectByType

2. **VoxelRenderer.cs** (`Assets/Voxels/`)
   - Subscribes to URP rendering pipeline
   - Dispatches compute shader every frame
   - `LateUpdate()` pulls texture from Bootstrap if null

3. **VoxelWeaponController.cs** (`Assets/Combat/`)
   - Handles mouse click input
   - Fires raycast into voxel volume
   - Applies two-stage damage (Material ID remapping)
   - Re-uploads modified data to GPU
   - `Update()` pulls texture from Bootstrap if null

4. **VoxelRaymarch.compute** (`Assets/Voxels/`)
   - GPU compute shader for raymarching
   - CSRaymarch kernel (8×8×1 thread groups)

### Data Files
- **test_cube.stasset** (`Assets/StreamingAssets/`)
  - Binary voxel asset (1 KB)
  - Generated by Python tool: `tools/asset_generator/generator.py`

### Documentation
- **UNITY_SETUP_GUIDE.md** - Step-by-step Unity Editor setup
- **RECENT_CHANGES.md** - Development history & bug fixes
- **PROJECT_SUMMARY_FOR_DESIGNER.md** - This document

---

## 🎨 Visual Design

### Rendering Style
- **Clean flat-shaded low-poly aesthetic**
- **No grain, no noise, no procedural texture** (this was a previous bug)
- **Sharp face lighting** to define cube edges
- **Solid colors** determined by Material ID

### Damage Visualization
```
Stage 1: Pristine Concrete (tan/gray)
   ↓ [Left-click]
Stage 2: Damaged Concrete (BRIGHT CRIMSON RED) ← Highly visible damage scar
   ↓ [Left-click again]
Stage 3: Air (transparent) ← Permanent hole, can see through cube
```

### Expected Final Look
- **Solid tan/gray concrete cube** floating at origin (0, 0, 0)
- **Camera at (4, 4, -6)** looking at cube
- **Clean, sharp edges** with directional lighting
- **Click to create red damage clusters** (3×3×3 voxels per click)
- **Click red areas to carve holes** straight through

---

## 🔍 Debugging Strategy (Next Steps)

### Added Diagnostic Logging
We've just added extensive logging to identify exactly where the failure occurs:

**PrototypeBootstrap.cs** (texture creation):
```csharp
Debug.Log($"Created Texture3D: {width}x{height}x{depth}, format={format}");
Debug.Log($"GeneratedVoxelTexture property returns: {VALID/NULL}");
Debug.Log($"VoxelRenderer found, assigning texture directly...");
Debug.Log($"Verification: voxelRenderer.voxelVolume is now {ASSIGNED/NULL}");
```

**VoxelRenderer.cs** (texture pull):
```csharp
Debug.Log("[VoxelRenderer] voxelVolume is null, attempting to pull from Bootstrap...");
// + warnings if Bootstrap not found or texture is null
Debug.Log($"Successfully pulled Texture3D reference: {width}x{height}x{depth}");
```

### Test Instructions
1. **Clear Console** in Unity
2. **Press Play** ▶
3. **Watch for NEW diagnostic logs**:
   - Does texture creation succeed?
   - Does direct assignment report "ASSIGNED"?
   - Does pull logic ever run?
   - Which specific step fails?

### Expected Diagnostic Output
If everything works, you should see:
```
[SteelTide] Created Texture3D: 8x8x8, format=R16
[SteelTide] GeneratedVoxelTexture property returns: VALID
[SteelTide] VoxelRenderer found, assigning texture directly...
[SteelTide] Uploaded 1024 bytes to GPU Texture3D.
[SteelTide] Verification: voxelRenderer.voxelVolume is now ASSIGNED
[VoxelRenderer] voxelVolume is null, attempting to pull from Bootstrap...
[VoxelRenderer] Successfully pulled Texture3D reference: 8x8x8
```

---

## 🎯 Success Criteria

When the bug is fixed, you will see:

### Visual
- ✅ **Solid tan/gray cube** in Game window (no blue sky)
- ✅ **Clean flat-shaded faces** with sharp lighting
- ✅ **Left-click** → red damage clusters appear
- ✅ **Click red** → permanent holes carved through cube

### Console
- ✅ Zero errors, zero warnings
- ✅ All diagnostic logs show "VALID" and "ASSIGNED"
- ✅ Inspector shows **valid Texture3D reference** in both Voxel Volume slots

### Interaction
- ✅ Mouse clicks log: `Hit at (4,2,3): 27 voxels changed (Material 3)`
- ✅ No more "Missed — no solid voxel hit" errors
- ✅ Damage persists throughout Play session

---

## 🚀 Future Features (After Bug Fix)

1. **Complex voxel structures** (load walker legs, gun barrels, ship hulls)
2. **Physics-based destruction** (explosion forces, shockwaves)
3. **Particle effects** (debris, sparks, smoke on damage)
4. **Multi-material structures** (concrete + steel + armor in same object)
5. **Save/load damaged state** (persist destruction to disk)

---

## 📞 Questions for Designer?

1. Is the diagnostic logging approach correct?
2. Should we try a different texture assignment strategy?
3. Any Unity 6 / URP texture handling gotchas we should know about?
4. Does Texture3D need special initialization for runtime-created textures?

---

**Last Updated**: June 25, 2026, 10:37 PM  
**Status**: Awaiting diagnostic log output from next Play test
