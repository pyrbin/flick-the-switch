using UnityEngine;

[RequireComponent(typeof(Clickable))]
public class Shakeable : MonoBehaviour
{
    public bool ShowText = false;

    [SerializeField]
    [ShowIf(nameof(ShowText))]
    public FloatingTextParams FloatingTextParams;

    [ReadOnly]
    public Clickable? Clickable;

    public void Awake()
    {
        TryGetComponent(out Clickable);
        Clickable!.OnClick += OnClick;
    }

    const float animDuration = 0.33f;
    public void OnClick(Transform _cursor)
    {
        TweenTools.Shake2(transform, animDuration, 0.1f);

        if (ShowText)
        {
            FloatingText.Instance.Show(transform.position + new Vector3(0, 2.5f, 0), ref FloatingTextParams);
        }
    }
}
