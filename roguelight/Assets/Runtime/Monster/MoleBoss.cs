using UnityEngine;


[RequireComponent(typeof(Clickable), typeof(Health))]
public class MoleBoss : MonoBehaviour
{
    [ReadOnly]
    public Clickable? Clickable;

    [ReadOnly]
    public Enemy? Enemy;

    public void Awake()
    {
        TryGetComponent(out Enemy);
        TryGetComponent(out Clickable);
        Clickable!.OnClick += OnClick;
    }

    [Button("Run Spawn Animation")]
    public void TestSpawnAnimation()
    {
        RunSpawnAnimation(3f);
    }

    public void RunSpawnAnimation(float moveDuration = 3f)
    {
        Enemy.IsInvincible = true;
        transform.position = new Vector3(transform.position.x, transform.position.y - 15, transform.position.z);

        Sequence sequence = DOTween.Sequence();
        sequence.Append(transform.DOMoveY(0, moveDuration).SetEase(Ease.Linear));
        sequence.Insert(moveDuration * 0.7f, transform.DORotate(new Vector3(0, 180, 0), moveDuration / 2).SetEase(Ease.Linear));

        sequence.Play().OnComplete(() => {
            Enemy.IsInvincible = false;
        });
    }

    public void RunDespawnAnimation(float moveDuration = 3f)
    {
        transform.DOMoveY(-15f, moveDuration).SetEase(Ease.Linear).OnComplete(() => {
            transform.DOComplete();
            transform.DOKill();
            Destroy(gameObject);
        });
    }

    public void OnClick(Transform _cursor)
    {
    }
}
