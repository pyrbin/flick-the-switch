
public class Health : PoolBase
{
    public override StatType Type() => StatType.Health;

    public override void OnModify(ModifierMode mode, float value)
    {
        // Do nothing
    }
}
