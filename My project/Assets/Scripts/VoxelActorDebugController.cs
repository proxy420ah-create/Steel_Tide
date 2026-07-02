using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(VoxelActor))]
[DisallowMultipleComponent]
public class VoxelActorDebugController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The actor to reset/control. Auto-fills from this GameObject if null.")]
    public VoxelActor actor;

    [Header("Time Controls")]
    [Tooltip("Key to slow time to slowTimeScale.")]
    public Key slowTimeKey = Key.T;
    [Tooltip("Key to pause / unpause.")]
    public Key pauseKey = Key.P;
    [Tooltip("Key to return to normal speed.")]
    public Key normalTimeKey = Key.N;
    [Tooltip("Time scale used by slow-time toggle.")]
    public float slowTimeScale = 0.1f;

    [Header("Actor Controls")]
    [Tooltip("Reset the actor to its bind pose and spawn position.")]
    public Key resetActorKey = Key.K;
    [Tooltip("Respawn the actor at its spawn position without rebuilding.")]
    public Key respawnKey = Key.R;
    [Tooltip("Spawn position. If not set, actor's initial position is captured on Awake.")]
    public Transform spawnPoint;

    [Header("Subsystem Toggles")]
    [Tooltip("Toggle balance on/off.")]
    public Key toggleBalanceKey = Key.B;
    [Tooltip("Toggle ground collision on/off.")]
    public Key toggleGroundKey = Key.G;
    [Tooltip("Toggle revoxelization on/off.")]
    public Key toggleRevoxelizationKey = Key.V;
    [Tooltip("Toggle joint constraints on/off (requires rebuild).")]
    public Key toggleJointsKey = Key.J;

    [Header("Hints")]
    public bool showDebugHints = true;
    public Vector2 hintPosition = new Vector2(10, 10);

    private float _lastTimeScale = 1f;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private bool _wasPaused;

    void Awake()
    {
        actor = actor ?? GetComponent<VoxelActor>();
        if (actor != null)
        {
            _spawnPosition = spawnPoint != null ? spawnPoint.position : actor.transform.position;
            _spawnRotation = spawnPoint != null ? spawnPoint.rotation : actor.transform.rotation;
        }
    }

    void Update()
    {
        if (actor == null) return;

        HandleTimeControls();
        HandleActorControls();
        HandleSubsystemToggles();
    }

    private void HandleTimeControls()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (WasPressedThisFrame(slowTimeKey))
        {
            if (Mathf.Abs(Time.timeScale - slowTimeScale) < 0.001f)
                Time.timeScale = _lastTimeScale > 0f ? _lastTimeScale : 1f;
            else
            {
                _lastTimeScale = Time.timeScale;
                Time.timeScale = slowTimeScale;
            }
            Debug.Log($"[Debug] Time.timeScale = {Time.timeScale}");
        }

        if (WasPressedThisFrame(pauseKey))
        {
            if (Mathf.Abs(Time.timeScale) < 0.001f)
            {
                Time.timeScale = _lastTimeScale > 0f ? _lastTimeScale : 1f;
                _wasPaused = false;
            }
            else
            {
                _lastTimeScale = Time.timeScale;
                Time.timeScale = 0f;
                _wasPaused = true;
            }
            Debug.Log($"[Debug] Time.timeScale = {Time.timeScale} ({(_wasPaused ? "paused" : "running")})");
        }

        if (WasPressedThisFrame(normalTimeKey))
        {
            Time.timeScale = 1f;
            _wasPaused = false;
            Debug.Log("[Debug] Time.timeScale = 1");
        }
    }

    private void HandleActorControls()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (WasPressedThisFrame(resetActorKey))
        {
            ResetActor();
        }

        if (WasPressedThisFrame(respawnKey))
        {
            RespawnActor();
        }
    }

    private void HandleSubsystemToggles()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (WasPressedThisFrame(toggleBalanceKey))
        {
            actor.enableBalance = !actor.enableBalance;
            Debug.Log($"[Debug] enableBalance = {actor.enableBalance}");
        }

        if (WasPressedThisFrame(toggleGroundKey))
        {
            actor.enableGroundCollision = !actor.enableGroundCollision;
            Debug.Log($"[Debug] enableGroundCollision = {actor.enableGroundCollision}");
        }

        if (WasPressedThisFrame(toggleRevoxelizationKey))
        {
            actor.enableRevoxelization = !actor.enableRevoxelization;
            Debug.Log($"[Debug] enableRevoxelization = {actor.enableRevoxelization}");
        }

        if (WasPressedThisFrame(toggleJointsKey))
        {
            actor.buildJoints = !actor.buildJoints;
            Debug.Log($"[Debug] buildJoints = {actor.buildJoints} (rebuild needed)");
        }
    }

    public void ResetActor()
    {
        if (actor == null) return;
        actor.ResetActor();
        actor.transform.position = _spawnPosition;
        actor.transform.rotation = _spawnRotation;
        Debug.Log("[Debug] Actor reset to bind pose and spawn position.");
    }

    public void RespawnActor()
    {
        if (actor == null) return;
        actor.transform.position = _spawnPosition;
        actor.transform.rotation = _spawnRotation;
        actor.SetKinematic(false);
        Debug.Log("[Debug] Actor respawned at spawn position.");
    }

    private bool WasPressedThisFrame(Key key)
    {
        if (key == Key.None) return false;
        var k = Keyboard.current[key];
        return k != null && k.wasPressedThisFrame;
    }

    void OnGUI()
    {
        if (!showDebugHints) return;
        if (actor == null) return;

        GUILayout.BeginArea(new Rect(hintPosition.x, hintPosition.y, 280, 220));
        GUILayout.BeginVertical("box");
        GUILayout.Label("VoxelActor Debug Controls");
        GUILayout.Label($"{resetActorKey} = Reset actor");
        GUILayout.Label($"{respawnKey} = Respawn position");
        GUILayout.Label($"{slowTimeKey} = Slow time ({slowTimeScale}x)");
        GUILayout.Label($"{pauseKey} = Pause / unpause");
        GUILayout.Label($"{normalTimeKey} = Normal time");
        GUILayout.Label($"{toggleBalanceKey} = Balance: {actor.enableBalance}");
        GUILayout.Label($"{toggleGroundKey} = Ground: {actor.enableGroundCollision}");
        GUILayout.Label($"{toggleRevoxelizationKey} = Revoxel: {actor.enableRevoxelization}");
        GUILayout.Label($"{toggleJointsKey} = Joints: {actor.buildJoints}");
        GUILayout.Label($"Time.scale: {Time.timeScale:F2}");
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}
