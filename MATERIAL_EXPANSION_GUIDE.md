# 🎨 Material Expansion Guide

**Version**: 1.0  
**Last Updated**: June 27, 2026  
**Purpose**: Intelligent guidelines for adding new materials to Steel Tide's voxel system

---

## 📋 **Core Principles**

### **1. Never Modify Existing IDs (0-20)**
- ✅ **Safe**: Add new materials (21-255)
- ✅ **Safe**: Change colors of existing materials
- ✅ **Safe**: Rename materials for clarity
- ❌ **UNSAFE**: Change material ID numbers (breaks existing assets)
- ❌ **UNSAFE**: Delete material IDs (breaks existing assets)

### **2. Material Slot Budget**
- **Total Available**: 256 slots (0-255)
- **Currently Used**: 21 slots (0-20)
- **Remaining**: 235 slots
- **Reserved for Future**: Recommend keeping 50+ slots free

### **3. Thematic Consistency**
- **Setting**: Distant planet border colony (futuristic sci-fi)
- **Tone**: Utilitarian, military, resource-scarce
- **Avoid**: Fantasy elements, modern Earth-specific items (unless imported)
- **Prefer**: Sci-fi materials, alien variants, military-grade tech

---

## 🗂️ **Material Categories & Slot Ranges**

### **Core System (0-20) - LOCKED ✅**
```
0-5:   Core Building Materials
6-10:  Terrain & Environment
11-12: Utility/Clothing
13-15: Damage States
16-20: Advanced Military Materials
```

### **Expansion Slots (21-255) - Available**

**Recommended Organization:**

```
21-30:  Decorative/Interior Materials
31-40:  Additional Terrain Variants
41-50:  Liquids & Semi-Transparent
51-60:  Energy/Glowing Materials
61-70:  Organic/Biological
71-80:  Industrial/Machinery
81-90:  Exotic/Alien Materials
91-100: Weather/Environmental Effects
101+:   Unit-Specific Armor/Tech (when needed)
```

---

## 🚀 **Adding New Materials: Step-by-Step**

### **Step 1: Design the Material**

**Ask These Questions:**
1. **What's the use case?** (Building? Terrain? Decoration?)
2. **What's the visual identity?** (Color, texture concept)
3. **What's the resistance?** (0.0 = fragile, 1.0 = indestructible)
4. **Does it fit the setting?** (Futuristic border colony)
5. **Is there already a similar material?** (Avoid duplicates)

**Example:**
```
Material: Holographic Display Panel
Use Case: Interior decoration, computer screens
Color: Cyan with transparency (0.3, 0.8, 1.0, 0.6)
Resistance: 0.1 (fragile electronics)
Setting Fit: ✅ Futuristic tech
Similar?: Transparent Aluminum (but different use)
```

### **Step 2: Choose the Next Available Slot**

**Check Current Max ID:**
```python
# In material_library.py
max_id = max(MATERIALS.keys())  # Currently 20
next_id = max_id + 1            # Use 21
```

**Or use category ranges:**
```python
# Decorative material? Use 21-30 range
next_decorative_id = 21  # First in range
```

### **Step 3: Add to `material_library.py`**

```python
# In MATERIALS dictionary
21: {"name": "Holographic Display", "color": (0.3, 0.8, 1.0, 0.6)},
```

**Add to DEFAULT_MATERIALS list:**
```python
DEFAULT_MATERIALS = [
    # ... existing ...
    21,  # Holographic Display
]
```

### **Step 4: Update Unity Material Colors**

1. Open Unity Inspector on any VoxelRenderer component
2. Expand **Material Colors** array
3. Set **Size** to new count (e.g., 22)
4. Set **Element 21** color:
   - R: 0.3
   - G: 0.8
   - B: 1.0
   - A: 0.6

### **Step 5: Document in `MATERIAL_SYSTEM.md`**

Add row to the material table:
```markdown
| **21** | Holographic Display | Cyan (77, 204, 255) | Interior tech | 0.1 |
```

### **Step 6: Test in Voxel Studio**

1. Launch Voxel Asset Studio
2. Check material palette - new material should appear
3. Paint some voxels with it
4. Save test asset
5. Load in Unity and verify color matches

---

## 📦 **Suggested Material Packs**

### **Pack 1: Interior Decoration (21-30)**

```python
21: {"name": "Holographic Display", "color": (0.3, 0.8, 1.0, 0.6)},    # Cyan transparent
22: {"name": "Carpet Tile", "color": (0.35, 0.25, 0.2, 1.0)},          # Dark brown
23: {"name": "Ceiling Panel", "color": (0.85, 0.85, 0.82, 1.0)},       # Off-white
24: {"name": "Grating", "color": (0.4, 0.4, 0.42, 1.0)},               # Dark gray metal
25: {"name": "Neon Light Strip", "color": (1.0, 0.2, 0.4, 1.0)},       # Hot pink (glowing)
26: {"name": "Rubber Flooring", "color": (0.15, 0.15, 0.18, 1.0)},     # Near-black
27: {"name": "Insulation Foam", "color": (0.95, 0.85, 0.6, 1.0)},      # Yellow-tan
28: {"name": "Wiring Conduit", "color": (0.5, 0.45, 0.4, 1.0)},        # Brown-gray
29: {"name": "Ventilation Duct", "color": (0.55, 0.55, 0.57, 1.0)},    # Silver-gray
30: {"name": "Emergency Lighting", "color": (1.0, 0.8, 0.0, 1.0)},     # Yellow (glowing)
```

### **Pack 2: Terrain Variants (31-40)**

```python
31: {"name": "Sand", "color": (0.86, 0.78, 0.62, 1.0)},                # Tan
32: {"name": "Gravel", "color": (0.5, 0.48, 0.45, 1.0)},               # Gray-brown
33: {"name": "Ice", "color": (0.85, 0.92, 0.98, 0.7)},                 # Light blue transparent
34: {"name": "Lava Rock", "color": (0.2, 0.15, 0.12, 1.0)},            # Black-brown
35: {"name": "Crystal Formation", "color": (0.6, 0.3, 0.8, 0.6)},      # Purple transparent
36: {"name": "Toxic Sludge", "color": (0.4, 0.6, 0.1, 0.8)},           # Sickly green
37: {"name": "Ash", "color": (0.3, 0.28, 0.26, 1.0)},                  # Dark gray
38: {"name": "Coral (Alien)", "color": (0.8, 0.4, 0.6, 1.0)},          # Pink-purple
39: {"name": "Permafrost", "color": (0.7, 0.75, 0.8, 1.0)},            # Icy gray
40: {"name": "Sulfur Deposits", "color": (0.9, 0.85, 0.2, 1.0)},       # Yellow
```

### **Pack 3: Energy/Glowing (51-60)**

```python
51: {"name": "Plasma Conduit", "color": (0.2, 0.6, 1.0, 0.8)},         # Bright blue glow
52: {"name": "Reactor Core", "color": (0.0, 1.0, 0.8, 0.9)},           # Cyan glow
53: {"name": "Energy Cell", "color": (1.0, 0.9, 0.2, 0.8)},            # Yellow glow
54: {"name": "Force Field", "color": (0.4, 0.7, 1.0, 0.3)},            # Blue transparent
55: {"name": "Radiation Warning", "color": (1.0, 0.8, 0.0, 1.0)},      # Yellow-orange
56: {"name": "Bioluminescent", "color": (0.2, 0.9, 0.6, 0.7)},         # Green glow
57: {"name": "Arc Welder Spark", "color": (0.9, 0.95, 1.0, 1.0)},      # White-blue
58: {"name": "Laser Emitter", "color": (1.0, 0.1, 0.1, 0.9)},          # Red glow
59: {"name": "Shield Generator", "color": (0.5, 0.8, 1.0, 0.5)},       # Light blue
60: {"name": "Power Relay", "color": (0.8, 0.6, 1.0, 0.7)},            # Purple glow
```

---

## ⚠️ **Common Mistakes to Avoid**

### **❌ Mistake 1: Reusing IDs**
```python
# DON'T DO THIS!
7: {"name": "Xenoflora", ...},     # Already exists
7: {"name": "New Material", ...},  # Overwrites Xenoflora!
```

### **❌ Mistake 2: Skipping Documentation**
```python
# Added material 21 but forgot to:
# - Update MATERIAL_SYSTEM.md
# - Update Unity Material Colors array
# - Test in Voxel Studio
# Result: Material renders as black in Unity!
```

### **❌ Mistake 3: Breaking Theme**
```python
# Setting: Futuristic border colony
42: {"name": "Medieval Stone Brick", ...},  # ❌ Doesn't fit!
43: {"name": "Dragon Scale", ...},          # ❌ Fantasy element!
44: {"name": "Smartphone Screen", ...},     # ❌ Too modern/Earth-specific!
```

### **❌ Mistake 4: Color Conflicts**
```python
# Two materials with identical colors
8: {"name": "Basalt", "color": (0.25, 0.25, 0.27, 1.0)},
21: {"name": "Dark Metal", "color": (0.25, 0.25, 0.27, 1.0)},  # ❌ Same color!
# Players can't tell them apart visually!
```

---

## ✅ **Material Addition Checklist**

Before adding a new material, verify:

- [ ] Material fits the futuristic border colony theme
- [ ] Color is visually distinct from existing materials
- [ ] Use case is clear and documented
- [ ] Next available ID is used (no gaps, no overwrites)
- [ ] Added to `material_library.py` MATERIALS dict
- [ ] Added to `material_library.py` DEFAULT_MATERIALS list
- [ ] Unity Material Colors array updated (size + color)
- [ ] `MATERIAL_SYSTEM.md` table updated
- [ ] Tested in Voxel Studio (appears in palette)
- [ ] Tested in Unity (renders with correct color)
- [ ] Resistance value makes sense for material type

---

## 🔧 **Maintenance Tasks**

### **Quarterly Review**
- Check for unused materials (IDs added but never used in assets)
- Verify color consistency across Python and Unity
- Update documentation for any renamed materials
- Review resistance values for game balance

### **Before Major Releases**
- Freeze material IDs (no new additions during release)
- Backup `material_library.py`
- Export Unity Material Colors array as JSON
- Document any material-specific gameplay mechanics

---

## 📞 **Quick Reference**

**Files to Update:**
1. `VoxelAssetStudio/material_library.py` (Python)
2. Unity VoxelRenderer Material Colors array (Inspector)
3. `MATERIAL_SYSTEM.md` (Documentation)

**Current Material Count:** 21 (IDs 0-20)  
**Next Available ID:** 21  
**Remaining Slots:** 235

**Theme:** Distant Planet Border Colony (Futuristic Sci-Fi)  
**Tone:** Utilitarian, Military, Resource-Scarce

---

*Last Updated: June 27, 2026 - Initial futuristic material system established*
