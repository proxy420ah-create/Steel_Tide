# 🏗️ Unity Project Structure - Steel Tide

**Purpose**: Prevent file location confusion and ensure Unity can find all assets

---

## 📁 **Critical Rule: Everything Goes in Assets!**

### **✅ CORRECT Structure:**

```
My project\                          ← Unity project root
├─ Assets\                           ← EVERYTHING Unity uses goes HERE!
│  ├─ Scripts\                       ← All C# scripts
│  │  ├─ VoxelWorld.cs
│  │  ├─ VoxelPhysics.cs
│  │  ├─ VoxelModifier.cs
│  │  └─ VoxelObject.cs
│  ├─ Materials\                     ← Unity materials
│  ├─ Scenes\                        ← Scene files (.unity)
│  ├─ Prefabs\                       ← Prefabs
│  ├─ Textures\                      ← Textures/images
│  └─ StreamingAssets\               ← .stasset files (voxel models)
│     ├─ Ground_Voxel.stasset
│     ├─ Player_Voxel.stasset
│     └─ Building_House01.stasset
├─ Library\                          ← Unity cache (auto-generated)
├─ Logs\                             ← Unity logs (auto-generated)
├─ Packages\                         ← Package manager
├─ ProjectSettings\                  ← Project settings
└─ Temp\                             ← Temporary files (auto-generated)
```

### **❌ WRONG Structure:**

```
My project\
├─ Scripts\                          ← ❌ WRONG! Outside Assets!
│  └─ VoxelWorld.cs                  ← Unity CAN'T see this!
├─ Assets\
│  └─ (empty)
```

---

## 🎯 **The Golden Rule**

**If Unity needs to use it → Put it in `Assets\`**

### **What Goes in Assets:**
- ✅ Scripts (.cs files)
- ✅ Scenes (.unity files)
- ✅ Materials (.mat files)
- ✅ Prefabs (.prefab files)
- ✅ Textures/Images (.png, .jpg)
- ✅ Models (.fbx, .obj)
- ✅ Audio (.wav, .mp3)
- ✅ Custom assets (.stasset for voxels)

### **What Stays Outside Assets:**
- ✅ Library\ (Unity cache)
- ✅ Logs\ (Unity logs)
- ✅ Temp\ (temporary files)
- ✅ ProjectSettings\ (project config)
- ✅ Packages\ (package manager)

---

## 🚨 **Common Mistakes & Fixes**

### **Mistake 1: Scripts at Project Root**

**Problem:**
```
My project\
├─ Scripts\           ← ❌ Wrong location!
└─ Assets\
```

**Fix:**
```
Move Scripts\ folder INTO Assets\

Result:
My project\
└─ Assets\
   └─ Scripts\        ← ✅ Correct!
```

### **Mistake 2: .stasset Files Outside StreamingAssets**

**Problem:**
```
My project\
├─ Ground_Voxel.stasset    ← ❌ Unity can't load this!
└─ Assets\
```

**Fix:**
```
Move .stasset files to Assets\StreamingAssets\

Result:
My project\
└─ Assets\
   └─ StreamingAssets\
      └─ Ground_Voxel.stasset    ← ✅ Correct!
```

### **Mistake 3: Creating Files in Wrong Location**

**Problem:**
```
Created new script in Windows Explorer at project root
Unity doesn't see it!
```

**Fix:**
```
ALWAYS create files through Unity:
1. In Unity Project panel
2. Right-click in Assets folder
3. Create → C# Script (or other asset type)
4. Unity creates it in the correct location automatically!
```

---

## 📋 **File Creation Checklist**

### **Creating C# Scripts:**

**Method 1: Through Unity (RECOMMENDED)**
```
1. Unity Project panel → Assets\Scripts
2. Right-click → Create → C# Script
3. Name it (e.g., "MyScript")
4. Unity creates it in correct location ✅
```

**Method 2: Through File System (CAREFUL)**
```
1. Navigate to: My project\Assets\Scripts\
2. Create file: MyScript.cs
3. Return to Unity → Auto-compiles ✅
```

**❌ DON'T DO THIS:**
```
1. Navigate to: My project\ (root)
2. Create file: MyScript.cs
3. Unity can't see it! ❌
```

### **Creating Scenes:**

**Through Unity:**
```
File → New Scene
File → Save As → Assets\Scenes\MyScene.unity ✅
```

### **Adding Voxel Models:**

**Correct Location:**
```
Copy .stasset files to:
My project\Assets\StreamingAssets\

VoxelObject component will find them here ✅
```

---

## 🔍 **How to Verify Correct Location**

### **In Unity Project Panel:**

**✅ Correct - You should see:**
```
Assets
├─ Scripts
│  └─ VoxelWorld.cs (visible!)
├─ Scenes
│  └─ SampleScene.unity (visible!)
└─ StreamingAssets
   └─ Ground_Voxel.stasset (visible!)
```

**❌ Wrong - If you see:**
```
Assets
└─ (empty or missing folders)

Scripts folder not visible in Unity!
```

### **In File System:**

**Check the path:**
```
✅ CORRECT:
My project\Assets\Scripts\VoxelWorld.cs

❌ WRONG:
My project\Scripts\VoxelWorld.cs
```

---

## 🛠️ **Quick Fixes**

### **"Unity can't find my script!"**

**Check:**
1. Is it in `Assets\Scripts\`? (not just `Scripts\`)
2. Did Unity compile it? (check Console for errors)
3. Is the filename the same as the class name?

**Fix:**
```
Move script to Assets\Scripts\
Return to Unity → Auto-compiles
```

### **"VoxelObject can't find my .stasset file!"**

**Check:**
1. Is it in `Assets\StreamingAssets\`?
2. Is the filename correct in Inspector?

**Fix:**
```
Move .stasset to Assets\StreamingAssets\
In VoxelObject Inspector: Asset File Name = "Ground_Voxel.stasset"
```

### **"Add Component doesn't show my script!"**

**Check:**
1. Is script in `Assets\` folder?
2. Did it compile without errors? (check Console)
3. Is class name same as filename?

**Fix:**
```
Ensure script is in Assets\Scripts\
Check Console for compile errors
Fix any errors → Script appears in Add Component
```

---

## 📚 **Unity Folder Purposes**

| Folder | Purpose | Unity Sees? | User Edits? |
|--------|---------|-------------|-------------|
| **Assets/** | All project content | ✅ Yes | ✅ Yes |
| Library/ | Unity cache | ❌ No | ❌ No (auto-generated) |
| Logs/ | Unity logs | ❌ No | ❌ No (auto-generated) |
| Packages/ | Package dependencies | ✅ Yes | ⚠️ Rarely |
| ProjectSettings/ | Project config | ✅ Yes | ⚠️ Through Unity UI |
| Temp/ | Temporary build files | ❌ No | ❌ No (auto-generated) |

**Key Insight:** Only `Assets/` is for YOUR content!

---

## 🎯 **Best Practices**

### **1. Always Use Unity to Create Assets**
```
✅ Unity Project panel → Right-click → Create
❌ Windows Explorer → New file
```

### **2. Organize Assets Folder**
```
Assets\
├─ Scripts\           ← All C# scripts
├─ Scenes\            ← All scenes
├─ Materials\         ← All materials
├─ Prefabs\           ← All prefabs
├─ Textures\          ← All textures
└─ StreamingAssets\   ← Runtime-loaded files (.stasset)
```

### **3. Never Edit Auto-Generated Folders**
```
❌ Don't touch: Library\, Logs\, Temp\
✅ Unity manages these automatically
```

### **4. Use StreamingAssets for Runtime Files**
```
.stasset files → Assets\StreamingAssets\
Unity can load these at runtime
```

---

## 🚀 **Quick Reference Card**

**Creating a new C# script:**
```
Unity → Assets\Scripts → Right-click → Create → C# Script
```

**Adding a voxel model:**
```
Copy .stasset to: Assets\StreamingAssets\
```

**Creating a new scene:**
```
File → New Scene → Save to: Assets\Scenes\
```

**If Unity can't find something:**
```
Check: Is it in Assets\ folder?
```

---

## ✅ **Verification Checklist**

After creating any new file, verify:

- [ ] File is inside `Assets\` folder (or subfolder)
- [ ] Unity Project panel shows the file
- [ ] No compile errors in Console
- [ ] File appears in relevant Unity menus (Add Component, etc.)

**If any checkbox fails → File is in wrong location!**

---

**Last Updated**: June 28, 2026  
**Related**: `VOXEL_SCENE_SETUP_GUIDE.md`, `VOXEL_PHYSICS_SCRIPTS_STATUS.md`
