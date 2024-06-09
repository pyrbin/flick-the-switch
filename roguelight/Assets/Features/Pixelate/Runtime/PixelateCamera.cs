using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ooze.Runtime.Pixelate.Runtime {

[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[DisallowMultipleComponent]
public class PixelateCamera : MonoBehaviour {
    [NaughtyAttributes.ShowNativeProperty]
    public float OrthographicSize => _Camera.orthographicSize;

    [NaughtyAttributes.ShowNativeProperty]
    public float ScaleFactor => _PixelateRenderFeature == null ? 1 : _PixelateRenderFeature.ScaleFactor;

    [SerializeField, NaughtyAttributes.ReadOnly]
    private Rect _RenderViewport, _DisplayViewport = new(0, 0, 0, 0);

    // Total displacement of the camera in percentage of the render size when snapped to pixel grid-space.
    // This is updated if sub-pixel camera smoothing is enabled.
    [NaughtyAttributes.ShowNativeProperty]
    public Vector2 PixelSnapDisplacement { get; private set; }

    private Camera _Camera;

    public Rect RenderViewport => _RenderViewport;
    public Rect DisplayViewport => _DisplayViewport;

    private PixelateRenderFeature _PixelateRenderFeature;

    private DynamicTransformAccessArray _SnappableObjects;
    private JobHandle _SnapToPixelJobHandle = default;
    private JobHandle _UnsnapFromPixelJobHandle = default;

    private RenderingPath _PreviousRenderingPath;
    private float _PreviousOrthographicSize;
    private float3 _PreviousPosition;
    private quaternion _PreviousRotation;

    public bool IsDirty { get; private set; } = true;

    internal PixelateRenderFeature RenderFeature => _PixelateRenderFeature;

    internal bool SetIsDirty(bool value) => IsDirty = value;

    public int RegisterSnappable(Transform transform)
    {
        if (!_SnappableObjects.IsCreated) return -1;
        var index = _SnappableObjects.Register(transform);
        return index;
    }

    public void DeregisterSnappable(int index)
    {
        if (!_SnappableObjects.IsCreated) return;
        _SnappableObjects.Deregister(index);
    }

    private void OnEnable() {
        _Camera = GetComponent<Camera>();
        RenderPipelineManager.beginCameraRendering += PreRenderCamera;
        RenderPipelineManager.endCameraRendering += PostRenderCamera;
        IsDirty = true;

        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        var feature = urp.GetRenderFeature<PixelateRenderFeature>();

        if (feature.IsSome())
        {
            _PixelateRenderFeature = feature.OrDefault();
            _PixelateRenderFeature.SetCameraReference(this);
            DetectDirtyChanges();
        } else {
            Debug.LogError($"An active {nameof(PixelateRenderFeature)} is not found in URP asset");
        }
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= PreRenderCamera;
        RenderPipelineManager.endCameraRendering -= PostRenderCamera;

        if (_PixelateRenderFeature != null && _PixelateRenderFeature.Camera == this)
            _PixelateRenderFeature.SetCameraReference(null);

        IsDirty = true;
        _Camera.rect = new Rect(0, 0, 1, 1);
        PixelSnapDisplacement = float2.zero;
    }

    private void Awake()
    {
        if (!Application.isPlaying) return;

        const int DEFAULT_CAPACITY = 3_000;
        _SnappableObjects = new DynamicTransformAccessArray(DEFAULT_CAPACITY, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        if (_SnappableObjects.IsCreated)
            _SnappableObjects.Dispose();
    }

    private void PreRenderCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != _Camera || !enabled) return;
        if (_PixelateRenderFeature == null || !_PixelateRenderFeature.isActive) {
            Debug.LogError($"An active {nameof(PixelateRenderFeature)} is not found in URP asset");
            _Camera.rect = new Rect(0, 0, 1, 1);
            IsDirty = true;
            return;
        }

        DetectDirtyChanges();

        _PreviousPosition = _Camera.transform.position;
        _PreviousRotation = _Camera.transform.rotation;
        _Camera.pixelRect = _RenderViewport;

        if (_PixelateRenderFeature.CameraPixelSnapping) {
            PixelateSnappingUtils.PixelSnap(_Camera.transform.rotation, _PixelateRenderFeature.UnitsPerPixel, _Camera.transform, out var displacement);
            if (_PixelateRenderFeature.CameraSubPixelSmoothing) {
                // TODO: if we should use z-axis or y-axis for vertical smoothing I'm not sure ... probably depends on the camera's orientation
                var displacementInPixels = displacement.xy * _PixelateRenderFeature.PixelsPerUnit;
                PixelSnapDisplacement = new float2(displacementInPixels.x / _RenderViewport.width, displacementInPixels.y / _RenderViewport.height);
            }
        }

        if (_PixelateRenderFeature.EnablePixelSnapping && _SnappableObjects.IsCreated)
        {
            _SnapToPixelJobHandle = _SnappableObjects.ScheduleReadWriteTransforms(new PixelateSnappingUtils.SnapToPixelJob
            {
                CameraRotation = _Camera.transform.rotation,
                UnitsPerPixel = _PixelateRenderFeature.UnitsPerPixel,
            }, _SnapToPixelJobHandle);
            _SnappableObjects.WaitTillJobsComplete();
        }
    }

    private void PostRenderCamera(ScriptableRenderContext context, Camera camera)
    {
        if (camera != _Camera) return;

        _Camera.ResetProjectionMatrix();
        _Camera.pixelRect = _DisplayViewport;

        if (_PixelateRenderFeature == null) return;

        if (_PixelateRenderFeature.CameraPixelSnapping) {
            _Camera.transform.SetPositionAndRotation(_PreviousPosition, _PreviousRotation);
        }

        if (_PixelateRenderFeature.EnablePixelSnapping && _SnappableObjects.IsCreated)
        {
            _UnsnapFromPixelJobHandle = _SnappableObjects.ScheduleWriteTransforms(new PixelateSnappingUtils.UnsnapFromPixelJob
            {
                Positions = _SnappableObjects.Positions.AsArray(),
            }, _UnsnapFromPixelJobHandle);

            _SnappableObjects.WaitTillJobsComplete();
        }

        // puke:
        if (_PixelateRenderFeature.OverlayUITexture is not null)
        {
            RenderTexture rt = UnityEngine.RenderTexture.active;
            UnityEngine.RenderTexture.active = _PixelateRenderFeature.OverlayUITexture;
            GL.Clear(false, true, Color.clear);
            UnityEngine.RenderTexture.active = rt;
        }

        // if (_PixelateRenderFeature.OverlayCursorTexture is not null)
        // {
        //     RenderTexture rt = UnityEngine.RenderTexture.active;
        //     UnityEngine.RenderTexture.active = _PixelateRenderFeature.OverlayCursorTexture;
        //     GL.Clear(false, true, Color.clear);
        //     UnityEngine.RenderTexture.active = rt;
        // }
    }

    public void DetectDirtyChanges()
    {
        if (_DisplayViewport.width != Screen.width || _DisplayViewport.height != Screen.height) {
            _DisplayViewport.width = Screen.width;
            _DisplayViewport.height = Screen.height;
            IsDirty = true;
        }

        IsDirty = IsDirty || _PixelateRenderFeature == null
            || _PreviousOrthographicSize != OrthographicSize
            || _PreviousRenderingPath != _Camera.actualRenderingPath;

        if (IsDirty) {
            _PreviousOrthographicSize = OrthographicSize;
            _PreviousRenderingPath = _Camera.actualRenderingPath;
        }
    }

    public void UpdateRenderViewport()
    {
        if (_PixelateRenderFeature == null) {
            var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            var feature = urp.GetRenderFeature<PixelateRenderFeature>();
            if (feature.IsSome())
            {
                _PixelateRenderFeature = feature.OrDefault();
                _PixelateRenderFeature.SetCameraReference(this);
            }
        }

        _RenderViewport.width = (int)(_DisplayViewport.width / _PixelateRenderFeature.ScaleFactor);
        _RenderViewport.height = (int)(_DisplayViewport.height / _PixelateRenderFeature.ScaleFactor);
    }
}

}
