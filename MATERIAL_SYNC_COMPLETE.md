# 🎨 Material Synchronization - Quick Guide

**Status**: Materials 0-16 ✅ | Materials 17-20 ⚠️ Pending

---

## ✅ **What You've Done**

You've successfully synced materials **0-16** using the eyedropper technique! Great work!

**Completed Materials:**
- ✅ 0-10: Core materials (Air → Transparent Aluminum)
- ✅ 11: Uniform (dark green)
- ✅ 13-15: Damage states (crimson, orange, dark red)
- ✅ 16: Ablative Plating (charcoal)

---

## ⚠️ **What's Left**

**Remaining Materials (17-20):**

```
ID  | Name             | RGB Color       | Description
----|------------------|-----------------|---------------------------
17  | Reactive Armor   | (89, 92, 97)    | Gunmetal explosive tiles
18  | Foam-Crete       | (191, 191, 191) | Light gray quick-deploy
19  | Nanomesh Fabric  | (26, 38, 64)    | Dark blue smart fabric
20  | Plasteel Panels  | (115, 115, 115) | Medium gray hybrid armor
```

---

## 🚀 **Quick Sync Method**

### **Option 1: Material Sampler (RECOMMENDED)**

**5-Minute Workflow:**

1. **Open Voxel Asset Studio**
   ```bash
   cd VoxelAssetStudio
   python main.py
   ```

2. **Generate Material Sampler**
   ```
   Generate → 🎨 Material Sampler (All Materials Grid)
   ```
   - Creates a grid showing ALL 21 materials
   - Each material is a distinct block
   - Perfect for eyedropper matching

3. **Save the Sampler**
   ```
   File → Save As → MaterialSampler.stasset
   ```

4. **Load in Unity**
   ```
   - Create GameObject
   - Add VoxelObject component
   - Asset File Name: MaterialSampler.stasset
   - Press Play
   ```

5. **Use Eyedropper on Materials 17-20**
   ```
   - Pause game
   - Scene view → Eyedropper tool
   - Hover over material label to identify each material
   - Copy RGB values to Material Colors array
   ```

6. **Update Unity Inspector**
   ```
   VoxelRenderer → Material Colors → Size: 21
   
   Element 17: (89, 92, 97)     ← Reactive Armor
   Element 18: (191, 191, 191)  ← Foam-Crete
   Element 19: (26, 38, 64)     ← Nanomesh Fabric
   Element 20: (115, 115, 115)  ← Plasteel Panels
   ```

---

### **Option 2: Manual Entry (FASTEST)**

**2-Minute Workflow:**

Just copy-paste these exact values into Unity Inspector:

```
VoxelRenderer → Material Colors → Size: 21

Element 17:
  R: 89
  G: 92
  B: 97

Element 18:
  R: 191
  G: 191
  B: 191

Element 19:
  R: 26
  G: 38
  B: 64

Element 20:
  R: 115
  G: 115
  B: 115
```

**Done!** ✅

---

## 🔍 **Verification**

**Test with a character:**

1. **Generate humanoid in Voxel Studio**
   ```
   Character Generator → Generate Character
   Limb Material: Plasteel Panels (20)
   Save as: TestCharacter.stasset
   ```

2. **Load in Unity**
   ```
   - Create GameObject
   - Add VoxelObject
   - Asset File Name: TestCharacter.stasset
   - Press Play
   ```

3. **Check limbs**
   - Should be **medium gray** (115, 115, 115)
   - NOT black or wrong color

**If limbs are correct → Sync complete!** ✅

---

## 📋 **Complete Material Reference**

**For your reference, here's the FULL list:**

```csharp
// Unity Inspector → VoxelRenderer → Material Colors → Size: 21

Element 0:  R=0,   G=0,   B=0     // Air
Element 1:  R=166, G=153, B=140   // Prefab Composite (tan) ✅
Element 2:  R=64,  G=51,  B=46    // Regolith Concrete (dark brown) ✅
Element 3:  R=128, G=128, B=128   // Concrete (gray) ✅
Element 4:  R=255, G=153, B=153   // Flesh (pink) ✅
Element 5:  R=71,  G=77,  B=82    // Durasteel (blue-gray) ✅
Element 6:  R=102, G=76,  B=51    // Regolith (brown) ✅
Element 7:  R=38,  G=140, B=128   // Xenoflora (teal) ✅
Element 8:  R=64,  G=64,  B=69    // Basalt (dark gray) ✅
Element 9:  R=153, G=102, B=51    // Wood (tan) ✅
Element 10: R=217, G=235, B=255   // Transparent Aluminum (light blue) ✅
Element 11: R=51,  G=76,  B=51    // Uniform (dark green) ✅
Element 12: R=0,   G=0,   B=0     // Reserved ✅
Element 13: R=217, G=38,  B=38    // Damaged Concrete (crimson) ✅
Element 14: R=230, G=102, B=26    // Damaged Steel (orange) ✅
Element 15: R=204, G=51,  B=51    // Damaged Armor (dark red) ✅
Element 16: R=38,  G=41,  B=46    // Ablative Plating (charcoal) ✅
Element 17: R=89,  G=92,  B=97    // Reactive Armor (gunmetal) ⚠️
Element 18: R=191, G=191, B=191   // Foam-Crete (light gray) ⚠️
Element 19: R=26,  G=38,  B=64    // Nanomesh Fabric (dark blue) ⚠️
Element 20: R=115, G=115, B=115   // Plasteel Panels (medium gray) ⚠️
```

---

## 🎯 **Why This Matters**

**Before sync:**
```
Voxel Studio: Plasteel Panels (20) = Medium Gray
Unity:        Material 20 = Black (missing!)
Result:       Character limbs appear BLACK ❌
```

**After sync:**
```
Voxel Studio: Plasteel Panels (20) = Medium Gray (115, 115, 115)
Unity:        Material 20 = Medium Gray (115, 115, 115)
Result:       Character limbs appear CORRECT ✅
```

---

## 📞 **Related Files**

- **Master Reference**: `MATERIAL_SYSTEM.md`
- **Unity Guide**: `UNITY_MATERIAL_COLORS_REFERENCE.md`
- **Scene Setup**: `VOXEL_SCENE_SETUP_GUIDE.md` (Material Synchronization section)
- **Character Guide**: `CHARACTER_GENERATION_GUIDE.md`

---

## ✅ **Completion Checklist**

- [x] Materials 0-16 synced (eyedropper method)
- [ ] Materials 17-20 synced (pending)
- [ ] Material Colors array size = 21
- [ ] Test character loaded and verified
- [ ] Colors match between Voxel Studio and Unity

**Once all checked → Material sync complete!** 🎉

---

**Last Updated**: June 28, 2026  
**Next Step**: Sync materials 17-20 (5 minutes!)
