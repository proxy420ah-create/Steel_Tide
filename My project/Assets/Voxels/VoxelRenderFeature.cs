// Steel Tide: First Device — Voxels / URP Renderer Feature
// VoxelRenderFeature.cs
//
// RenderGraph-native ScriptableRendererFeature that draws the Phase 2 proxy-box
// voxel raymarch into the camera's active color + depth targets.
//
// Why this exists:
//   The old VoxelRenderer drew via RenderPipelineManager.endCameraRendering +
//   CommandBuffer.DrawMesh. Under Unity 6 / URP 17 RenderGraph, when an
//   intermediate color texture is used (e.g. SSAO, post-processing, HDR, MSAA,
//   render scale != 1), URP does a final blit to the backbuffer AFTER the
//   endCameraRendering callback. Draws issued in that callback therefore landed
//   on a target that was discarded, so the voxels never appeared in the Game
//   view (the Scene-view camera preview uses a simpler path, so it still showed).
//
//   A RendererFeature is injected INSIDE the pipeline and binds the correct
//   active color/depth targets, so it composites correctly regardless of
//   post-processing / SSAO / intermediate textures.
//
// Setup:
//   Add this feature to your URP Renderer (e.g. PC_Renderer / Mobile_Renderer):
//   select the renderer asset -> "Add Renderer Feature" -> "Voxel Render Feature".

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace SteelTide.Voxels
{
    public class VoxelRenderFeature : ScriptableRendererFeature
    {
        [Tooltip("Where in the URP frame the voxel proxy is drawn. AfterRenderingOpaques composites with opaque scene depth.")]
        public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingOpaques;

        private VoxelRenderPass _pass;

        public override void Create()
        {
            _pass = new VoxelRenderPass
            {
                renderPassEvent = injectionPoint
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null)
                return;

            _pass.renderPassEvent = injectionPoint;
            renderer.EnqueuePass(_pass);
        }

        private class VoxelRenderPass : ScriptableRenderPass
        {
            private readonly List<VoxelRenderer.ProxyDrawItem> _items = new List<VoxelRenderer.ProxyDrawItem>();

            private class PassData
            {
                public Material material;
                public List<VoxelRenderer.ProxyDrawItem> items;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                if (cameraData == null)
                    return;

                Camera camera = cameraData.camera;
                if (camera == null)
                    return;

                // Find the VoxelRenderer that owns this camera (null for Scene-view / other cameras).
                VoxelRenderer voxelRenderer = VoxelRenderer.GetForCamera(camera);
                if (voxelRenderer == null || !voxelRenderer.ProxyPhase2Enabled)
                    return;

                Material material = voxelRenderer.ProxyMaterial;
                if (material == null)
                    return;

                // Build per-volume draw items (mesh + matrix + property block) for this frame.
                if (!voxelRenderer.BuildProxyPhase2DrawItems(_items))
                    return;

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                if (resourceData == null || !resourceData.activeColorTexture.IsValid())
                    return;

                using (var builder = renderGraph.AddRasterRenderPass<PassData>("VoxelProxyRaymarch", out PassData passData))
                {
                    passData.material = material;
                    passData.items = _items;

                    // ReadWrite: the proxy shader discards non-hit pixels, so the existing
                    // scene color must be preserved (loaded) and the voxels composited on top.
                    builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);

                    if (resourceData.activeDepthTexture.IsValid())
                    {
                        // ReadWrite so the proxy depth (SV_Depth) tests against scene depth and writes back.
                        builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.ReadWrite);
                    }

                    builder.AllowPassCulling(false);

                    builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                    {
                        List<VoxelRenderer.ProxyDrawItem> items = data.items;
                        for (int i = 0; i < items.Count; i++)
                        {
                            VoxelRenderer.ProxyDrawItem item = items[i];
                            if (item.mesh == null || item.properties == null)
                                continue;

                            ctx.cmd.DrawMesh(item.mesh, item.matrix, data.material, 0, 0, item.properties);
                        }
                    });
                }
            }
        }
    }
}
