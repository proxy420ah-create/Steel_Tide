using UnityEngine;
using SteelTide.ActorPhysics;

[RequireComponent(typeof(VoxelActor))]
[DisallowMultipleComponent]
public class VoxelBalance : MonoBehaviour
{
    [Header("Balance")]
    [Tooltip("Active balance controller settings. Strength 0 = pure ragdoll.")]
    public BalanceSettings balanceSettings = new BalanceSettings();

    private VoxelActor _actor;

    void Awake() => _actor = GetComponent<VoxelActor>();

    public void Initialize()
    {
        if (balanceSettings == null)
            balanceSettings = new BalanceSettings();
        if (_actor != null && _actor.Skeleton != null && _actor.RootBody != null)
            _actor.Balance = new BalanceController(_actor.Skeleton, _actor.BoneTransforms, _actor.RootBody, balanceSettings);
    }

    public void UpdateBalance()
    {
        if (_actor != null && _actor.Balance != null && balanceSettings != null && balanceSettings.balanceStrength > 0f)
            _actor.Balance.UpdateBalance();
    }

    void OnGUI()
    {
        if (_actor == null || !_actor.IsInitialized || _actor.Balance == null) return;
        if (balanceSettings == null || !balanceSettings.drawBalanceGizmos) return;

        float margin = 8f;
        float width = 200f;
        float lineHeight = 20f;
        float lines = 7f;
        float height = margin + lines * lineHeight;

        Rect panel = new Rect(Screen.width - width - margin, margin + 90f, width, height);
        GUI.color = new Color(0f, 0f, 0f, 0.65f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle headerStyle = new GUIStyle(GUI.skin.label);
        headerStyle.fontSize = 14;
        headerStyle.normal.textColor = new Color(1f, 0.85f, 0.3f);
        GUIStyle valueStyle = new GUIStyle(GUI.skin.label);
        valueStyle.fontSize = 14;
        valueStyle.normal.textColor = Color.white;

        float y = panel.y + margin;
        float x = panel.x + margin;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), "Balance Diagnostics", headerStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Strength: {balanceSettings.balanceStrength:F0}", valueStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Lean: {_actor.Balance.LastLeanMagnitude:F2}", valueStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Applied Torque: {_actor.Balance.LastAppliedTorque.magnitude:F1}", valueStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Required Torque: {_actor.Balance.LastRequiredTorque:F1}", valueStyle);
        y += lineHeight;
        float ratio = _actor.Balance.LastRequiredTorque > 0f
            ? _actor.Balance.LastAppliedTorque.magnitude / _actor.Balance.LastRequiredTorque
            : 0f;
        GUIStyle ratioStyle = new GUIStyle(valueStyle);
        ratioStyle.normal.textColor = ratio < 1f ? new Color(1f, 0.4f, 0.4f)
                                  : ratio > 1.2f ? new Color(0.4f, 1f, 0.4f)
                                  : new Color(1f, 0.85f, 0.3f);
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Torque Ratio: {ratio:F2}", ratioStyle);
        y += lineHeight;
        GUI.Label(new Rect(x, y, width - margin * 2f, lineHeight), $"Mass: {_actor.Balance.TotalMass:F1}", valueStyle);
    }
}
