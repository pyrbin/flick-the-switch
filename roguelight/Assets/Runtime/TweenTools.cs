using UnityEngine;

public static class TweenTools
{
    public static void Shake(RectTransform rectTransform, float shakeDuration = 0.5f, float shakeStrength = 0.56f)
    {
        const int shakeVibrato = 10;
        const float shakeRandomness = 90f;

        rectTransform.DOComplete();
        rectTransform.DOKill();

        Vector2 originalPosition = rectTransform.anchoredPosition;

        rectTransform.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrato, shakeRandomness)
            .OnComplete(() => rectTransform.anchoredPosition = originalPosition); // Ensure it resets to original position
    }

    public static void Shake2(Transform target, float shakeDuration = 0.5f, float shakeStrength = 0.56f)
    {
        const int vibrato = 6;
        const float randomness = 90f;

        target.DOComplete();
        target.DOKill();

        var originalPosition = target.localPosition;
        target.localPosition = originalPosition;
        var newTween = target.DOShakePosition(shakeDuration, shakeStrength, vibrato, randomness)
                .OnComplete(() => {
                    target.localPosition = originalPosition;
                });
    }

    private static Dictionary<Transform, Tween> _tweens = new();
    public static void Shake(Transform target, float shakeDuration = 0.5f, float shakeStrength = 0.56f)
    {
        if (_tweens.TryGetValue(target, out var tween))
        {
            if (tween.CompletedLoops() - tween.Loops() <= 1 && tween.IsPlaying() && tween.ElapsedPercentage() >= 0.5f)
            {
                tween.SetLoops(tween.Loops() + 1, LoopType.Yoyo);
            }
            return;
        }

        const int vibrato = 6;
        const float randomness = 90f;

        var originalPosition = target.localPosition;
        target.localPosition = originalPosition;
        var newTween = target.DOShakePosition(shakeDuration, shakeStrength, vibrato, randomness)
                .OnComplete(() => {
                    target.localPosition = originalPosition;
                    _tweens.Remove(target);
                });

        _tweens.Add(target, newTween);
    }

    public static void Press(Transform target, float duration = 0.5f)
    {
        target.DOComplete();
        target.DOKill();

        var oldScale = target.localScale;
        target.DOScale(oldScale * 1.2f, duration * 0.5f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => target.DOScale(oldScale, duration * 0.5f)
                  .SetEase(Ease.InOutSine));
    }

    public static void Press(RectTransform target, float duration = 0.5f)
    {
        target.DOComplete();
        target.DOKill();

        var oldScale = target.localScale;
        target.DOScale(oldScale * 1.2f, duration * 0.5f)
            .SetEase(Ease.InOutSine)
            .OnComplete(() => target.DOScale(oldScale, duration * 0.5f)
                  .SetEase(Ease.InOutSine));
    }
}
