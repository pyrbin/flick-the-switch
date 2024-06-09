using UnityEngine;

public class Cursor : MonoBehaviour
{
    public Light? Light;
    public LayerMask IncludeLayer;
    public bool HideCursor = false;
    public RectTransform? CursorRectTransform;
    public Transform? CursorCollider;

    [Header("Light Position")]
    [Range(-10f, 10f)]
    public float YOffset = 0f;

    [Range(-10f, 10f)]
    public float XOffset = 0f;

    [Range(0, 60f)]
    public float XRotationOffset = 30f;
    [Range(0, 60f)]
    public float YRotationOffset = 30f;

    public static Cursor Instance { get; private set; }

    public bool EnableLights
    {
        get => _lightsEnabled;
        set
        {
            _lightsEnabled = value;
            Light!.gameObject.SetActive(value);
        }
    }

    private bool _lightsEnabled = false;

    private Camera _camera;

    public float3 WorldPosition { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        Assert.IsFalse(Light is null);
        _camera = Camera.main;
        Assert.IsFalse(_camera is null);

        EnableLights = false;
    }

    public void Update()
    {
        UnityEngine.Cursor.visible = !HideCursor;
        UpdateCursorPosition();

        if (_lightsEnabled)
        {
            UpdateLightPosition();
        }
    }

    public void UpdateLightPosition()
    {
        var mousePosition = new float3(Input.mousePosition.x, Input.mousePosition.y, 0f);
        var ray = _camera.ScreenPointToRay(mousePosition);
        var hit = Physics2D.GetRayIntersection(ray, 30f, IncludeLayer);

        if (hit.collider is null)
        {
            Light!.gameObject.SetActive(false);
            return;
        }

        Light!.gameObject.SetActive(true);

        var y = YOffset + hit.point.y;
        var x = XOffset + hit.point.x;

        Light!.transform.position = new float3(x, y, hit.transform.position.z);
        CursorCollider!.transform.position = new float3(x, y, hit.transform.position.z);

        var colliderBounds = hit.collider!.bounds;
        float yHit = Mathfs.Clamp01((hit.point.y - colliderBounds.min.y) / colliderBounds.size.y);
        float xHit = Mathfs.Clamp01((hit.point.x - colliderBounds.min.x) / colliderBounds.size.x);

        var rotY = (float)(xHit * (YRotationOffset * 2.0) - YRotationOffset);
        var rotX = -(float)(yHit * (XRotationOffset * 2.0) - XRotationOffset);

        var eulerAngles = Light!.transform.eulerAngles;
        Light!.transform.rotation = Quaternion.Euler(new float3(rotX, rotY, eulerAngles.z));
    }

    public float2 NormalizedNdc()
    {
        Vector3 mousePosition = Input.mousePosition;
        float normalizedX = mousePosition.x / Screen.width;
        float normalizedY = mousePosition.y / Screen.height;
        //normalizedY = 1 - normalizedY;
        return new float2(normalizedX, normalizedY);
    }

    public void UpdateCursorPosition()
    {
        var mousePosition = new float3(Input.mousePosition.x, Input.mousePosition.y, 1f);
        WorldPosition = Camera.main.ScreenToWorldPoint(mousePosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(CursorRectTransform.parent.GetComponent<RectTransform>(), mousePosition.xy, Camera.main, out var localPoint);
        CursorRectTransform.anchoredPosition = localPoint;
    }
}
