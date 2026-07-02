# Voxel Ragdoll Physics Iteration Guide

This guide records the step-by-step exploration of taking a bone-only voxel actor from a rigid statue to a true ragdoll in Unity. It focuses on the practical mistakes and fixes discovered while working with the `VoxelRagdoll` system.

---

## 1. Goal

Build a physics-enabled voxel actor that:

1. First stands and falls as a rigid body.
2. Then gains joints one at a time to become a controllable ragdoll.
3. Keeps correct colliders on arms, legs, torso, and head throughout.

The model used is a bone-only clone of `ActorSymmetric` (all voxels are material `12` / bone, skeleton removed), named `ActorBone`.

---

## 2. Starting Point: `ActorBone_Connected.stasset`

- 111 bone voxels.
- One connected component (no floating limbs).
- A minimal skeleton: one root joint at the pelvis, one spine bone from pelvis to head.

Result in Unity: the model stands and falls as a single rigid plank. It works, but it lands on a single small collider.

---

## 3. Iteration 1: Single Waist Joint

Added a two-bone skeleton with one waist joint (`stasset_add_waist_joint.py`):

- Pelvis bone (hips + legs).
- Upper-spine bone (torso + arms + head).
- Waist joint with BALL limits of ±30°.

Result: the model could bend at the waist and topple. This confirmed the joint pipeline works.

---

## 4. Iteration 2: Fitted Colliders (Workaround)

The waist-test model rested on a tiny pelvis capsule. The first attempt to fix this was to expand each bone's `voxel_bounds` to cover all voxels assigned to it (`stasset_fit_colliders_to_voxels.py`).

This helped the pelvis, but it did not fully solve the arms because the upper-spine bone still uses a **capsule** oriented along the spine. A vertical capsule cannot cover horizontal arms.

Lesson: fitting bounds to a single bone is only useful if the bone's shape already matches the voxels it owns.

---

## 5. Iteration 3: Full Limb Skeleton with 0° Limits

The correct fix is to give each limb its own bone. The original `ActorSymmetric.stasset` already has a full 16-bone skeleton with arms, legs, spine, and neck. We copied that skeleton onto the bone-only voxels and stiffened every joint by setting all angle limits to 0 (`stasset_rigid_limb_skeleton.py`).

Result:

- Each limb got its own collider.
- The model briefly collapsed into a heap when hit, then snapped back to the bind pose.

This was unexpected. The joints were supposed to be stiff.

---

## 6. The Root Cause: "Limited to 0" Is Not Locked

In `VoxelRagdoll.ConfigureAngular`, BALL and HINGE joints are configured like this:

```csharp
if (pivot == null || pivot.type == JointType.Root)
{
    cj.angularXMotion = cj.angularYMotion = cj.angularZMotion =
        ConfigurableJointMotion.Locked;
    return;
}

// Ball: cone-limited on all three angular axes.
cj.angularXMotion = ConfigurableJointMotion.Limited;
cj.angularYMotion = ConfigurableJointMotion.Limited;
cj.angularZMotion = ConfigurableJointMotion.Limited;
```

When we set `max_angle_x = max_angle_y = max_angle_z = 0` on a BALL joint, the motion is still `Limited`, not `Locked`. Unity treats a "0° limit" as a soft spring constraint. Under impact, the joint can temporarily bend, then spring back.

That is exactly what produced the heap-then-recover behavior in the screenshots.

---

## 7. The Fix: Change Joint Type to `ROOT`

To make the chain truly rigid, every non-ROOT joint must be changed to type `ROOT`. This triggers the early branch in `ConfigureAngular` and sets all angular motions to `Locked`.

The script supports this with the `--hard-lock` flag:

```bash
python stasset_rigid_limb_skeleton.py --hard-lock
```

This produces `ActorBone_RigidLimbs_HardLocked.stasset` (later renamed to `ActorBone_Rigid.stasset`), which has:

- 16 bones.
- 14 joints.
- 0 non-ROOT joints.

Result in Unity: the model falls and topples as a single rigid shape, but each limb has its own collider. The T-pose is preserved while the model lies on its back.

---

## 8. Key Takeaway

| Goal | Joint Type | Behavior |
|------|-----------|----------|
| True ragdoll | BALL / HINGE with real angle limits | Bends under physics |
| Stiff but chain-linked | BALL / HINGE with 0° limits | Springs back after impact |
| Truly rigid multi-collider body | All non-ROOT joints changed to `ROOT` | No relative motion at all |

If you want a rigid body with multiple colliders, use the hard-lock approach. If you want a ragdoll, restore BALL/HINGE joints with real limits.

---

## 9. Scripts in the Toolbox

| Script | Purpose |
|--------|---------|
| `stasset_bone_only_clone.py` | Convert a rigged actor to bone-only, skeleton-free. |
| `stasset_connect_components.py` | Add bridge voxels so the model is one connected component. |
| `stasset_add_simple_spine.py` | Add a minimal one-bone skeleton for basic physics registration. |
| `stasset_add_waist_joint.py` | Add a two-bone skeleton with one waist joint. |
| `stasset_fit_colliders_to_voxels.py` | Expand bone `voxel_bounds` to cover assigned voxels (quick workaround). |
| `stasset_rigid_limb_skeleton.py` | Copy a full skeleton and stiffen joints; `--hard-lock` makes them truly rigid. |

---

## 10. Recommended Next Steps

To move from rigid to ragdoll, unlock joints one at a time:

1. Start with `ActorBone_Rigid.stasset` (fully rigid).
2. Create a copy where the **neck** joint is changed from `ROOT` back to `BALL` with small limits. The current file is `ActorBone_Rigid_Neck.stasset`.
3. Test that the head can flop while the rest stays rigid.
4. Repeat for shoulders, elbows, hips, knees, and ankles.
5. At each step, test that the model still stands and falls predictably.

This incremental approach makes it easy to find which joint causes instability if the ragdoll starts exploding.

Old temporary test files (`ActorBone_Ragdoll_WaistTest`, `ActorBone_Ragdoll_WaistTest_Fitted`, `ActorBone_RigidLimbs`, `ActorBone_RigidLimbs_HardLocked`) have been cleaned up. The new baseline is `ActorBone_Rigid.stasset`, and each unlocked-joint test gets its own file.

### Quick Reset for Testing

`VoxelRagdoll` now snapshots the bind pose at initialization and provides a `ResetRagdoll()` method to teleport every body back to its starting position and clear velocities. This makes it easy to drop the model repeatedly:

- **In play mode:** press the configured key (`F5` by default, exposed as `resetKey` in the Inspector). Uses Unity's new Input System, so it works even when the Game view does not have focus. The default is `F5` to avoid conflicts with Unity's `R` Scale tool and the player's `Space` jump.
- **In the editor:** right-click the `VoxelRagdoll` component and choose **Reset Ragdoll**.
- **From a UI button:** call `GetComponent<VoxelRagdoll>().ResetRagdoll()`.

---

## 11. Jitter Bug: Re-Voxelization Moving the Parent Transform

After adding a `BalanceController` and enabling re-voxelization, a strong physical jitter appeared. The colliders visibly shook even when the body should have been still.

### Process of Elimination

- `simulatePhysics = false` → jitter stopped.
- `enableRevoxelization = false` → jitter stopped.

This isolated the jitter to the re-voxelization path, not the balance controller.

### Root Cause

`RevoxelizeFrame` recentered the render cube on the pelvis every frame:

```csharp
_vo.transform.position = pelvisWorld - _cubeOriginOffset;
```

Because the bone bodies (`_boneRoot`) were children of the same `VoxelObject` GameObject, moving the transform to recenter the volume also **teleported every physics body**. PhysX does not tolerate having its Rigidbodies yanked around by their parent transform. The result was the strong, random jitter.

### Why This Didn't Happen Before

Before the pelvis anchor, the voxel volume stayed fixed in world space and the actor simply moved inside it. The physics bodies were never teleported by their parent transform. The pelvis anchor was added to keep the actor centered in the volume for long-distance travel, but it was implemented by moving the parent transform.

---

## 12. Robust Fix: Decouple Physics Bodies from the Visual Transform

### Solution

- Make `_boneRoot` a **top-level GameObject** (not a child of the VoxelObject).
- Re-enable the pelvis anchor so the voxel volume dynamically follows the actor.
- The physics bodies stay at their world positions because the moving transform no longer owns them.

```csharp
_boneRoot = new GameObject($"{name}_RagdollBodies");
_boneRoot.transform.position = Vector3.zero;
// Intentionally NOT parented to the VoxelObject so the render volume
// can recenter without teleporting the physics bodies.
```

This restores both features:

- **Dynamic volume box**: the voxel volume follows the pelvis, so the actor can travel long distances.
- **Stable physics**: the ragdoll bodies are not moved by the visual transform, so ground collision and balance work correctly.

### Architecture Rule

**Physics bodies must never be children of the visual voxel volume that recenters.**

Visual-only transforms can move freely (recentering, LOD snapping, animation). Physics bodies must stay in world space, or be moved through Rigidbody APIs only.

### Why This Matters for the VoxelActor Architecture

This decoupling is a core requirement for the future `VoxelActor` system. If the player walks across the map, the render volume follows the actor, but the bone Rigidbodies remain under physics control. Any future system that combines dynamic re-voxelization with ragdoll physics must follow this rule.

---

## 13. Balance Controller Time-Scale Sensitivity

After the jitter fix, balance testing revealed that the actor fell in different directions at different `Time.timeScale` values. At normal speed the actor fell forward; at 0.1 speed it stabilized briefly then fell backward.

### Two Separate Causes

1. **Ground friction was applied per FixedUpdate step**, not per second.
   - At `Time.timeScale = 0.1`, `FixedUpdate` runs 10× more often per game-second.
   - The per-step friction made the ground 10× stickier, letting the controller briefly hold the actor before overshooting.
   - Fix: `groundFriction` is now a per-second rate, applied with `Mathf.Exp(-groundFriction * Time.fixedDeltaTime)`.

2. **Ground penetration correction was not time-step independent.**
   - The push velocity was `penetration * groundStiffness`.
   - At smaller `Time.fixedDeltaTime`, the body penetrates less per step, so the push velocity was much smaller.
   - This made the slow-motion landing feel soft and the real-time landing feel like a springboard.
   - Fix: `groundStiffness` now divides by `Time.fixedDeltaTime` so the correction speed is `penetration * groundStiffness / Time.fixedDeltaTime`.
   - Default `groundStiffness` changed from `30` to `0.1` (dimensionless aggressiveness, 0.1 = soft settling over several steps).
   - Added `groundRestitution` (0–1) so the ground can absorb impact instead of always bouncing. Default 0 = inelastic landing.
   - Friction is now applied whenever a body is near or touching the ground, not only when it is actively penetrating. This fixes the case where a resting body slides because the upward push keeps it exactly at `surfaceY`.

3. **The balance torque was instantaneous and underdamped.**
   - The controller used the raw lean vector, so the red torque arrow could flip direction every frame.
   - The default `balanceDamping` range (0–2) was too low for the design-doc `balanceStrength` values (10–20).
   - Fix: added EMA smoothing on the lean vector (`leanSmoothing`, time constant in seconds) and raised the `balanceDamping` range to 0–10.

4. **The controller was actively correcting while airborne.**
   - At slow speed the actor has more visible hang time, so the controller had more frames to rotate the body in the air before landing.
   - This produced different fall directions because the controller could push the body off-axis before ground contact.
   - Fix: `ResolveVoxelGround` now tracks how many feet are grounded and feeds a `GroundContactScale` to the controller.
   - The controller scales corrective torque by `GroundContactScale` and uses `airborneBalance` (default 10%) while airborne.

### EMA Smoothing

```csharp
float alpha = 1f - Mathf.Exp(-Time.fixedDeltaTime / leanSmoothing);
_leanSmoothed = Vector3.Lerp(_leanSmoothed, lean, alpha);
```

Using `Time.fixedDeltaTime` in the EMA keeps the time constant consistent regardless of game speed. The yellow gizmo line now shows the smoothed lean the controller actually uses, while a fainter line shows the raw instantaneous lean.

---

## 14. Modular Actor Refactor

To make debugging easier, the monolithic `VoxelRagdoll.cs` was split into a `VoxelActor` orchestrator plus swappable subsystems. Each subsystem is a separate MonoBehaviour that can be enabled/disabled in the Inspector to isolate bugs.

```
VoxelActor (orchestrator)
├─ VoxelBodyManager      → build bodies and joints
├─ VoxelGroundResolver   → voxel ground collision, friction, restitution
├─ VoxelBalance          → balance controller wrapper
├─ VoxelRevoxelizer      → visual voxel update
└─ VoxelActorGizmos      → debug drawing
```

| Toggle | Use |
|---|---|
| `enableBodies` | Disable to skip body/joint build entirely |
| `enableGroundCollision` | Disable to test free-fall ragdoll without ground response |
| `enableBalance` | Disable to test raw physics without corrective torque |
| `enableRevoxelization` | Disable to test physics without visual update |
| `buildJoints` | Disable to make bones independent (no joint constraints) |

The old `VoxelRagdoll.cs` remains intact as a fallback while the new modular system is tested.

---

## 15. Debug Controls

`VoxelActorDebugController.cs` provides runtime shortcuts for isolating bugs without opening the Inspector.

| Key | Default | Action |
|---|---|---|
| `K` | Reset | Reset actor to bind pose and spawn position |
| `R` | Respawn | Move actor to spawn position without rebuilding |
| `T` | Slow time | Toggle `Time.timeScale` between normal and slow (default 0.1x) |
| `P` | Pause | Toggle `Time.timeScale` 0 / 1 |
| `N` | Normal time | Force `Time.timeScale = 1` |
| `B` | Toggle balance | Enable/disable `VoxelActor.enableBalance` |
| `G` | Toggle ground | Enable/disable `VoxelActor.enableGroundCollision` |
| `V` | Toggle revoxel | Enable/disable `VoxelActor.enableRevoxelization` |
| `J` | Toggle joints | Enable/disable `VoxelActor.buildJoints` (requires rebuild) |

All keys are exposed in the Inspector and can be rebound or disabled by setting them to `None`.

---

## 16. Related Files

- `My project/Assets/Scripts/VoxelRagdoll.cs` — legacy monolithic ragdoll (still functional).
- `My project/Assets/Scripts/VoxelActor.cs` — new modular orchestrator.
- `My project/Assets/Scripts/VoxelBodyManager.cs` — body/joint build subsystem.
- `My project/Assets/Scripts/VoxelGroundResolver.cs` — ground collision subsystem.
- `My project/Assets/Scripts/VoxelBalance.cs` — balance controller subsystem.
- `My project/Assets/Scripts/VoxelRevoxelizer.cs` — visual re-voxelization subsystem.
- `My project/Assets/Scripts/VoxelActorGizmos.cs` — debug gizmos for modular actor.
- `My project/Assets/Scripts/VoxelActorDebugController.cs` — runtime debug controls.
- `My project/Assets/Voxels/StAssetSkeleton.cs` — parses the skeleton JSON block.
- `My project/Assets/Voxels/VoxelObject.cs` — loads the `.stasset` and registers it with the renderer.
- `docs/VOXEL_ASSET_TRIMMING_METHOD.md` — the earlier workflow that produced the bone-only model.
- `VOXEL_ACTOR_SYSTEM_DESIGN.md` — future architecture where all actors use this decoupled ragdoll system.
- `docs/BALANCE_CONTROLLER_IMPLEMENTATION_PLAN.md` — balance controller algorithm and tunable parameters.
