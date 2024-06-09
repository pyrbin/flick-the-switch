using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ooze.Runtime.Pixelate.Runtime.RenderPasses
{
    internal class PixelateFinalRenderPass : ScriptableRenderPass
    {
        private PixelateCamera _Camera;

        public PixelateFinalRenderPass()
        {
            const int PassOrderOffset = 10;
            renderPassEvent = RenderPassEvent.AfterRendering + PassOrderOffset;
        }

        public void Setup(PixelateCamera Camera)
        {
            _Camera = Camera;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

            var cmd = CommandBufferPool.Get();
            var colorTarget = cameraData.renderer.cameraColorTargetHandle;

            if (PixelateUpscaleColorPass.UpscaledRT == null || PixelateUpscaleColorPass.UpscaledRT.rt == null) return;

            using (new ProfilingScope(cmd, new ProfilingSampler(nameof(PixelateFinalRenderPass))))
            {
                var scaleBias = new Vector4(1, -1f, 0f, 1f);

                if (_Camera.RenderFeature != null && _Camera.RenderFeature.CameraSubPixelSmoothing)
                {
                    scaleBias.z += _Camera.PixelSnapDisplacement.x;
                    scaleBias.w += _Camera.PixelSnapDisplacement.y;
                }

                Blitter.BlitCameraTexture(cmd, PixelateUpscaleColorPass.UpscaledRT, colorTarget, scaleBias, 0, bilinear: true);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose() {}
    }
}
