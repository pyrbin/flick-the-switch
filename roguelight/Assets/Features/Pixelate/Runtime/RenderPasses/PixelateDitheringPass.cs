using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ooze.Runtime.Pixelate.Runtime.RenderPasses {
    [Serializable]
    public class DitheringSettings {
        [Range(0, 100)]
        public float DitherThreshold = 0.5f;

        [Range(0, 10)]
        public float DitherStrength = 0.5f;

        [Range(0, 5)]
        public float DitherScale = 1;
    }

    internal class PixelateDitheringPass : ScriptableRenderPass {
        private static Material _Material;

        private readonly DitheringSettings _Settings;
        private RTHandle _Intermediate;

        public PixelateDitheringPass(DitheringSettings settings) {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            _Settings = settings;

            const string ShaderPath = "Hidden/Pixelate/Dithering";
            if (_Material == null) _Material = CoreUtils.CreateEngineMaterial(ShaderPath);

            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Normal);
        }

        public void Setup(PixelateCamera Camera) { }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor) {
            var desc = cameraTextureDescriptor;
            desc.msaaSamples = (int)MSAASamples.None;
            desc.depthBufferBits = 0;
            desc.depthStencilFormat = GraphicsFormat.R8G8B8A8_SRGB;
            RenderingUtils.ReAllocateIfNeeded(ref _Intermediate, desc, name: "_PixelateDitheringIntermediate");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_Material) return;
            ref var cameraData = ref renderingData.cameraData;

            var cmd = CommandBufferPool.Get();
            var colorTarget = cameraData.renderer.cameraColorTargetHandle;

            using (new ProfilingScope(cmd, new ProfilingSampler(nameof(PixelateDitheringPass))))
            {
               _Material.SetFloat("_DitherThreshold", _Settings.DitherThreshold);
               _Material.SetFloat("_DitherStrength", _Settings.DitherStrength);
               _Material.SetFloat("_DitherScale", _Settings.DitherScale);

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
