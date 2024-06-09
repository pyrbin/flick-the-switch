using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Clickable))]
public class Switch : MonoBehaviour
{
    [SerializeField]
    public bool IsOn = true;

    [ReadOnly]
    public Clickable? Clickable;

    public Transform? Top;

    public Transform? Bottom;

    public event Action<bool>? OnToggle;

    public static Switch Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        UpdateVisuals();
    }

    void Start()
    {
        UpdateVisuals();
    }

    public void OnEnable()
    {
        Clickable = GetComponent<Clickable>();
        Clickable.OnClick += OnClick;
        UpdateVisuals();
    }

    public void OnDisable()
    {
        UpdateVisuals();
        if (Clickable is null) return;
        Clickable.OnClick -= OnClick;
        Clickable = null;
    }

    private bool _locked = false;
    public void OnClick(Transform _cursor)
    {
        if (_locked) return;
        IsOn = !IsOn;
        OnToggle?.Invoke(IsOn);
        UpdateVisuals();
        _locked = true;
        UniTask.Void(async () => {
            await UniTask.Delay(TimeSpan.FromSeconds(2.0), ignoreTimeScale: false);
            _locked = false;
        });
    }

    public void ForceToggleSilent(bool isOn)
    {
        IsOn = isOn;
        UpdateVisuals();
    }

    public void Update()
    {
        if (Clickable is not null)
            Clickable!.IsEnabled = !_locked;
    }

    public void UpdateVisuals()
    {
        if (IsOn)
        {
            var eulerAngles = Top!.transform.eulerAngles;
            Top!.transform.rotation = Quaternion.Euler(new float3(-20,  eulerAngles.y, eulerAngles.z));
            Bottom!.transform.rotation = Quaternion.Euler(new float3(0,0,0));
        }
        else
        {
            var eulerAngles = Top!.transform.eulerAngles;
            Bottom!.transform.rotation = Quaternion.Euler(new float3(20,  eulerAngles.y, eulerAngles.z));
            Top!.transform.rotation = Quaternion.Euler(new float3(0,0,0));
        }
    }



}
