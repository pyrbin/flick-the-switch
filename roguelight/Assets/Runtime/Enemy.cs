using JSAM;
using UnityEngine;

[RequireComponent(typeof(Clickable))]
public class Enemy : MonoBehaviour
{
    public EnemyAudio enemyAudio;
    public Health? Health;
    public GoldReward? GoldReward;

    [ReadOnly]
    public Clickable? Clickable;

    // private Material _material;
    // private Color _originalColor;

    public bool IsDying { get; private set; } = false;
    public bool IsDead { get; private set; } = false;

    public void Start()
    {
        // var renderer = GetComponentInChildren<MeshRenderer>();
        // if (renderer != null)
        // {
        //     _material = renderer.material;
        //     _originalColor = _material.GetColor("_BaseColor");
        // }
    }

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

    private float _timeSinceHit = 0;

    public void OnClick(Transform _cursor)
    {
        if (IsDying || IsDead) return;
        enemyAudio.PlayHitSound();

        TweenTools.Shake2(transform, animDuration);

        _timeSinceHit = 0.0f;

        // if (_material != null)
        // {
        //     _material.SetColor("_BaseColor", Color.red);
        // }
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
        enemyAudio.PlayDeathSound();
        IsDead = true;
        transform.DOKill();
        Player.Instance.AddGold((int)GoldReward!.Value);
        Destroy(gameObject);
    }
}
