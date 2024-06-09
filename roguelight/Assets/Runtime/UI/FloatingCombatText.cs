using UnityEngine;
using UnityEngine.Pool;
using Utilities.Extensions;

[System.Serializable]
public struct FloatingTextParams
{
    public string Text;
    public Color Color;
    public FloatingCombatText.FontSize FontSize;
}

public class FloatingCombatText : MonoBehaviour
{
    public float enterScale = 1.5f;
    public float enterScaleAnimationDuration = 0.5f;
    public float exitScale = 0.7f;
    public float animationDuration = 1.5f;
    public float curveAmplitude = 1.0f;
    public float fadeDuration = 0.5f;

    private TMPro.TMP_Text Text;
    private RectTransform RectTransform;
    private bool _Animating = false;

    public event Action? Finished;

    private IObjectPool<FloatingCombatText>? _objectPool;

    public IObjectPool<FloatingCombatText> ObjectPool { set => _objectPool = value; }

    [System.Serializable]
    public enum FontSize { Small, Medium, Large };

    public void Start()
    {
        FindReferences();
    }

    public void FindReferences()
    {
        Text = GetComponent<TMPro.TMP_Text>();
        RectTransform = GetComponent<RectTransform>();
    }

    public float GetFontSize(FontSize size)
    {
        switch (size)
        {
            case FontSize.Small: return 10f;
            case FontSize.Medium: return 16f;
            case FontSize.Large: return 22f;
            default: return 10f;
        }
    }

    public void Setup(float3 position, ref FloatingTextParams para)
    {
        FindReferences();
        // Take a random position with in 0.5 of a unit circle around position
        const float radius = 0.75f;
        position += new float3(Freya.Random.Range(-radius, radius), Freya.Random.Range(-radius, radius), 0);
        transform.position = position;

        this.SetActive(false);
        Text.text = para.Text;
        Text.color = para.Color;
        Text.fontSize = GetFontSize(para.FontSize);
    }

    [Button("Run")]
    public void Run()
    {
        if (_Animating) return;
        _Animating = true;
        this.SetActive(true);
        Color color = Text.color;
        color.a = 1f;
        Text.color = color;
        UniTask.Void(async () => {
            // Scale up on enter
            RectTransform.localScale = Vector3.zero;
            await RectTransform.DOScale(enterScale, enterScaleAnimationDuration).SetEase(Ease.OutBack).AwaitForComplete();

            float direction = Freya.Random.Range(0, 2) == 0 ? 1 : -1;
            Vector3[] path = new Vector3[3];
            path[0] = transform.position;
            path[1] = transform.position + new Vector3(curveAmplitude * direction,curveAmplitude,0);
            path[2] = transform.position + new Vector3(curveAmplitude * direction * 2,curveAmplitude * 2,0);

            transform.DOPath(path, animationDuration, PathType.CatmullRom).SetEase(Ease.OutSine);
            RectTransform.DOScale(exitScale, animationDuration).SetEase(Ease.InSine);

            // Fade out during the animation
            await Text.DOFade(0, animationDuration - fadeDuration).SetEase(Ease.InSine).SetDelay(fadeDuration).AsyncWaitForCompletion();
            _Animating = false;
            Finished?.Invoke();
            Reset();
        });
    }

    public void ForceDestroy()
    {
        this.DOComplete();
        this.DOKill();
        this.SetActive(false);
        _Animating = false;
    }

    public void Reset()
    {
        if (_Animating) return;
        this.DOComplete();
        this.DOKill();
        _Animating = false;
        this.SetActive(false);
        this.Finished = null;
    }
}
