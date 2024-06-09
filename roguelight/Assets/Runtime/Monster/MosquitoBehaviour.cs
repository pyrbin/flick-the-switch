using UnityEngine;

public class MosquitoBehaviour : MonoBehaviour
{
    public float Height = 8f;
    public float Speed = 2f;
    public LayerMask wallLayer;
    private bool movingRight;
    private Tween currentTween;

    [ReadOnly]
    public Clickable? Clickable;

    public void Awake()
    {
        TryGetComponent(out Clickable);
        Clickable!.OnClick += OnClick;
    }

    const float switchChance = 0.38f;
    public void OnClick(Transform _cursor)
    {
        if (Freya.Random.Range(0f, 1f) < switchChance)
        {
            SwapDirection();
        }
    }

    float currentHeight;

    void Start()
    {
        currentHeight = Freya.Random.Range(3f, Height);
        movingRight = Freya.Random.Value > 0.5f;

        Move();
    }

    void Update()
    {
        CheckForWalls();
    }

    void Move()
    {
        float direction = movingRight ? 1f : -1f;
        Vector3 endPosition = transform.position + Vector3.right * direction * 10f;

        endPosition.y = currentHeight;

        Vector3 lookDirection = endPosition - transform.position;
        transform.DOLookAt(transform.position + lookDirection, 0.5f);

        currentTween = transform.DOMove(endPosition, 10f / Speed)
            .SetSpeedBased()
            .SetEase(Ease.Linear)
            .OnComplete(OnMovementComplete);
    }

    void OnMovementComplete()
    {
        Move();
    }

    void CheckForWalls()
    {
        float direction = movingRight ? 1f : -1f;
        Vector3 origin = transform.position;
        Vector3 directionVector = Vector3.right * direction;
        if (Physics.Raycast(origin, directionVector, 2f, wallLayer))
        {
            SwapDirection();
        }
    }

    void SwapDirection()
    {
        currentTween.Kill();
        movingRight = !movingRight;
        Move();
    }
}
