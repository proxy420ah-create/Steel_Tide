// Steel Tide: Voxel Actor Balance Controller
// BalanceController.cs
//
// Active stability system for voxel ragdoll actors. Measures the actor's center
// of mass relative to the foot support polygon and applies corrective torques
// to keep the character upright. Inspired by the same feedback principles used
// in real bipedal robotics (e.g., Boston Dynamics Atlas): track CoM, compare to
// support, apply corrective force.
//
// This is intentionally simpler than full model predictive control: it is
// real-time, tunable, and designed to produce the "wobbly but recoverable" feel
// of physics-driven games like Human Fall Flat / Gang Beasts / TABS.

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using SteelTide.Voxels;

namespace SteelTide.ActorPhysics
{
    [System.Serializable]
    public class BalanceSettings
    {
        [Tooltip("Normalized balance gain. 0 = pure ragdoll, 1.0 = matches gravity torque, 2.0+ = recovers.")]
        [Range(0f, 5f)]
        public float balanceStrength = 1.0f;

        [Tooltip("Ignore lean below this threshold to prevent micro-jitter.")]
        [Range(0f, 1f)]
        public float balanceDeadzone = 0.1f;

        [Tooltip("Lean distance at which the actor loses balance and transitions to ragdoll (future).")]
        public float fallThreshold = 2.0f;

        [Tooltip("How much ankle joints contribute to balance correction relative to the pelvis.")]
        [Range(0f, 1f)]
        public float ankleAssist = 0.3f;

        [Tooltip("Damping on the pelvis angular velocity. 0 = no damping (can oscillate). Higher = less overshoot.")]
        [Range(0f, 10f)]
        public float balanceDamping = 2.0f;

        [Tooltip("Smoothing time in seconds for the lean vector. 0 = instantaneous. 0.1-0.3 = less twitchy.")]
        [Range(0f, 1f)]
        public float leanSmoothing = 0.15f;

        [Tooltip("How much balance correction applies while airborne. 0 = no correction in air, 1 = full correction.")]
        [Range(0f, 1f)]
        public float airborneBalance = 0.1f;

        [Tooltip("Draw debug gizmos for center of mass, support center, and lean vector.")]
        public bool drawBalanceGizmos = true;

        [Tooltip("Draw debug gizmos for gravity force and tipping torque.")]
        public bool drawGravityGizmos = true;
    }

    public class BalanceController
    {
        private readonly VoxelSkeleton _skel;
        private readonly Dictionary<int, Transform> _boneTransforms;
        private readonly Transform _rootBody;
        private readonly BalanceSettings _settings;

        // Cached references to foot bones for quick lookup.
        private Transform _leftFoot;
        private Transform _rightFoot;
        private bool _leftFootAnkleUnlocked;
        private bool _rightFootAnkleUnlocked;
        private bool _hasFeet;

        // Debug state exposed for gizmos and tuning.
        public Vector3 LastCenterOfMass { get; private set; }
        public Vector3 LastSupportCenter { get; private set; }
        public Vector3 LastLean { get; private set; }
        public Vector3 LastLeanSmoothed { get; private set; }
        public float LastLeanMagnitude { get; private set; }
        public Vector3 LastAppliedTorque { get; private set; }
        public float LastRequiredTorque { get; private set; }
        public float TotalMass { get; private set; }

        private Vector3 _leanSmoothed;

        // Ground contact scale from VoxelRagdoll: 1.0 = feet on ground, 0.0 = fully airborne.
        // The balance controller uses this to suppress or reduce corrective torque while falling.
        public float GroundContactScale { get; set; } = 1f;

        public BalanceController(VoxelSkeleton skel, Dictionary<int, Transform> boneTransforms, Transform rootBody, BalanceSettings settings)
        {
            _skel = skel;
            _boneTransforms = boneTransforms;
            _rootBody = rootBody;
            _settings = settings;
            CacheFeet();
        }

        private void CacheFeet()
        {
            _leftFoot = FindBoneByRole("foot", "L")
                     ?? FindBoneByRole("foot", "left")
                     ?? FindBoneByRole("foot", "Left");
            _rightFoot = FindBoneByRole("foot", "R")
                      ?? FindBoneByRole("foot", "right")
                      ?? FindBoneByRole("foot", "Right");
            _leftFootAnkleUnlocked = IsAnkleUnlocked(_leftFoot);
            _rightFootAnkleUnlocked = IsAnkleUnlocked(_rightFoot);
            _hasFeet = _leftFoot != null || _rightFoot != null;
        }

        private static bool IsAnkleUnlocked(Transform foot)
        {
            if (foot == null) return false;
            var joint = foot.GetComponent<ConfigurableJoint>();
            if (joint == null) return false;
            return joint.angularXMotion != ConfigurableJointMotion.Locked
                || joint.angularYMotion != ConfigurableJointMotion.Locked
                || joint.angularZMotion != ConfigurableJointMotion.Locked;
        }

        private Transform FindBoneByRole(string role, string side)
        {
            if (_skel == null || _skel.bones == null) return null;
            foreach (var bone in _skel.bones)
            {
                if (bone.role == null) continue;
                if (!bone.role.Equals(role, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!string.IsNullOrEmpty(side) && bone.side != null &&
                    !bone.side.Equals(side, System.StringComparison.OrdinalIgnoreCase)) continue;
                if (!_boneTransforms.TryGetValue(bone.id, out var t)) continue;
                return t;
            }
            return null;
        }

        public void UpdateBalance()
        {
            if (_settings == null || _settings.balanceStrength <= 0f) return;
            if (_rootBody == null) return;

            Vector3 com = ComputeCenterOfMass();
            Vector3 support = ComputeSupportCenter();
            Vector3 lean = com - support;
            lean.y = 0f;

            // Smooth the lean over time so the corrective torque doesn't flip
            // direction every frame. Alpha is computed from dt so the time constant
            // stays consistent regardless of Time.timeScale.
            float tau = Mathf.Max(0.001f, _settings.leanSmoothing);
            float alpha = 1f - Mathf.Exp(-Time.fixedDeltaTime / tau);
            _leanSmoothed = Vector3.Lerp(_leanSmoothed, lean, alpha);
            if (_leanSmoothed.magnitude < 0.001f && lean.magnitude < 0.001f)
                _leanSmoothed = Vector3.zero;

            LastCenterOfMass = com;
            LastSupportCenter = support;
            LastLean = lean;
            LastLeanSmoothed = _leanSmoothed;
            LastLeanMagnitude = _leanSmoothed.magnitude;

            // Required torque to hold balance against gravity: weight × horizontal moment arm.
            float weight = TotalMass * Physics.gravity.magnitude;
            LastRequiredTorque = weight * LastLeanMagnitude;

            if (_leanSmoothed.magnitude > _settings.balanceDeadzone)
                ApplyCorrectiveTorques(_leanSmoothed);

            // Future: notify VoxelRagdoll to transition to Ragdoll mode when lean exceeds threshold.
            // For now, the controller keeps fighting until the physics simulation naturally falls.
        }

        private Vector3 ComputeCenterOfMass()
        {
            Vector3 com = Vector3.zero;
            TotalMass = 0f;

            var rootRb = _rootBody.GetComponent<Rigidbody>();
            if (rootRb != null)
            {
                com += rootRb.position * rootRb.mass;
                TotalMass += rootRb.mass;
            }

            foreach (var kvp in _boneTransforms)
            {
                var rb = kvp.Value.GetComponent<Rigidbody>();
                if (rb == null) continue;
                com += rb.position * rb.mass;
                TotalMass += rb.mass;
            }

            if (TotalMass > 0f)
                com /= TotalMass;

            return com;
        }

        private Vector3 ComputeSupportCenter()
        {
            if (!_hasFeet)
                return _rootBody.position;

            Vector3 support = Vector3.zero;
            int count = 0;
            if (_leftFoot != null) { support += _leftFoot.position; count++; }
            if (_rightFoot != null) { support += _rightFoot.position; count++; }

            return count > 0 ? support / count : _rootBody.position;
        }

        private void ApplyCorrectiveTorques(Vector3 lean)
        {
            Vector3 leanDir = lean.normalized;
            // Torque axis is perpendicular to both up and the lean direction.
            // Cross(leanDir, up) gives the rotation that tilts the body back toward upright.
            // (Cross(up, leanDir) would push it further over, which is the wrong sign.)
            Vector3 torqueAxis = Vector3.Cross(leanDir, Vector3.up);

            // Required torque to hold the current lean: weight × horizontal moment arm.
            float weight = TotalMass * Physics.gravity.magnitude;
            float requiredTorque = weight * lean.magnitude;

            // Balance strength is now a normalized gain: 1.0 = matches gravity, 2.0+ = overcorrects.
            // Scale by ground contact: feet on ground = full correction, airborne = airborneBalance.
            float contactScale = Mathf.Clamp01(GroundContactScale);
            float effectiveStrength = Mathf.Lerp(_settings.airborneBalance * _settings.balanceStrength, _settings.balanceStrength, contactScale);

            Vector3 pelvisTorque = torqueAxis * effectiveStrength * requiredTorque;
            // Ankles assist in the opposite direction, creating a stabilizing couple.
            // Ankle torque is only useful when the foot is actually touching ground.
            Vector3 ankleTorque = -torqueAxis * effectiveStrength * requiredTorque * _settings.ankleAssist * contactScale;

            LastAppliedTorque = pelvisTorque;

            var rootRb = _rootBody.GetComponent<Rigidbody>();
            if (rootRb != null)
            {
                // Damping torque opposes the pelvis's current spin to prevent high-gain oscillation.
                // Scale with balanceStrength so higher gains stay critically damped.
                float damping = TotalMass * _settings.balanceDamping * effectiveStrength;
                Vector3 dampingTorque = -rootRb.angularVelocity * damping;

                rootRb.AddTorque(pelvisTorque + dampingTorque, ForceMode.Force);
                LastAppliedTorque = pelvisTorque + dampingTorque;
            }

            // Only apply ankle torque if the ankle joint can actually articulate.
            // Locked ankles (ROOT type) receive no torque — the force would be wasted
            // and could destabilize the rigid foot contact instead of helping.
            if (_leftFoot != null && _leftFootAnkleUnlocked)
            {
                var rb = _leftFoot.GetComponent<Rigidbody>();
                if (rb != null) rb.AddTorque(ankleTorque, ForceMode.Force);
            }
            if (_rightFoot != null && _rightFootAnkleUnlocked)
            {
                var rb = _rightFoot.GetComponent<Rigidbody>();
                if (rb != null) rb.AddTorque(ankleTorque, ForceMode.Force);
            }
        }

        public void DrawGizmos()
        {
            if (_settings == null || !_settings.drawBalanceGizmos) return;

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(LastCenterOfMass, 0.05f);

            Gizmos.color = Color.green;
            Gizmos.DrawSphere(LastSupportCenter, 0.05f);

            // Live strength meter: shows Balance Strength value as a horizontal bar.
            float maxMeterStrength = 5f;
            float fill = Mathf.Clamp01(_settings.balanceStrength / maxMeterStrength);
            Vector3 meterOrigin = LastCenterOfMass + Vector3.up * 0.8f;
            Vector3 meterDir = Vector3.right;
            float barLength = 1.0f;

            Gizmos.color = Color.gray;
            Gizmos.DrawLine(meterOrigin, meterOrigin + meterDir * barLength);
            Gizmos.color = Color.Lerp(Color.green, Color.red, fill);
            Gizmos.DrawLine(meterOrigin, meterOrigin + meterDir * (barLength * fill));
            Gizmos.DrawSphere(meterOrigin + meterDir * (barLength * fill), 0.05f);

#if UNITY_EDITOR
            // Live numeric debug label so the user can verify values are changing.
            string label = $"Str={_settings.balanceStrength:F0}  Lean={LastLeanMagnitude:F2}  " +
                           $"Torq={LastAppliedTorque.magnitude:F1}  Need={LastRequiredTorque:F1}";
            Handles.Label(LastCenterOfMass + Vector3.up * 1.2f, label);
#endif

            // Raw lean line: faint, shows the instantaneous CoM offset.
            if (LastLean.magnitude > 0.001f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawLine(LastSupportCenter, LastCenterOfMass);
            }

            // Smoothed lean line: yellow, this is the input the controller actually uses.
            if (LastLeanSmoothed.magnitude > 0.001f)
            {
                Gizmos.color = Color.yellow;
                Vector3 smoothedTip = LastSupportCenter + LastLeanSmoothed;
                Gizmos.DrawLine(LastSupportCenter, smoothedTip);
            }

            // Balance torque arrow: shows the axis/direction the pelvis is being torqued to right the body.
            if (LastAppliedTorque.magnitude > 0.001f)
            {
                Vector3 torqueDir = LastAppliedTorque.normalized;
                float arrowLength = Mathf.Clamp(LastAppliedTorque.magnitude * 0.05f, 0.3f, 3.0f);
                Vector3 torqueTip = LastCenterOfMass + torqueDir * arrowLength;

                Gizmos.color = Color.red;
                Gizmos.DrawLine(LastCenterOfMass, torqueTip);
                DrawArrowHead(torqueTip, torqueDir, 0.15f);
            }

            // Desired recovery direction: the horizontal direction the CoM needs to move to return over support.
            if (LastLeanSmoothed.magnitude > 0.001f)
            {
                Vector3 recoveryDir = -LastLeanSmoothed.normalized;
                Vector3 recoveryTip = LastCenterOfMass + recoveryDir * 1.0f;

                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(LastCenterOfMass, recoveryTip);
                DrawArrowHead(recoveryTip, recoveryDir, 0.12f);
            }

            // Gravity force: total weight pulling the CoM down.
            if (_settings.drawGravityGizmos && TotalMass > 0f)
            {
                float gravityMag = Physics.gravity.magnitude;
                float weight = TotalMass * gravityMag;
                float gravityArrowLength = Mathf.Clamp(weight * 0.02f, 0.5f, 3.0f);
                Vector3 gravityTip = LastCenterOfMass + Vector3.down * gravityArrowLength;

                Gizmos.color = new Color(1f, 0.55f, 0f); // orange
                Gizmos.DrawLine(LastCenterOfMass, gravityTip);
                DrawArrowHead(gravityTip, Vector3.down, 0.12f);

                // Gravity-induced tipping torque: direction the body is trying to fall.
                if (LastLeanSmoothed.magnitude > 0.001f)
                {
                    Vector3 fallTorque = Vector3.Cross(LastLeanSmoothed, Vector3.down * weight);
                    Vector3 fallDir = fallTorque.normalized;
                    Vector3 fallTip = LastCenterOfMass + fallDir * 0.6f;

                    Gizmos.color = new Color(1f, 0.85f, 0f); // yellow-orange
                    Gizmos.DrawLine(LastCenterOfMass, fallTip);
                    DrawArrowHead(fallTip, fallDir, 0.1f);
                }
            }
        }

        private static void DrawArrowHead(Vector3 tip, Vector3 direction, float size)
        {
            Vector3 dir = direction.normalized;
            Vector3 cross = Vector3.Cross(dir, Vector3.up).normalized * size;
            if (cross.magnitude < 0.001f) cross = Vector3.right * size;
            Vector3 back = tip + dir * -size * 1.5f;
            Gizmos.DrawLine(tip, back + cross);
            Gizmos.DrawLine(tip, back - cross);
            Gizmos.DrawLine(tip, back + dir * size * 0.5f);
        }
    }
}
