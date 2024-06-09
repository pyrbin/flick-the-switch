using UnityEngine;

[RequireComponent(typeof(Clickable))]
public class Lamp : MonoBehaviour
{
    public float SwingDuration = 0.5f;
    public float Angle = 30f;

    [ReadOnly]
    public Clickable? Clickable;

    public bool IsSwinging = false;
    private Vector3 originalRotation;

    public void Awake()
    {
        TryGetComponent(out Clickable);
        Clickable!.OnClick += OnClick;
        originalRotation = transform.localEulerAngles;
    }

    public void OnClick(Transform _cursor)
    {
        if (IsSwinging) return;

        int direction = Freya.Random.Range(0, 2) == 0 ? -1 : 1;
        float targetAngle = Angle * direction;

        IsSwinging = true;

        UniTask.Void(async () => {
            await transform.DORotate(new Vector3(0, targetAngle, 0), SwingDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine).AwaitForComplete();
            await transform.DORotate(new Vector3(0, -1f * targetAngle * 1.5f, 0), SwingDuration * 1.5f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine).AwaitForComplete();
            await transform.DORotate(new Vector3(0, targetAngle * .75f, 0), SwingDuration * 1.75f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine).AwaitForComplete();
            await transform.DORotate(new Vector3(0, -1f * targetAngle * 0.25f, 0), SwingDuration * 1.5f, RotateMode.LocalAxisAdd).SetEase(Ease.InOutSine).AwaitForComplete();
            IsSwinging = false;
         });
    }

}
