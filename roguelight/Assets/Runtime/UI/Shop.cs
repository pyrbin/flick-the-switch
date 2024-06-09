using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class Shop : MonoBehaviour
{
    public HorizontalLayoutGroup ShopLayoutGroup;

    public static Shop Instance { get; private set; }

    public UpgradeItem SpawnedUpgradeItemPrefab;
    public RectTransform HolderPrefab;

    public int ShopItemCount => SpawnedUpgradeItems.Count;

    public bool HasItems => SpawnedUpgradeItems.Count > 0;

    [ReadOnly]
    [ReorderableList]
    public List<UpgradeItem> SpawnedUpgradeItems = new();

    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public void SpawnUpgradeItem(Upgrade upgrade)
    {
        var holder = Instantiate(HolderPrefab, ShopLayoutGroup.transform);
        var item = Instantiate(SpawnedUpgradeItemPrefab, holder.transform);
        item.SetUpgrade(upgrade);
        item.SetIsShopItem(true);
        item.EnterAnimation();

        SpawnedUpgradeItems.Add(item);
    }

    public void RemoveFromShop(UpgradeItem item)
    {
        if (!SpawnedUpgradeItems.Contains(item)) return;
        SpawnedUpgradeItems.Remove(item);
        if (item.transform.parent?.gameObject is not null)
            Destroy(item.transform.parent.gameObject);
        item.transform?.SetParent(null);
    }

    public void Reset()
    {
        foreach (var item in SpawnedUpgradeItems)
        {
            Destroy(item.transform.parent.gameObject);
            item.transform?.SetParent(null);
            item.CancelAnimations();
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }

        SpawnedUpgradeItems.Clear();
    }
}
