using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ooze.Runtime.Pixelate.Runtime {

public class Snappable : MonoBehaviour {

    [ReadOnly, SerializeField]
    public int _InstanceId = -1;

    [Button("Register")]
    public void Register() => RegisterSelf();

    [Button("Deregister")]
    public void Deregister() => DeregisterSelf();

    private PixelateCamera _CameraReference;

    public void Start() => RegisterSelf();

    public void OnDestroy() => DeregisterSelf();

    public void RegisterSelf() {
        DeregisterSelf();
        var urp = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
        var optionalFeature = urp.GetRenderFeature<PixelateRenderFeature>();
        if (optionalFeature.IsSome())
        {
            var feature = optionalFeature.OrDefault();
            _CameraReference = feature.Camera;
            if (_CameraReference != null)
            {
                _InstanceId = _CameraReference.RegisterSnappable(transform);
            }
        }
    }

    public void DeregisterSelf() {
        if (_InstanceId == -1) return;
        if (_CameraReference != null)
        {
            _CameraReference.DeregisterSnappable(_InstanceId);
            _InstanceId = -1;
        }
    }
}

}
