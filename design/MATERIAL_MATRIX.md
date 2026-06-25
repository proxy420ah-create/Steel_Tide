# Material Matrix & Penetration System

**Version**: 0.1.0-alpha
**Last Updated**: June 25, 2026
**Location**: `design/MATERIAL_MATRIX.md`

---

## 1. Voxel ID Byte Mapping

Each voxel is **1 byte**. The low value encodes a material ID that indexes into a
material property table. This keeps the volume buffer at `width × height × depth`
bytes — extremely compact and GPU-friendly.

| ID | Material | Role | Density (pen. cost) | Notes |
|---:|---|---|---:|---|
| 0 | **Air** | Empty | 0 | Skipped by raymarch & penetration |
| 1 | **Energy Shield** | Active defense | 12 | Regenerates; absorbs then collapses |
| 2 | **Chobham Armor** | Vehicle/hull plate | 40 | High cost; protects interiors |
| 3 | **Concrete** | Structure | 18 | Bulk building material; crumbles |
| 4 | **Flesh** | Biological | 6 | Soldiers / vital organs |

> The table is intentionally small for the prototype. Extend with reserved IDs
> (5–255) for glass, rubble, fuel, electronics, etc. Keep IDs ≤ 255 (single byte).

### Material Property Table (sketch)

```csharp
struct VoxelMaterial
{
    public byte   id;
    public float  density;       // penetration power subtracted per voxel
    public bool   shreddable;    // can be deleted when penetrated
    public bool   isStructural;  // participates in flood-fill island detection
    public half   shieldRegen;   // per-tick regen for ID 1, else 0
}
```

---

## 2. The Raycast Penetration Loop (Amanatides–Woo)

Beams/projectiles traverse the voxel grid one cell at a time using the
**Amanatides–Woo** 3D DDA algorithm. This visits exactly the voxels the ray
crosses, in order, with no gaps.

### Pseudocode

```
penetration = weapon.basePenetration
voxel = floor(rayOrigin / voxelSize)
step  = sign(rayDir)                       // per-axis ±1
tMax  = distance to first voxel boundary   // per-axis
tDelta= voxelSize / abs(rayDir)            // per-axis

while penetration > 0 and inBounds(voxel):
    mat = materials[ grid[voxel] ]
    if mat.id != AIR:
        penetration -= mat.density
        if penetration <= 0:
            applyImpact(voxel)             // stop here (absorbed)
            break
        if mat.shreddable:
            grid[voxel] = AIR              // SHRED — expose interior
            markDirty(voxel)               // queue collision-mesh + island recheck

    // advance to next voxel (pick smallest tMax axis)
    advanceDDA(voxel, tMax, tDelta, step)
```

### Shreddable Armor Math

- Each non-air voxel **subtracts its density** from remaining penetration.
- If penetration stays **> 0**, the voxel is **deleted (set to Air / ID 0)**.
- Subsequent shots along the same path now reach **interior structural layers or
  enemy vital organs**.
- A voxel set to Air **markDirty** triggers: (a) global collision-mesh patch, and
  (b) a flood-fill recheck for **Dynamic Simulation Islands**.

---

## 3. Energy Shield Special Case (ID 1)

- Shields absorb penetration like armor but do **not** shred to Air on a single
  hit until a shield-HP pool is depleted; instead they accumulate damage and
  collapse as a region.
- When collapsed, the shield voxels flip to Air and **regenerate** after a cooldown
  if the emitter voxel still exists.

---

## 4. Integration Points

| Concern | Where |
|---|---|
| Ray traversal & shredding | `voxels/VoxelPenetration.cs` |
| GPU rendering of volume | `voxels/VoxelRaymarch.compute` |
| Island detection after shred | flood-fill job (see `TECH_STACK.md` §2) |
| Damage history pre-carve | Probabilistic Damage Overlay (`ARCHITECTURE.md` §2) |
