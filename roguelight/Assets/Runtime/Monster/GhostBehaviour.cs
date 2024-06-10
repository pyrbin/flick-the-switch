using JSAM;
using UnityEngine;

[RequireComponent(typeof(Clickable), typeof(Health))]
public class GhostBehaviour : MonoBehaviour
{
    public float Height = 8f;

    [ReadOnly]
    public Clickable? Clickable;

    [ReadOnly]
    public LookAtCamera? LookAtCamera;

    [ReadOnly]
    public Health? Health;

    public void Awake()
    {
        TryGetComponent(out Clickable);
        TryGetComponent(out Health);
        TryGetComponent(out LookAtCamera);

        Clickable!.OnClick += OnClick;
    }

    void Start()
    {
        float randomHeight = Freya.Random.Range(1f, Height);
        transform.position = new Vector3(transform.position.x, randomHeight, transform.position.z);
    }

    const float teleportChance = 0.99f;
    const float teleportHealthThreshold = 0.6f;
    bool _teleported = false;
    public void OnClick(Transform _cursor)
    {
        if (Health.Percentage() >= teleportHealthThreshold)
            return;

        if (Freya.Random.Range(0f, 1f) < (teleportChance - (_teleported ? teleportChance * 0.6f : 0f)))
        {
            Teleport();
        }
    }

    public void Teleport()
    {
        AudioManager.PlaySound(Audio.Sounds.MaskTeleport);
        _teleported = true;
        LookAtCamera!.Look();
        var position = RoundManager.Instance.RequestSpawnPosition();
        float randomHeight = Freya.Random.Range(1f, Height);
        transform.position = new Vector3(position.x, randomHeight, position.z);
    }
}
