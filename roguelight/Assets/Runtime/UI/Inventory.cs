using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public GridLayoutGroup InventoryLayoutGroup;
    public RectTransform HolderPrefab;

    public void AddToInventory(UpgradeItem item)
    {
        var holder = Instantiate(HolderPrefab, InventoryLayoutGroup.transform);

        item.gameObject.ActivateAllComponents();
        item.gameObject.SetActive(true);
        item.SetUpgrade(item.Upgrade);
        item.SetIsShopItem(false);
        item.transform.SetParent(holder.transform);
        item.RestoreStyles();
        var itemRect = item.GetComponent<RectTransform>();
        itemRect.anchoredPosition.Set(0, 25f);
    }

    public void PopFromInventory(UpgradeItem item)
    {
        Destroy(item.transform.parent.gameObject);
        item.transform?.SetParent(null);
    }
}
