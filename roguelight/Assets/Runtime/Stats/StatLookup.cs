using UnityEngine;

[System.Serializable]
public enum StatType
{
    Damage,
    GoldReward,
    Luminosity,
    Oil,
    Health,
    MultiStrike,
    MultiChance,
}

// THis is so bad xD
public static class StatLookup
{
    public static Option<StatBase> Get(StatType type, GameObject obj)
    {
        return type switch
        {
            StatType.Damage => obj.TryGetComponent<Damage>(out var damage) ? damage as StatBase : Option<StatBase>.None,
            StatType.GoldReward => obj.TryGetComponent<GoldReward>(out var goldReward) ? goldReward as StatBase : Option<StatBase>.None,
            StatType.Luminosity => obj.TryGetComponent<Luminosity>(out var luminosity) ? luminosity as StatBase : Option<StatBase>.None,
            StatType.Oil => obj.TryGetComponent<Oil>(out var oil) ? oil as StatBase : Option<StatBase>.None,
            StatType.Health => obj.TryGetComponent<Health>(out var health) ? health as StatBase : Option<StatBase>.None,
            StatType.MultiStrike => obj.TryGetComponent<MultiStrike>(out var strike) ? strike as StatBase : Option<StatBase>.None,
            StatType.MultiChance => obj.TryGetComponent<MultiChance>(out var strike) ? strike as StatBase : Option<StatBase>.None,
            _ => Option<StatBase>.None
        };
    }
}
