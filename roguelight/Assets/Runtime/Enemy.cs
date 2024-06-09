using JSAM;
using UnityEngine;

[RequireComponent(typeof(Clickable))]
public class Enemy : MonoBehaviour
{
    public MonsterData Data;
    public EnemyAudio EnemyAudio;
    public Health? Health;
    public GoldReward? GoldReward;
    public Transform? ShakePivot;

    [ReadOnly]
    public Clickable? Clickable;

    public bool IsDying { get; private set; } = false;
    public bool IsDead { get; private set; } = false;

    public void OnEnable()
    {
        TryGetComponent(out Clickable);
        TryGetComponent(out Health);
        TryGetComponent(out GoldReward);

        Health!.OnDepleted += OnHealthDepleted;
        Clickable!.OnClick += OnClick;
    }

    public void OnDisable()
    {
        Health!.OnDepleted -= OnHealthDepleted;
        Health = null;
        Clickable!.OnClick -= OnClick;
        Clickable = null;
    }

    const float animDuration = 0.5f;
    public void Update()
    {
        Clickable!.IsEnabled = !IsDead && !IsDying;
    }

    public void OnClick(Transform _cursor)
    {
        if (IsDying || IsDead) return;
        EnemyAudio.PlayHitSound();
        AudioManager.PlaySound(Audio.Sounds.Hit);
        TweenTools.Shake2(ShakePivot!, animDuration);
    }

    public void OnHealthDepleted()
    {
        IsDying = true;
        Clickable!.IsEnabled = false;

        RoundManager.Instance.RemoveFromState(this.gameObject);

        UniTask.Void(async () => {
            await UniTask.Delay(TimeSpan.FromSeconds(0.333), ignoreTimeScale: false);
            Kill();
        });
    }

    public void Kill()
    {
        Player.Instance.NotifyKilled(this.transform);
        EnemyAudio.PlayDeathSound();
        IsDead = true;
        transform.DOKill();
        Player.Instance.AddGold((int)GoldReward!.Value);
        Destroy(gameObject);
    }
}
