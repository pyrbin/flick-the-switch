using Ooze.Runtime.Pixelate.Runtime.RenderPasses;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ooze.Runtime.Pixelate.Runtime {

[DisallowMultipleRendererFeature(nameof(PixelateRenderFeature))]
[Tooltip("Pixelate Render Feature")]
public class PixelateRenderFeature : ScriptableRendererFeature {
    internal const int MIN_PIXELS_PER_UNIT = 1;
    internal const int MAX_PIXELS_PER_UNIT = 100;

    [Range(MIN_PIXELS_PER_UNIT, MAX_PIXELS_PER_UNIT)]
    [Tooltip("Density of pixels per world unit")]
    public int PixelsPerUnit = 10;

    // #FB_NOTE: Super bad hack to get UI working as we want it
    public RenderTexture OverlayCursorTexture;
    public RenderTexture OverlayUITexture;

    [Header("Snapping")]
    public bool EnablePixelSnapping = true;

    [Header("Camera")]
    public bool CameraPixelSnapping = true;
    public bool CameraSubPixelSmoothing = true;

    [Space(10)]

    [SerializeField]
    private DitheringSettings _DitheringSettings;

    [SerializeField]
    private EdgeHighlightSettings _EdgeHighlightSettings;

    public PixelateCamera? Camera { get; private set; }

    public float UnitsPerPixel { get; private set; } = 0f;
    public float ScaleFactor { get; private set; } = 1.0f;
    public bool HasCamera => Camera != null && Camera.isActiveAndEnabled;
    private PixelateDitheringPass? _PixelateDitheringPass;
    private PixelateEdgeHighlightPass? _PixelateEdgeHighlightPass;
    private PixelateUpscaleColorPass? _PixelateUpscaleColorPass;
    private PixelateFinalRenderPass? _PixelateFinalRenderPass;
    private UniversalRenderPipelineAsset? _UniversalRenderPipelineAsset;

#if UNITY_EDITOR
    [HideInInspector]
    public bool Inspector_ScaleFactorClamped = false;
#endif

    protected override void Dispose(bool disposing) {
        if (!disposing) return;
        _PixelateEdgeHighlightPass?.Dispose();
        _PixelateUpscaleColorPass?.Dispose();
        _PixelateFinalRenderPass?.Dispose();
        _PixelateDitheringPass?.Dispose();
    }

    public override void Create()
    {
        name = "Pixelate Render Feature";

        if (Camera == null)
        {
            Camera = FindCameraReference().OrDefault();
        }

        CreateOverlayTexture();

        _PixelateDitheringPass = new PixelateDitheringPass(_DitheringSettings);
        _PixelateEdgeHighlightPass = new PixelateEdgeHighlightPass(_EdgeHighlightSettings);
        _PixelateUpscaleColorPass = new PixelateUpscaleColorPass(ref OverlayUITexture, ref OverlayCursorTexture);
        _PixelateFinalRenderPass = new PixelateFinalRenderPass();

        _UniversalRenderPipelineAsset = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;

        PixelsPerUnit = math.max(PixelsPerUnit, MIN_PIXELS_PER_UNIT);
        UnitsPerPixel = 1.0f / PixelsPerUnit;

        if (HasCamera) {
            UpdateScaleFactor();
            Camera?.UpdateRenderViewport();
            Camera?.DetectDirtyChanges();
            UpdateOverlayTextures();
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        if (!HasCamera || renderingData.cameraData.camera.gameObject != Camera.gameObject) return;
#if UNITY_EDITOR
        if(renderingData.cameraData.isPreviewCamera || renderingData.cameraData.isSceneViewCamera) return;
#endif
        _UniversalRenderPipelineAsset.upscalingFilter = UpscalingFilterSelection.Linear;

        // renderer.EnqueuePass(_PixelateDitheringPass);
        // renderer.EnqueuePass(_PixelateEdgeHighlightPass);
        renderer.EnqueuePass(_PixelateUpscaleColorPass);
        renderer.EnqueuePass(_PixelateFinalRenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData) {
        if (!HasCamera) {
            Debug.LogWarning($"No {nameof(PixelateCamera)} found in scene. {nameof(PixelateRenderFeature)} will not function properly.");
            return;
        }

        if (Camera.IsDirty) {
            UpdateScaleFactor();
            Camera.UpdateRenderViewport();
            Camera.SetIsDirty(false);
            UpdateOverlayTextures();
        }

        _PixelateUpscaleColorPass.Setup(Camera);
        _PixelateFinalRenderPass.Setup(Camera);
    }

    public void SetCameraReference(PixelateCamera camera) {
        Camera = camera;
        if (HasCamera) {
            UpdateScaleFactor();
            Camera.UpdateRenderViewport();
            UpdateOverlayTextures();
        }
    }

    private Option<PixelateCamera> FindCameraReference() {
        var cameras = FindObjectsByType<PixelateCamera>(FindObjectsSortMode.None);

        if (cameras.Length > 0) {
            return cameras[0];
        }

        return Option<PixelateCamera>.None;
    }

    private void UpdateScaleFactor() {
        var pixelScale = 2 * PixelsPerUnit * Camera.OrthographicSize;
        var scaleFactorX = math.max(1, Camera.DisplayViewport.width / pixelScale);
        var scaleFactorY = math.max(1, Camera.DisplayViewport.height / pixelScale);
        var scaleFactor = math.min(scaleFactorX, scaleFactorY);

        const int MIN_SCALE_FACTOR = 1;
#if UNITY_EDITOR
        if (scaleFactor <= MIN_SCALE_FACTOR) {
            Inspector_ScaleFactorClamped = true;
        } else {
            Inspector_ScaleFactorClamped = false;
        }
#endif
        ScaleFactor = math.max(MIN_SCALE_FACTOR, scaleFactor);
    }

    // puke:
    private void CreateOverlayTexture()
    {
        if (OverlayUITexture is not null)
        {
            OverlayUITexture.Release();
            OverlayUITexture.width = Mathf.Clamp(((int?)Camera?.DisplayViewport.width) ?? Screen.width, 10, 16384);
            OverlayUITexture.height = Mathf.Clamp(((int?)Camera?.DisplayViewport.height) ?? Screen.height, 10, 16384);
            OverlayUITexture.depth = 32;
            OverlayUITexture.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
            OverlayUITexture.Create();

            RenderTexture rt = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = OverlayUITexture;
            GL.Clear(false, true, Color.clear);
            UnityEngine.RenderTexture.active = rt;
        }

        if (OverlayCursorTexture is not null)
        {
            OverlayCursorTexture.Release();
            OverlayCursorTexture.width = Mathf.Clamp((int?)Camera?.RenderViewport.width ?? 480, 10, 16384);
            OverlayCursorTexture.height = Mathf.Clamp((int?)Camera?.RenderViewport.height ?? 270, 10, 16384);
            OverlayCursorTexture.depth = 32;
            OverlayCursorTexture.graphicsFormat = GraphicsFormat.R32G32B32A32_SFloat;
            OverlayCursorTexture.Create();

            RenderTexture rt = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = OverlayCursorTexture;
            GL.Clear(false, true, Color.clear);
            UnityEngine.RenderTexture.active = rt;
        }
    }

    private void UpdateOverlayTextures()
    {
        if (OverlayUITexture is not null)
        {
            OverlayUITexture.Release();
            OverlayUITexture.width = Mathf.Clamp(((int?)Camera?.DisplayViewport.width) ?? Screen.width, 10, 16384);
            OverlayUITexture.height = Mathf.Clamp(((int?)Camera?.DisplayViewport.height) ?? Screen.height, 10, 16384);
            OverlayUITexture.Create();
        }

        if (OverlayCursorTexture is not null)
        {
            OverlayCursorTexture.Release();
            OverlayCursorTexture.width = Mathf.Clamp((int?)Camera?.RenderViewport.width ?? 480, 10, 16384);
            OverlayCursorTexture.height = Mathf.Clamp((int?)Camera?.RenderViewport.height ?? 270, 10, 16384);
            OverlayCursorTexture.Create();
        }
    }
}

}
