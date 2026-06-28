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
using System.Collections.Generic;

namespace SteelTide.Voxels
{
    [RequireComponent(typeof(Camera))]
    public class VoxelRenderer : MonoBehaviour
    {
        [Header("Compute Shader")]
        public ComputeShader raymarchShader;
        
        [Header("Render Settings")]
        public int maxSteps = 256;
        
        [Header("Multi-Volume System")]
        private List<VoxelObject> _registeredVolumes = new List<VoxelObject>();
        
        [Header("Debug")]
        public bool showDebugInfo = true;

        [Header("Material Colors (index by material ID)")]
        public Color[] materialColors = new Color[]
        {
            // Primary Materials (0-5)
            new Color(0.0f, 0.0f, 0.0f, 0f),      // 0: Air (transparent)
            new Color(0f, 1f, 0.97f, 1f),         // 1: Energy Shield (cyan)
            new Color(0.6f, 0.4f, 0.2f, 1f),      // 2: Chobham Armor (brown)
            new Color(0.5f, 0.5f, 0.5f, 1f),      // 3: Concrete (gray)
            new Color(1f, 0.6f, 0.6f, 1f),        // 4: Flesh (pink)
            new Color(0.3f, 0.3f, 0.3f, 1f),      // 5: Steel (dark gray)
            
            // Terrain Materials (6-10)
            new Color(0.4f, 0.3f, 0.2f, 1f),      // 6: Dirt (brown)
            new Color(0.2f, 0.6f, 0.2f, 1f),      // 7: Grass (green)
            new Color(0.4f, 0.4f, 0.4f, 1f),      // 8: Stone (gray)
            new Color(0.6f, 0.4f, 0.2f, 1f),      // 9: Wood (tan)
            new Color(0.78f, 0.9f, 1.0f, 0.5f),   // 10: Glass (light blue, semi-transparent)
            
            // Clothing/Organic (11-12)
            new Color(0.2f, 0.3f, 0.2f, 1f),      // 11: Uniform (dark green)
            new Color(0f, 0f, 0f, 0f),            // 12: Reserved
            
            // Damaged States (13-15)
            new Color(0.85f, 0.15f, 0.15f, 1f),   // 13: Damaged Concrete (crimson)
            new Color(0.9f, 0.4f, 0.1f, 1f),      // 14: Damaged Steel (orange)
            new Color(0.8f, 0.2f, 0.2f, 1f),      // 15: Damaged Armor (dark red)
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
        
        // Bootstrap removed - VoxelObjects register themselves automatically

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
            // Always use multi-volume system (Bootstrap removed)
            DispatchMultiVolume();
        }
        
        private void DispatchMultiVolume()
        {
            // Log registered volumes count (first frame only)
            if (!_hasLoggedParams)
            {
                Debug.Log($"[VoxelRenderer] Multi-volume dispatch: {_registeredVolumes.Count} volumes registered");
            }
            
            if (_registeredVolumes.Count == 0)
            {
                // No volumes to render - just clear to background
                Graphics.SetRenderTarget(_output);
                GL.Clear(true, true, _camera.backgroundColor);
                Graphics.SetRenderTarget(null);
                return;
            }
            
            // Clear output to background color first
            Graphics.SetRenderTarget(_output);
            GL.Clear(true, true, _camera.backgroundColor);
            Graphics.SetRenderTarget(null);
            
            // Set shared parameters once
            raymarchShader.SetTexture(_kernelIndex, "_Output", _output);
            raymarchShader.SetBuffer(_kernelIndex, "_MaterialColors", _colorBuffer);
            raymarchShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            raymarchShader.SetMatrix("_InvProjection", _camera.projectionMatrix.inverse);
            raymarchShader.SetVector("_ScreenSize", new Vector2(_output.width, _output.height));
            raymarchShader.SetInt("_MaxSteps", maxSteps);
            raymarchShader.SetVector("_BackgroundColor", _camera.backgroundColor);
            
            int threadGroupsX = Mathf.CeilToInt(_output.width / 8f);
            int threadGroupsY = Mathf.CeilToInt(_output.height / 8f);
            
            // Sort volumes back-to-front for proper depth ordering
            _registeredVolumes.Sort((a, b) => {
                float distA = Vector3.Distance(a.transform.position, _camera.transform.position);
                float distB = Vector3.Distance(b.transform.position, _camera.transform.position);
                return distB.CompareTo(distA); // Render farthest first
            });
            
            // Render each volume in sequence
            // First volume clears to background, subsequent volumes composite on top
            for (int i = 0; i < _registeredVolumes.Count; i++)
            {
                VoxelObject vol = _registeredVolumes[i];
                if (vol == null || vol.GetVoxelBuffer() == null)
                    continue;
                
                // Set per-volume parameters
                raymarchShader.SetBuffer(_kernelIndex, "_VoxelData", vol.GetVoxelBuffer());
                Unity.Mathematics.int3 dims = vol.GetVolumeDims();
                raymarchShader.SetInts("_VolumeDims", dims.x, dims.y, dims.z);
                raymarchShader.SetFloat("_VoxelSize", vol.GetVoxelSize());
                
                // Camera origin relative to THIS volume's offset
                Vector3 volumeOffset = vol.GetVolumeOffset();
                Vector3 camOrigin = _camera.transform.position - volumeOffset;
                raymarchShader.SetVector("_CameraOrigin", camOrigin);
                
                // Dispatch
                raymarchShader.Dispatch(_kernelIndex, threadGroupsX, threadGroupsY, 1);
                
                // Log first frame
                if (!_hasLoggedParams)
                {
                    Debug.Log($"[VoxelRenderer] Multi-volume render #{i}: {vol.gameObject.name} at {volumeOffset}");
                }
            }
            
            _hasLoggedParams = true;
        }

        // Gizmos now drawn by individual VoxelObjects
        
        // ===== MULTI-VOLUME SYSTEM =====
        
        /// <summary>
        /// Register a VoxelObject to be rendered by this renderer.
        /// Called automatically by VoxelObject.Start()
        /// </summary>
        public void RegisterVolume(VoxelObject voxelObject)
        {
            if (voxelObject == null)
            {
                Debug.LogWarning("[VoxelRenderer] Attempted to register null VoxelObject!");
                return;
            }
            
            if (!_registeredVolumes.Contains(voxelObject))
            {
                _registeredVolumes.Add(voxelObject);
                Debug.Log($"[VoxelRenderer] ✅ Registered volume: {voxelObject.gameObject.name} (Total: {_registeredVolumes.Count})");
            }
            else
            {
                Debug.LogWarning($"[VoxelRenderer] Volume already registered: {voxelObject.gameObject.name}");
            }
        }
        
        /// <summary>
        /// Unregister a VoxelObject (called when destroyed)
        /// </summary>
        public void UnregisterVolume(VoxelObject voxelObject)
        {
            if (_registeredVolumes.Remove(voxelObject))
            {
                Debug.Log($"[VoxelRenderer] Unregistered volume: {voxelObject.gameObject.name} (Remaining: {_registeredVolumes.Count})");
            }
        }
        
        /// <summary>
        /// Get all registered voxel volumes
        /// </summary>
        public List<VoxelObject> GetRegisteredVolumes()
        {
            return _registeredVolumes;
        }
    }
}
