// Steel Tide: First Device — Voxels / GPU Renderer
// VoxelRenderer.cs
//
// Dispatches the VoxelRaymarch.compute shader each frame to render a voxel volume
// via GPU raymarching. Outputs to a RenderTexture displayed on the main camera.
//
// Attach this to a GameObject with a Camera component. The voxel data must be
// uploaded to a ComputeBuffer (StructuredBuffer<uint>) by PrototypeBootstrap before rendering starts.
//
// Unity 6 + URP compatible: uses RenderPipelineManager.endCameraRendering instead
// of the deprecated OnRenderImage callback.

using UnityEngine;
using UnityEngine.Rendering;

namespace SteelTide.Voxels
{
    [RequireComponent(typeof(Camera))]
    public class VoxelRenderer : MonoBehaviour
    {
        [Header("Compute Shader")]
        public ComputeShader raymarchShader;
        public ComputeBuffer voxelBuffer;  // set by PrototypeBootstrap at runtime
        public Unity.Mathematics.int3 volumeDims;  // voxel grid dimensions

        [Header("Render Settings")]
        public int maxSteps = 256;
        public float voxelSize = 0.5f;
        public Vector3 volumeOffset = Vector3.zero;  // world-space origin of the voxel grid
        
        [Header("Editor Gizmo (for Scene view preview)")]
        public bool showVolumeGizmo = true;
        public Unity.Mathematics.int3 editorVolumeDims = new Unity.Mathematics.int3(8, 8, 8);  // Preview size in editor

        [Header("Material Colors (index by material ID)")]
        public Color[] materialColors = new Color[]
        {
            new Color(0.02f, 0.03f, 0.08f, 0f),   // 0: Air (transparent)
            new Color(0f, 1f, 0.97f, 1f),         // 1: Energy_Shield (cyan)
            new Color(0.5f, 0.5f, 0.5f, 1f),      // 2: Chobham_Armor (gray)
            new Color(0.6f, 0.6f, 0.55f, 1f),     // 3: Concrete (tan/gray)
            new Color(1f, 0.27f, 0.27f, 1f),      // 4: Flesh (red)
            new Color(0.7f, 0.7f, 0.8f, 1f),      // 5: Steel (blue-gray)
            new Color(0f, 0f, 0f, 0f),            // 6-12: Reserved
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 0f),
            new Color(0f, 0f, 0f, 0f),
            new Color(0.85f, 0.15f, 0.15f, 1f),   // 13: Damaged_Concrete (crimson red)
            new Color(0.9f, 0.4f, 0.1f, 1f),      // 14: Damaged_Steel (hot orange)
            new Color(0.8f, 0.2f, 0.2f, 1f),      // 15: Damaged_Armor (dark red)
        };

        private RenderTexture _output;
        private ComputeBuffer _colorBuffer;
        private Camera _camera;
        private int _kernelIndex;
        private bool _hasLoggedParams = false;

        void Start()
        {
            _camera = GetComponent<Camera>();
            if (raymarchShader == null)
            {
                Debug.LogError("[VoxelRenderer] No compute shader assigned!");
                enabled = false;
                return;
            }

            _kernelIndex = raymarchShader.FindKernel("CSRaymarch");
        }
        
        void LateUpdate()
        {
            // Pull buffer reference from Bootstrap if not yet assigned
            if (voxelBuffer == null)
            {
                Debug.Log("[VoxelRenderer] voxelBuffer is null, attempting to pull from Bootstrap...");
                var bootstrap = FindFirstObjectByType<SteelTide.Prototype.PrototypeBootstrap>();
                if (bootstrap == null)
                {
                    Debug.LogWarning("[VoxelRenderer] Could not find PrototypeBootstrap!");
                    return;
                }
                
                if (bootstrap.GeneratedVoxelBuffer == null)
                {
                    Debug.LogWarning("[VoxelRenderer] Bootstrap.GeneratedVoxelBuffer is null!");
                    return;
                }
                
                voxelBuffer = bootstrap.GeneratedVoxelBuffer;
                Debug.Log($"[VoxelRenderer] Successfully pulled ComputeBuffer reference from Bootstrap");
            }
        }

        void OnEnable()
        {
            // Subscribe to Unity 6 URP rendering event
            RenderPipelineManager.endCameraRendering += OnEndCameraRendering;
        }

        void OnDisable()
        {
            // Unsubscribe to prevent memory leaks
            RenderPipelineManager.endCameraRendering -= OnEndCameraRendering;
        }

        void OnDestroy()
        {
            if (_output != null) _output.Release();
            if (_colorBuffer != null) _colorBuffer.Release();
        }

        // Unity 6 URP compatible rendering callback
        void OnEndCameraRendering(ScriptableRenderContext context, Camera camera)
        {
            // Only render for this camera
            if (camera != _camera)
                return;
                
            if (voxelBuffer == null)
            {
                Debug.LogWarning("[VoxelRenderer] OnEndCameraRendering: voxelBuffer is NULL, skipping render!");
                return;
            }
            
            // Confirm buffer available (log once)
            if (!_hasLoggedParams)
            {
                Debug.Log($"[VoxelRenderer] Rendering frame with buffer: {volumeDims.x}x{volumeDims.y}x{volumeDims.z} voxels");
            }

            // Ensure buffers match current resolution
            if (_output == null || _output.width != camera.pixelWidth || _output.height != camera.pixelHeight)
                InitializeBuffers();

            // Dispatch compute shader to generate raymarched output
            DispatchRaymarch();

            // Blit to camera's target using command buffer
            CommandBuffer cmd = CommandBufferPool.Get("VoxelRaymarch");
            cmd.Blit(_output, camera.activeTexture);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private void InitializeBuffers()
        {
            if (_output != null) _output.Release();
            _output = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0,
                                        RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);
            _output.enableRandomWrite = true;
            _output.Create();

            if (_colorBuffer != null) _colorBuffer.Release();
            _colorBuffer = new ComputeBuffer(materialColors.Length, sizeof(float) * 4);
            _colorBuffer.SetData(materialColors);
        }

        private void DispatchRaymarch()
        {
            raymarchShader.SetBuffer(_kernelIndex, "_VoxelData", voxelBuffer);
            raymarchShader.SetTexture(_kernelIndex, "_Output", _output);
            raymarchShader.SetBuffer(_kernelIndex, "_MaterialColors", _colorBuffer);

            // Pass explicit integer dimensions to ensure GPU knows exact grid bounds
            raymarchShader.SetInts("_VolumeDims", volumeDims.x, volumeDims.y, volumeDims.z);
            raymarchShader.SetFloat("_VoxelSize", voxelSize);
            
            Vector3 camOrigin = _camera.transform.position - volumeOffset;
            raymarchShader.SetVector("_CameraOrigin", camOrigin);
            
            // DIAGNOSTIC: Log shader parameters (once only)
            if (!_hasLoggedParams)
            {
                Vector3 volumeMax = new Vector3(volumeDims.x, volumeDims.y, volumeDims.z) * voxelSize;
                Debug.Log($"[VoxelRenderer] SHADER PARAMS:");
                Debug.Log($"  VolumeDims: {volumeDims.x}x{volumeDims.y}x{volumeDims.z}");
                Debug.Log($"  VoxelSize: {voxelSize}");
                Debug.Log($"  VolumeWorldBounds: (0,0,0) to ({volumeMax.x},{volumeMax.y},{volumeMax.z})");
                Debug.Log($"  CameraPos: {_camera.transform.position}");
                Debug.Log($"  VolumeOffset: {volumeOffset}");
                Debug.Log($"  CameraOrigin (shader): {camOrigin}");
                Debug.Log($"  Material Colors Count: {materialColors.Length}");
                Debug.Log($"  Expected cube center: ({volumeMax.x/2},{volumeMax.y/2},{volumeMax.z/2})");
                _hasLoggedParams = true;
            }
            
            // Pass separate camera matrices for robust ray reconstruction
            raymarchShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            raymarchShader.SetMatrix("_InvProjection", _camera.projectionMatrix.inverse);
            
            raymarchShader.SetVector("_ScreenSize", new Vector2(_output.width, _output.height));
            raymarchShader.SetInt("_MaxSteps", maxSteps);
            
            // Pass camera background color as fallback for missed rays
            raymarchShader.SetVector("_BackgroundColor", _camera.backgroundColor);

            int threadGroupsX = Mathf.CeilToInt(_output.width / 8f);
            int threadGroupsY = Mathf.CeilToInt(_output.height / 8f);
            raymarchShader.Dispatch(_kernelIndex, threadGroupsX, threadGroupsY, 1);
        }

        // Draw volume bounds in Scene view for easier editing
        void OnDrawGizmos()
        {
            if (!showVolumeGizmo) return;
            
            // Use runtime dims if available, otherwise use editor preview dims
            Unity.Mathematics.int3 dims = (volumeDims.x > 0) ? volumeDims : editorVolumeDims;
            
            if (dims.x > 0 && dims.y > 0 && dims.z > 0)
            {
                Vector3 size = new Vector3(dims.x, dims.y, dims.z) * voxelSize;
                Vector3 center = volumeOffset + size / 2;
                
                // Draw wireframe cube showing voxel volume bounds
                Gizmos.color = new Color(0, 1, 1, 0.5f); // Cyan
                Gizmos.DrawWireCube(center, size);
                
                // Draw corner markers
                Gizmos.color = Color.cyan;
                float markerSize = voxelSize * 0.5f;
                Gizmos.DrawWireSphere(volumeOffset, markerSize);
                Gizmos.DrawWireSphere(volumeOffset + size, markerSize);
            }
        }
    }
}
