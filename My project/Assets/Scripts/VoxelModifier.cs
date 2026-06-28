using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles dynamic voxel modification (destruction/placement) at runtime.
/// Attach to player or weapon object.
/// </summary>
public class VoxelModifier : MonoBehaviour
{
    [Header("Modification Settings")]
    [Tooltip("Maximum raycast distance for voxel interaction")]
    public float maxReachDistance = 10f;
    
    [Tooltip("Material ID to place (0 = destroy)")]
    public byte placeMaterialID = 1;
    
    [Tooltip("Destruction radius (1 = single voxel, 2 = 3x3x3 cube, etc.)")]
    public int destructionRadius = 1;
    
    [Header("Input")]
    [Tooltip("Key to destroy voxel")]
    public KeyCode destroyKey = KeyCode.Mouse0; // Left Click
    
    [Tooltip("Key to place voxel")]
    public KeyCode placeKey = KeyCode.Mouse1; // Right Click
    
    [Header("Visual Feedback")]
    [Tooltip("Show crosshair in center of screen")]
    public bool showCrosshair = true;
    
    [Tooltip("Crosshair color")]
    public Color crosshairColor = Color.white;
    
    [Tooltip("Crosshair size")]
    public int crosshairSize = 10;
    
    [Tooltip("Show target voxel highlight")]
    public bool showTargetHighlight = true;
    
    [Tooltip("Highlight color")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.5f);
    
    [Header("Audio (Optional)")]
    public AudioClip destroySound;
    public AudioClip placeSound;
    
    [Header("References")]
    [Tooltip("Camera to raycast from (defaults to main camera)")]
    public Camera playerCamera;
    
    // Internal state
    private VoxelWorld voxelWorld;
    private AudioSource audioSource;
    private VoxelHit currentTarget;
    private bool hasTarget;
    
    // Highlight visualization
    private GameObject highlightCube;
    private MeshRenderer highlightRenderer;
    
    private void Start()
    {
        voxelWorld = VoxelWorld.Instance;
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Setup audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
        
        // Create highlight cube
        if (showTargetHighlight)
        {
            CreateHighlightCube();
        }
    }
    
    private void Update()
    {
        // Update target voxel
        UpdateTargetVoxel();
        
        // Handle input
        var mouse = Mouse.current;
        if (mouse != null)
        {
            // Left click to destroy
            if (mouse.leftButton.wasPressedThisFrame)
            {
                DestroyVoxel();
            }
            
            // Right click to place
            if (mouse.rightButton.wasPressedThisFrame)
            {
                PlaceVoxel();
            }
        }
    }
    
    #region Voxel Targeting
    
    private void UpdateTargetVoxel()
    {
        // Raycast from camera center
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        
        currentTarget = voxelWorld.RaymarchChunk(rayOrigin, rayDirection, maxReachDistance);
        hasTarget = currentTarget.hit;
        
        // Update highlight cube
        if (showTargetHighlight && highlightCube != null)
        {
            if (hasTarget)
            {
                highlightCube.SetActive(true);
                highlightCube.transform.position = currentTarget.worldPosition;
                highlightCube.transform.localScale = Vector3.one * voxelWorld.voxelSize;
            }
            else
            {
                highlightCube.SetActive(false);
            }
        }
    }
    
    #endregion
    
    #region Voxel Modification
    
    private void DestroyVoxel()
    {
        if (!hasTarget) return;
        
        if (destructionRadius == 1)
        {
            // Single voxel destruction
            voxelWorld.SetVoxel(currentTarget.worldPosition, 0);
        }
        else
        {
            // Radius destruction (sphere)
            DestroyVoxelsInRadius(currentTarget.voxelPosition, destructionRadius);
        }
        
        // Play sound
        if (destroySound != null)
        {
            audioSource.PlayOneShot(destroySound);
        }
        
        Debug.Log($"Destroyed voxel at {currentTarget.voxelPosition}");
    }
    
    private void PlaceVoxel()
    {
        if (!hasTarget) return;
        
        // Place voxel on the face that was hit (adjacent to hit voxel)
        Vector3Int placePosition = currentTarget.voxelPosition + currentTarget.normal;
        Vector3 worldPlacePosition = voxelWorld.VoxelGridToWorld(placePosition);
        
        // Check if placement position is occupied
        byte existingMaterial = voxelWorld.GetVoxel(worldPlacePosition);
        if (existingMaterial != 0)
        {
            Debug.Log("Cannot place voxel - position occupied");
            return;
        }
        
        // Check if placement would overlap player
        if (Vector3.Distance(worldPlacePosition, transform.position) < 1f)
        {
            Debug.Log("Cannot place voxel - too close to player");
            return;
        }
        
        // Place voxel
        voxelWorld.SetVoxel(worldPlacePosition, placeMaterialID);
        
        // Play sound
        if (placeSound != null)
        {
            audioSource.PlayOneShot(placeSound);
        }
        
        Debug.Log($"Placed voxel (material {placeMaterialID}) at {placePosition}");
    }
    
    private void DestroyVoxelsInRadius(Vector3Int center, int radius)
    {
        // Iterate through cube around center
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    
                    // Check if within sphere radius
                    if (offset.magnitude <= radius)
                    {
                        Vector3Int voxelPos = center + offset;
                        Vector3 worldPos = voxelWorld.VoxelGridToWorld(voxelPos);
                        voxelWorld.SetVoxel(worldPos, 0);
                    }
                }
            }
        }
    }
    
    #endregion
    
    #region Visual Feedback
    
    private void CreateHighlightCube()
    {
        highlightCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        highlightCube.name = "VoxelHighlight";
        
        // Remove collider
        Destroy(highlightCube.GetComponent<Collider>());
        
        // Setup material
        highlightRenderer = highlightCube.GetComponent<MeshRenderer>();
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = highlightColor;
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
        
        highlightRenderer.material = mat;
        highlightCube.SetActive(false);
    }
    
    private void OnGUI()
    {
        if (!showCrosshair) return;
        
        // Draw crosshair in center of screen
        float centerX = Screen.width / 2f;
        float centerY = Screen.height / 2f;
        
        // Horizontal line
        GUI.color = crosshairColor;
        GUI.DrawTexture(
            new Rect(centerX - crosshairSize, centerY - 1, crosshairSize * 2, 2),
            Texture2D.whiteTexture
        );
        
        // Vertical line
        GUI.DrawTexture(
            new Rect(centerX - 1, centerY - crosshairSize, 2, crosshairSize * 2),
            Texture2D.whiteTexture
        );
        
        // Target info
        if (hasTarget)
        {
            GUI.color = Color.white;
            GUI.Label(
                new Rect(centerX + 20, centerY - 10, 300, 20),
                $"Target: {currentTarget.voxelPosition} | Material: {currentTarget.materialID} | Distance: {currentTarget.distance:F2}m"
            );
        }
    }
    
    #endregion
    
    #region Public API
    
    /// <summary>
    /// Set the material ID to place when using place key
    /// </summary>
    public void SetPlaceMaterial(byte materialID)
    {
        placeMaterialID = materialID;
        Debug.Log($"Place material set to: {materialID}");
    }
    
    /// <summary>
    /// Set destruction radius
    /// </summary>
    public void SetDestructionRadius(int radius)
    {
        destructionRadius = Mathf.Max(1, radius);
        Debug.Log($"Destruction radius set to: {destructionRadius}");
    }
    
    /// <summary>
    /// Get currently targeted voxel
    /// </summary>
    public VoxelHit GetTargetVoxel()
    {
        return currentTarget;
    }
    
    /// <summary>
    /// Check if currently targeting a voxel
    /// </summary>
    public bool HasTarget()
    {
        return hasTarget;
    }
    
    #endregion
}
