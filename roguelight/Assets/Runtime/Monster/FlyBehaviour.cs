using UnityEngine;

[RequireComponent(typeof(Clickable), typeof(Health))]
public class FlyBehaviour : MonoBehaviour
{
    [ReadOnly]
    public Clickable? Clickable;

    public void Awake()
    {
        TryGetComponent(out Clickable);
        Clickable!.OnClick += OnClick;
    }

    public void OnClick(Transform _cursor)
    {

    }
}
