# VoxelWorld Architecture - Master Reference

**Version:** 1.0.0  
**Date:** June 29, 2026  
**Status:** 🔒 **FOUNDATIONAL - READ FIRST**  
**Criticality:** ⚠️ **MANDATORY READING FOR ALL DEVELOPMENT**

---

## 🎯 Purpose of This Document

This is the **single source of truth** for Steel Tide's dynamic voxel world system. Every feature, system, and enhancement MUST be compatible with this architecture.

**⚠️ CRITICAL**: Before implementing ANY feature that interacts with voxels, collision, physics, or world state, you MUST read and understand this document completely.

---

## 📐 Core Architecture Principle

### **The VoxelWorld-Centric Paradigm**

Steel Tide uses a **unified voxel world model** where:

1. **VoxelWorld is the single source of truth** for all voxel state
2. **VoxelObject is a view/renderer** that syncs TO VoxelWorld
3. **All systems query VoxelWorld** for collision, physics, AI, gameplay
4. **Visual and physics state are ALWAYS synchronized**

```
┌─────────────────────────────────────────────────────────┐
│                      VoxelWorld                         │
│  (Single Source of Truth - Sparse Dictionary)          │
│                                                         │
│  Dictionary<Vector3Int, byte> voxelData                 │
│  - Only stores SOLID voxels                             │
│  - Air/destroyed voxels = removed from dictionary       │
│  - O(1) lookup performance                              │
│  - Serializable for save/load                           │
└─────────────────┬───────────────────────────────────────┘
                  │
                  │ Queries (Read)
                  ├──────────────────────────────────────┐
                  │                                      │
         ┌────────▼────────┐                   ┌────────▼────────┐
         │  VoxelPhysics   │                   │   AI Systems    │
         │  - Ground check │                   │   - Pathfinding │
         │  - Raycasting   │                   │   - Cover       │
         │  - Collision    │                   │   - Navigation  │
         └─────────────────┘                   └─────────────────┘
                  │
                  │ Updates (Write)
         ┌────────▼────────┐
         │  VoxelObject    │
         │  - Renderer     │
         │  - GPU mesh     │
         │  - Damage queue │
         └─────────────────┘
                  │
                  │ SyncVoxelToWorld()
                  └──────────────────────────────────────┐
                                                         │
                  ┌──────────────────────────────────────▼─┐
                  │  VoxelWorld.SetVoxel(pos, material)    │
                  │  - Updates dictionary                  │
                  │  - Notifies all systems                │
                  │  - Maintains consistency               │
                  └────────────────────────────────────────┘
```

---

## 🏗️ Component Responsibilities

### **1. VoxelWorld (The Authority)**

**File**: `Assets/Scripts/VoxelWorld.cs`

**Role**: Global singleton that maintains the authoritative voxel state

**Responsibilities**:
- ✅ Store all solid voxels in sparse dictionary
- ✅ Provide O(1) voxel lookups by world position
- ✅ Handle voxel registration from VoxelObjects
- ✅ Remove destroyed voxels from dictionary
- ✅ Serialize/deserialize for save/load
- ✅ Coordinate system conversion (world ↔ grid)

**Key Methods**:
```csharp
// Query voxel state
byte GetVoxel(Vector3 worldPosition)
byte GetVoxel(Vector3Int gridPosition)

// Update voxel state
void SetVoxel(Vector3 worldPosition, byte materialID)
void SetVoxel(Vector3Int gridPosition, byte materialID)

// Registration
void RegisterVoxelObject(Vector3 position, string assetName, float voxelSize)

// Coordinate conversion
Vector3Int WorldToVoxelGrid(Vector3 worldPosition)
Vector3 VoxelGridToWorld(Vector3Int gridPosition)

// Raycasting
bool RaymarchChunk(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
```

**Data Structure**:
```csharp
Dictionary<Vector3Int, byte> voxelData = new Dictionary<Vector3Int, byte>();
// Key: Grid position (integer coordinates)
// Value: Material ID (0 = air/destroyed, 1-255 = materials)
```

**Critical Invariants**:
- ❌ NEVER store air voxels (materialID = 0) in dictionary
- ✅ ALWAYS remove entries when voxel destroyed
- ✅ Dictionary size = number of solid voxels only
- ✅ Smaller dictionary = faster lookups = better performance

---

### **2. VoxelObject (The Renderer)**

**File**: `Assets/Voxels/VoxelObject.cs`

**Role**: Individual voxel volume that renders and syncs to VoxelWorld

**Responsibilities**:
- ✅ Load .stasset files into local voxel array
- ✅ Render voxels via GPU compute shader
- ✅ Handle damage/destruction events
- ✅ **SYNC all changes back to VoxelWorld**
- ✅ Maintain local-to-world coordinate mapping

**Key Methods**:
```csharp
// Voxel modification
void SetVoxel(int x, int y, int z, ushort value)
void SetVoxel(int3 voxel, ushort value)
void SetVoxelsBulk(List<(int3 pos, ushort mat)> updates)

// VoxelWorld synchronization (CRITICAL)
private void SyncVoxelToWorld(int x, int y, int z, ushort value)

// Batch updates
void BeginBatchUpdate()
void EndBatchUpdate()
```

**Synchronization Flow**:
```csharp
// EVERY voxel change MUST follow this pattern:
public void SetVoxel(int x, int y, int z, ushort value)
{
    // 1. Update local array
    voxelData[index] = value;
    
    // 2. CRITICAL: Sync to VoxelWorld
    SyncVoxelToWorld(x, y, z, value);
    
    // 3. Upload to GPU (visual update)
    if (_batchUpdateDepth == 0)
        UploadToGPU();
}

private void SyncVoxelToWorld(int x, int y, int z, ushort value)
{
    if (_voxelWorld == null) return;
    
    // Translate local coords → world grid coords
    Vector3Int worldGridPos = _worldGridOrigin + new Vector3Int(x, y, z);
    
    // Extract material ID
    byte material = (byte)(value & VoxelBits.MaterialMask);
    
    // Update VoxelWorld (physics authority)
    _voxelWorld.SetVoxel(worldGridPos, material);
}
```

**Critical Rules**:
- ⚠️ **NEVER modify voxelData without calling SyncVoxelToWorld()**
- ⚠️ **ALWAYS sync BEFORE GPU upload**
- ⚠️ **Bulk updates MUST sync every voxel individually**

---

### **3. VoxelPhysics (The Consumer)**

**File**: `Assets/Scripts/VoxelPhysics.cs`

**Role**: Physics queries against VoxelWorld

**Responsibilities**:
- ✅ Ground detection via raycasting
- ✅ Collision detection
- ✅ Movement validation
- ✅ **ALWAYS query VoxelWorld, NEVER local arrays**

**Key Methods**:
```csharp
bool CheckGrounded(Vector3 position, float radius, float height)
bool Raycast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hit)
```

**Query Pattern**:
```csharp
// CORRECT: Query VoxelWorld
byte material = VoxelWorld.Instance.GetVoxel(worldPosition);
if (material == 0) {
    // Air/destroyed - can move here
}

// WRONG: Query VoxelObject directly
// ❌ VoxelObject.voxelData is LOCAL coords, not synced in real-time
```

---

### **4. VoxelPlayerController (The User)**

**File**: `Assets/Player/VoxelPlayerController.cs`

**Role**: Player movement with voxel-aware collision

**Responsibilities**:
- ✅ Ground detection using VoxelWorld raycasts
- ✅ Wall collision detection
- ✅ CharacterController integration
- ✅ Debug visualization

**Ground Detection Pattern**:
```csharp
bool CheckGroundVoxel()
{
    // 1. CRITICAL: Check center first (detects holes)
    if (Physics.Raycast(origin, Vector3.down, distance, voxelLayer))
        return true;
    
    // 2. Check ring around player
    for (int i = 0; i < raycastSamples; i++) {
        Vector3 offset = GetRingOffset(i);
        if (Physics.Raycast(origin + offset, Vector3.down, distance, voxelLayer))
            return true;
    }
    
    return false;  // No ground detected
}
```

**Critical Settings**:
```csharp
// CharacterController configuration
_controller.skinWidth = 0.01f;  // Reduced for voxel precision
_controller.radius = 0.5f;      // Match voxel scale
```

---

## 🔄 Data Flow Patterns

### **Pattern 1: Voxel Destruction**

```
Player shoots → VoxelWeaponController.FireWeapon()
                        ↓
                VoxelDamageQueue.ApplyDamage()
                        ↓
                VoxelObject.SetVoxel(x, y, z, AIR)
                        ↓
                ┌───────┴───────┐
                ↓               ↓
        voxelData[i] = 0    SyncVoxelToWorld()
        (local update)              ↓
                ↓           VoxelWorld.SetVoxel(gridPos, 0)
        UploadToGPU()               ↓
        (visual update)     voxelData.Remove(gridPos)
                                    ↓
                            Physics sees hole ✅
```

**Key Points**:
- Visual and physics update in SAME frame
- No desync possible
- Destroyed voxels removed from dictionary (performance win)

---

### **Pattern 2: Ground Detection**

```
Player moves → VoxelPlayerController.CheckGroundVoxel()
                        ↓
                Center raycast + Ring raycasts
                        ↓
                Physics.Raycast() → Unity Physics
                        ↓
                VoxelWorld.RaymarchChunk()
                        ↓
                voxelData.ContainsKey(gridPos)
                        ↓
                ┌───────┴───────┐
                ↓               ↓
            Found solid     Found air/destroyed
                ↓               ↓
            Grounded        Falling ✅
```

**Key Points**:
- O(1) dictionary lookup
- Destroyed voxels = not in dictionary = air
- Real-time response to destruction

---

### **Pattern 3: Multiplayer Sync**

```
Server: Player destroys voxel
                ↓
        VoxelWorld.SetVoxel(pos, 0)
                ↓
        Broadcast to all clients
                ↓
        ┌───────┼───────┐
        ↓       ↓       ↓
    Client1 Client2 Client3
        ↓       ↓       ↓
    VoxelObject.SetVoxel()
        ↓       ↓       ↓
    SyncVoxelToWorld()
        ↓       ↓       ↓
    All clients have identical state ✅
```

**Key Points**:
- Server is authority
- Clients sync to VoxelWorld
- No desync bugs
- Deterministic state

---

## 🎮 Gameplay Systems Enabled

### **1. Dynamic Destruction**
- Real-time structural integrity
- Collapsing buildings
- Persistent damage
- Environmental storytelling

### **2. Tactical Combat**
- Cover destruction
- Armor penetration
- Ricochet through holes
- Emergent tactics

### **3. Physics Simulation**
- Fluid flow through holes
- Fire spreading
- Gas leaking
- Gravity-based collapse

### **4. AI Behavior**
- Pathfinding around destruction
- Cover evaluation
- Tactical positioning
- Adaptive behavior

### **5. Persistent World**
- Save/load destruction state
- Multiplayer synchronization
- Shared world changes
- Environmental memory

### **6. Resource Systems**
- Mining/gathering
- Crafting materials
- Voxel regeneration
- Material transformation

---

## ⚠️ Critical Rules for Developers

### **Rule 1: VoxelWorld is Authority**
```csharp
// ✅ CORRECT
byte material = VoxelWorld.Instance.GetVoxel(worldPos);

// ❌ WRONG
byte material = voxelObject.voxelData[localIndex];
```

### **Rule 2: Always Sync After Modification**
```csharp
// ✅ CORRECT
voxelData[index] = newValue;
SyncVoxelToWorld(x, y, z, newValue);
UploadToGPU();

// ❌ WRONG
voxelData[index] = newValue;
UploadToGPU();  // Visual only, physics broken!
```

### **Rule 3: Never Store Air Voxels**
```csharp
// ✅ CORRECT
if (materialID == 0) {
    voxelData.Remove(gridPos);  // Remove from dictionary
}

// ❌ WRONG
voxelData[gridPos] = 0;  // Wastes memory, slows lookups
```

### **Rule 4: Coordinate System Awareness**
```csharp
// VoxelObject uses LOCAL coordinates (0 to volumeDims)
int localIndex = x + y * volumeDims.x + z * volumeDims.x * volumeDims.y;

// VoxelWorld uses WORLD GRID coordinates
Vector3Int worldGrid = _worldGridOrigin + new Vector3Int(x, y, z);

// ALWAYS translate when syncing!
```

### **Rule 5: Batch Updates for Performance**
```csharp
// ✅ CORRECT (bulk destruction)
BeginBatchUpdate();
foreach (var voxel in destroyedVoxels) {
    SetVoxel(voxel.pos, AIR);  // Syncs each, uploads once
}
EndBatchUpdate();

// ❌ WRONG (individual updates)
foreach (var voxel in destroyedVoxels) {
    SetVoxel(voxel.pos, AIR);  // Uploads GPU every iteration!
}
```

---

## 🔧 Implementation Checklist

When implementing ANY feature that interacts with voxels:

- [ ] Does it query VoxelWorld for state? (not VoxelObject)
- [ ] Does it sync changes to VoxelWorld? (via SetVoxel)
- [ ] Does it handle air voxels correctly? (remove from dictionary)
- [ ] Does it translate coordinates properly? (local ↔ world grid)
- [ ] Does it batch updates for performance? (BeginBatch/EndBatch)
- [ ] Does it work in multiplayer? (server authority)
- [ ] Does it persist across save/load? (VoxelWorld serialization)
- [ ] Does it maintain visual/physics sync? (SyncVoxelToWorld)

---

## 📊 Performance Characteristics

### **Sparse Dictionary Benefits**

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| Voxel lookup | O(1) | Dictionary.ContainsKey() |
| Voxel update | O(1) | Dictionary add/remove |
| Raycast | O(n) | n = voxels along ray |
| Memory usage | O(solid voxels) | Air voxels = 0 bytes |

### **Scaling Properties**

```
World size: 1000 × 1000 × 100 voxels = 100M potential voxels

Naive approach:
- Store all voxels: 100M × 1 byte = 100 MB
- Check all voxels: 100M iterations

Sparse dictionary approach:
- Store only solid: ~10M × 1 byte = 10 MB (90% reduction)
- Check only solid: ~10M iterations (90% faster)
- More destruction = BETTER performance!
```

---

## 🚀 Future Expansion Points

### **Phase 2: Advanced Physics**
- Structural integrity simulation
- Gravity-based collapse
- Debris physics
- Fluid dynamics

### **Phase 3: Material Properties**
- Hardness/durability per material
- Damage resistance
- Material transformations (ice → water → steam)
- Chemical reactions

### **Phase 4: Procedural Systems**
- Voxel regeneration (healing, growth)
- Erosion simulation
- Weather effects (rain, snow accumulation)
- Organic growth (plants, crystals)

### **Phase 5: Optimization**
- Spatial hashing for faster raycasts
- Chunk-based updates
- GPU-accelerated queries
- Async physics updates

---

## 📝 Version History

| Version | Date | Changes |
|---------|------|---------|
| 1.0.0 | 2026-06-29 | Initial architecture document |

---

## 🔗 Related Documentation

- `VOXEL_RENDERING_ROADMAP.md` - GPU rendering pipeline
- `MATERIAL_SYSTEM.md` - Material ID definitions
- `VOXEL_METRICS_AND_UNITS.md` - Coordinate systems and scaling
- `VoxelWorld.cs` - Source code implementation
- `VoxelObject.cs` - Source code implementation
- `VoxelPhysics.cs` - Source code implementation

---

## ⚡ Quick Reference

### **Most Common Operations**

```csharp
// Check if voxel is solid
byte mat = VoxelWorld.Instance.GetVoxel(worldPos);
bool isSolid = (mat != 0);

// Destroy voxel
VoxelWorld.Instance.SetVoxel(worldPos, 0);

// Create voxel
VoxelWorld.Instance.SetVoxel(worldPos, MaterialID.Concrete);

// Raycast for collision
if (VoxelWorld.Instance.RaymarchChunk(origin, direction, maxDist, out hit)) {
    // Hit something solid
}

// Convert coordinates
Vector3Int gridPos = VoxelWorld.Instance.WorldToVoxelGrid(worldPos);
Vector3 worldPos = VoxelWorld.Instance.VoxelGridToWorld(gridPos);
```

---

**END OF DOCUMENT**

⚠️ **Remember**: This architecture is FOUNDATIONAL. All future systems must be compatible with this design. When in doubt, consult this document first.
