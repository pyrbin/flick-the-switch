
public class Damage : StatBase
{
    public override StatType Type() => StatType.Damage;

    public override void OnModify(ModifierMode mode, float value)
    {
        // Do nothing
    }
}