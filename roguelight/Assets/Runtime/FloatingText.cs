using UnityEngine;
using UnityEngine.Pool;


public class FloatingText : MonoBehaviour
{
    [SerializeField]
    private FloatingCombatText Prefab;

    [SerializeField]
    private int DefaultCapacity = 30;

    [SerializeField]
    private int MaxSize = 100;

    private IObjectPool<FloatingCombatText> Pool;

    public static FloatingText Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        Pool = new ObjectPool<FloatingCombatText>(Create,
                OnGetFromPool, OnReleaseToPool, OnDestroyPooledObject,
                true, DefaultCapacity, MaxSize);
    }

    public void Show(float3 position, ref FloatingTextParams para)
    {
        var instance = Pool.Get();
        if (instance == null) return;

        instance.FindReferences();
        instance.Setup(position, ref para);
        instance.Run();
    }

    private FloatingCombatText Create()
    {
        FloatingCombatText instance = Instantiate(Prefab, this.transform);
        instance.transform.position = float3.zero;
        instance.ObjectPool = Pool;
        instance.Reset();
        return instance;
    }

    private void OnReleaseToPool(FloatingCombatText instance)
    {
        instance.Reset();
    }

    private void OnGetFromPool(FloatingCombatText instance)
    {
        instance.Reset();
        instance.Finished += () =>
        {
            Pool.Release(instance);
        };
    }

    private void OnDestroyPooledObject(FloatingCombatText instance)
    {
        instance.Reset();
        Destroy(instance.gameObject);
    }
}
