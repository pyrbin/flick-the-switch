using UnityEngine;

public class Oil : PoolBase
{
    public override StatType Type() => StatType.Oil;

    static public float IsLowThreshold = 0.25f;

    public bool IsLow => Percentage() <= IsLowThreshold;

    public override void OnModify(ModifierMode mode, float value)
    {
        return;
    }
}
