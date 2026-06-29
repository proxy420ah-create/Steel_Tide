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
        
        [Header("Proxy Box Raymarch")]
        [Tooltip("When enabled, renders the first registered volume with the proxy-box fragment shader to validate SV_Depth output.")]
        public bool useProxyRaymarchPhase1 = false;
        [Tooltip("Renders every registered volume via proxy-box raymarching (Phase 2). Ignores the compute path while enabled.")]
        public bool useProxyRaymarchPhase2 = false;
        [Tooltip("Shader used for the proxy-box raymarch path. Defaults to SteelTide/Voxels/VoxelProxyRaymarch if left empty.")]
        public Shader proxyRaymarchShader;
        private Material _proxyMaterial;
        private Mesh _proxyCubeMesh;
        private MaterialPropertyBlock _proxyPropertyBlock;

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

            if (proxyRaymarchShader == null)
            {
                proxyRaymarchShader = Shader.Find("SteelTide/Voxels/VoxelProxyRaymarch");
            }
            if (proxyRaymarchShader != null)
            {
                _proxyMaterial = new Material(proxyRaymarchShader);
            }
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

            if (useProxyRaymarchPhase2 && TryRenderProxyPhase2(context, camera))
            {
                return;
            }

            if (useProxyRaymarchPhase1 && TryRenderProxyPhase1(context, camera))
            {
                return;
            }

            // Ensure buffers match current resolution
            if (_output == null || _output.width != camera.pixelWidth || _output.height != camera.pixelHeight)
                InitializeBuffers();

            // Dispatch compute shader to generate raymarched output
            DispatchRaymarch();

            // Blit to camera's target using command buffer
            CommandBuffer cmd = CommandBufferPool.Get("VoxelRaymarch");
            
            // Blit color output
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

            EnsureColorBuffer();

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

        private void EnsureColorBuffer()
        {
            int desiredCount = materialColors?.Length ?? 0;
            if (desiredCount <= 0)
            {
                desiredCount = 1;
            }

            bool lengthChanged = _colorBuffer == null || _materialCount != (materialColors?.Length ?? 0);

            if (lengthChanged)
            {
                if (_colorBuffer != null)
                {
                    _colorBuffer.Release();
                }

                _materialCount = materialColors?.Length ?? 0;
                _colorBuffer = new ComputeBuffer(Mathf.Max(_materialCount, 1), sizeof(float) * 4);
                if (materialColors != null && materialColors.Length > 0)
                {
                    _colorBuffer.SetData(materialColors);
                }
                else
                {
                    Color[] fallback = new Color[] { Color.black };
                    _colorBuffer.SetData(fallback);
                }
            }
            else if (materialColors != null && materialColors.Length > 0)
            {
                _colorBuffer.SetData(materialColors);
            }
        }

        private bool TryRenderProxyPhase1(ScriptableRenderContext context, Camera camera)
        {
            if (_registeredVolumes.Count == 0)
                return false;

            if (_proxyMaterial == null)
                return false;

            VoxelObject vol = _registeredVolumes[0];
            if (vol == null || vol.GetVoxelBuffer() == null)
                return false;

            EnsureColorBuffer();
            EnsureProxyResources();
            if (_proxyCubeMesh == null)
                return false;

            if (_proxyPropertyBlock == null)
            {
                _proxyPropertyBlock = new MaterialPropertyBlock();
            }
            else
            {
                _proxyPropertyBlock.Clear();
            }

            Unity.Mathematics.int3 dims = vol.GetVolumeDims();
            Vector3 dimsVec = new Vector3(dims.x, dims.y, dims.z);
            float voxelSize = vol.GetVoxelSize();
            Vector3 volumeOrigin = vol.GetVolumeOffset();

            _proxyPropertyBlock.SetBuffer("_VoxelData", vol.GetVoxelBuffer());
            _proxyPropertyBlock.SetBuffer("_MaterialColors", _colorBuffer);
            _proxyPropertyBlock.SetInt("_MaterialCount", _materialCount);
            _proxyPropertyBlock.SetVector("_VolumeDims", new Vector4(dims.x, dims.y, dims.z, 0f));
            _proxyPropertyBlock.SetFloat("_VoxelSize", voxelSize);
            _proxyPropertyBlock.SetVector("_VolumeOrigin", volumeOrigin);
            _proxyPropertyBlock.SetInt("_MaxSteps", maxSteps);
            _proxyPropertyBlock.SetColor("_BackgroundColor", _camera.backgroundColor);

            Vector3 scale = dimsVec * voxelSize;
            Vector3 center = volumeOrigin + scale * 0.5f;
            Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, scale);

            // Use CommandBuffer to draw into the camera's render target
            CommandBuffer cmd = CommandBufferPool.Get("VoxelProxyPhase1");
            cmd.DrawMesh(
                _proxyCubeMesh,
                matrix,
                _proxyMaterial,
                0,
                0,
                _proxyPropertyBlock
            );
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            if (showDebugInfo && !_hasLoggedParams)
            {
                Debug.Log("[VoxelRenderer] Phase 1 proxy draw issued for volume " + vol.gameObject.name);
            }

            _hasLoggedParams = true;
            return true;
        }

        private bool TryRenderProxyPhase2(ScriptableRenderContext context, Camera camera)
        {
            if (_registeredVolumes.Count == 0)
                return false;

            if (_proxyMaterial == null)
                return false;

            EnsureColorBuffer();
            EnsureProxyResources();
            if (_proxyCubeMesh == null)
                return false;

            if (_proxyPropertyBlock == null)
            {
                _proxyPropertyBlock = new MaterialPropertyBlock();
            }

            CommandBuffer cmd = CommandBufferPool.Get("VoxelProxyPhase2");

            for (int i = 0; i < _registeredVolumes.Count; i++)
            {
                VoxelObject vol = _registeredVolumes[i];
                if (vol == null || vol.GetVoxelBuffer() == null)
                    continue;

                Unity.Mathematics.int3 dims = vol.GetVolumeDims();
                Vector3 dimsVec = new Vector3(dims.x, dims.y, dims.z);
                float voxelSize = vol.GetVoxelSize();
                Vector3 volumeOrigin = vol.GetVolumeOffset();

                _proxyPropertyBlock.Clear();
                _proxyPropertyBlock.SetBuffer("_VoxelData", vol.GetVoxelBuffer());
                _proxyPropertyBlock.SetBuffer("_MaterialColors", _colorBuffer);
                _proxyPropertyBlock.SetInt("_MaterialCount", _materialCount);
                _proxyPropertyBlock.SetVector("_VolumeDims", new Vector4(dims.x, dims.y, dims.z, 0f));
                _proxyPropertyBlock.SetFloat("_VoxelSize", voxelSize);
                _proxyPropertyBlock.SetVector("_VolumeOrigin", volumeOrigin);
                _proxyPropertyBlock.SetInt("_MaxSteps", maxSteps);
                _proxyPropertyBlock.SetColor("_BackgroundColor", _camera.backgroundColor);

                Vector3 scale = dimsVec * voxelSize;
                Vector3 center = volumeOrigin + scale * 0.5f;
                Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, scale);

                cmd.DrawMesh(
                    _proxyCubeMesh,
                    matrix,
                    _proxyMaterial,
                    0,
                    0,
                    _proxyPropertyBlock
                );

                if (showDebugInfo && !_hasLoggedParams)
                {
                    Debug.Log($"[VoxelRenderer] Phase 2 proxy draw issued for volume {vol.gameObject.name}");
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);

            _hasLoggedParams = true;
            return true;
        }

        private void EnsureProxyResources()
        {
            if (_proxyCubeMesh == null)
            {
                _proxyCubeMesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            }
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
