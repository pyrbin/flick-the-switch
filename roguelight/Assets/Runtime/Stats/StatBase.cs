using UnityEngine;

[System.Serializable]
public enum ModifierMode
{
    Flat,
    Mult
}

public abstract class StatBase : MonoBehaviour
{
    [SerializeField]
    private float _base;

    private float _multiplier = 0f;
    private float _flat = 0f;

    [ShowNativeProperty]
    public float Value => (_base * (1.0f + _multiplier)) + _flat;

    [ShowNativeProperty]
    public float BaseValue => _base;

    [ShowNativeProperty]
    public float Mult => _multiplier;

    [ShowNativeProperty]
    public float Flat => _flat;

    public event Action<float>? OnValueChanged;

    public abstract StatType Type();

    public abstract void OnModify(ModifierMode mode, float value);

    public virtual void Modify(ModifierMode mode, float value)
    {
        switch (mode)
        {
            case ModifierMode.Flat:
                _flat += value;
                break;
            case ModifierMode.Mult:
                _multiplier += value;
                break;
        }

        OnModify(mode, value);
        OnValueChanged?.Invoke(value);
    }

    [Button("Reset")]
    public void Reset()
    {
        _flat = 0f;
        _multiplier = 0f;
    }
}
