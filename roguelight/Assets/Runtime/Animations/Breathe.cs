using UnityEngine;

public class Breathe : MonoBehaviour
{
    [Range(0f, 1f)]
    public float ScaleMin = 1f;

    [Range(1f, 2f)]
    public float ScaleMax = 1.3f;

    public float Duration = 0f;

    public void Start()
    {
        transform.localScale = ScaleMin * Vector3.one;
        transform.DOScale(ScaleMax * Vector3.one, Duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
