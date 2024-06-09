using UnityEngine;

public class Pickup : MonoBehaviour
{
    public float RewardCount;
    public Health? Health;
    public bool GoldReward;
    public bool OilReward;
    public Transform? ShakePivot;

    public bool IsDying { get; private set; } = false;
    public bool IsDead { get; private set; } = false;

    [ReadOnly]
    public Clickable? Clickable;
    const float animDuration = 0.5f;

    public void Awake()
    {
        TryGetComponent(out Health);
        TryGetComponent(out Clickable);

        Health!.OnDepleted += OnHealthDepleted;
        Clickable!.OnClick += OnClick;
    }

    public void OnClick(Transform _cursor)
    {
        if (IsDying || IsDead) return;
        TweenTools.Shake2(ShakePivot!, animDuration);
    }

    public void OnHealthDepleted()
    {
        IsDying = true;
        Clickable!.IsEnabled = false;
        UniTask.Void(async () => {
            await UniTask.Delay(TimeSpan.FromSeconds(0.2), ignoreTimeScale: false);
            Kill();
        });
    }

    public void Kill()
    {
        IsDead = true;
        transform.DOComplete();
        transform.DOKill();

        if (GoldReward)
            Player.Instance.AddGold((int)RewardCount);
        if (OilReward)
            Player.Instance.AddOil(Player.Instance.Oil.AmountFromPercentage((float)RewardCount));

        Destroy(gameObject);
    }
}
