using UnityEngine;

[RequireComponent(typeof(Clickable))]
public class Shakeable : MonoBehaviour
{
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
    }
}
