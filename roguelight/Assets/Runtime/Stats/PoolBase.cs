using UnityEngine;

public abstract class PoolBase : StatBase
{
    private const float Uinitialized = -1;

    public float Current = Uinitialized;

    public event Action? OnDepleted;
    public event Action? OnFull;
    public event Action<float>? OnCurrentChanged;

    public override void Modify(ModifierMode mode, float value)
    {
        var old = value;
        var oldPercentage = Current / Value;
        base.Modify(mode, value);
        Current = oldPercentage * Value;
    }

    public void Start()
    {
        if (Current == Uinitialized)
        {
            Current = Value;
        }

        OnCurrentChanged?.Invoke(Current);
    }

    public void Update()
    {
        Current = Mathfs.Clamp(Current, 0f, Value);
    }

    public float Percentage() => Mathfs.Clamp(Current / Value, 0f, 1f);

    public void Increase(float value)
    {
        var old = Current;
        Current = Mathfs.Clamp(Current + value, 0f, Value);
        if (Current == Value && Current != old) OnFull?.Invoke();
        OnCurrentChanged?.Invoke(Current);
    }

    public void Reduce(float value)
    {
        var old = Current;
        Current = Mathfs.Clamp(Current - value, 0f, Value);
        if (Current == 0 && Current != old) OnDepleted?.Invoke();
        OnCurrentChanged?.Invoke(Current);
    }

    public void SetFull()
    {
        Current = Value;
    }
}
