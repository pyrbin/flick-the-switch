using JSAM;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
[CreateAssetMenu(fileName = "Upgrade", menuName = "Scriptable Objects/Upgrade")]
public class Upgrade : ScriptableObject
{
    public string Name;
    public Sprite Image;
    public Color Color = Color.gray;
    public Color AccentColor = Color.white;
    public StatType Type;
    public float Value = 0;
    public int GoldCost = 0;
    public ModifierMode Modifier = ModifierMode.Flat;
    public string? OverrideDescription = null;

    [ShowNativeProperty]
    public string Description => ( OverrideDescription?.NullIfEmpty() is not null
        ? OverrideDescription
        : StatLookup.NameOf(Type) + (Value > 0 ? " +" : "-") + (Modifier == ModifierMode.Mult ? $"{Value*100}%" : Value)).ToLowerInvariant();

    private float _RealValue = 0;

    public void ApplyStats(GameObject obj)
    {


        AudioManager.PlaySound(Audio.Sounds.ShopUpgrade);
        var statComponent = StatLookup.Get(Type, obj);
        if (statComponent.IsNone()) return;

        if (Type == StatType.MultiChance && Modifier == ModifierMode.Flat)
        {
            var stat = statComponent.OrDefault()!;
            var remainder = 100 - stat.Value;
            var real = (Value / 100) * remainder;
            _RealValue = real;
            statComponent.OrDefault()!.Modify(Modifier, _RealValue);

        } else {
            statComponent.OrDefault()!.Modify(Modifier, Value);
        }

    }

    public void RemoveStats(GameObject obj)
    {
        var statComponent = StatLookup.Get(Type, obj);
        if (statComponent.IsNone()) return;

        if (Type == StatType.MultiChance && Modifier == ModifierMode.Flat)
        {
            statComponent.OrDefault()!.Modify(Modifier, -_RealValue);
        } else {
            statComponent.OrDefault()!.Modify(Modifier, -Value);
        }
    }
    public virtual void AddCustomSetup(GameObject Target) {}
    public virtual void RemoveCustomSetup(GameObject Target) {}
}
