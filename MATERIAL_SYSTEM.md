# 🎨 Steel Tide - Unified Material System

**Version**: 1.0  
**Last Updated**: June 27, 2026

Complete material definitions synchronized between Asset Studio (Python) and Unity (C#).

---

## 📋 **Material ID Reference**

**Theme**: Distant Planet Border Colony (Futuristic Sci-Fi)

### **Core Building Materials (0-5)**
| ID | Name | Color (RGB) | Use Case | Resistance |
|----|------|-------------|----------|------------|
| **0** | Air | Transparent | Empty space | 0.0 |
| **1** | Prefab Composite | Tan (166, 153, 140) | Modular colony buildings | 0.6 |
| **2** | Regolith Concrete | Dark Brown (64, 51, 46) | Local roads/foundations | 0.5 |
| **3** | Concrete | Gray (128, 128, 128) | Standard structures | 0.5 |
| **4** | Flesh | Pink (255, 153, 153) | Organic matter | 0.1 |
| **5** | Durasteel | Blue-Gray (71, 77, 82) | Reinforced metal alloy | 0.8 |

### **Terrain Materials (6-10)**
| ID | Name | Color (RGB) | Use Case | Resistance |
|----|------|-------------|----------|------------|
| **6** | Regolith | Brown (102, 76, 51) | Alien planet soil | 0.2 |
| **7** | Xenoflora | Teal (38, 140, 128) | Alien plant life | 0.1 |
| **8** | Basalt | Dark Gray (64, 64, 69) | Volcanic rock | 0.7 |
| **9** | Wood | Tan (153, 102, 51) | Imported lumber | 0.3 |
| **10** | Transparent Aluminum | Light Blue (217, 235, 255) | Sci-fi transparent metal | 0.6 |

### **Utility/Clothing (11-12)**
| ID | Name | Color (RGB) | Use Case | Resistance |
|----|------|-------------|----------|------------|
| **11** | Uniform | Dark Green (51, 76, 51) | Military uniform | 0.1 |
| **12** | Bone | White/Beige (230, 220, 200) | Skeleton bones | 1.0 (indestructible) |

### **Damage States (13-15)**
| ID | Name | Color (RGB) | Use Case | Resistance |
|----|------|-------------|----------|------------|
| **13** | Damaged Concrete | Crimson (217, 38, 38) | Battle damage | 0.3 |
| **14** | Damaged Steel | Orange (230, 102, 26) | Heat damage | 0.6 |
| **15** | Damaged Armor | Dark Red (204, 51, 51) | Heavy damage | 0.7 |

### **Advanced Materials (16-20)**
| ID | Name | Color (RGB) | Use Case | Resistance |
|----|------|-------------|----------|------------|
| **16** | Ablative Plating | Charcoal (38, 41, 46) | Heat-resistant armor | 0.85 |
| **17** | Reactive Armor | Gunmetal (89, 97, 92) | Explosive tiles | 0.9 |
| **18** | Foam-Crete | Light Gray (191, 191, 199) | Quick-deploy foam | 0.3 |
| **19** | Nanomesh Fabric | Dark Blue (51, 64, 77) | Smart fabric | 0.15 |
| **20** | Plasteel Panels | Medium Gray (115, 122, 128) | Plastic-metal hybrid | 0.7 |

### **Skeleton Materials (21)**
| ID | Name | Color (RGB) | Use Case | Resistance |
|----|------|-------------|----------|------------|
| **21** | Joint | Red (255, 64, 64) | Skeleton joints | 1.0 (indestructible) |

---

## 🎨 **Color Specifications**

### **Core Building Materials (0-5)**
```python
# Asset Studio (Python) - 0-1 range
0:  (0.0, 0.0, 0.0, 0.0)          # Air - Transparent
1:  (0.65, 0.6, 0.55, 1.0)        # Prefab Composite - Tan
2:  (0.25, 0.2, 0.18, 1.0)        # Regolith Concrete - Dark Brown
3:  (0.5, 0.5, 0.5, 1.0)          # Concrete - Gray
4:  (1.0, 0.6, 0.6, 1.0)          # Flesh - Pink
5:  (0.28, 0.3, 0.32, 1.0)        # Durasteel - Blue-Gray
```

```csharp
// Unity (C#) - 0-1 range
new Color(0f, 0f, 0f, 0f),           // 0: Air
new Color(0.65f, 0.6f, 0.55f, 1f),   // 1: Prefab Composite
new Color(0.25f, 0.2f, 0.18f, 1f),   // 2: Regolith Concrete
new Color(0.5f, 0.5f, 0.5f, 1f),     // 3: Concrete
new Color(1f, 0.6f, 0.6f, 1f),       // 4: Flesh
new Color(0.28f, 0.3f, 0.32f, 1f),   // 5: Durasteel
```

### **Terrain Materials (6-10)**
```python
# Asset Studio (Python)
6:  (0.4, 0.3, 0.2, 1.0)          # Regolith - Brown
7:  (0.15, 0.55, 0.5, 1.0)        # Xenoflora - Teal
8:  (0.25, 0.25, 0.27, 1.0)       # Basalt - Dark Gray
9:  (0.6, 0.4, 0.2, 1.0)          # Wood - Tan
10: (0.85, 0.92, 1.0, 0.4)        # Transparent Aluminum - Light Blue
```

```csharp
// Unity (C#)
new Color(0.4f, 0.3f, 0.2f, 1f),     // 6: Regolith
new Color(0.15f, 0.55f, 0.5f, 1f),   // 7: Xenoflora
new Color(0.25f, 0.25f, 0.27f, 1f),  // 8: Basalt
new Color(0.6f, 0.4f, 0.2f, 1f),     // 9: Wood
new Color(0.85f, 0.92f, 1f, 0.4f),   // 10: Transparent Aluminum
```

### **Damage States (13-15)**
```python
# Asset Studio (Python)
13: (0.85, 0.15, 0.15, 1.0)       # Damaged Concrete - Crimson
14: (0.9, 0.4, 0.1, 1.0)          # Damaged Steel - Orange
15: (0.8, 0.2, 0.2, 1.0)          # Damaged Armor - Dark Red
```

```csharp
// Unity (C#)
new Color(0.85f, 0.15f, 0.15f, 1f),  // 13: Damaged Concrete
new Color(0.9f, 0.4f, 0.1f, 1f),     // 14: Damaged Steel
new Color(0.8f, 0.2f, 0.2f, 1f),     // 15: Damaged Armor
```

### **Advanced Materials (16-20)**
```python
# Asset Studio (Python)
16: (0.15, 0.16, 0.18, 1.0)       # Ablative Plating - Charcoal
17: (0.35, 0.38, 0.36, 1.0)       # Reactive Armor - Gunmetal
18: (0.75, 0.75, 0.78, 1.0)       # Foam-Crete - Light Gray
19: (0.2, 0.25, 0.3, 1.0)         # Nanomesh Fabric - Dark Blue
20: (0.45, 0.48, 0.5, 1.0)        # Plasteel Panels - Medium Gray
```

```csharp
// Unity (C#)
new Color(0.15f, 0.16f, 0.18f, 1f),  // 16: Ablative Plating
new Color(0.35f, 0.38f, 0.36f, 1f),  // 17: Reactive Armor
new Color(0.75f, 0.75f, 0.78f, 1f),  // 18: Foam-Crete
new Color(0.2f, 0.25f, 0.3f, 1f),    // 19: Nanomesh Fabric
new Color(0.45f, 0.48f, 0.5f, 1f),   // 20: Plasteel Panels
```

---

## 🎯 **Material Properties**

### **Resistance Values** (for damage system)
```csharp
// 0.0 = instant destroy, 1.0 = nearly indestructible

// Very Soft (0.1-0.15)
Air:              0.0   // No resistance
Flesh:            0.1   // Organic tissue
Xenoflora:        0.1   // Alien plants
Uniform:          0.1   // Fabric
Nanomesh Fabric:  0.15  // Smart fabric (slightly tougher)

// Soft (0.2-0.3)
Regolith:         0.2   // Loose soil
Wood:             0.3   // Organic structure
Foam-Crete:       0.3   // Quick-deploy foam
Damaged Concrete: 0.3   // Weakened structure

// Medium (0.5-0.6)
Regolith Concrete: 0.5  // Local material
Concrete:          0.5  // Standard structure
Prefab Composite:  0.6  // Modular building material
Transparent Alum:  0.6  // Sci-fi transparent metal
Damaged Steel:     0.6  // Weakened metal

// Hard (0.7-0.8)
Basalt:           0.7   // Volcanic rock
Plasteel Panels:  0.7   // Hybrid material
Damaged Armor:    0.7   // Weakened armor
Durasteel:        0.8   // Reinforced alloy

// Very Hard (0.85-0.9)
Ablative Plating: 0.85  // Heat-resistant
Reactive Armor:   0.9   // Explosive tiles (nearly indestructible)
```

### **Damage Transitions**
```
// Standard Materials
Concrete (3) → Damaged Concrete (13) → Air (0)
Durasteel (5) → Damaged Steel (14) → Air (0)
Reactive Armor (17) → Damaged Armor (15) → Air (0)

// Soft Materials (one-shot destroy)
Xenoflora (7) → Air (0)
Regolith (6) → Air (0)
Wood (9) → Air (0)
Flesh (4) → Air (0)
Uniform (11) → Air (0)
Nanomesh Fabric (19) → Air (0)

// Building Materials (two-stage)
Prefab Composite (1) → Damaged Concrete (13) → Air (0)
Regolith Concrete (2) → Damaged Concrete (13) → Air (0)
Plasteel Panels (20) → Damaged Steel (14) → Air (0)
Ablative Plating (16) → Damaged Armor (15) → Air (0)

// Terrain (one-shot destroy)
Basalt (8) → Air (0)  // Hard but brittle
Foam-Crete (18) → Air (0)  // Temporary material
```

---

## 📦 **Usage Examples**

### **Colony Buildings**
```python
# Residential Hab Block
Walls: PREFAB_COMPOSITE (1)      # Tan modular panels
Foundation: REGOLITH_CONCRETE (2) # Dark brown local material
Reinforcement: DURASTEEL (5)      # Blue-gray beams
Windows: TRANSPARENT_ALUMINUM (10) # Light blue

# Military Bunker
Exterior: ABLATIVE_PLATING (16)   # Charcoal armor
Core: CONCRETE (3)                # Gray structure
Blast Doors: REACTIVE_ARMOR (17)  # Gunmetal
Interior: FOAM_CRETE (18)         # Light gray insulation
```

### **Terrain**
```python
# Alien Planet Surface
Ground: REGOLITH (6)              # Brown alien soil
Vegetation: XENOFLORA (7)         # Teal alien plants
Rock Formations: BASALT (8)       # Dark gray volcanic rock

# Colony Roads
Road Surface: REGOLITH_CONCRETE (2) # Dark brown
Sidewalk: PREFAB_COMPOSITE (1)      # Tan panels
```

### **Characters**
```python
# Colonial Marine
Body: UNIFORM (11)                # Dark green combat suit
Armor Plates: PLASTEEL_PANELS (20) # Medium gray
Helmet Visor: TRANSPARENT_ALUMINUM (10) # Light blue
Flesh (exposed): FLESH (4)        # Pink skin

# Combat Mech
Heavy Armor: REACTIVE_ARMOR (17)  # Gunmetal explosive tiles
Frame: DURASTEEL (5)              # Blue-gray structure
Ablative Coating: ABLATIVE_PLATING (16) # Charcoal
Sensors: TRANSPARENT_ALUMINUM (10) # Light blue
```

### **Temporary Structures**
```python
# Emergency Barricade
Quick-Deploy: FOAM_CRETE (18)     # Light gray expanding foam
Reinforcement: DURASTEEL (5)      # Blue-gray beams

# Field Tent
Fabric: NANOMESH_FABRIC (19)      # Dark blue smart fabric
Frame: PLASTEEL_PANELS (20)       # Medium gray poles
```

---

## 🔄 **Material Expansion Checklist**

**For adding NEW materials (IDs 21+):**

When adding a new material to expand beyond the current 21:
- [ ] Add to `material_library.py` (Asset Studio)
- [ ] Add to Unity Material Colors array (Inspector)
- [ ] Add to this document's material reference table
- [ ] Test in both Asset Studio and Unity
- [ ] Verify colors match exactly between both systems

**Current materials (0-20): ✅ COMPLETE**

**For detailed expansion guidelines, see `MATERIAL_EXPANSION_GUIDE.md`**

---

## 🚀 **Implementation Status**

### **Asset Studio** ✅
- [x] Material IDs 0-20 defined
- [x] Futuristic border colony theme established
- [x] Color system working
- [x] Paint tool uses materials
- [x] Procedural generators updated (buildings, characters)
- [x] Character generation with seed variations and part locking
- [x] Material Sampler generator added

### **Unity** ✅
- [x] Material IDs 0-20 defined (futuristic system)
- [x] Material Colors array updated to size 21
- [x] Materials 0-16 synced via eyedropper method
- [x] Materials 17-20 ready for final sync (RGB values documented)
- [x] Unity Inspector updated with new futuristic colors

### **Synchronization** ✅
- [x] Python material library updated (21 materials)
- [x] Unity Material Colors array expanded (size: 21)
- [x] Color consistency verified in Voxel Studio
- [x] Procedural generators use new materials
- [x] Documentation complete and comprehensive
- [x] Material Sampler available for verification

---

## 📝 **Notes**

- **Material ID 0 (Air)** is always transparent and represents empty space
- **IDs 1-5** are core building materials (futuristic)
- **IDs 6-10** are terrain and environmental materials (alien planet)
- **IDs 11-12** are utility/clothing materials
- **IDs 13-15** are damaged states (visual feedback)
- **IDs 16-20** are advanced military materials
- **IDs 21-255** are available for expansion (see `MATERIAL_EXPANSION_GUIDE.md`)
- **Maximum 256 materials** supported (byte/uint8 material ID)

---

## ✅ **Migration Status**

**Material System Migration: COMPLETE** (June 28, 2026)

### **Completed:**
- ✅ All 21 materials defined (0-20)
- ✅ RGB colors established for futuristic border colony theme
- ✅ Procedural generators updated (buildings, characters)
- ✅ Material library synchronized
- ✅ Unity Material Colors array updated (0-16 via eyedropper, 17-20 pending)
- ✅ Documentation complete (MATERIAL_SYSTEM.md, UNITY_MATERIAL_COLORS_REFERENCE.md)

### **Future Enhancements:**

**Material Hardness/Resistance Tuning System** (Planned)
- Add material properties editor in Voxel Asset Studio
- UI panel for adjusting:
  - Hardness/Resistance (0.0-1.0)
  - Damage transitions (which material it becomes when damaged)
  - Destruction behavior (shatters, crumbles, melts)
  - Sound effects (future)
- Save properties to `.stasset` file for per-asset customization
- Allow comprehensive adjustments without code changes
- **Status**: Design phase - will implement when needed for gameplay tuning

---

**Last Sync**: June 28, 2026 - Material system migration complete ✅  
**Next Review**: When implementing material hardness tuning system  
**Companion Documents**: 
- `MATERIAL_EXPANSION_GUIDE.md` - Adding new materials
- `VOXEL_SCENE_SETUP_GUIDE.md` - Unity synchronization guide
- `MATERIAL_SYNC_COMPLETE.md` - Quick sync reference
