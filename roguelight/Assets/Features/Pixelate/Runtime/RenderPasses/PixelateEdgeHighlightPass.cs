using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ooze.Runtime.Pixelate.Runtime.RenderPasses {
    [Serializable]
    public class EdgeHighlightSettings {
        [Range(0, 1)]
        public float ConvexHighlight = 0.5f;

        [Range(0, 1)]
        public float OutlineShadow = 0.5f;

        [Range(0, 1)]
        public float ConcaveShadow = 1;
    }

    internal class PixelateEdgeHighlightPass : ScriptableRenderPass {
        private readonly EdgeHighlightSettings _Settings;
        private static Material _Material;
        private RTHandle _Intermediate;

        public PixelateEdgeHighlightPass(EdgeHighlightSettings settings) {
            renderPassEvent = RenderPassEvent.BeforeRenderingTransparents;
            _Settings = settings;

            const string ShaderPath = "Hidden/PixelOutlines";
            if (_Material == null) _Material = CoreUtils.CreateEngineMaterial(ShaderPath);

            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal);
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var desc = cameraTextureDescriptor;
            desc.msaaSamples = (int)MSAASamples.None;
            desc.depthBufferBits = 0;
            desc.depthStencilFormat = GraphicsFormat.R8G8B8A8_SRGB;
            RenderingUtils.ReAllocateHandleIfNeeded(ref _Intermediate, desc, name: "_PixelateEdgeHighlightIntermediate");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_Material) return;
            ref var cameraData = ref renderingData.cameraData;

            var cmd = CommandBufferPool.Get();
            var colorTarget = cameraData.renderer.cameraColorTargetHandle;

            using (new ProfilingScope(cmd, new ProfilingSampler(nameof(PixelateEdgeHighlightPass))))
            {
                _Material.SetFloat("_ConvexHighlight", _Settings.ConvexHighlight);
                _Material.SetFloat("_OutlineShadow", _Settings.OutlineShadow);
                _Material.SetFloat("_ConcaveShadow", _Settings.ConcaveShadow);

                cmd.Blit(colorTarget, _Intermediate, _Material, 0);
                cmd.Blit(_Intermediate, colorTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose() {
            _Intermediate?.Release();
        }
    }
}
