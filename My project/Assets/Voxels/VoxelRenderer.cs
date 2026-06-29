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
        [Tooltip("Synchronized with MATERIAL_SYSTEM.md. Use the Material Sampler to verify." )]
        public Color[] materialColors = new Color[]
        {
            // Core Building Materials (0-5)
            new Color(0.0f, 0.0f, 0.0f, 0f),        // 0: Air (transparent)
            new Color(0.65f, 0.6f, 0.55f, 1f),      // 1: Prefab Composite
            new Color(0.25f, 0.2f, 0.18f, 1f),      // 2: Regolith Concrete
            new Color(0.5f, 0.5f, 0.5f, 1f),        // 3: Concrete
            new Color(1.0f, 0.6f, 0.6f, 1f),        // 4: Flesh
            new Color(0.28f, 0.3f, 0.32f, 1f),      // 5: Durasteel

            // Terrain Materials (6-10)
            new Color(0.4f, 0.3f, 0.2f, 1f),        // 6: Regolith
            new Color(0.15f, 0.55f, 0.5f, 1f),      // 7: Xenoflora
            new Color(0.25f, 0.25f, 0.27f, 1f),     // 8: Basalt
            new Color(0.6f, 0.4f, 0.2f, 1f),        // 9: Wood
            new Color(0.85f, 0.92f, 1.0f, 0.4f),    // 10: Transparent Aluminum

            // Utility / Clothing (11-12)
            new Color(0.2f, 0.3f, 0.2f, 1f),        // 11: Uniform
            new Color(0.0f, 0.0f, 0.0f, 0f),        // 12: Reserved

            // Damage States (13-15)
            new Color(0.85f, 0.15f, 0.15f, 1f),     // 13: Damaged Concrete
            new Color(0.9f, 0.4f, 0.1f, 1f),        // 14: Damaged Steel
            new Color(0.8f, 0.2f, 0.2f, 1f),        // 15: Damaged Armor

            // Advanced Materials (16-20)
            new Color(0.15f, 0.16f, 0.18f, 1f),     // 16: Ablative Plating
            new Color(0.35f, 0.38f, 0.36f, 1f),     // 17: Reactive Armor
            new Color(0.75f, 0.75f, 0.78f, 1f),     // 18: Foam-Crete
            new Color(0.2f, 0.25f, 0.3f, 1f),       // 19: Nanomesh Fabric
            new Color(0.45f, 0.48f, 0.5f, 1f),      // 20: Plasteel Panels
        };

        private static readonly Color DepthClearColor = new Color(float.MaxValue, 0f, 0f, 0f);

        private RenderTexture _output;
        private RenderTexture _depth;
        private ComputeBuffer _colorBuffer;
        private int _materialCount;
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
        
        private void ClearRenderTargets()
        {
            if (_output != null)
            {
                Graphics.SetRenderTarget(_output);
                GL.Clear(true, true, _camera.backgroundColor);
                Graphics.SetRenderTarget(null);
            }

            if (_depth != null)
            {
                Graphics.SetRenderTarget(_depth);
                GL.Clear(true, true, DepthClearColor);
                Graphics.SetRenderTarget(null);
            }
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
            if (_depth != null) _depth.Release();
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
            
            // Blit color output
            cmd.Blit(_output, camera.activeTexture);
            
            // TODO: Depth buffer integration for proper occlusion
            // The compute shader writes depth to _depth RenderTexture, but we need
            // to convert this to actual Z-buffer writes. Options:
            // 1. Use a depth-writing shader pass after raymarch
            // 2. Modify the raymarch to output depth in a format Unity can use
            // 3. Use Graphics.SetRenderTarget with depth attachment
            // For now, voxels may not properly occlude background geometry
            
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
            _materialCount = materialColors?.Length ?? 0;
            _colorBuffer = new ComputeBuffer(Mathf.Max(_materialCount, 1), sizeof(float) * 4);
            _colorBuffer.SetData(materialColors);

            if (_depth != null) _depth.Release();
            _depth = new RenderTexture(_camera.pixelWidth, _camera.pixelHeight, 0,
                                       RenderTextureFormat.RFloat, RenderTextureReadWrite.Linear);
            _depth.enableRandomWrite = true;
            _depth.Create();
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
                ClearRenderTargets();
                return;
            }
            
            // Clear output/depth to background values first
            ClearRenderTargets();
            
            // Set shared parameters once
            raymarchShader.SetTexture(_kernelIndex, "_Output", _output);
            raymarchShader.SetTexture(_kernelIndex, "_DepthBuffer", _depth);
            raymarchShader.SetBuffer(_kernelIndex, "_MaterialColors", _colorBuffer);
            raymarchShader.SetInt("_MaterialCount", _materialCount);
            raymarchShader.SetMatrix("_CameraToWorld", _camera.cameraToWorldMatrix);
            raymarchShader.SetMatrix("_WorldToCamera", _camera.worldToCameraMatrix);
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
                
                // Camera origin and per-volume offset
                Vector3 volumeOffset = vol.GetVolumeOffset();
                Vector3 camOrigin = _camera.transform.position - volumeOffset;
                raymarchShader.SetVector("_CameraOrigin", camOrigin);
                raymarchShader.SetVector("_VolumeOffset", volumeOffset);
                
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
