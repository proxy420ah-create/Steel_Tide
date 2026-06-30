using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Manages voxel data for the entire world.
/// Handles loading, modification, and raymarching collision detection.
/// </summary>
public class VoxelWorld : MonoBehaviour
{
    [Header("World Settings")]
    [Tooltip("Size of each voxel in world units")]
    public float voxelSize = 0.125f;
    
    [Header("Debug")]
    public bool showDebugRays = false;
    public Color debugRayColor = Color.yellow;
    
    // Voxel data storage: position -> material ID (0 = air)
    private Dictionary<Vector3Int, byte> voxelData = new Dictionary<Vector3Int, byte>();
    
    // Chunk cache for loaded voxel objects
    private Dictionary<string, VoxelChunk> loadedChunks = new Dictionary<string, VoxelChunk>();
    
    private static VoxelWorld _instance;
    public static VoxelWorld Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<VoxelWorld>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("VoxelWorld");
                    _instance = go.AddComponent<VoxelWorld>();
                }
            }
            return _instance;
        }
    }
    
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }
    
    #region Voxel Data Management
    
    /// <summary>
    /// Register a voxel object's data into the world grid
    /// </summary>
    public void RegisterVoxelObject(Vector3 worldPosition, string assetFileName, float objectVoxelSize)
    {
        Debug.Log($"[VoxelWorld] RegisterVoxelObject called: {assetFileName} at {worldPosition}");
        
        // Load voxel data from .stasset file
        VoxelChunk chunk = LoadVoxelAsset(assetFileName);
        if (chunk == null)
        {
            Debug.LogError($"[VoxelWorld] Failed to load voxel asset: {assetFileName}");
            return;
        }
        
        Debug.Log($"[VoxelWorld] Chunk loaded with {chunk.voxels.Count} voxels");
        
        // Convert world position to voxel grid position
        Vector3Int gridOrigin = WorldToVoxelGrid(worldPosition);
        Debug.Log($"[VoxelWorld] Grid origin: {gridOrigin}");
        
        // Register each voxel in the world grid
        int registeredCount = 0;
        foreach (var kvp in chunk.voxels)
        {
            Vector3Int localPos = kvp.Key;
            byte materialID = kvp.Value;
            
            Vector3Int worldVoxelPos = gridOrigin + localPos;
            voxelData[worldVoxelPos] = materialID;
            registeredCount++;
        }
        
        Debug.Log($"[VoxelWorld] Registered {registeredCount} voxels from {assetFileName} at {worldPosition}. Total voxels in world: {voxelData.Count}");
    }
    
    /// <summary>
    /// Get voxel material at world position
    /// </summary>
    public byte GetVoxel(Vector3 worldPosition)
    {
        Vector3Int gridPos = WorldToVoxelGrid(worldPosition);
        return voxelData.ContainsKey(gridPos) ? voxelData[gridPos] : (byte)0;
    }
    
    /// <summary>
    /// Set voxel material at world position (0 = air/destroy)
    /// </summary>
    public void SetVoxel(Vector3 worldPosition, byte materialID)
    {
        Vector3Int gridPos = WorldToVoxelGrid(worldPosition);
        SetVoxel(gridPos, materialID);
    }

    /// <summary>
    /// Set voxel material directly via grid coordinate (0 = air/destroy)
    /// </summary>
    public void SetVoxel(Vector3Int gridPos, byte materialID)
    {
        if (materialID == 0)
        {
            voxelData.Remove(gridPos); // Remove air voxels to save memory
        }
        else
        {
            voxelData[gridPos] = materialID;
        }
    }
    
    #endregion
    
    #region Raymarching Physics
    
    /// <summary>
    /// DDA raymarching through voxel grid
    /// Returns hit info including position, normal, and material
    /// </summary>
    public VoxelHit RaymarchChunk(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance = 100f)
    {
        VoxelHit hit = new VoxelHit();
        
        // Normalize direction
        rayDirection.Normalize();
        
        // Current voxel grid position
        Vector3Int currentVoxel = WorldToVoxelGrid(rayOrigin);
        
        // Step direction (+1 or -1 for each axis)
        Vector3Int step = new Vector3Int(
            rayDirection.x > 0 ? 1 : -1,
            rayDirection.y > 0 ? 1 : -1,
            rayDirection.z > 0 ? 1 : -1
        );
        
        // Distance to next voxel boundary along each axis
        Vector3 tMax = new Vector3(
            GetTMax(rayOrigin.x, rayDirection.x, step.x),
            GetTMax(rayOrigin.y, rayDirection.y, step.y),
            GetTMax(rayOrigin.z, rayDirection.z, step.z)
        );
        
        // Distance to cross one voxel along each axis
        Vector3 tDelta = new Vector3(
            Mathf.Abs(voxelSize / rayDirection.x),
            Mathf.Abs(voxelSize / rayDirection.y),
            Mathf.Abs(voxelSize / rayDirection.z)
        );
        
        // DDA traversal
        float distanceTraveled = 0f;
        Vector3Int lastVoxel = currentVoxel;
        int stepsChecked = 0;
        
        if (showDebugRays && Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VoxelWorld] Raymarch START: origin={rayOrigin}, dir={rayDirection}, startVoxel={currentVoxel}, totalVoxelsInWorld={voxelData.Count}");
        }
        
        while (distanceTraveled < maxDistance)
        {
            // Check current voxel
            byte material = voxelData.ContainsKey(currentVoxel) ? voxelData[currentVoxel] : (byte)0;
            stepsChecked++;
            
            if (showDebugRays && Time.frameCount % 60 == 0 && stepsChecked <= 5)
            {
                Debug.Log($"[VoxelWorld] Step {stepsChecked}: checking voxel {currentVoxel}, material={material}, hasKey={voxelData.ContainsKey(currentVoxel)}");
            }
            
            if (material != 0) // Hit solid voxel
            {
                hit.hit = true;
                hit.voxelPosition = currentVoxel;
                hit.worldPosition = VoxelGridToWorld(currentVoxel);
                hit.materialID = material;
                hit.distance = distanceTraveled;
                
                // Calculate normal based on which face was hit
                hit.normal = new Vector3Int(
                    currentVoxel.x - lastVoxel.x,
                    currentVoxel.y - lastVoxel.y,
                    currentVoxel.z - lastVoxel.z
                );
                
                if (showDebugRays)
                {
                    Debug.DrawLine(rayOrigin, hit.worldPosition, Color.red, 0.1f);
                }
                
                return hit;
            }
            
            // Step to next voxel
            lastVoxel = currentVoxel;
            
            if (tMax.x < tMax.y)
            {
                if (tMax.x < tMax.z)
                {
                    currentVoxel.x += step.x;
                    distanceTraveled = tMax.x;
                    tMax.x += tDelta.x;
                }
                else
                {
                    currentVoxel.z += step.z;
                    distanceTraveled = tMax.z;
                    tMax.z += tDelta.z;
                }
            }
            else
            {
                if (tMax.y < tMax.z)
                {
                    currentVoxel.y += step.y;
                    distanceTraveled = tMax.y;
                    tMax.y += tDelta.y;
                }
                else
                {
                    currentVoxel.z += step.z;
                    distanceTraveled = tMax.z;
                    tMax.z += tDelta.z;
                }
            }
        }
        
        if (showDebugRays)
        {
            Debug.DrawRay(rayOrigin, rayDirection * maxDistance, debugRayColor, 0.1f);
        }
        
        return hit; // No hit
    }
    
    private float GetTMax(float origin, float direction, int step)
    {
        if (direction == 0) return Mathf.Infinity;
        
        float voxelBoundary = Mathf.Floor(origin / voxelSize) * voxelSize;
        if (step > 0) voxelBoundary += voxelSize;
        
        return (voxelBoundary - origin) / direction;
    }
    
    #endregion
    
    #region Coordinate Conversion
    
    public Vector3Int WorldToVoxelGrid(Vector3 worldPos)
    {
        return new Vector3Int(
            Mathf.FloorToInt(worldPos.x / voxelSize),
            Mathf.FloorToInt(worldPos.y / voxelSize),
            Mathf.FloorToInt(worldPos.z / voxelSize)
        );
    }
    
    public Vector3 VoxelGridToWorld(Vector3Int gridPos)
    {
        return new Vector3(
            gridPos.x * voxelSize + voxelSize * 0.5f,
            gridPos.y * voxelSize + voxelSize * 0.5f,
            gridPos.z * voxelSize + voxelSize * 0.5f
        );
    }
    
    #endregion
    
    #region Asset Loading (Placeholder)
    
    private VoxelChunk LoadVoxelAsset(string fileName)
    {
        if (loadedChunks.ContainsKey(fileName))
        {
            return loadedChunks[fileName];
        }
        
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        
        if (!File.Exists(path))
        {
            Debug.LogError($"Voxel asset not found: {path}");
            return null;
        }
        
        // Parse .stasset file (SteelTide format)
        VoxelChunk chunk = new VoxelChunk();
        
        try
        {
            byte[] fileData = File.ReadAllBytes(path);
            
            // Validate header (16 bytes total)
            if (fileData.Length < 16)
            {
                Debug.LogError($"File too small: {fileData.Length} bytes");
                return null;
            }
            
            // Check magic "STAS"
            if (fileData[0] != 0x53 || fileData[1] != 0x54 || fileData[2] != 0x41 || fileData[3] != 0x53)
            {
                Debug.LogError($"Invalid magic bytes (not a .stasset file)");
                return null;
            }
            
            // Parse dimensions (uint16 at offsets 6, 8, 10)
            int dimX = fileData[6] | (fileData[7] << 8);
            int dimY = fileData[8] | (fileData[9] << 8);
            int dimZ = fileData[10] | (fileData[11] << 8);
            
            int totalVoxels = dimX * dimY * dimZ;
            int dataOffset = 16; // After 16-byte header
            
            // Parse voxel data (ushort = 2 bytes per voxel)
            for (int i = 0; i < totalVoxels; i++)
            {
                int byteIndex = dataOffset + (i * 2);
                if (byteIndex + 1 >= fileData.Length) break;
                
                ushort materialID = (ushort)(fileData[byteIndex] | (fileData[byteIndex + 1] << 8));
                
                if (materialID != 0) // Only store solid voxels
                {
                    // Convert linear index to 3D position
                    int x = i % dimX;
                    int y = (i / dimX) % dimY;
                    int z = i / (dimX * dimY);
                    
                    chunk.voxels[new Vector3Int(x, y, z)] = (byte)(materialID & 0xFF); // Convert to byte
                }
            }
            
            Debug.Log($"Loaded {chunk.voxels.Count} solid voxels from {fileName} (dims: {dimX}×{dimY}×{dimZ})");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to parse {fileName}: {e.Message}");
            return null;
        }
        
        loadedChunks[fileName] = chunk;
        return chunk;
    }
    
    #endregion
}

/// <summary>
/// Voxel chunk data container
/// </summary>
public class VoxelChunk
{
    public Dictionary<Vector3Int, byte> voxels = new Dictionary<Vector3Int, byte>();
}

/// <summary>
/// Raycast hit result
/// </summary>
public struct VoxelHit
{
    public bool hit;
    public Vector3Int voxelPosition;
    public Vector3 worldPosition;
    public Vector3Int normal;
    public byte materialID;
    public float distance;
}
