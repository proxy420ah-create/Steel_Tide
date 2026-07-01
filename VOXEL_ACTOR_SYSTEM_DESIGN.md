# Voxel Actor System Design

**Version**: 1.0.0  
**Date**: June 30, 2026  
**Status**: Design Phase — Pending Final Review  
**Project**: SteelTide

---

## Executive Summary

The **Voxel Actor System** unifies player and NPC physics under a single ragdoll-driven architecture. All actors — player, enemies, allies, civilians — use the same pipeline: rigged `.stasset` → bone Rigidbodies + ConfigurableJoints → procedural pose targets → re-voxelization → GPU raymarch render. This replaces the split between `VoxelPhysics` (kinematic `CharacterController` for player) and `VoxelRagdoll` (multi-body physics for NPCs) with one system where every actor is equal.

### Core Philosophy

- **One physics system for all actors** — no player/NPC divide
- **Physics-driven movement** — momentum, stumbling, recovery are features, not bugs
- **Procedural animation** — pose targets feed joint drives, physics resolves the rest
- **Emergent gameplay** — tactical positioning, stabilization timing, environmental interaction
- **Voxel-native rendering** — every actor is a re-voxelized GPU volume, same as the world

---

## Current State (Pre-Unification)

| Component | Used By | Physics Body | Input | Voxel Collision | Rendering |
|---|---|---|---|---|---|
| `VoxelPhysics.cs` | Player | `CharacterController` (kinematic) | Keyboard/mouse | `RaymarchChunk` (ground + walls) | Static volume |
| `VoxelRagdoll.cs` | NPCs | `Rigidbody` + `ConfigurableJoint` per bone | None | `RaymarchChunk` (ground only) | Re-voxelized per frame |

**Problem**: Two mutually exclusive physics paradigms. Player can't ragdoll. NPCs can't accept input. Shared voxel collision logic is duplicated.

---

## Target Architecture

```
┌─────────────────────────────────────────────────────────┐
│                    VoxelActor                            │
│  (orchestrator + state machine on the GameObject)       │
│                                                         │
│  ActorMode: Kinematic | Active | Ragdoll                │
│  InputSource: PlayerInput | AIInput                     │
│                                                         │
│  ┌──────────────┐  ┌───────────────┐  ┌──────────────┐ │
│  │ Bone Bodies  │  │ Balance       │  │ Pose Driver  │ │
│  │ (Rigidbody + │  │ Controller    │  │ (procedural  │ │
│  │  Joints)     │  │ (CoM → torque)│  │  targets)    │ │
│  └──────────────┘  └───────────────┘  └──────────────┘ │
│         │                  │                │           │
│         ▼                  ▼                ▼           │
│  ┌─────────────────────────────────────────────────────┐│
│  │         Re-Voxelization Engine                      ││
│  │  (stamp voxels from bone transforms → GPU upload)   ││
│  └─────────────────────────────────────────────────────┘│
│                         │                               │
│                         ▼                               │
│              VoxelObject (GPU raymarch)                 │
└─────────────────────────────────────────────────────────┘
```

### Data Flow Per Frame

```
1. Input/AI → compute desired movement direction + pose
2. Pose Driver → set ConfigurableJoint.targetRotation per bone
3. Movement → AddForce/MovePosition on pelvis root Rigidbody
4. Balance Controller → measure CoM lean, apply corrective torques
5. Unity PhysX → resolve all forces, joints, collisions (FixedUpdate)
6. Re-Voxelize → stamp each voxel from its bone's world transform
7. GPU Upload → single ComputeBuffer.SetData per actor
8. Raymarch Render → VoxelRaymarch.compute draws the volume
```

---

## Component Specifications

### 1. VoxelActor (Orchestrator)

**Replaces**: `VoxelRagdoll.cs` (renamed/extended)  
**Also replaces**: `VoxelPhysics.cs` (retired after migration)

```csharp
[RequireComponent(typeof(VoxelObject))]
public class VoxelActor : MonoBehaviour
{
    public enum ActorMode
    {
        Kinematic,   // bind pose, no physics (cutscenes, placement)
        Active,      // physics + input + balance (normal gameplay)
        Ragdoll,     // pure physics, no input/balance (death, knockout)
    }

    public enum InputKind
    {
        Player,      // keyboard/mouse via InputSystem
        AI,          // AI controller drives movement + poses
        None,        // no input (static prop, unconscious)
    }

    [Header("Mode")]
    public ActorMode mode = ActorMode.Active;
    public InputKind inputKind = InputKind.Player;

    [Header("Movement")]
    public float moveForce = 800f;
    public float maxMoveSpeed = 5f;
    public float jumpForce = 8f;
    public float sprintMultiplier = 1.5f;

    [Header("Balance")]
    [Range(0f, 30f)]
    public float balanceStrength = 10f;  // wobble recovery speed
    public float balanceDeadzone = 0.1f; // ignore tiny leans

    [Header("Camera (Player only)")]
    public Transform cameraBone;  // neck or head bone
    public float mouseSensitivity = 2f;
}
```

**Mode Transitions**:
- `Active → Ragdoll`: on death, massive explosion, knockout
- `Ragdoll → Active`: on revive/get-up animation (future)
- `Active → Kinematic`: on cutscene freeze, possession
- `Kinematic → Active`: on cutscene end

### 2. Voxel-Bounds-Derived Compound Collider System

**Replaces**: Single `CapsuleCollider` per bone with hardcoded radius  
**Key insight**: Collider shapes and sizes are derived from the actual voxel cluster geometry in the rig data, not hardcoded constants. A 2-voxel-wide bone gets a wider collider; a beach-ball-sized knee joint gets a proportionally large sphere.

#### Voxel Bounds in Rig Data

Each bone and joint in the `.stasset` rig stores the bounding box of its voxel cluster:

```json
{
  "id": 0,
  "name": "left_thigh",
  "role": "thigh",
  "side": "L",
  "start": [6, 8, 4],
  "end": [6, 5, 4],
  "voxel_bounds_min": [5, 5, 4],
  "voxel_bounds_max": [6, 7, 4],
  "voxel_count": 6,
  "mass": 10.8
}
```

Unity reads these bounds and sizes colliders to match:

```csharp
Vector3 boundsMin = V(bone.voxelBoundsMin);
Vector3 boundsMax = V(bone.voxelBoundsMax);
Vector3 size = (boundsMax - boundsMin + Vector3.one) * voxelSize;
Vector3 center = (boundsMin + boundsMax) * 0.5f * voxelSize;
```

#### Collider Type Per Bone Role (Shape from Role, Size from Voxel Bounds)

| Bone Role | Collider Type | Size Source |
|---|---|---|
| **foot** | `BoxCollider` (flat sole) + `SphereCollider` (ankle) | Box from foot voxel bounds; Sphere from ankle joint bounds |
| **hand** | `BoxCollider` (palm) + `SphereCollider` (wrist) | Box from hand voxel bounds; Sphere from wrist joint bounds |
| **shin** / **forearm** | `CapsuleCollider` | Radius from min(X,Z) of bounds; Height from Y extent |
| **thigh** / **upper_arm** | `CapsuleCollider` | Same — limb segment |
| **pelvis** | `BoxCollider` | Full voxel bounds (wide stable base) |
| **spine** | `CapsuleCollider` | Radius from min(X,Z); Height from Y extent |
| **neck** | `SphereCollider` | Radius from bounds magnitude |
| **head** | `SphereCollider` | Radius from bounds magnitude |
| **knee** / **elbow** | `SphereCollider` | Radius from joint voxel bounds |

#### Implementation

```csharp
private void AddBoneColliders(GameObject go, SkeletonBone bone, Vector3 voxelSize)
{
    Vector3 bMin = V(bone.voxelBoundsMin);
    Vector3 bMax = V(bone.voxelBoundsMax);
    Vector3 size = (bMax - bMin + Vector3.one) * voxelSize;
    Vector3 center = (bMin + bMax) * 0.5f * voxelSize;

    switch (bone.role)
    {
        case "foot":
        case "hand":
            // Flat box from voxel bounds + sphere at joint end
            var box = go.AddComponent<BoxCollider>();
            box.size = size;
            box.center = center;
            // Sphere at the ankle/wrist joint (from joint voxel bounds)
            AddJointSphere(go, bone.parentJoint, voxelSize);
            break;
        case "pelvis":
            var pelvisBox = go.AddComponent<BoxCollider>();
            pelvisBox.size = size;
            pelvisBox.center = center;
            break;
        case "head":
        case "neck":
            var sphere = go.AddComponent<SphereCollider>();
            sphere.radius = size.magnitude * 0.5f;
            sphere.center = center;
            break;
        default:
            // Limb bones: capsule sized from voxel bounds
            var cap = go.AddComponent<CapsuleCollider>();
            cap.radius = Mathf.Min(size.x, size.z) * 0.5f;
            cap.height = size.y;
            cap.center = center;
            // Pick dominant axis from bone direction
            cap.direction = DominantAxis(bone.end - bone.start);
            break;
    }
}
```

#### Scaling Example

**Current actor (2-wide legs)**:
```
Voxels:              Colliders (from bounds):
  ##                  [capsule r=1.0, h=3.0]   ← from 2×3×1 bounds
  ##
  ##  knee            [sphere r=1.0]           ← from 2×1×1 joint bounds
  ##                  [capsule r=1.0, h=3.0]   ← from 2×3×1 bounds
  ##
Foot: 2×3 flat        [box 2×1×3]              ← from 2×1×3 bounds
```

**Cellar spider mech (5-wide legs, large knee)**:
```
Voxels:                    Colliders (from bounds):
  #####                     [capsule r=2.5, h=8.0]  ← from 5×8×1 bounds
  #########  knee           [sphere r=4.5]           ← from 9×1×1 joint bounds
  #####                     [capsule r=2.5, h=8.0]  ← from 5×8×1 bounds
Foot: 6×8 pad              [box 6×2×8]              ← from 6×2×8 bounds
```

#### Compound Collider Physics

Multiple colliders on one `Rigidbody` = one physics body with multiple contact surfaces. Unity handles this natively — no extra joints, no performance cost beyond the additional collider queries.

```
Foot Rigidbody:
  ├── SphereCollider (ankle sphere)    ← articulation pivot, sized from joint voxel bounds
  └── BoxCollider (flat sole)          ← stable contact patch, sized from foot voxel bounds
```

#### Bounds vs. Full Voxel Lists

**Current approach**: Bounding box (min/max XYZ) — 6 ints per bone/joint. Sufficient for sizing sphere/box/capsule colliders.

**Future upgrade**: Full voxel coordinate list — exact shape matching for convex mesh colliders (clawed hands, irregular armor). Upgrade when needed for non-rectangular collider shapes.

### 3. Active Balance Controller

**Purpose**: Keep the actor upright through active torque correction. Provides the "wobbly but recoverable" gameplay feel.

#### Center of Mass (CoM)

The **Center of Mass** is the average position of all body mass, weighted by each part's mass. PhysX computes this automatically from all `Rigidbody` components. With per-bone mass from the rig:
- Heavy legs → lower CoM → more stable
- Heavy head/chest → higher CoM → top-heavy, tips easily

#### Algorithm (per FixedUpdate)

```
1. Compute CoM: weighted average of all bone Rigidbody positions
2. Project CoM onto ground plane (Y = 0 at foot level)
3. Compute support center: midpoint between left and right foot positions
4. Lean vector = CoM_horizontal - support_center_horizontal
5. If |lean| > balanceDeadzone:
   a. Apply corrective torque to pelvis root: -lean × balanceStrength
   b. Apply ankle target rotations: push feet opposite to lean
   c. Scale by balanceStrength (tunable per actor state)
6. If |lean| > fallThreshold:
   → mode = Ragdoll (lost balance, character falls)
```

#### Tunable Parameters

| Parameter | Default | Effect |
|---|---|---|
| `balanceStrength` | 10 | Recovery speed. 20 = snappy, 10 = human, 5 = drunken, 0 = no recovery |
| `balanceDeadzone` | 0.1 | Ignore leans smaller than this (prevents jitter) |
| `fallThreshold` | 2.0 | Lean distance at which actor falls to ragdoll mode |
| `ankleAssist` | 0.3 | How much ankle torque contributes vs. pelvis torque |

#### Dynamic Balance States

Balance strength can change at runtime based on actor state:

```
Healthy:     balanceStrength = 10  → normal wobble recovery
Injured:     balanceStrength = 5   → sluggish recovery, vulnerable
Sprinting:   balanceStrength = 7   → lean into movement, less upright
Casting:     balanceStrength = 15  → brace for spell recoil
Dead:        balanceStrength = 0   → pure ragdoll, mode = Ragdoll
```

#### Why This Complements Compound Colliders

```
Colliders = passive stability
  "Don't fall over just standing there"
  Flat feet + wide pelvis = support polygon

Balance controller = active stability
  "Recover when pushed, lean into turns, catch yourself"
  Torque on pelvis + ankles = fighting back against disturbance
```

Without balance controller: flat feet keep character standing, but any push = permanent fall.  
Without good colliders: balance controller fights a losing battle — corrective torque slides on rounded surfaces.  
**With both**: stable stance + active recovery = the tactical wobble gameplay loop.

### 4. Procedural Pose System

**Purpose**: Drive `ConfigurableJoint.targetRotation` on limb bones toward computed poses for walking, fighting, etc.

#### Pose Library

Each pose defines target rotations per bone role:

```csharp
public struct PoseTarget
{
    public string role;           // "thigh", "shin", "foot", etc.
    public string side;           // "L", "R", ""
    public Quaternion targetRot;  // local-space target rotation
    public float driveSpring;     // joint drive spring force
    public float driveDamper;     // joint drive damping
}

public class Pose
{
    public string name;           // "idle", "walk", "run", "jump", "attack"
    public List<PoseTarget> targets;
}
```

#### Pose Blending

Interpolate between poses based on actor state:

```
Speed 0.0  → idle pose (100%)
Speed 0.5  → idle (50%) + walk (50%)
Speed 1.0  → walk (100%)
Speed 2.0  → walk (50%) + run (50%)
Speed 3.0  → run (100%)
```

Walk/run cycles use time-parameterized sin waves for leg swing, arm counter-swing, and torso bob.

#### Joint Drive Configuration

Each `ConfigurableJoint` gets a `JointDrive` on angular axes:

```csharp
var drive = new JointDrive
{
    positionSpring = poseSpring,    // how hard to reach target
    positionDamper = poseDamper,    // resistance to velocity
    maximumForce = maxDriveForce,   // cap so physics can override
};
cj.angularXDrive = drive;
cj.angularYZDrive = drive;
```

- **High spring** → snappy pose matching (robotic)  
- **Low spring** → loose, physics-dominant (wobbly, natural)  
- **maximumForce cap** → ensures collisions/explosions can override poses (no infinite-strength limbs)

### 5. Input System

#### Abstract Interface

```csharp
public interface IInputSource
{
    Vector2 GetMoveInput();     // desired horizontal movement direction
    bool GetJumpInput();        // jump requested
    bool GetSprintInput();      // sprint requested
    Vector2 GetLookInput();     // camera look delta (player only)
    Pose GetDesiredPose();      // which pose to blend toward
}
```

#### PlayerInput Implementation

- Reads keyboard (WASD), mouse (look), space (jump), shift (sprint) via Unity InputSystem
- Maps movement to pelvis root force direction (camera-relative)
- Camera attaches to neck/head bone transform

#### AIInput Implementation

- Driven by AI controller (pathfinding, combat logic, state machine)
- Same interface — actor doesn't know or care if input is human or AI
- Can request any pose (idle, walk, combat stance, flee)

### 6. Actor-Actor Collision

**Current**: `selfCollision = false` disables all internal collisions per actor.  
**For cross-actor**: Each actor's bone colliders are on standard Unity physics layers. Different actors' colliders interact normally — a punch (hand box collider) hits another actor's head sphere collider.

**Layer Setup**:
```
Layer "ActorSelf"    → ignores other ActorSelf (within-actor, same as selfCollision=false)
Layer "ActorOther"   → collides with ActorOther (cross-actor bone collisions)
Layer "VoxelGround"  → collides with both (voxel world ground, handled by VoxelRaycast)
```

Each actor puts all its bone colliders on "ActorSelf" for internal ignoring, but sets them to collide with "ActorOther" for cross-actor interaction. This is configured via `Physics.IgnoreLayerCollision()`.

### 7. Re-Voxelization (Existing, Unchanged)

The existing re-voxelization pipeline in `VoxelRagdoll` remains as-is:
1. Recenter render cube on pelvis root
2. Clear voxel data
3. Stamp each voxel from its bone's current world transform
4. Single GPU upload via `ComputeBuffer.SetData`

**Performance per actor**:
- CPU stamp: ~66-200 voxels (trivial)
- GPU upload: ~512KB per actor (one `SetData` call)
- GPU render: one raymarch dispatch per actor

**Capacity estimate**: 10-30 actors comfortable on modern hardware. 100+ requires future optimization (batched volumes or sparse single-volume approach).

---

## Studio Asset Design Guidelines

### Bone Proportions for Stable Characters

```
Cross-section (top-down, 18×8 grid):

Current (1-wide):        Recommended (2-3 wide):
     |                        ###
     |                        ###
     |                        ###
     |                        ###
     |                        ###
     |                        ###
     *  (pelvis)             #####  (pelvis)
    / \                      /   \
   |   |                    ##   ##
   |   |                    ##   ##
   |   |                    ##   ##
   |   |                    ##   ##
   ___                      _______
  (1-wide feet)           (wide flat feet)
```

| Bone | Current Width | Recommended Width | Notes |
|---|---|---|---|
| Pelvis | 1 voxel | 3-4 voxels | Lowers CoM, wide support base |
| Thigh | 1 voxel | 2-3 voxels | Mass distribution, wider collider |
| Shin | 1 voxel | 2-3 voxels | Mass, stability |
| Foot | 1 voxel | 3-4 long, 2 wide | Most critical — the contact patch |
| Spine | 1 voxel | 2 voxels | Stability without rigidity |
| Arms | 1 voxel | 1-2 voxels | Can stay thin — not load-bearing |
| Neck | 1 voxel | 1-2 voxels | Articulation, not load-bearing |
| Head | 1 voxel | 2-3 voxels | Combat target, natural shape |

### Why Wider Bones Help Automatically

The per-material mass system computes bone mass from voxel count × material density. Wider bones = more voxels = more mass in legs = **lower CoM = more stable**. No extra configuration needed — the physics falls out of the voxel data.

---

## Implementation Phases

### Phase 0: Studio — Voxel Bounds + Magic Wand Selector
**Goal**: Rig data includes voxel cluster bounds; magic wand speeds up rigging.

1. Update `skeleton_generator_actor.py` to compute and store `voxel_bounds_min` / `voxel_bounds_max` per bone and joint
2. Update `rigging_panel.py` to record voxel bounds from selection box when adding bones/joints
3. Implement magic wand selector in `viewport_widget.py` — flood-fill connected voxels of same material
4. Wire magic wand as alternative to drag-box selection in the rigging panel
5. Regenerate `Actor.stasset` with voxel bounds data
6. Test: load Actor.stasset in Studio, verify bounds in rig data

### Phase 1: Voxel-Bounds-Derived Colliders + Balance Controller
**Goal**: Make existing actor stand stably and recover from pushes.

1. Update `StAssetSkeleton.cs` — add `voxelBoundsMin` / `voxelBoundsMax` to `SkeletonBone` and `SkeletonJoint`
2. Update `VoxelRagdoll.cs` — replace hardcoded collider sizing with bounds-derived sizing
3. Add role-based compound colliders (sphere+box for feet/hands, box for pelvis, sphere for head)
4. Implement `BalanceController` — CoM projection, lean detection, corrective torques
5. Add `balanceStrength`, `fallThreshold`, `balanceDeadzone` tunables
6. Test: spawn actor, push it, verify wobble recovery

### Phase 2: Input + Movement Layer
**Goal**: Player can control a ragdoll actor with WASD + mouse.

1. Add `IInputSource` interface + `PlayerInput` implementation
2. Map movement to pelvis root `Rigidbody.AddForce`
3. Map jump to `AddForce(Impulse)` on pelvis root
4. Attach camera to neck/head bone
5. Add `maxMoveSpeed` clamping via velocity damping
6. Test: walk around the voxel world as a ragdoll actor

### Phase 3: Procedural Pose System
**Goal**: Actors walk, idle, and transition poses naturally.

1. Define `Pose` data structure + pose library (idle, walk, run, jump)
2. Implement pose blender (speed-based interpolation)
3. Configure `JointDrive` on all limb `ConfigurableJoint`s
4. Add walk cycle: sin-wave leg swing, arm counter-swing, torso bob
5. Test: player walks with animated limbs, not just sliding pelvis

### Phase 4: AI Input + NPC Activation
**Goal**: NPCs use the same system with AI-driven input.

1. Implement `AIInput` with basic pathfinding → movement direction
2. AI selects poses based on state (idle when stationary, walk when moving)
3. Enable cross-actor collisions (layer setup)
4. Test: NPC walks toward player, collides, both wobble from impact

### Phase 5: Retire VoxelPhysics
**Goal**: Remove the old player controller.

1. Verify all player functionality works through VoxelActor
2. Move `VoxelPhysics.cs` to `_obsolete/`
3. Update scene/prefab references
4. Final test: full gameplay session with no VoxelPhysics

### Phase 6: Combat + Hit Reactions (Future)
**Goal**: Dynamic combat using the physics system.

1. Hit detection: bone A impacts bone B → damage + reaction force
2. Pose overrides: hit reaction poses override walk cycle briefly
3. Balance disruption: hits reduce `balanceStrength` temporarily
4. Grappling: joint-based attachment between actors
5. Weapon colliders: extended colliders on hand bones for weapons

---

## Performance Considerations

### Per-Actor Cost (FixedUpdate + LateUpdate)

| Operation | Cost | Frequency |
|---|---|---|
| PhysX solve (18+ bodies, joints) | ~0.1ms | FixedUpdate |
| Balance controller | ~0.01ms | FixedUpdate |
| Voxel ground probe (per body) | ~0.05ms | FixedUpdate |
| Re-voxelize (stamp ~200 voxels) | ~0.02ms | LateUpdate |
| GPU buffer upload | ~0.05ms | LateUpdate |
| Raymarch dispatch | GPU-bound | Per-frame |
| **Total per actor** | **~0.23ms CPU** | |

### Capacity Estimates

| Actor Count | CPU Time | Feasibility |
|---|---|---|
| 1-10 | <2.3ms | Comfortable |
| 10-30 | <7ms | Good |
| 30-50 | <12ms | Tight but viable |
| 50-100 | <23ms | Needs optimization |
| 100+ | — | Future: batched volumes |

### Future Optimizations (Not Phase 1)

- **LOD balance**: distant actors get `balanceStrength = 0` (pure ragdoll, no CPU on balance)
- **LOD re-voxelization**: distant actors update volume every Nth frame instead of every frame
- **Batched GPU upload**: multiple actor volumes in one `SetData` call
- **Sleeping**: stationary actors skip re-voxelization (volume unchanged)
- **Single large volume**: all actors in one sparse voxel volume (major refactor)

---

## File Impact Summary

### New Files
| File | Purpose |
|---|---|
| `VoxelActor.cs` | Main orchestrator (renamed/extended from `VoxelRagdoll.cs`) |
| `BalanceController.cs` | CoM-based active balance system |
| `PoseLibrary.cs` | Pose definitions + blending logic |
| `IInputSource.cs` | Input abstraction interface |
| `PlayerInput.cs` | Keyboard/mouse input implementation |
| `AIInput.cs` | AI-driven input implementation |

### Modified Files
| File | Changes |
|---|---|
| `VoxelRagdoll.cs` → `VoxelActor.cs` | Rename, add mode/input/balance, voxel-bounds-derived colliders |
| `StAssetSkeleton.cs` | Add `voxelBoundsMin`/`voxelBoundsMax` to `SkeletonBone` and `SkeletonJoint` |
| `skeleton_generator_actor.py` | Compute and store voxel bounds per bone/joint |
| `rigging_panel.py` | Record voxel bounds from selection; support magic wand selection |
| `viewport_widget.py` | Add magic wand flood-fill selector; fix hover highlight offset |

### Retired Files
| File | Fate |
|---|---|
| `VoxelPhysics.cs` | Move to `_obsolete/` after Phase 5 |

---

## Design Decisions Log

| Decision | Rationale | Date |
|---|---|---|
| Unify player + NPC under ragdoll physics | Uniform capability, emergent gameplay, single codebase | Jun 30 2026 |
| Compound colliders (sphere + box) per bone role | Joint articulation + flat contact patches for stability | Jun 30 2026 |
| Active balance controller vs. passive only | Enables "wobbly but recoverable" tactical gameplay | Jun 30 2026 |
| 2-3 voxel wide load-bearing bones | Physics-honest support polygon, lower CoM | Jun 30 2026 |
| Procedural poses via JointDrive targets | Physics-resolved animation, collisions override poses | Jun 30 2026 |
| Voxel-bounds-derived colliders | Collider size matches actual voxel cluster geometry, not hardcoded | Jun 30 2026 |
| Magic wand selector for rigging | Speeds up bone/joint assignment for irregular voxel shapes | Jun 30 2026 |
| Bounding box over full voxel lists | Sufficient for collider sizing; upgrade to voxel lists for convex mesh colliders later | Jun 30 2026 |
| Retire VoxelPhysics rather than merge | Mutually exclusive physics paradigms; clean replacement | Jun 30 2026 |
| ConfigurableJoint.targetRotation for animation | Unity-native, physics-integrated, no external IK solver needed | Jun 30 2026 |

---

## Open Questions

1. **Get-up animation**: When an actor falls to Ragdoll mode, how do they get back to Active? Options: scripted get-up pose sequence, or ragdoll → freeze → snap to standing. Design in Phase 5.

2. **Weapon colliders**: Should weapons be separate Rigidbody objects attached to hand bones, or extended colliders on the hand bone itself? Decide in Phase 6.

3. **Structural physics**: Buildings/furniture using the same bone+rigidbody system? Same architecture, different scale. Future design document.

4. **Multi-actor GPU optimization**: When is the right time to batch volumes or move to a single sparse volume? Profile at 30+ actors first.

---

*This document represents the agreed design between user and AI assistant as of June 30, 2026. Implementation begins upon final review approval.*
