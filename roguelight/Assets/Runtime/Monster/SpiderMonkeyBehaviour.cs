using UnityEngine;

[RequireComponent(typeof(Clickable), typeof(Health))]
public class SpiderMonkeyBehaviour : MonoBehaviour
{
    public float Radius = 5f;
    public float Speed = 2f;
    public float VerticalRange = 1f;
    public float VerticalSpeed = 1f;
    public float LookAheadTime = 0.1f; // How far ahead to look when determining direction

    private float _angle = 0f;
    private Vector3 _centerPosition;

    [ReadOnly]
    public Clickable? Clickable;

    public void Awake()
    {
        TryGetComponent(out Clickable);
        Clickable!.OnClick += OnClick;
    }

    void Start()
    {
        _centerPosition = transform.position;
        MoveVertically();
        MoveInCircle();
    }

    void MoveInCircle()
    {
        _angle += Speed * Time.deltaTime;

        float x = Mathf.Cos(_angle) * Radius;
        float z = Mathf.Sin(_angle) * Radius;

        // Calculate the new position
        Vector3 newPosition = new Vector3(_centerPosition.x + x, transform.position.y, _centerPosition.z + z);
        transform.position = newPosition;
        DOVirtual.DelayedCall(Time.deltaTime, MoveInCircle);
    }

    void MoveVertically()
    {
        float targetY = _centerPosition.y + Freya.Random.Range(1f, VerticalRange);

        transform.DOMoveY(targetY, VerticalSpeed)
            .SetEase(Ease.InOutSine)
            .OnComplete(MoveVertically);
    }

    public void OnClick(Transform _cursor)
    {
    }
}
