using UnityEngine;

public class Clickable : MonoBehaviour
{
    public bool IsEnabled = true;

    public event Action<Transform?>? OnClick;

    public void Click(Transform? transform)
    {
        if (!IsEnabled) return;
        OnClick?.Invoke(transform);
    }
}
