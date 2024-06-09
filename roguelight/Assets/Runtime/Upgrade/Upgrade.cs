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
        : Type.ToString() + (Value > 0 ? " +" : "-") + Value + (Modifier == ModifierMode.Mult ? "x" : "")).ToLowerInvariant();

    public void ApplyStats(GameObject obj)
    {
        var statComponent = StatLookup.Get(Type, obj);
        if (statComponent.IsNone()) return;
        statComponent.OrDefault()!.Modify(Modifier, Value);
    }

    public void RemoveStats(GameObject obj)
    {
        var statComponent = StatLookup.Get(Type, obj);
        if (statComponent.IsNone()) return;
        statComponent.OrDefault()!.Modify(Modifier, -Value);
    }
    public virtual void AddCustomSetup(GameObject Target) {}
    public virtual void RemoveCustomSetup(GameObject Target) {}
}
