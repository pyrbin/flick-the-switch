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

    public void OnClick(InputAction.CallbackContext context)
    {
        if (!context.started || _camera is null) return;
        var ray = _camera.ScreenPointToRay(Mouse.current.position.ReadValue());

        int excludeLayerNumber = ExcludeLayer.value;
        int layerMask = ~excludeLayerNumber;

        var hit = Physics2D.GetRayIntersection(ray, 30f, layerMask);
        Debug.DrawRay(ray.origin, ray.direction * 30f, Color.red, 1f);
        if (!hit.collider) return;

        if (!hit.collider.TryGetComponent<Clickable>(out var clickable) && clickable != null && clickable.IsEnabled)
            return;

        clickable!.Click(Cursor.Instance.transform);
        OnClicked?.Invoke(hit.collider.transform);
    }
}
