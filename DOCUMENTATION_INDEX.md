# 📚 Steel Tide - Documentation Index

**Purpose**: Central hub for all project documentation - helps coding agents find information fast and efficiently

**Last Updated**: June 28, 2026  
**Project**: Steel Tide - Voxel Destruction Game

---

## 🎯 **Quick Navigation**

| Category | Documents | Status |
|----------|-----------|--------|
| **Material System** | 4 docs | ✅ Complete |
| **Voxel Asset Studio** | 3 docs | ✅ Complete |
| **Unity Integration** | 4 docs | ✅ Complete |
| **Character Generation** | 2 docs | ✅ Complete |
| **Project Structure** | 2 docs | ✅ Complete |

---

## 📁 **Documentation Categories**

### **1. Material System** ✅ COMPLETE

**Master Reference:**
- **`MATERIAL_SYSTEM.md`** - Complete material reference (21 materials, 0-20)
  - Material IDs, RGB colors, use cases
  - Futuristic border colony theme
  - Material categories (core, terrain, utility, damage, advanced)
  - Migration status: COMPLETE (June 28, 2026)

**Unity Synchronization:**
- **`UNITY_MATERIAL_COLORS_REFERENCE.md`** - Quick copy-paste RGB values for Unity
  - Material Colors array setup
  - Exact RGB values for all 21 materials
  
- **`MATERIAL_SYNC_COMPLETE.md`** - Quick sync guide
  - Status: 0-16 synced, 17-20 pending
  - 5-minute workflow for remaining materials
  - Verification checklist

**Procedural Updates:**
- **`PROCEDURAL_GENERATOR_UPDATE_SUMMARY.md`** - Generator migration summary
  - Updated files: procedural_buildings.py, procedural_characters.py
  - Material usage in generators
  - Testing checklist

**Keywords**: materials, colors, RGB, synchronization, futuristic, sci-fi

---

### **2. Voxel Asset Studio** ✅ COMPLETE

**Quick Start:**
- **`QUICK_START_GUIDE.md`** - Getting started with Voxel Studio
  - Installation and setup
  - Basic workflow
  - Tool usage

**Character Generation:**
- **`CHARACTER_GENERATION_GUIDE.md`** - Advanced character system
  - Seed-based variations
  - Part locking system (head, torso, arms, legs, entire)
  - Material selection per part
  - Larger character models (32 voxels tall)
  - Complete workflow examples

- **`PART_LOCKING_SYSTEM_COMPLETE.md`** - Part locking implementation
  - Technical details of part extraction
  - Part compositing logic
  - Testing scenarios
  - Use cases (squad creation, material testing, iterative refinement)

**Keywords**: voxel studio, character generator, part locking, procedural generation

---

### **3. Unity Integration** ✅ COMPLETE

**Scene Setup:**
- **`VOXEL_SCENE_SETUP_GUIDE.md`** - Complete Unity scene setup (v3.1.0)
  - Phase 1: Hybrid Setup (mesh + voxel) ✅ COMPLETE
  - Phase 2: Voxel Physics (raymarching collision) ← CURRENT
  - Phase 3: Full Voxel Integration
  - Quick Start section for jumping to voxel physics
  - Complete C# script code (VoxelWorld, VoxelPhysics, VoxelModifier)
  - Material synchronization guide

**Project Structure:**
- **`UNITY_PROJECT_STRUCTURE.md`** - File organization guide
  - Assets folder structure
  - Common mistakes and fixes
  - File creation best practices
  - Golden Rule: Everything goes in Assets!

**Scripts Status:**
- **`VOXEL_PHYSICS_SCRIPTS_STATUS.md`** - Script creation reference
  - Scripts that exist: VoxelObject.cs, VoxelRenderer.cs
  - Scripts to create: VoxelWorld.cs, VoxelPhysics.cs, VoxelModifier.cs
  - Creation workflow
  - Dependency order

**Player Setup:**
- **`PLAYER_CHARACTER_SETUP.md`** - Detailed player configuration (if exists)
  - VoxelPhysics component setup
  - Movement parameters
  - Camera setup

**Keywords**: unity, scene setup, voxel physics, raymarching, scripts, project structure

---

### **4. Character Generation** ✅ COMPLETE

**Main Guide:**
- **`CHARACTER_GENERATION_GUIDE.md`** - Complete character system
  - Features: Seed variations, part locking, material selection
  - Character sizes and proportions
  - Workflow examples
  - Technical implementation
  - UI controls

**Implementation:**
- **`PART_LOCKING_SYSTEM_COMPLETE.md`** - Part locking deep dive
  - How part extraction works
  - How part compositing works
  - Testing scenarios
  - Edge cases handled

**Integration:**
- **`CHARACTER_PANEL_INTEGRATION.md`** - UI panel integration (if exists)
  - How character panel connects to main editor
  - Signal/slot connections
  - Parameter passing

**Keywords**: character, humanoid, procedural, part locking, seed variations

---

### **5. Project Structure & Organization**

**File Organization:**
- **`UNITY_PROJECT_STRUCTURE.md`** - Unity project file structure
  - Assets folder organization
  - Where to put scripts, scenes, models
  - Common mistakes

**Material Organization:**
- **`MATERIAL_EXPANSION_GUIDE.md`** - Adding new materials (if exists)
  - How to add materials 21+
  - Synchronization checklist

**Keywords**: project structure, file organization, best practices

---

## 🔍 **Search by Topic**

### **Materials & Colors**
- Material IDs and RGB values → `MATERIAL_SYSTEM.md`
- Unity Material Colors array → `UNITY_MATERIAL_COLORS_REFERENCE.md`
- Quick sync guide → `MATERIAL_SYNC_COMPLETE.md`
- Procedural generator updates → `PROCEDURAL_GENERATOR_UPDATE_SUMMARY.md`

### **Voxel Studio**
- Getting started → `QUICK_START_GUIDE.md`
- Character generation → `CHARACTER_GENERATION_GUIDE.md`
- Part locking → `PART_LOCKING_SYSTEM_COMPLETE.md`

### **Unity Setup**
- Scene setup → `VOXEL_SCENE_SETUP_GUIDE.md`
- Project structure → `UNITY_PROJECT_STRUCTURE.md`
- Script status → `VOXEL_PHYSICS_SCRIPTS_STATUS.md`

### **Physics & Collision**
- Raymarching physics → `VOXEL_SCENE_SETUP_GUIDE.md` (Phase 2)
- VoxelWorld script → `VOXEL_SCENE_SETUP_GUIDE.md` (Phase 3, Step 1)
- VoxelPhysics script → `VOXEL_SCENE_SETUP_GUIDE.md` (Phase 3, Step 2)

### **Character System**
- Character generation → `CHARACTER_GENERATION_GUIDE.md`
- Part locking → `PART_LOCKING_SYSTEM_COMPLETE.md`
- Material selection → `CHARACTER_GENERATION_GUIDE.md` (Materials section)

### **File Organization**
- Where to put files → `UNITY_PROJECT_STRUCTURE.md`
- Assets folder structure → `UNITY_PROJECT_STRUCTURE.md`
- Common mistakes → `UNITY_PROJECT_STRUCTURE.md` (Common Mistakes section)

---

## 📊 **Project Status Dashboard**

### **Material System** ✅ COMPLETE
- [x] 21 materials defined (0-20)
- [x] Futuristic theme established
- [x] Procedural generators updated
- [x] Documentation complete
- [x] Unity sync guide created
- [ ] Materials 17-20 final sync (5 minutes remaining)

### **Voxel Asset Studio** ✅ COMPLETE
- [x] Character generator with seed variations
- [x] Part locking system implemented
- [x] Material Sampler generator added
- [x] Documentation complete

### **Unity Integration** 🔄 IN PROGRESS
- [x] Phase 1: Hybrid Setup (mesh + voxel)
- [x] Scripts created and compiling
- [ ] Phase 2: Voxel Physics (current focus)
- [ ] Phase 3: Full Voxel Integration

### **Documentation** ✅ COMPLETE
- [x] Material system docs
- [x] Character generation docs
- [x] Unity setup docs
- [x] Project structure docs
- [x] Documentation index (this file!)

---

## 🎯 **Common Tasks - Quick Links**

### **"I need to sync materials between Voxel Studio and Unity"**
→ `MATERIAL_SYNC_COMPLETE.md` (5-minute workflow)

### **"I need to create a new character"**
→ `CHARACTER_GENERATION_GUIDE.md` (Character Generator Panel section)

### **"I need to set up voxel physics in Unity"**
→ `VOXEL_SCENE_SETUP_GUIDE.md` (Quick Start section)

### **"I need to know where to put files in Unity"**
→ `UNITY_PROJECT_STRUCTURE.md` (Golden Rule section)

### **"I need to add a new material"**
→ `MATERIAL_SYSTEM.md` (Material Expansion Checklist section)

### **"I need to lock character parts"**
→ `PART_LOCKING_SYSTEM_COMPLETE.md` (Complete Workflow section)

### **"I need to create Unity scripts"**
→ `VOXEL_PHYSICS_SCRIPTS_STATUS.md` (Creation Workflow section)

---

## 🔧 **For Coding Agents**

### **Always Check Documentation First:**
1. **Read this index** to find relevant docs
2. **Read the specific doc** completely before coding
3. **Use grep/code_search** only after reading docs

### **Documentation-First Rule:**
```
User Request → Read DOCUMENTATION_INDEX.md → Find relevant doc → Read doc → THEN code
```

### **Key Documents by Phase:**

**Phase 1 (Materials):**
- MATERIAL_SYSTEM.md
- MATERIAL_SYNC_COMPLETE.md

**Phase 2 (Voxel Physics):**
- VOXEL_SCENE_SETUP_GUIDE.md
- VOXEL_PHYSICS_SCRIPTS_STATUS.md
- UNITY_PROJECT_STRUCTURE.md

**Phase 3 (Character System):**
- CHARACTER_GENERATION_GUIDE.md
- PART_LOCKING_SYSTEM_COMPLETE.md

### **File Locations:**
```
All docs in: SteelTide\ (root)
Unity project: SteelTide\My project\
Voxel Studio: SteelTide\VoxelAssetStudio\
Unity scripts: SteelTide\My project\Assets\Scripts\
```

---

## 📝 **Document Maintenance**

### **When to Update This Index:**
- New document created → Add to relevant category
- Document renamed → Update all references
- Major feature complete → Update status dashboard
- New category needed → Add new section

### **Document Naming Convention:**
- ALL_CAPS_WITH_UNDERSCORES.md for major docs
- Descriptive names (e.g., MATERIAL_SYSTEM.md, not MATERIALS.md)
- Include version/status in filename if needed

### **Status Indicators:**
- ✅ COMPLETE - Fully documented, tested, working
- 🔄 IN PROGRESS - Being worked on
- ⚠️ PENDING - Planned but not started
- 🐛 NEEDS FIX - Has known issues

---

## ⚠️ **CRITICAL: File Organization Rules**

### **Unity Script Locations (NO DUPLICATES!)**

**RULE: One script name = One location ONLY**

| Script Name | Correct Location | Notes |
|-------------|------------------|-------|
| `VoxelObject.cs` | `Assets/Voxels/` | ✅ ONLY location (uses namespace) |
| `VoxelWorld.cs` | `Assets/Scripts/` | Core physics system |
| `VoxelPhysics.cs` | `Assets/Scripts/` | Player physics |
| `VoxelModifier.cs` | `Assets/Scripts/` | Voxel destruction |
| `VoxelRenderer.cs` | `Assets/Voxels/` | Rendering system |

**Before Creating ANY Script:**
1. ✅ Search for existing file: `find_by_name` tool
2. ✅ Check BOTH `Assets/Scripts/` AND `Assets/Voxels/`
3. ✅ If duplicate exists → DELETE the unused one immediately
4. ✅ Update this index with the correct location

**Why This Matters:**
- Unity uses the FIRST script it finds (unpredictable)
- Editing the wrong file = wasted time, no effect
- Duplicate files = confusion, bugs, frustration

**Last Duplicate Incident**: June 28, 2026 - VoxelObject.cs (resolved)

---

## 🚀 **Quick Reference Card**

**Material System:**
- Master list → MATERIAL_SYSTEM.md
- Unity sync → MATERIAL_SYNC_COMPLETE.md

**Unity Setup:**
- Scene setup → VOXEL_SCENE_SETUP_GUIDE.md
- File structure → UNITY_PROJECT_STRUCTURE.md

**Character System:**
- Generation → CHARACTER_GENERATION_GUIDE.md
- Part locking → PART_LOCKING_SYSTEM_COMPLETE.md

**Scripts:**
- Status → VOXEL_PHYSICS_SCRIPTS_STATUS.md
- Code → VOXEL_SCENE_SETUP_GUIDE.md (Phase 3)

---

**Last Updated**: June 28, 2026  
**Version**: 1.0.0  
**Maintainer**: Development Team  
**Purpose**: Central documentation hub for efficient information retrieval
