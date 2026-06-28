# 🤖 Character Generation System

**Version**: 1.0  
**Last Updated**: June 27, 2026

Advanced procedural character generation with seed-based variations and part locking.

---

## 🎯 **Features**

### **1. Seed-Based Variations**
Every time you click "Re-Generate", a new random seed creates a unique character variant:

**What Varies:**
- Body width/depth (±10%)
- Limb thickness (±15%)
- Head size (±10%)
- Shoulder width (±20%)

**What Stays Consistent:**
- Overall proportions (head/torso/leg ratios)
- Material assignments
- Height (unless you change it)

### **2. Part Locking System**
Lock individual parts to keep them while regenerating others:

**Lockable Parts:**
- 🔒 **Head** - Keep the same head shape/size
- 🔒 **Torso** - Keep the same body shape/shoulders
- 🔒 **Arms** - Keep the same arm thickness/length
- 🔒 **Legs** - Keep the same leg thickness/length
- 🔐 **Entire Character** - Lock everything (useful for material testing)

**Use Cases:**
- Found a great head? Lock it and regenerate the body
- Like the body proportions? Lock torso and regenerate limbs
- Testing materials? Lock entire character and change materials only

### **3. Material Control**
Separate material selection for each part:

- **Head Material** - Default: Flesh (pink)
- **Body Material** - Default: Uniform (dark green)
- **Limb Material** - Default: Plasteel Panels (gray)
- **Armor Material** - Default: Plasteel Panels (gray) - *Future use*

---

## 📏 **Size & Proportions**

### **Default Character (Height = 32 voxels)**

```
Height: 32 voxels (4m tall in voxel world)
├─ Head:  4 voxels (0.5m)
├─ Torso: 12 voxels (1.5m)
└─ Legs:  16 voxels (2m)

Width: ~10 voxels (1.25m)
Depth: ~6 voxels (0.75m)

Limb Thickness:
├─ Arms: 2-3 voxels wide
└─ Legs: 3-4 voxels wide
```

### **Why 32 Voxels (Not 16)?**

**Old system (16 voxels):**
- Arms were 1 voxel wide (stick figures!)
- Legs were 2 voxels wide (too thin)
- Characters looked like matchsticks

**New system (32 voxels):**
- Arms are 2-3 voxels wide (visible!)
- Legs are 3-4 voxels wide (sturdy)
- Characters look like actual soldiers/mechs

**Adjustable Range:** 16-64 voxels (use spinner in UI)

---

## 🎨 **Workflow Examples**

### **Example 1: Find the Perfect Character**

1. Click **"Generate Character"** → Random character appears
2. Not quite right? Click **"Re-Generate"** → New variation
3. Like the head? Check **"Lock Head"**
4. Click **"Re-Generate"** → New body, same head
5. Like the body now? Check **"Lock Torso"**
6. Click **"Re-Generate"** → New limbs, same head/body
7. Perfect! Save the asset

**Result:** You iterated through ~5-10 variations to find the perfect combination

### **Example 2: Material Testing**

1. Generate a character you like
2. Check **"Lock Entire Character"** 🔐
3. Change **Head Material** to different options
4. Click **"Re-Generate"** → Same shape, new head color
5. Change **Body Material** → Test different uniforms
6. Change **Limb Material** → Test different armor

**Result:** Test all material combinations without changing the shape

### **Example 3: Create a Squad**

1. Generate **Soldier 1** → Save as `Soldier_01.stasset`
2. Lock **Head** and **Torso**
3. Re-generate 3 times → Save as `Soldier_02`, `Soldier_03`, `Soldier_04`

**Result:** Squad of 4 soldiers with same face/body but different limb variations

---

## 🔧 **Technical Details**

### **Seed-Based Variation Formula**

```python
# Base dimensions
base_width = 10 voxels
base_depth = 6 voxels

# Seed determines variation
width_variation = random.uniform(0.9, 1.1)   # ±10%
depth_variation = random.uniform(0.9, 1.1)   # ±10%
limb_variation = random.uniform(0.85, 1.15)  # ±15%
head_variation = random.uniform(0.9, 1.1)    # ±10%
shoulder_variation = random.uniform(0.8, 1.2) # ±20%

# Final dimensions
width = base_width * width_variation
arm_width = (base_width // 5) * limb_variation
```

### **Part Locking Implementation**

When a part is locked:
1. Generator stores the voxel data for that part
2. On re-generate, locked parts are **copied** from storage
3. Unlocked parts are **regenerated** with new seed
4. Parts are **composited** together into final character

**Note:** Part locking is currently **prepared** in the code but requires integration with the main editor.

---

## 📋 **UI Controls**

### **Character Generator Panel**

```
🤖 Character Generator
├─ Size
│  └─ Height: [32] voxels
├─ Materials
│  ├─ Head: [Flesh (4)]
│  ├─ Body: [Uniform (11)]
│  ├─ Limbs: [Plasteel Panels (20)]
│  └─ Armor: [Plasteel Panels (20)]
├─ 🔒 Lock Parts
│  ├─ ☐ Lock Head
│  ├─ ☐ Lock Torso
│  ├─ ☐ Lock Arms
│  ├─ ☐ Lock Legs
│  └─ ☐ 🔐 Lock Entire Character
├─ Buttons
│  ├─ ✨ Generate Character
│  ├─ 🔄 Re-Generate (New Variation)
│  └─ 🔓 Unlock All Parts
└─ Seed: 1234
```

---

## 🚀 **Integration Status**

### **✅ Completed**

- [x] Seed-based variation system
- [x] Larger character size (32 voxels default)
- [x] Thicker limbs (2-3 voxels wide)
- [x] Material selection per part
- [x] Character generator panel UI
- [x] Lock checkbox UI

### **⚠️ Pending Integration**

- [ ] Wire character panel to main editor
- [ ] Implement part data storage
- [ ] Implement part locking logic
- [ ] Add to Shape menu or dedicated Character menu
- [ ] Test with Unity import

---

## 📝 **Usage Instructions**

### **Current Workflow (Manual)**

1. Open Voxel Asset Studio
2. Go to **Shape** menu → **Humanoid**
3. Character generates with random seed
4. Click **Re-Generate** button in toolbar for new variation
5. Save when you find one you like

### **Future Workflow (With Panel)**

1. Open Voxel Asset Studio
2. Open **Character Generator** panel (left sidebar)
3. Adjust height, materials as desired
4. Click **"Generate Character"**
5. Click **"Re-Generate"** to iterate
6. Lock parts you like, regenerate others
7. Save final character

---

## 🎯 **Best Practices**

### **Finding Good Characters**

1. **Start with defaults** - Generate 5-10 characters to see the range
2. **Lock incrementally** - Lock one part at a time as you find good ones
3. **Vary materials** - Test different material combinations
4. **Save variations** - Save multiple versions for variety

### **Creating Consistent Squads**

1. **Lock head + torso** - Gives consistent "look"
2. **Regenerate limbs** - Adds variety without breaking consistency
3. **Use same materials** - Uniform appearance
4. **Number sequentially** - `Marine_01`, `Marine_02`, etc.

### **Material Testing**

1. **Lock entire character** - Preserve the shape
2. **Change one material at a time** - See the effect clearly
3. **Screenshot comparisons** - Document which materials work best

---

## 🔮 **Future Enhancements**

### **Planned Features**

- **Armor Layer** - Separate armor plating over body
- **Equipment Slots** - Weapons, backpacks, helmets
- **Pose Variations** - Arms up, crouching, etc.
- **Gender/Build Presets** - Bulky, slim, average
- **Face Details** - Eyes, mouth (at larger scales)
- **Color Variations** - Skin tone, uniform colors

### **Advanced Locking**

- **Lock by material** - "Keep all gray parts"
- **Lock by region** - "Keep upper body"
- **Symmetry lock** - "Mirror left/right"

---

## 📞 **Quick Reference**

**Default Character:**
- Height: 32 voxels (4m)
- Width: ~10 voxels (1.25m)
- Arms: 2-3 voxels wide
- Legs: 3-4 voxels wide

**Variation Ranges:**
- Body: ±10%
- Limbs: ±15%
- Head: ±10%
- Shoulders: ±20%

**Materials:**
- Head: Flesh (4)
- Body: Uniform (11)
- Limbs: Plasteel Panels (20)

**Files:**
- Generator: `procedural_characters.py`
- UI Panel: `character_generator_panel.py`
- This Guide: `CHARACTER_GENERATION_GUIDE.md`

---

**Last Updated**: June 27, 2026  
**Related Documents**: `MATERIAL_SYSTEM.md`, `PROCEDURAL_GENERATOR_UPDATE_SUMMARY.md`
