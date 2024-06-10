using UnityEngine;

public enum HandSide { Left, Right }

[RequireComponent(typeof(Clickable), typeof(Health))]
public class MoleHand : MonoBehaviour
{
    public HandSide Side;

    [ReadOnly]
    public Clickable? Clickable;

    [ReadOnly]
    public Enemy? Enemy;

    public void Awake()
    {
        var scaleX = Side == HandSide.Right ? -transform.localScale.x : transform.localScale.x;
        transform.localScale = new Vector3(scaleX, transform.localScale.y, transform.localScale.z);

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

        // set position 10 below ground
        transform.position = new Vector3(transform.position.x, transform.position.y - 10, transform.position.z);

        // set rotation to euler -100, 180, 0
        transform.rotation = Quaternion.Euler(new Vector3(-100, 180, 0));

        // Create a sequence
        Sequence sequence = DOTween.Sequence();

        sequence.Append(transform.DOMoveY(0, moveDuration).SetEase(Ease.Linear));
        sequence.Insert(moveDuration / 2, transform.DORotate(new Vector3(0, 180, 0), moveDuration / 2).SetEase(Ease.Linear));

        sequence.Play().OnComplete(() => {
            Enemy.IsInvincible = false;
        });
    }

    public void RunDespawnAnimation(float moveDuration = 3f)
    {
        transform.DOMoveY(-10f, moveDuration).SetEase(Ease.Linear).OnComplete(() => {
            transform.DOComplete();
            transform.DOKill();
            Destroy(gameObject);
        });
    }

    public void OnClick(Transform _cursor)
    {

    }
}
