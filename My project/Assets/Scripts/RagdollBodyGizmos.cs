// Steel Tide: First Device — Ragdoll Body Gizmos
// RagdollBodyGizmos.cs
//
// Draws the bone and joint gizmos on the generated RagdollBodies container.
// Lets the user compare the authored/debug bone shapes against the real Unity colliders.

using UnityEngine;

public class RagdollBodyGizmos : MonoBehaviour
{
    [Tooltip("Draw the bone/collider wire gizmos.")]
    public bool drawBoneGizmos = true;
    [Tooltip("Draw colored wire spheres at every joint position.")]
    public bool drawJointGizmos = true;
    [Tooltip("Joint gizmo radius as a multiple of voxelSize.")]
    public float jointGizmoRadiusVoxels = 0.8f;
    [Tooltip("Draw gizmos even when this object is not selected.")]
    public bool drawGizmosAlways = false;
    [Tooltip("World-space offset applied to the gizmos so you can silhouette them against the real colliders.")]
    public Vector3 gizmoOffset = Vector3.zero;

    [Tooltip("Reference to the parent VoxelRagdoll that owns the skeleton/bones.")]
    public VoxelRagdoll parentRagdoll;
    [Tooltip("Reference to the parent VoxelActor (modular replacement) that owns the skeleton/bones.")]
    public VoxelActor parentActor;

    private void Draw()
    {
        if (parentRagdoll != null)
        {
            parentRagdoll.DrawBodyGizmos(drawBoneGizmos, drawJointGizmos, jointGizmoRadiusVoxels, gizmoOffset);
            return;
        }
        if (parentActor != null)
        {
            var gizmos = parentActor.GetComponent<VoxelActorGizmos>();
            if (gizmos != null)
                gizmos.DrawBodyGizmos(drawBoneGizmos, drawJointGizmos, jointGizmoRadiusVoxels, gizmoOffset);
        }
    }

    void OnDrawGizmosSelected()
    {
        Draw();
    }

    void OnDrawGizmos()
    {
        if (!drawGizmosAlways) return;
        Draw();
    }
}
