using UnityEngine;

public enum GroundMode
{
    VoxelRaycast,   // resolve against VoxelWorld voxels via RaymarchChunk
    UnityColliders, // rely on real Unity ground colliders only
}

[RequireComponent(typeof(VoxelActor))]
[DisallowMultipleComponent]
public class VoxelGroundResolver : MonoBehaviour
{
    [Header("Ground Collision")]
    [Tooltip("VoxelRaycast = land on voxel-only ground using VoxelWorld (no collider). UnityColliders = use real colliders.")]
    public GroundMode groundMode = GroundMode.VoxelRaycast;
    [Tooltip("How far down (world units) each body probes for voxel ground.")]
    public float groundProbeDistance = 50f;
    [Tooltip("Fraction of horizontal velocity lost per second while touching ground (0 = frictionless, 20 = full stop).")]
    [Range(0f, 50f)] public float groundFriction = 20f;
    [Tooltip("Ground penetration correction aggressiveness. 0.1 = soft, 1 = close in one step. Keep low (0.05–0.3) to avoid resting micro-bounce.")]
    [Range(0.01f, 2f)] public float groundStiffness = 0.1f;
    [Tooltip("Maximum upward correction speed (m/s) so deep penetration can't rocket bodies.")]
    public float groundMaxPushSpeed = 4f;
    [Tooltip("Bounciness of the voxel ground. 0 = inelastic, 1 = fully elastic.")]
    [Range(0f, 1f)] public float groundRestitution = 0f;

    private VoxelActor _actor;

    void Awake() => _actor = GetComponent<VoxelActor>();

    public void ResolveVoxelGround()
    {
        if (groundMode != GroundMode.VoxelRaycast || _actor.VoxelWorld == null) return;

        int feetGrounded = 0;
        int feetTotal = 0;

        for (int i = 0; i < _actor.Bodies.Count; i++)
        {
            var entry = _actor.Bodies[i];
            Rigidbody rb = entry.rb;
            float r = entry.radius;
            int boneId = entry.boneId;
            bool isFoot = _actor.FootBoneIds.Contains(boneId);
            if (isFoot) feetTotal++;
            if (rb == null || rb.isKinematic) continue;

            Vector3 pos = rb.position;
            Vector3 origin = pos + Vector3.up * _actor.VoxelSize;
            VoxelHit hit = _actor.VoxelWorld.RaymarchChunk(origin, Vector3.down, groundProbeDistance);
            if (!hit.hit) continue;

            float surfaceY = hit.worldPosition.y + _actor.VoxelSize * 0.5f;
            float bottom = pos.y - r;
            float groundDistance = surfaceY - bottom;
            float contactMargin = Mathf.Max(_actor.VoxelSize * 0.05f, r * 0.1f);
            if (groundDistance < -contactMargin) continue;

            bool touching = groundDistance > -contactMargin * 0.5f;
            if (isFoot && touching) feetGrounded++;

            if (groundDistance > 0f)
            {
                float push = Mathf.Clamp(groundDistance * groundStiffness / Time.fixedDeltaTime, 0f, groundMaxPushSpeed);
                Vector3 v = rb.linearVelocity;
                if (v.y < 0f)
                    v.y = -v.y * groundRestitution;
                if (v.y < push) v.y = push;
                rb.linearVelocity = v;
            }

            if (touching)
            {
                Vector3 v = rb.linearVelocity;
                float frictionFactor = Mathf.Exp(-groundFriction * Time.fixedDeltaTime);
                v.x *= frictionFactor;
                v.z *= frictionFactor;
                rb.linearVelocity = v;
            }
        }

        if (_actor.Balance != null)
        {
            if (feetTotal > 0)
                _actor.Balance.GroundContactScale = Mathf.Clamp01((float)feetGrounded / feetTotal);
            else
                _actor.Balance.GroundContactScale = 1f;
        }
    }
}
