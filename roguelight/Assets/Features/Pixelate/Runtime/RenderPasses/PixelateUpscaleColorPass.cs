using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ooze.Runtime.Pixelate.Runtime.RenderPasses {
    internal class PixelateUpscaleColorPass : ScriptableRenderPass
    {
        private static RTHandle _UpscaledRT;
        private static RTHandle _OverlayUIRT;
        private static RTHandle _OverlayCursorRT;

        private static Material _Material;

        private PixelateCamera _Camera;
        private RenderTexture _OverlayUISourceRT;
        private RenderTexture _OverlayCursorSourceRT;

        public static ref RTHandle UpscaledRT => ref _UpscaledRT;

        public PixelateUpscaleColorPass(ref RenderTexture uiOverlay, ref RenderTexture cursorTexture)
        {
            renderPassEvent = RenderPassEvent.AfterRendering - 1;

            _OverlayUISourceRT = uiOverlay;
            _OverlayCursorSourceRT = cursorTexture;

            const string ShaderPath = "Hidden/Pixelate/Smooth-Pixel Upscale";
            if (_Material == null) _Material = CoreUtils.CreateEngineMaterial(ShaderPath);

            ConfigureInput(ScriptableRenderPassInput.Color | ScriptableRenderPassInput.Depth);
        }

        public void Setup(PixelateCamera Camera)
        {
            _Camera = Camera;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            var desc = cameraTextureDescriptor;
            desc.width = (int)_Camera.DisplayViewport.width;
            desc.height = (int)_Camera.DisplayViewport.height;
            desc.enableRandomWrite = true;
            desc.msaaSamples = (int)MSAASamples.None;
            desc.depthBufferBits = 0;
            desc.depthStencilFormat = GraphicsFormat.None;

            RenderingUtils.ReAllocateIfNeeded(
                ref _UpscaledRT,
                desc,
                wrapMode: TextureWrapMode.Clamp,
                filterMode: FilterMode.Point,
                anisoLevel: 16,
                name: "_PixelateUpscaledRT"
            );

            RenderingUtils.ReAllocateIfNeeded(
                ref _OverlayUIRT,
                desc,
                wrapMode: TextureWrapMode.Clamp,
                filterMode: FilterMode.Point,
                anisoLevel: 16,
                name: "_PixelateOverlayUIRT"
            );

            RenderingUtils.ReAllocateIfNeeded(
                ref _OverlayCursorRT,
                desc,
                wrapMode: TextureWrapMode.Clamp,
                filterMode: FilterMode.Point,
                anisoLevel: 16,
                name: "_PixelateOverlayCursorRT"
            );
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_Material) return;
            ref var cameraData = ref renderingData.cameraData;

            var cmd = CommandBufferPool.Get();
            var colorTarget = cameraData.renderer.cameraColorTargetHandle;
            cameraData.resolveFinalTarget = false;

            using (new ProfilingScope(cmd, new ProfilingSampler(nameof(PixelateUpscaleColorPass))))
            {
                cmd.Blit(_OverlayUISourceRT, _OverlayUIRT);
                cmd.Blit(_OverlayCursorSourceRT, _OverlayCursorRT);

                _Material.SetTexture("_UITex", _OverlayUISourceRT);
                _Material.SetTexture("_CursorTex", _OverlayCursorRT);
                cmd.Blit(colorTarget, _UpscaledRT, _Material, 0);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _UpscaledRT?.Release();
            _OverlayUIRT?.Release();
            _OverlayCursorRT?.Release();
        }
    }
}
