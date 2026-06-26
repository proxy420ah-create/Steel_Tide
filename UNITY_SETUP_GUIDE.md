# Unity Editor Setup Guide — First Cube Render

**Goal**: Wire up the GPU rendering pipeline so you see the test cube when you press Play.

---

## Step 1: Wait for Compilation

After the scripts compile, Unity will generate `.meta` files for all new components:
- `VoxelRenderer.cs` (Unity 6 + URP compatible GPU dispatcher)
- `test_cube.stasset` (in StreamingAssets/)
- Updated `PrototypeBootstrap.cs`

**Unity 6 Compatibility**: The renderer uses `RenderPipelineManager.endCameraRendering` 
instead of the deprecated `OnRenderImage`, ensuring full compatibility with Unity 6's 
Universal Render Pipeline (URP).

Check the Unity **Console** (bottom panel). If you see errors, stop and report them. Otherwise, proceed.

---

## Step 2: Create the Scene Objects

### 2A: Create a Main Camera GameObject (if not already present)

1. In the **Hierarchy** panel (left), check if you have a `Main Camera`.
2. If not, right-click in Hierarchy → **Camera** → name it `Main Camera`.
3. Select `Main Camera` in the Hierarchy.
4. In the **Inspector** panel (right), set **Position** to `(4, 4, 6)` (so it's looking at the cube).
5. Set **Rotation** to `(20, -30, 0)` (angled down toward the origin).

### 2B: Add the VoxelRenderer Component

1. With `Main Camera` still selected in the Hierarchy, click **Add Component** at the bottom of the Inspector.
2. Type `VoxelRenderer` in the search box.
3. Click **VoxelRenderer** to attach it to the camera.

**Inspector should now show**:
```
Voxel Renderer (Script)
  Raymarch Shader: None
  Voxel Volume: None (set at runtime by Bootstrap)
  Max Steps: 256
  Voxel Size: 0.5
  Volume Offset: (0, 0, 0)
  Material Colors: (size 6 array with default colors)
```

4. In the **Project** panel (bottom), navigate to `Assets/Voxels/`.
5. **Drag `VoxelRaymarch.compute`** into the **Raymarch Shader** slot in the Inspector.

### 2B: Add Interactive Weapon Controller (Optional but Recommended)

1. With `Main Camera` still selected, click **Add Component** again.
2. Type `VoxelWeaponController` and select it.
3. Leave default settings:
   - **Damage Radius**: 1 (affects 3×3×3 voxel cluster)
   - **Left-click** to fire (Unity 6 New Input System)

**Note**: Bootstrap will automatically wire this controller at runtime.

---

## Step 3: Create the Bootstrap GameObject

1. In the **Hierarchy**, right-click → **Create Empty** → name it `Bootstrap`.
2. Select `Bootstrap` in the Hierarchy.
3. Click **Add Component** → type `PrototypeBootstrap` → add it.

**Inspector should now show**:
```
Prototype Bootstrap (Script)
  Voxel Sandbox
    Asset File Name: test_cube.stasset
    Voxel Size: 0.5
    Voxel Renderer: None
    Weapon Controller: None

  Penetration Test
    Beam Power: 200

  Withdrawal Sandbox
    ...
```

4. **[OPTIONAL]** Drag the `Main Camera` GameObject from the Hierarchy into:
   - The **Voxel Renderer** slot
   - The **Weapon Controller** slot
   
   **Note**: Bootstrap automatically finds these components at runtime via `FindObjectOfType`, so manual assignment is optional. You'll see "Auto-located VoxelRenderer component" in the console if auto-detection works.

---

## Step 4: Verify StreamingAssets

1. In the **Project** panel, navigate to `Assets/StreamingAssets/`.
2. You should see `test_cube.stasset` (1 KB).
3. If it's missing, re-run the Python generator:
   ```powershell
   cd "c:\Users\NADECC\ATSTradingDashboard Project\Cursor Workshop\SteelTide\tools\asset_generator"
   python generator.py examples\test_cube.json -o "..\..\My project\Assets\StreamingAssets\test_cube.stasset"
   ```

---

## Step 5: Press Play!

1. Click the **▶ Play** button at the top center of Unity.
2. **Watch the Console** for these messages:
   ```
   [SteelTide] Auto-located VoxelRenderer component.
   [SteelTide] Auto-located VoxelWeaponController component.
   [SteelTide] Loaded 'test_cube.stasset': (8, 8, 8) grid, 512/512 solid voxels (100.0%)
   [SteelTide] Uploaded 1024 bytes to GPU Texture3D.
   [VoxelRenderer] Pulled Texture3D reference from PrototypeBootstrap.
   [SteelTide] VoxelWeaponController initialized — left-click to blast craters!
   [VoxelWeaponController] Pulled Texture3D reference from PrototypeBootstrap.
   [SteelTide] Penetration: absorbed=...
   ```

3. **Look at the Game window** (center/top tabs). You should see:
   - A **clean, flat-shaded tan/gray concrete cube** (8×8×8 voxels = 4×4×4 world units)
   - Sharp faces with directional lighting (no grain or "dusty" appearance)
   - Works from ANY camera position — proper AABB ray-box intersection implemented.
   
4. **Try different camera positions**:
   - Position `(4, 4, 6)`, Rotation `(20, -30, 0)` — angled top-down view
   - Position `(2, 2, -6)`, Rotation `(20, 0, 0)` — front view at a distance
   - Position `(8, 2, 0)`, Rotation `(0, -90, 0)` — side view

5. **🎯 BLAST CRATERS! (Interactive Destruction)**:
   - **Left-click on the cube** (aim at any face)
   - **1st hit**: Voxels flash to **CRIMSON RED** (damaged state)
   - **2nd hit** (click the red scar): Voxels vanish to **AIR** (hole carved through)
   - **Peer through holes**: Ray tracing passes through air — permanent structural damage!
   - Console logs each hit with location and voxel count
   - Damage persists throughout Play session

---

## Step 6: Troubleshooting

### "Cube won't render!" warning in Console
- **Fix**: Bootstrap should auto-locate VoxelRenderer at runtime. Check console for "Auto-located VoxelRenderer component."
- **If auto-location fails**: Manually drag `Main Camera` into `Bootstrap → Voxel Renderer` slot (optional since auto-location was added).

### Black screen / no cube visible
- **Background color test**: Change camera's **Background** to bright blue. If you see blue, the shader is working but the cube might be out of view. If still black, check the console for errors.
- **Camera position**: Select `Main Camera`, set Position to `(4, 4, 6)`, Rotation to `(20, -30, 0)`.
- **Cube position**: The cube spawns at world origin `(0, 0, 0)`. Check `Bootstrap → Volume Offset` is `(0, 0, 0)`.
- **Matrix patch applied**: The latest VoxelRaymarch.compute uses proper near/far plane reconstruction. If you see the background color, the ray math is working correctly.

### Shader errors in Console
- **Check**: `Assets/Voxels/VoxelRaymarch.compute` compiled without errors.
- **Verify**: The compute shader is assigned to `VoxelRenderer.Raymarch Shader` in the Inspector.

### Compilation errors
- **Missing namespace**: Make sure all scripts are in the correct `Assets/` folders (Voxels, Prototype, etc.).
- **DOTS packages**: Verify `Packages/manifest.json` has the required dependencies (should already be there from Phase 3).

### Input System errors (InvalidOperationException)
- **Error**: "You are trying to read Input using the UnityEngine.Input class..."
- **Cause**: Unity 6 uses New Input System by default, old Input API conflicts
- **Fix**: VoxelWeaponController.cs has been patched to use `Mouse.current.leftButton.wasPressedThisFrame` and `Mouse.current.position.ReadValue()`
- **If errors persist**: Click **Clear** in Console, wait for recompilation, then Play again

### Material Colors showing Size 6 instead of 16
- **Symptom**: Inspector shows old 6-color array, cube looks "dusty"
- **Cause**: Unity hasn't recompiled VoxelRenderer.cs or is showing cached Inspector data
- **Fix 1**: Click on VoxelRenderer.cs in Project panel → right-click → **Reimport**
- **Fix 2**: Select Main Camera → in Inspector, find VoxelRenderer → click the gear icon → **Reset**
- **Fix 3**: Stop Play mode, close/reopen Unity Editor
- **Expected**: After recompile, Material Colors should show **Size: 16** with crimson/orange damage colors at slots 13-15

### Cube still has grain/dust appearance
- **Cause**: Compute shader didn't recompile with new flat-shading code
- **Fix 1**: Click on VoxelRaymarch.compute in Project panel → right-click → **Reimport**
- **Fix 2**: Edit → Preferences → External Tools → **Regenerate project files**
- **Expected**: Cube should render as clean, flat-shaded solid color with sharp face lighting

### Voxel Volume shows "None (Texture3D)" during Play mode
- **Symptom**: Inspector shows empty Texture3D slots, cube has noisy "fingerprint" texture, clicks report "Missed — no solid voxel hit"
- **Root cause**: Components couldn't pull texture reference from Bootstrap
- **Fix**: Pull-based architecture now implemented (latest patch)
  - VoxelRenderer pulls texture in `LateUpdate()` from `PrototypeBootstrap.GeneratedVoxelTexture`
  - VoxelWeaponController pulls texture in `Update()` from same source
- **Check console for**:
  ```
  [SteelTide] Auto-located VoxelRenderer component.
  [SteelTide] Auto-located VoxelWeaponController component.
  [VoxelRenderer] Pulled Texture3D reference from PrototypeBootstrap.
  [VoxelWeaponController] Pulled Texture3D reference from PrototypeBootstrap.
  ```
- **Expected during Play**: Inspector slots populate with valid Texture3D reference within 1-2 frames
- **If still empty**: Bootstrap might not be creating texture — check for errors during asset load

### CS0618 obsolete warnings
- **Symptom**: Yellow warnings about `FindObjectOfType` being obsolete in Unity 6
- **Fix**: All scripts updated to use Unity 6's `FindFirstObjectByType` API
- **Expected**: Zero obsolete warnings in console

---

## Expected Result

When everything is wired correctly, pressing Play should show:
- **Console**: Load confirmation, GPU upload confirmation, penetration test output.
- **Game window**: A solid tan cube (8×8×8 voxels at 0.5 world units = 4×4×4 Unity units) rendered via GPU raymarching.

**This is your "Hello World" for the Steel Tide voxel pipeline.**

Once you see the cube, the entire data flow is proven:
```
JSON → Python → .stasset → C# NativeArray → GPU Texture3D → Compute Shader → Screen
```

Next steps: Rotate the cube, load complex assets, add lighting, wire up real-time destruction.

---

**Estimated setup time**: 5-10 minutes  
**Compiler dependencies**: DOTS packages (already installed)  
**External dependencies**: None (Python generator already run)
