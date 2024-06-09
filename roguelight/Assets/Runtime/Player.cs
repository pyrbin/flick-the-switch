using UnityEngine;
using Utilities.Extensions;

public enum GoldChangedMode {
    Added,
    Removed
}

[ExecuteAlways]
public class Player : MonoBehaviour
{
    [Header("References")]
    public Oil? Oil;
    public Damage? Damage;
    public Luminosity? Luminosity;

    [Header("Gameplay")]
    public int Gold = 0;
    public bool OilAffectsLightRange = true;
    public List<UpgradeItem> InventoryList { get; private set; } = new();

    public event Action<int, GoldChangedMode>? GoldChanged;
    public event Action<UpgradeItem>? OnAddToInventory;
    public event Action<UpgradeItem>? OnRemoveFromInventory;
    public event Action? OnKill;

    public static Player Instance { get; private set; }

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void OnEnable()
    {
        TryGetComponent(out Oil);
        TryGetComponent(out Damage);
        TryGetComponent(out Luminosity);
    }

    public void Start()
    {
        UserInput.Instance.OnClicked += OnClick;
    }

    private float _flickerTimer = 0f;
    private bool _shouldFlicker;
    public void Update()
    {
        if (Luminosity is not null && Cursor.Instance?.Light is not null && Cursor.Instance.EnableLights)
        {
            const float baseIntensity = 150f;
            const float baseRange = 25f;

            float intensity = baseIntensity;
            float range = Luminosity.Value + baseRange;
            if (OilAffectsLightRange && Oil!.IsLow)
            {
                float perc = Oil.Percentage();
                perc = Mathfs.Clamp(perc, 0, Oil.IsLowThreshold);
                perc = (perc / Oil.IsLowThreshold) + 0.1f;
                range = range * perc;
                intensity = baseIntensity * Mathfs.Clamp01(perc + 0.5f);
            }

            Cursor.Instance.Light.intensity = intensity;
            Cursor.Instance.Light.range = range;
        }
    }

    public void AddGold(int amount)
    {
        Gold += amount;
        GoldChanged?.Invoke(amount, GoldChangedMode.Added);
    }

    public void RemoveGold(int amount)
    {
        Gold -= amount;
        GoldChanged?.Invoke(amount, GoldChangedMode.Removed);
    }

    public bool CanPurchase(Upgrade upgrade) => Gold >= upgrade!.GoldCost;

    public void AddToInventory(UpgradeItem item)
    {
        if (item.Upgrade is null || InventoryList.Contains(item) || !CanPurchase(item.Upgrade))
        {
            Game.Instance.SetShopActionsDisabled(false);
            item.RestoreStyles();
            return;
        }

        Game.Instance.SetShopActionsDisabled(true);
        item.Upgrade!.ApplyStats(this.gameObject);
        item.Upgrade!.AddCustomSetup(this.gameObject);
        InventoryList.Add(item);
        Shop.Instance.RemoveFromShop(item);
        item.gameObject.SetActive(false);
        OnAddToInventory?.Invoke(item);
        Game.Instance.SetShopActionsDisabled(false);
        RemoveGold(item.Upgrade!.GoldCost);
    }

    public void RemoveFromInventory(UpgradeItem item, bool removeFromList = true)
    {
        item.SetActive(false);
        item.Upgrade!.RemoveStats(this.gameObject);
        item.Upgrade!.RemoveCustomSetup(this.gameObject);
        if (removeFromList)
            InventoryList.Remove(item);
        OnRemoveFromInventory?.Invoke(item);
        Destroy(item.gameObject.transform.parent.gameObject);
        item.transform?.SetParent(null);
        item.CancelAnimations();
        item.SetActive(false);
        Destroy(item.gameObject);
    }

    public void Reset()
    {
        Game.Instance.SetShopActionsDisabled(true);
        foreach (var item in InventoryList)
        {
            RemoveFromInventory(item, false);
        }
        InventoryList.Clear();
        Game.Instance.SetShopActionsDisabled(false);
        Gold = 0;
        Luminosity?.Reset();
        Oil?.Reset();
        Damage?.Reset();
    }

    public void OnClick(Transform target)
    {
        TweenTools.Shake(Cursor.Instance!.CursorGfx!.GetChild(0).transform, 0.25f, 0.05f);

        if (target.TryGetComponent<Health>(out var health))
        {
            RollForDamage(health);
        }
    }

    private FloatingTextParams _params = new();
    public void RollForDamage(Health target)
    {
        var damage = Damage!.Value;
        target.Reduce(damage);

        _params.Text = damage.ToString(); // format based on type
        _params.Color = Color.yellow; // calculate based on hit
        _params.FontSize = FloatingCombatText.FontSize.Small;

        var offset = new Vector3(1f, 3f, 0);
        FloatingText.Instance.Show(target.transform.position + offset, ref _params);
    }

    public void NotifyKilled(Transform? target)
    {
        OnKill?.Invoke();
    }
}
