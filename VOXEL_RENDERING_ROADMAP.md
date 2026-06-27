# Steel Tide: Voxel Rendering Roadmap
**Mass Battle & Mass Destruction Architecture**

---

## 🎯 **Vision: Production-Scale Voxel Game**

**Target Scale:**
- 100-1000+ destructible voxel objects simultaneously
- Mass battles with troops, vehicles, buildings
- Real-time destruction with GPU raymarching
- 60 FPS on mid-range hardware

---

## 📋 **Current Status (June 27, 2026)**

### ✅ **Phase 1: Foundation (COMPLETE)**
**Status:** Implemented and tested  
**Date Completed:** June 27, 2026

**What We Built:**
- `VoxelObject.cs` - Self-registering component for voxel assets
- `VoxelRenderer.cs` - Multi-volume registration system
- Auto-discovery of Main Camera renderer
- Gizmo visualization in Scene view

**Performance:**
- Good for: 1-5 voxel objects
- Architecture: MonoBehaviour-based
- Rendering: Sequential loop (CPU-driven)

**Files Modified:**
- `Assets/Voxels/VoxelObject.cs` (new)
- `Assets/Voxels/VoxelRenderer.cs` (multi-volume support added)

---

### ✅ **Phase 2: Multi-Volume Rendering (COMPLETE)**
**Status:** Implemented, testing in progress  
**Date Completed:** June 27, 2026

**What We Built:**
- `DispatchMultiVolume()` - Sequential rendering of all registered volumes
- Compute shader compositing (preserves existing pixels)
- Per-volume parameter passing (offset, size, buffer)
- Background clearing before first volume

**Performance:**
- Good for: 5-10 voxel objects
- Bottleneck: Sequential shader dispatches (CPU overhead)
- Rendering: One dispatch per volume per frame

**Files Modified:**
- `Assets/Voxels/VoxelRenderer.cs` (DispatchMultiVolume method)
- `Assets/Voxels/VoxelRaymarch.compute` (compositing support)

**Testing Goals:**
- [ ] Verify sphere renders at correct position
- [ ] Test with 5-10 armored objects (cube, sphere, cylinder)
- [ ] Measure baseline FPS
- [ ] Verify weapon shooting works on all objects
- [ ] Identify performance bottlenecks

---

## 🚀 **Phase 3: Unity ECS + Burst Jobs (NEXT)**

**Status:** Not started  
**Estimated Effort:** 2-3 weeks  
**Prerequisites:** Phase 2 testing complete, performance baseline established

### **Goals:**
- Migrate from MonoBehaviour to Unity ECS (Entity Component System)
- Parallelize voxel damage calculations using C# Job System
- Use Burst Compiler for native-code performance
- Support 50-100 dynamic voxel objects

### **Technical Details:**

#### **3.1: ECS Entity Structure**
```csharp
// Replace VoxelObject MonoBehaviour with ECS components
struct VoxelVolumeComponent : IComponentData
{
    public int3 volumeDims;
    public float voxelSize;
    public float3 position;
    public BlobAssetReference<VoxelData> voxelDataBlob;
}

struct VoxelBoundsComponent : IComponentData
{
    public AABB bounds;  // For frustum culling
}

struct VoxelDamageComponent : IComponentData
{
    public bool isDirty;  // Needs GPU buffer update
}
```

#### **3.2: Burst Job for Damage**
```csharp
[BurstCompile]
struct VoxelDamageJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<RaycastHit> hits;
    public NativeArray<VoxelData> voxelVolumes;
    
    public void Execute(int index)
    {
        // Apply damage to voxel grid in parallel
        // Two-stage damage system (intact → damaged → air)
    }
}
```

#### **3.3: Parallel Bounding Box Updates**
```csharp
[BurstCompile]
struct UpdateBoundsJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<VoxelVolumeComponent> volumes;
    [WriteOnly] public NativeArray<AABB> bounds;
    
    public void Execute(int index)
    {
        // Recalculate AABB after destruction
    }
}
```

### **Implementation Steps:**
1. Install Unity ECS package (`com.unity.entities`)
2. Create `VoxelVolumeAuthoring.cs` (converts MonoBehaviour → Entity)
3. Implement `VoxelDamageSystem.cs` (handles weapon hits)
4. Implement `VoxelRenderingSystem.cs` (culling + buffer management)
5. Migrate `VoxelWeaponController` to use ECS queries
6. Test with 20-50 objects, measure FPS improvement

### **Expected Performance:**
- **Before:** 5-10 objects @ 60 FPS
- **After:** 50-100 objects @ 60 FPS
- **Bottleneck:** Still sequential shader dispatches (fixed in Phase 4)

### **Files to Create:**
- `Assets/Voxels/ECS/VoxelVolumeAuthoring.cs`
- `Assets/Voxels/ECS/VoxelDamageSystem.cs`
- `Assets/Voxels/ECS/VoxelRenderingSystem.cs`
- `Assets/Voxels/ECS/VoxelComponents.cs`

---

## 🚀 **Phase 4: GPU Spatial Partitioning (FUTURE)**

**Status:** Not started  
**Estimated Effort:** 3-4 weeks  
**Prerequisites:** Phase 3 complete, ECS migration stable

### **Goals:**
- Eliminate sequential shader loop
- Implement GPU-side spatial hash grid
- Ray traversal only checks nearby volumes
- Support 100-1000 voxel objects

### **Technical Details:**

#### **4.1: GPU Grid Builder (Compute Shader)**
```hlsl
// VoxelGridBuilder.compute
#pragma kernel BuildSpatialGrid

struct VolumeEntry
{
    float3 minBounds;
    float3 maxBounds;
    uint volumeIndex;
    uint bufferOffset;
};

RWStructuredBuffer<VolumeEntry> _VolumeList;
RWStructuredBuffer<uint> _GridCells;  // 3D hash grid
uint3 _GridDimensions;  // e.g., 64x64x64 cells

[numthreads(64, 1, 1)]
void BuildSpatialGrid(uint3 id : SV_DispatchThreadID)
{
    // For each volume, insert into all overlapping grid cells
    VolumeEntry vol = _VolumeList[id.x];
    
    // Calculate grid cell range
    int3 minCell = floor(vol.minBounds / _CellSize);
    int3 maxCell = ceil(vol.maxBounds / _CellSize);
    
    // Insert into all overlapping cells
    for (int z = minCell.z; z <= maxCell.z; z++)
    for (int y = minCell.y; y <= maxCell.y; y++)
    for (int x = minCell.x; x <= maxCell.x; x++)
    {
        uint cellIndex = GridIndex(int3(x, y, z));
        // Atomic append to cell's volume list
        InterlockedAdd(_GridCells[cellIndex], 1 << id.x);
    }
}
```

#### **4.2: Modified Raymarcher (Spatial Traversal)**
```hlsl
// VoxelRaymarch.compute (Phase 4 version)
[numthreads(8, 8, 1)]
void CSRaymarch(uint3 id : SV_DispatchThreadID)
{
    float3 ro = _CameraOrigin;
    float3 rd = RayDirFromUV(uv);
    
    // Traverse spatial grid (Amanatides-Woo on grid cells)
    int3 gridCell = WorldToGrid(ro);
    int3 step = sign(rd);
    
    [loop]
    for (int i = 0; i < MAX_GRID_STEPS; i++)
    {
        uint cellIndex = GridIndex(gridCell);
        uint volumeMask = _GridCells[cellIndex];
        
        // Only raymarch volumes in THIS cell
        while (volumeMask != 0)
        {
            uint volumeIdx = firstbitlow(volumeMask);
            volumeMask &= ~(1 << volumeIdx);
            
            // Raymarch this specific volume
            if (RaymarchVolume(volumeIdx, ro, rd, color))
                break;  // Hit found
        }
        
        // Advance to next grid cell
        AdvanceGridCell(gridCell, step, tMax, tDelta);
    }
}
```

### **Implementation Steps:**
1. Create `VoxelGridBuilder.compute` (spatial hash construction)
2. Modify `VoxelRenderer.cs` to build grid each frame (or on change)
3. Rewrite `VoxelRaymarch.compute` with grid traversal
4. Implement `StructuredBuffer<VolumeEntry>` for all volumes
5. Add frustum culling (don't add off-screen volumes to grid)
6. Test with 100-500 objects

### **Expected Performance:**
- **Before:** 50-100 objects @ 60 FPS (Phase 3)
- **After:** 100-1000 objects @ 60 FPS
- **Bottleneck:** Pixel shader cost (fixed in Phase 5)

### **Files to Create:**
- `Assets/Voxels/Compute/VoxelGridBuilder.compute`
- `Assets/Voxels/VoxelSpatialGrid.cs` (CPU-side grid manager)

---

## 🚀 **Phase 5: Rendering Optimizations (POLISH)**

**Status:** Not started  
**Estimated Effort:** 2-3 weeks  
**Prerequisites:** Phase 4 complete, spatial grid working

### **Goals:**
- Reduce pixel shader cost by 50-75%
- Half-resolution raymarching with upsampling
- Temporal reprojection for stable image
- Checkerboard rendering for dense scenes

### **Technical Details:**

#### **5.1: Half-Res Raymarching**
```csharp
// VoxelRenderer.cs (Phase 5)
private RenderTexture _halfResOutput;  // 1/2 width, 1/2 height

void DispatchRaymarch()
{
    // Raymarch at half resolution
    int halfWidth = _camera.pixelWidth / 2;
    int halfHeight = _camera.pixelHeight / 2;
    
    raymarchShader.Dispatch(_kernelIndex, halfWidth / 8, halfHeight / 8, 1);
    
    // Upsample with depth-aware bilateral filter
    Graphics.Blit(_halfResOutput, _output, _upsampleMaterial);
}
```

#### **5.2: Temporal Reprojection**
```hlsl
// TemporalReproject.compute
Texture2D<float4> _CurrentFrame;
Texture2D<float4> _PreviousFrame;
Texture2D<float> _MotionVectors;

[numthreads(8, 8, 1)]
void Reproject(uint3 id : SV_DispatchThreadID)
{
    float2 motion = _MotionVectors[id.xy];
    float2 prevUV = (id.xy + motion) / _ScreenSize;
    
    float4 current = _CurrentFrame[id.xy];
    float4 previous = _PreviousFrame.SampleLevel(sampler_linear, prevUV, 0);
    
    // Blend 90% previous, 10% current (reduces noise)
    _Output[id.xy] = lerp(current, previous, 0.9);
}
```

#### **5.3: Checkerboard Rendering**
```hlsl
// VoxelRaymarch.compute (Phase 5 - Checkerboard)
[numthreads(8, 8, 1)]
void CSRaymarch(uint3 id : SV_DispatchThreadID)
{
    // Only render every other pixel in checkerboard pattern
    uint frameIndex = _FrameCount % 2;
    if ((id.x + id.y) % 2 != frameIndex)
        return;  // Skip this pixel this frame
    
    // Raymarch as normal
    // ...
}
```

### **Implementation Steps:**
1. Create half-resolution render target
2. Implement bilateral upsampling shader
3. Add motion vector generation
4. Implement temporal reprojection
5. Add checkerboard pattern option
6. Test with 500-1000 objects

### **Expected Performance:**
- **Before:** 100-1000 objects @ 60 FPS (Phase 4)
- **After:** 1000+ objects @ 60 FPS, even in dense battles
- **Bottleneck:** CPU overhead (mitigated by ECS in Phase 3)

### **Files to Create:**
- `Assets/Voxels/Shaders/BilateralUpsample.shader`
- `Assets/Voxels/Compute/TemporalReproject.compute`
- `Assets/Voxels/VoxelTemporalRenderer.cs`

---

## 📊 **Performance Targets**

| Phase | Object Count | FPS Target | Architecture |
|-------|-------------|------------|--------------|
| Phase 1-2 | 5-10 | 60 FPS | MonoBehaviour + Sequential Loop |
| Phase 3 | 50-100 | 60 FPS | ECS + Burst + Sequential Loop |
| Phase 4 | 100-1000 | 60 FPS | ECS + Burst + GPU Spatial Grid |
| Phase 5 | 1000+ | 60 FPS | Phase 4 + Half-Res + Temporal |

---

## 🎯 **Immediate Next Steps (After Phase 2 Testing)**

### **Testing Checklist:**
- [ ] Sphere renders correctly at X=5
- [ ] Generate 5 armored objects (2 cubes, 2 spheres, 1 cylinder)
- [ ] Place objects at different positions
- [ ] Measure FPS with 1, 3, 5, 10 objects
- [ ] Test weapon shooting on all objects
- [ ] Verify two-stage damage system works
- [ ] Document performance bottlenecks

### **Decision Point:**
After testing, choose:
- **If FPS > 45 with 10 objects:** Continue with Phase 2, add more gameplay
- **If FPS < 45 with 10 objects:** Start Phase 3 (ECS migration)
- **If FPS < 30 with 5 objects:** Investigate shader bugs first

---

## 📚 **Research References**

**Unity ECS:**
- [Unity DOTS Documentation](https://docs.unity3d.com/Packages/com.unity.entities@latest)
- [Burst Compiler Guide](https://docs.unity3d.com/Packages/com.unity.burst@latest)

**GPU Spatial Partitioning:**
- [GPU Gems 3: Chapter 32 - Broad-Phase Collision Detection](https://developer.nvidia.com/gpugems/gpugems3/part-v-physics-simulation/chapter-32-broad-phase-collision-detection-cuda)
- [Sparse Voxel Octrees](https://research.nvidia.com/publication/2010-02_efficient-sparse-voxel-octrees)

**Rendering Optimizations:**
- [Temporal Anti-Aliasing (TAA)](https://de45xmedrsdbp.cloudfront.net/Resources/files/TemporalAA_small-59732822.pdf)
- [Checkerboard Rendering in Quantum Break](https://www.gdcvault.com/play/1023521/4K-Checkerboard-in-Battlefield-1)

---

## 🔧 **Maintenance Notes**

**Last Updated:** June 27, 2026  
**Current Phase:** Phase 2 (Testing)  
**Next Review:** After Phase 2 testing complete  
**Document Owner:** Cascade AI + User

**Version History:**
- v1.0 (June 27, 2026) - Initial roadmap created after Phase 2 implementation

---

## 💡 **Key Insights**

1. **Don't Optimize Prematurely:** Phase 1-2 is good enough for prototyping
2. **Measure First:** Always benchmark before moving to next phase
3. **Staged Migration:** ECS → Spatial Grid → Rendering Opts (in that order)
4. **Keep It Working:** Each phase should be fully functional before next
5. **Gameplay First:** Don't sacrifice fun for performance until necessary

---

**END OF ROADMAP**
