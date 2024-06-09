using UnityEngine;
using UnityEngine.InputSystem;

public class UserInput : MonoBehaviour
{
    public static UserInput Instance { get; private set; }
    public LayerMask ExcludeLayer;

    public event Action<Transform>? OnClicked;

    private Camera? _camera;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        _camera = Camera.main;
    }

    const float maxRange = 40f;

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.started || _camera is null) return;
        var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

        int excludeLayerNumber = ExcludeLayer.value;
        int layerMask = ~excludeLayerNumber;

        RayCast3D(ray, layerMask);
        RayCast2D(ray, layerMask);
    }

    public void RayCast2D(Ray ray, int mask)
    {
        var hit = Physics2D.GetRayIntersection(ray, maxRange, mask);
        if (!hit.collider) return;
        if (!hit.collider.TryGetComponent<Clickable>(out var clickable) && clickable != null && clickable.IsEnabled)
            return;
        if (Cursor.Instance is null || Cursor.Instance?.transform is null) return;
        clickable?.Click(Cursor.Instance?.transform ?? null);
        if (clickable is null || clickable?.IsEnabled is false) return;
        OnClicked?.Invoke(hit.collider.transform);
    }

    public void RayCast3D(Ray ray, int mask)
    {
        if (!Physics.Raycast(ray, out var hit, maxRange, mask)) return;
        if (!hit.collider.TryGetComponent<Clickable>(out var clickable) && clickable != null && clickable.IsEnabled)
            return;
        if (Cursor.Instance is null || Cursor.Instance?.transform is null) return;
        clickable?.Click(Cursor.Instance?.transform ?? null);
        if (clickable is null || clickable?.IsEnabled is false) return;
        OnClicked?.Invoke(hit.collider.transform);
    }
}
