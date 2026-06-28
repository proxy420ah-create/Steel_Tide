# Unity Material Colors Array - Quick Reference

**Version**: 1.0  
**Last Updated**: June 27, 2026

Use this guide to update the Material Colors array in Unity Inspector.

---

## 📋 **Unity Inspector Setup**

1. Select any GameObject with **VoxelRenderer** component
2. Expand **Material Colors (Index by material ID)**
3. Set **Size** to `21`
4. Copy the RGB values below for each element

---

## 🎨 **Material Colors (0-1 Range)**

### **Element 0: Air**
- R: `0`
- G: `0`
- B: `0`
- A: `0`

### **Element 1: Prefab Composite**
- R: `0.65`
- G: `0.6`
- B: `0.55`
- A: `1`

### **Element 2: Regolith Concrete**
- R: `0.25`
- G: `0.2`
- B: `0.18`
- A: `1`

### **Element 3: Concrete**
- R: `0.5`
- G: `0.5`
- B: `0.5`
- A: `1`

### **Element 4: Flesh**
- R: `1`
- G: `0.6`
- B: `0.6`
- A: `1`

### **Element 5: Durasteel**
- R: `0.28`
- G: `0.3`
- B: `0.32`
- A: `1`

### **Element 6: Regolith**
- R: `0.4`
- G: `0.3`
- B: `0.2`
- A: `1`

### **Element 7: Xenoflora**
- R: `0.15`
- G: `0.55`
- B: `0.5`
- A: `1`

### **Element 8: Basalt**
- R: `0.25`
- G: `0.25`
- B: `0.27`
- A: `1`

### **Element 9: Wood**
- R: `0.6`
- G: `0.4`
- B: `0.2`
- A: `1`

### **Element 10: Transparent Aluminum**
- R: `0.85`
- G: `0.92`
- B: `1`
- A: `0.4`

### **Element 11: Uniform**
- R: `0.2`
- G: `0.3`
- B: `0.2`
- A: `1`

### **Element 12: Reserved**
- R: `0`
- G: `0`
- B: `0`
- A: `0`

### **Element 13: Damaged Concrete**
- R: `0.85`
- G: `0.15`
- B: `0.15`
- A: `1`

### **Element 14: Damaged Steel**
- R: `0.9`
- G: `0.4`
- B: `0.1`
- A: `1`

### **Element 15: Damaged Armor**
- R: `0.8`
- G: `0.2`
- B: `0.2`
- A: `1`

### **Element 16: Ablative Plating**
- R: `0.15`
- G: `0.16`
- B: `0.18`
- A: `1`

### **Element 17: Reactive Armor**
- R: `0.35`
- G: `0.38`
- B: `0.36`
- A: `1`

### **Element 18: Foam-Crete**
- R: `0.75`
- G: `0.75`
- B: `0.78`
- A: `1`

### **Element 19: Nanomesh Fabric**
- R: `0.2`
- G: `0.25`
- B: `0.3`
- A: `1`

### **Element 20: Plasteel Panels**
- R: `0.45`
- G: `0.48`
- B: `0.5`
- A: `1`

---

## 🎨 **Alternative: RGB (0-255 Range)**

If your Unity color picker uses 0-255 range:

```
Element 0:  RGB(0, 0, 0) A:0
Element 1:  RGB(166, 153, 140) A:255
Element 2:  RGB(64, 51, 46) A:255
Element 3:  RGB(128, 128, 128) A:255
Element 4:  RGB(255, 153, 153) A:255
Element 5:  RGB(71, 77, 82) A:255
Element 6:  RGB(102, 76, 51) A:255
Element 7:  RGB(38, 140, 128) A:255
Element 8:  RGB(64, 64, 69) A:255
Element 9:  RGB(153, 102, 51) A:255
Element 10: RGB(217, 235, 255) A:102
Element 11: RGB(51, 76, 51) A:255
Element 12: RGB(0, 0, 0) A:0
Element 13: RGB(217, 38, 38) A:255
Element 14: RGB(230, 102, 26) A:255
Element 15: RGB(204, 51, 51) A:255
Element 16: RGB(38, 41, 46) A:255
Element 17: RGB(89, 97, 92) A:255
Element 18: RGB(191, 191, 199) A:255
Element 19: RGB(51, 64, 77) A:255
Element 20: RGB(115, 122, 128) A:255
```

---

## ✅ **Verification Checklist**

After updating Unity Material Colors:

- [ ] Size set to `21`
- [ ] All 21 elements have colors assigned
- [ ] Element 0 (Air) is transparent (A=0)
- [ ] Element 10 (Transparent Aluminum) is semi-transparent (A=0.4 or 102)
- [ ] Element 12 (Reserved) is transparent (A=0)
- [ ] Press Play and check voxel rendering
- [ ] Verify colors match Voxel Studio preview

---

## 🔧 **Quick Copy-Paste (C# Code)**

If you need to set colors programmatically:

```csharp
Color[] materialColors = new Color[21]
{
    new Color(0f, 0f, 0f, 0f),           // 0: Air
    new Color(0.65f, 0.6f, 0.55f, 1f),   // 1: Prefab Composite
    new Color(0.25f, 0.2f, 0.18f, 1f),   // 2: Regolith Concrete
    new Color(0.5f, 0.5f, 0.5f, 1f),     // 3: Concrete
    new Color(1f, 0.6f, 0.6f, 1f),       // 4: Flesh
    new Color(0.28f, 0.3f, 0.32f, 1f),   // 5: Durasteel
    new Color(0.4f, 0.3f, 0.2f, 1f),     // 6: Regolith
    new Color(0.15f, 0.55f, 0.5f, 1f),   // 7: Xenoflora
    new Color(0.25f, 0.25f, 0.27f, 1f),  // 8: Basalt
    new Color(0.6f, 0.4f, 0.2f, 1f),     // 9: Wood
    new Color(0.85f, 0.92f, 1f, 0.4f),   // 10: Transparent Aluminum
    new Color(0.2f, 0.3f, 0.2f, 1f),     // 11: Uniform
    new Color(0f, 0f, 0f, 0f),           // 12: Reserved
    new Color(0.85f, 0.15f, 0.15f, 1f),  // 13: Damaged Concrete
    new Color(0.9f, 0.4f, 0.1f, 1f),     // 14: Damaged Steel
    new Color(0.8f, 0.2f, 0.2f, 1f),     // 15: Damaged Armor
    new Color(0.15f, 0.16f, 0.18f, 1f),  // 16: Ablative Plating
    new Color(0.35f, 0.38f, 0.36f, 1f),  // 17: Reactive Armor
    new Color(0.75f, 0.75f, 0.78f, 1f),  // 18: Foam-Crete
    new Color(0.2f, 0.25f, 0.3f, 1f),    // 19: Nanomesh Fabric
    new Color(0.45f, 0.48f, 0.5f, 1f),   // 20: Plasteel Panels
};
```

---

**Last Updated**: June 27, 2026  
**Related Documents**: `MATERIAL_SYSTEM.md`, `MATERIAL_EXPANSION_GUIDE.md`
