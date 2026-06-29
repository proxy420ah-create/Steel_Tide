using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Switches between first-person and third-person cameras
/// Press 'C' to toggle between camera modes
/// </summary>
public class CameraSwitcher : MonoBehaviour
{
    [Header("Camera References")]
    [Tooltip("First-person camera (child of player)")]
    public Camera firstPersonCamera;
    
    [Tooltip("Third-person camera (orbits around player)")]
    public Camera thirdPersonCamera;
    
    [Header("Third-Person Settings")]
    [Tooltip("Distance behind player")]
    public float thirdPersonDistance = 5f;
    
    [Tooltip("Height above player")]
    public float thirdPersonHeight = 2f;
    
    [Tooltip("Camera follows player smoothly")]
    public float followSpeed = 5f;
    
    private bool isFirstPerson = true;
    private Transform playerTransform;
    
    private void Start()
    {
        playerTransform = transform;
        
        // Ensure cameras exist
        if (firstPersonCamera == null)
        {
            Debug.LogError("[CameraSwitcher] First-person camera not assigned!");
        }
        
        if (thirdPersonCamera == null)
        {
            Debug.LogError("[CameraSwitcher] Third-person camera not assigned!");
        }
        
        // Start in first-person mode
        SetCameraMode(true);
    }
    
    private void Update()
    {
        // Toggle camera mode with C key (new Input System)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.cKey.wasPressedThisFrame)
        {
            isFirstPerson = !isFirstPerson;
            SetCameraMode(isFirstPerson);
            Debug.Log($"[CameraSwitcher] Switched to {(isFirstPerson ? "First-Person" : "Third-Person")} camera");
        }
        
        // Update third-person camera position
        if (!isFirstPerson && thirdPersonCamera != null)
        {
            UpdateThirdPersonCamera();
        }
    }
    
    private void SetCameraMode(bool firstPerson)
    {
        if (firstPersonCamera != null)
            firstPersonCamera.enabled = firstPerson;
        
        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = !firstPerson;
    }
    
    private void UpdateThirdPersonCamera()
    {
        // Calculate target position (behind and above player)
        Vector3 targetPosition = playerTransform.position 
            - playerTransform.forward * thirdPersonDistance 
            + Vector3.up * thirdPersonHeight;
        
        // Smooth follow
        thirdPersonCamera.transform.position = Vector3.Lerp(
            thirdPersonCamera.transform.position,
            targetPosition,
            followSpeed * Time.deltaTime
        );
        
        // Look at player
        thirdPersonCamera.transform.LookAt(playerTransform.position + Vector3.up * 2f);
    }
}
