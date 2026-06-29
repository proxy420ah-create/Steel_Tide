# 🧊 Dynamic Chunk Physics System - Design Document

**Version**: 1.0  
**Created**: June 28, 2026  
**Status**: 📋 Design Phase  
**Target**: Post-MVP Feature (After basic physics/collision complete)

---

## 🎯 **Vision**

Implement Teardown-style dynamic voxel chunk physics where destroyed voxel groups:
- Break off from parent structures
- Fall with realistic physics
- Collide with terrain and other chunks
- Maintain high performance on low-end systems

**Core Philosophy**: Sacrifice graphical fidelity for raw performance through vectorization, data-oriented design, and clever optimization.

---

## 📊 **Performance Targets**

| Metric | Target | Stretch Goal |
|--------|--------|--------------|
| Active Chunks | 50 simultaneous | 100 simultaneous |
| Chunk Creation Time | < 16ms (1 frame) | < 8ms (half frame) |
| Physics Update | < 5ms per frame | < 3ms per frame |
| Memory per Chunk | < 100KB | < 50KB |
| Min FPS (Low-End) | 30 FPS | 45 FPS |

**Test Hardware**: Intel i5-8400, GTX 1050 Ti, 8GB RAM

---

## 🏗️ **System Architecture**

### **Component Overview**

```
┌─────────────────────────────────────────────────────────┐
│                    VoxelWorld (Static Grid)              │
│  - Global voxel dictionary (Vector3Int → byte)          │
│  - Raymarching collision detection                      │
└─────────────────────────────────────────────────────────┘
                            ↓
                    Voxel Destruction Event
                            ↓
┌─────────────────────────────────────────────────────────┐
│                   ChunkDetector (Unity Jobs)             │
│  - Flood-fill algorithm (multi-threaded)                │
│  - Identifies disconnected voxel groups                 │
│  - Filters by size (min/max thresholds)                 │
└─────────────────────────────────────────────────────────┘
                            ↓
                   List<VoxelChunkData>
                            ↓
┌─────────────────────────────────────────────────────────┐
│                      ChunkSpawner                        │
│  - Generates mesh (box-based or marching cubes)         │
│  - Creates GameObject + Rigidbody + Collider            │
│  - Removes voxels from VoxelWorld grid                  │
│  - Applies initial velocity/explosion force             │
└─────────────────────────────────────────────────────────┘
                            ↓
                   Active Dynamic Chunks
                            ↓
┌─────────────────────────────────────────────────────────┐
│                   ChunkLifecycleManager                  │
│  - Tracks chunk velocity and movement                   │
│  - Detects "sleeping" chunks (stopped moving)           │
│  - Merges sleeping chunks back to VoxelWorld grid       │
│  - Handles chunk pooling and cleanup                    │
└─────────────────────────────────────────────────────────┘
```

---

## 🔧 **Core Components**

### **1. ChunkDetector**

**Purpose**: Identify floating voxel groups after destruction

**Algorithm**: Flood-fill with Unity Jobs System
```csharp
Input: HashSet<Vector3Int> dirtyVoxels (recently destroyed)
Process:
  1. For each unvisited voxel in grid
  2. Start flood-fill job (BFS/DFS)
  3. Mark all connected voxels
  4. Check if group touches ground (has support)
  5. If floating → add to chunk list
Output: List<VoxelChunkData>
```

**Optimizations**:
- Use NativeHashSet for O(1) lookups
- Parallel jobs for multiple chunks
- Early exit if chunk touches ground
- Cache ground voxel positions

**Data Structure**:
```csharp
struct VoxelChunkData
{
    public NativeList<Vector3Int> voxelPositions;
    public NativeList<byte> materialIDs;
    public Vector3 centerOfMass;
    public int voxelCount;
    public bool isGrounded;
}
```

---

### **2. ChunkSpawner**

**Purpose**: Convert VoxelChunkData into physics-enabled GameObjects

**Mesh Generation Strategies**:

| Voxel Count | Strategy | Reason |
|-------------|----------|--------|
| 1-10 | Single BoxCollider | Cheapest collision |
| 11-50 | Combined box mesh | Fast, good enough |
| 51-500 | Greedy meshing | Balance quality/performance |
| 501+ | Split into sub-chunks | Avoid huge colliders |

**Process**:
```csharp
1. Generate mesh from voxel positions
   - Use greedy meshing for efficiency
   - Apply material colors from materialIDs
   
2. Create GameObject
   - Name: "VoxelChunk_[ID]"
   - Layer: "DynamicVoxels"
   
3. Add components
   - MeshFilter + MeshRenderer
   - Rigidbody (mass based on voxel count)
   - MeshCollider or BoxCollider
   
4. Apply physics
   - Initial velocity (explosion direction)
   - Angular velocity (tumbling effect)
   
5. Remove from VoxelWorld
   - Delete voxels from global grid
   - Update collision data
```

**Pooling System**:
- Pre-allocate 20 chunk GameObjects
- Reuse instead of Instantiate/Destroy
- Reset transform and physics on reuse

---

### **3. ChunkLifecycleManager**

**Purpose**: Manage active chunks and merge sleeping chunks back to grid

**Chunk States**:
```
ACTIVE → SLOWING → SLEEPING → MERGED
```

**State Transitions**:
```csharp
ACTIVE:
  - Rigidbody.isKinematic = false
  - Check velocity every frame
  - If velocity < 0.1 for 0.5s → SLOWING

SLOWING:
  - Continue checking velocity
  - If velocity < 0.05 for 2.0s → SLEEPING
  - If velocity increases → ACTIVE

SLEEPING:
  - Rigidbody.isKinematic = true
  - Wait 1.0s (ensure truly stopped)
  - → MERGED

MERGED:
  - Add voxels back to VoxelWorld grid
  - Return GameObject to pool
  - Update collision data
```

**Collision Handling**:
```csharp
OnCollisionEnter:
  - Play impact sound (based on velocity)
  - Spawn particle effects
  - Check for chunk merging (if both sleeping)
```

---

## ⚡ **Performance Optimizations**

### **1. Deferred Chunk Creation**

**Problem**: Rapid destruction creates too many tiny chunks

**Solution**: Batch and delay
```csharp
Queue<Vector3Int> pendingDestroyedVoxels;
float batchTimer = 0.1f; // Wait 100ms

OnVoxelDestroyed(Vector3Int pos):
  pendingDestroyedVoxels.Enqueue(pos);
  
Update():
  batchTimer -= Time.deltaTime;
  if (batchTimer <= 0):
    ProcessBatch(pendingDestroyedVoxels);
    pendingDestroyedVoxels.Clear();
    batchTimer = 0.1f;
```

**Benefits**:
- Merges nearby destroyed voxels into larger chunks
- Reduces total chunk count by 60-80%
- Creates more realistic destruction

---

### **2. Simplified Collision**

**Strategy Matrix**:

| Chunk Size | Collider Type | Complexity |
|------------|---------------|------------|
| 1-5 voxels | BoxCollider | O(1) |
| 6-20 voxels | Compound BoxColliders | O(n) |
| 21-100 voxels | Convex MeshCollider | O(n log n) |
| 101+ voxels | Non-convex MeshCollider | O(n²) |

**Implementation**:
```csharp
if (voxelCount <= 5)
{
    // Single box - super cheap
    var box = chunk.AddComponent<BoxCollider>();
    box.size = CalculateBounds(voxels);
}
else if (voxelCount <= 20)
{
    // Multiple boxes - still cheap
    foreach (var voxelGroup in ClusterVoxels(voxels))
        AddBoxCollider(voxelGroup);
}
else
{
    // Mesh collider - expensive but necessary
    var mesh = chunk.AddComponent<MeshCollider>();
    mesh.convex = (voxelCount < 100);
    mesh.sharedMesh = GenerateMesh(voxels);
}
```

---

### **3. Chunk Merging**

**When**: Two sleeping chunks are touching

**Process**:
```csharp
OnChunkCollision(Chunk A, Chunk B):
  if (A.isSleeping && B.isSleeping):
    // Merge into single chunk
    newVoxels = A.voxels + B.voxels;
    newChunk = CreateChunk(newVoxels);
    newChunk.state = SLEEPING;
    
    // Destroy old chunks
    DestroyChunk(A);
    DestroyChunk(B);
```

**Benefits**:
- Reduces active chunk count
- Larger chunks = fewer GameObjects
- Better for physics performance

---

### **4. Distance-Based LOD**

**Levels**:

| Distance | Mesh Detail | Collider | Update Rate |
|----------|-------------|----------|-------------|
| 0-10m | Full detail | Mesh | Every frame |
| 10-30m | Half detail | Box | Every 2 frames |
| 30-50m | Quarter detail | Box | Every 5 frames |
| 50m+ | Invisible | None | Paused |

**Implementation**:
```csharp
Update():
  foreach (chunk in activeChunks):
    distance = Vector3.Distance(player.position, chunk.position);
    
    if (distance > 50):
      chunk.SetActive(false);
    else if (distance > 30):
      chunk.SetLOD(LOD_LOW);
      chunk.updateRate = 5;
    else if (distance > 10):
      chunk.SetLOD(LOD_MEDIUM);
      chunk.updateRate = 2;
    else:
      chunk.SetLOD(LOD_HIGH);
      chunk.updateRate = 1;
```

---

### **5. Spatial Hashing**

**Purpose**: Fast chunk-chunk collision detection

**Grid Size**: 5 voxels (0.625 units)

**Structure**:
```csharp
Dictionary<Vector3Int, List<VoxelChunk>> spatialGrid;

RegisterChunk(chunk):
  cellPos = WorldToGridCell(chunk.position);
  spatialGrid[cellPos].Add(chunk);

GetNearbyChunks(position):
  cellPos = WorldToGridCell(position);
  return spatialGrid[cellPos] + Adjacent8Cells(cellPos);
```

**Benefits**:
- O(1) lookup instead of O(n) iteration
- Only check nearby chunks for collision
- Reduces physics overhead by 90%+

---

## 📋 **Implementation Phases**

### **Phase 1: Chunk Detection System** ⏱️ 1-2 days

**Goal**: Identify floating voxel groups

**Deliverables**:
- `ChunkDetector.cs` - Flood-fill algorithm
- `VoxelChunkData.cs` - Data structure
- Unit tests for flood-fill
- Integration with `VoxelModifier.cs`

**Acceptance Criteria**:
- ✅ Detects disconnected voxel groups
- ✅ Identifies floating vs grounded chunks
- ✅ Runs in < 16ms for 1000 voxels
- ✅ Console logs show detected chunks

**Testing**:
```
1. Destroy voxels in middle of structure
2. Check console for "Detected floating chunk: X voxels"
3. Verify grounded chunks are ignored
```

---

### **Phase 2: Dynamic Chunk Creation** ⏱️ 2-3 days

**Goal**: Convert chunk data into physics GameObjects

**Deliverables**:
- `ChunkSpawner.cs` - Mesh generation and GameObject creation
- `ChunkMeshGenerator.cs` - Greedy meshing algorithm
- `ChunkPool.cs` - Object pooling system
- Visual feedback (chunks appear and fall)

**Acceptance Criteria**:
- ✅ Chunks spawn with correct mesh
- ✅ Rigidbody and collider added
- ✅ Chunks fall with gravity
- ✅ Pooling system reuses GameObjects
- ✅ < 16ms spawn time per chunk

**Testing**:
```
1. Destroy voxels
2. See chunks spawn and fall
3. Verify collision with ground
4. Check object pool reuse
```

---

### **Phase 3: Chunk Lifecycle** ⏱️ 1-2 days

**Goal**: Manage chunk states and merge back to grid

**Deliverables**:
- `ChunkLifecycleManager.cs` - State machine
- Sleeping chunk detection
- Grid merge functionality
- Cleanup and pooling

**Acceptance Criteria**:
- ✅ Chunks transition through states correctly
- ✅ Sleeping chunks merge back to grid
- ✅ No memory leaks (pooling works)
- ✅ VoxelWorld grid updates correctly

**Testing**:
```
1. Spawn chunks
2. Wait for them to stop moving
3. Verify they merge back to grid
4. Check VoxelWorld has correct voxel data
```

---

### **Phase 4: Performance Optimizations** ⏱️ 2-3 days

**Goal**: Achieve 50+ simultaneous chunks at 30+ FPS

**Deliverables**:
- Deferred chunk creation
- Simplified collision system
- Chunk merging logic
- Distance-based LOD
- Spatial hashing

**Acceptance Criteria**:
- ✅ 50 active chunks at 30+ FPS (low-end)
- ✅ 100 active chunks at 60+ FPS (high-end)
- ✅ Chunk creation < 8ms
- ✅ Physics update < 5ms per frame

**Testing**:
```
1. Stress test: Destroy 500 voxels rapidly
2. Monitor FPS and frame time
3. Profile with Unity Profiler
4. Verify targets met
```

---

### **Phase 5: Polish & Effects** ⏱️ 1-2 days

**Goal**: Visual and audio feedback

**Deliverables**:
- Impact sounds (velocity-based)
- Particle effects (dust, debris)
- Explosion forces on destruction
- Camera shake on large impacts

**Acceptance Criteria**:
- ✅ Satisfying destruction feedback
- ✅ Audio scales with impact force
- ✅ Particles don't tank performance
- ✅ Feels like Teardown!

---

## 🎮 **User Experience Goals**

### **Destruction Feel**

**Target**: Teardown-level satisfaction

**Elements**:
1. **Weight** - Chunks fall realistically based on size
2. **Impact** - Satisfying collision sounds and effects
3. **Chaos** - Multiple chunks tumbling creates visual interest
4. **Cleanup** - Chunks disappear smoothly (no pop)

### **Performance Feel**

**Target**: Smooth even during heavy destruction

**Guarantees**:
- No frame drops during chunk creation
- Consistent physics simulation
- Responsive player controls
- No stuttering or hitches

---

## 🧪 **Testing Strategy**

### **Unit Tests**

```csharp
[Test] FloodFill_FindsConnectedVoxels()
[Test] FloodFill_IgnoresGroundedChunks()
[Test] ChunkSpawner_GeneratesValidMesh()
[Test] ChunkPool_ReusesGameObjects()
[Test] ChunkMerge_CombinesVoxelData()
```

### **Integration Tests**

```csharp
[Test] DestroyVoxels_CreatesChunks()
[Test] ChunkFalls_CollidesWithGround()
[Test] SleepingChunk_MergesBackToGrid()
[Test] MultipleChunks_DontInterfere()
```

### **Performance Tests**

```csharp
[Test] ChunkCreation_Under16ms()
[Test] PhysicsUpdate_Under5ms()
[Test] 50Chunks_Maintains30FPS()
[Test] MemoryUsage_Under100KBPerChunk()
```

### **Stress Tests**

```
1. Destroy 1000 voxels in 1 second
2. Spawn 100 chunks simultaneously
3. Run for 10 minutes (memory leak check)
4. Rapid create/destroy cycles
```

---

## 📊 **Success Metrics**

### **Minimum Viable Product (MVP)**

- ✅ Chunks break off and fall
- ✅ Basic collision with ground
- ✅ Chunks merge back to grid
- ✅ 30 FPS with 20 active chunks

### **Production Ready**

- ✅ 50+ simultaneous chunks at 30+ FPS
- ✅ Satisfying destruction feedback
- ✅ No memory leaks or crashes
- ✅ Smooth performance on low-end hardware

### **Stretch Goals**

- ✅ 100+ simultaneous chunks
- ✅ Chunk-to-chunk merging
- ✅ Advanced LOD system
- ✅ 60 FPS on low-end hardware

---

## 🚧 **Known Challenges**

### **1. Mesh Generation Performance**

**Challenge**: Generating meshes for large chunks is slow

**Solutions**:
- Use greedy meshing (reduces triangle count by 80%)
- Generate mesh on background thread (Unity Jobs)
- Cache common chunk shapes
- Use simple box meshes for small chunks

### **2. Physics Instability**

**Challenge**: Small chunks jitter and bounce forever

**Solutions**:
- Increase physics damping
- Use sleep thresholds (velocity < 0.05)
- Merge tiny chunks immediately
- Disable collision between very small chunks

### **3. Memory Management**

**Challenge**: Creating/destroying GameObjects causes GC spikes

**Solutions**:
- Object pooling (reuse GameObjects)
- NativeArray for voxel data (no GC)
- Batch operations to reduce allocations
- Profile and optimize hot paths

### **4. Ground Detection**

**Challenge**: Determining if chunk is "grounded" is expensive

**Solutions**:
- Cache ground voxel positions
- Use spatial hashing for fast lookup
- Raycast from chunk center downward
- Mark chunks as grounded on collision

---

## 🔮 **Future Enhancements**

### **Post-Launch Features**

1. **Chunk Fracturing**
   - Large chunks break into smaller pieces on impact
   - Adds more destruction detail

2. **Voxel Erosion**
   - Chunks slowly crumble at edges
   - Creates dust particles

3. **Water Physics**
   - Chunks float in water
   - Buoyancy simulation

4. **Fire Propagation**
   - Burning chunks spread fire to nearby voxels
   - Chunks disintegrate when fully burned

5. **Structural Integrity**
   - Unsupported structures collapse over time
   - Realistic building destruction

---

## 📚 **Technical References**

### **Algorithms**

- **Flood Fill**: https://en.wikipedia.org/wiki/Flood_fill
- **Greedy Meshing**: https://0fps.net/2012/06/30/meshing-in-a-minecraft-game/
- **Spatial Hashing**: https://conkerjo.wordpress.com/2009/06/13/spatial-hashing-implementation-for-fast-2d-collisions/

### **Unity Systems**

- **Jobs System**: https://docs.unity3d.com/Manual/JobSystem.html
- **NativeArray**: https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html
- **Object Pooling**: https://learn.unity.com/tutorial/object-pooling

### **Inspiration**

- **Teardown**: https://www.teardowngame.com/
- **Minecraft Physics Mod**: https://www.curseforge.com/minecraft/mc-mods/physics-mod
- **Red Faction Guerrilla**: Destruction tech talks

---

## 📝 **Notes**

- This is a **post-MVP feature** - implement after basic physics/collision is complete
- Focus on **performance first**, polish later
- Use **Unity Profiler** extensively during development
- Test on **low-end hardware** early and often
- Keep **scope manageable** - start with MVP, iterate

---

## ✅ **Prerequisites**

Before starting this feature, ensure:

- ✅ Basic player physics working (CharacterController + raymarching)
- ✅ Voxel destruction system functional (VoxelModifier)
- ✅ VoxelWorld collision detection stable
- ✅ Character alignment system complete
- ✅ Scene setup with ground and test structures

---

**Total Estimated Time**: 1-2 weeks for MVP, 3-4 weeks for production-ready

**Priority**: Medium (after core gameplay mechanics)

**Risk Level**: Medium (complex physics, performance-sensitive)

**Impact**: High (major gameplay feature, high player satisfaction)
