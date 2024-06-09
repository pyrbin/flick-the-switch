using System.Runtime.CompilerServices;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utilities.Extensions;

public class UpgradeItem
    : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerUpHandler, IPointerDownHandler
{
    public bool IsShopItem = true;

    public Upgrade? Upgrade;

    public Image Icon;
    public TMPro.TMP_Text Header;
    public TMPro.TMP_Text Description;
    public Image Background;
    public TMPro.TMP_Text GoldText;

    private Vector3 _originalScale;
    private Vector3 _originalIconScale;
    private Vector2 _originalGoldPosition;
    private Vector2 _originalDescriptionPosition;
    private Vector2 _originalPosition;

    public bool DisableInput => (IsShopItem && Game.Instance.IsShopActionsDisabled) || _disableInput;
    private bool _disableInput = false;

    public void Awake()
    {
        _originalScale = Vector3.one;
        _originalIconScale = Vector3.one * 0.8f;
        _originalPosition = this.GetComponent<RectTransform>().anchoredPosition;
        _originalGoldPosition = GoldText.rectTransform.anchoredPosition;
        _originalDescriptionPosition = Description.rectTransform.anchoredPosition;

        SetUpgrade(Upgrade);
        SetIsShopItem(IsShopItem);
    }

    public void EnterAnimation()
    {
        CancelAnimations();
        this.transform.localScale = Vector3.zero;
        RestoreStyles();
    }

    public void CancelAnimations()
    {
        this.DOComplete();
        this.DOKill();
        foreach (var g in transform.EnumerateHierarchy())
        {
            g.DOComplete();
            g.DOKill();
        }
    }

    void Update()
    {
        if (TryGetComponent(out RectTransform rect))
        {
            rect.anchoredPosition3D = new Vector3(rect.anchoredPosition3D.x, rect.anchoredPosition3D.y, 0f);
        }
        if (IsShopItem && Upgrade is not null && GoldText is not null)
        {
            GoldText.color = Player.Instance?.Gold < Upgrade.GoldCost ? Color.red : Color.white;
        }
    }

    public void SetIsShopItem(bool value)
    {
        SetTextAlpha(GoldText, value ? 1f : 0f);
        SetTextAlpha(Header, value ? 1f : 0f);
        SetTextAlpha(Description, value ? 1f : 0f);

        GoldText.SetActive(value);
        IsShopItem = value;
        _originalScale = !value ? _originalScale * 0.65f : _originalScale;
        RestoreStyles();
    }

    public void SetUpgrade(Upgrade? upgrade)
    {
        if (upgrade is null) return;
        Upgrade = upgrade;

        Icon.sprite = upgrade.Image;
        Header.text = upgrade.Name;
        Description.text = upgrade.Description;
        Icon.color = upgrade.AccentColor;
        Background.color = upgrade.Color;
        GoldText.text = upgrade.GoldCost.ToString();
        GoldText.color = Player.Instance?.Gold < upgrade.GoldCost ? Color.red : Color.white;
    }

    const float animDuration = 0.3f;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (DisableInput) return;
        if (IsShopItem)
        {
            this.transform.DOScale(_originalScale * 1.05f, animDuration);
            var thisRect = GetComponent<RectTransform>();
            thisRect.DOAnchorPos(thisRect.anchoredPosition + new Vector2(0, 4.25f), animDuration);
            Icon.transform.DOScale(_originalIconScale * 1.1f, animDuration);
            var moveBy = new Vector2(0, -4.5f);
            Description.rectTransform.DOAnchorPos(Description.rectTransform.anchoredPosition + moveBy, animDuration);
            moveBy = new Vector2(0, 5.25f);
            GoldText.rectTransform.DOAnchorPos(GoldText.rectTransform.anchoredPosition + moveBy, animDuration);
        } else {
            Header.DOFade(1, animDuration);
            Description.DOFade(1, animDuration);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (DisableInput) return;
        if (!IsShopItem)
        {
            Header.DOFade(0, animDuration);
            Description.DOFade(0, animDuration);
        }

        RestoreStyles();
    }

    public void RestoreStyles()
    {
        _disableInput = true;
        UniTask.Void(async () =>
        {
            await UniTask.WhenAll
            (
                this.transform.DOScale(_originalScale, animDuration).AsyncWaitForCompletion().AsUniTask(),
                this.GetComponent<RectTransform>().DOAnchorPos(_originalPosition, animDuration).AsyncWaitForCompletion().AsUniTask(),
                Icon.transform.DOScale(_originalIconScale, animDuration).AsyncWaitForCompletion().AsUniTask(),
                Description.rectTransform.DOAnchorPos(_originalDescriptionPosition, animDuration).AsyncWaitForCompletion().AsUniTask(),
                GoldText.rectTransform.DOAnchorPos(_originalGoldPosition, animDuration).AsyncWaitForCompletion().AsUniTask()
            );
            _disableInput = false;
        });
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (DisableInput) return;
        if (!IsShopItem) return;
        if (Upgrade is null) return;
        if (!Player.Instance.CanPurchase(Upgrade)) return;

        TransferToShop();
    }

    public async void TransferToShop()
    {
        Game.Instance.SetShopActionsDisabled(true);

        const float transferAnimDuration = 0.5f;
        var goldMove = new Vector2(0, -10.25f);

        await UniTask.WhenAll(
            GoldText.rectTransform.DOAnchorPos(GoldText.rectTransform.anchoredPosition + goldMove, transferAnimDuration / 2.0f)
                .SetEase(Ease.InOutSine)
                .AsyncWaitForCompletion()
                .AsUniTask(),
            this.transform.DOScale(Vector3.zero, transferAnimDuration)
                .SetDelay(0.1f)
                .SetEase(Ease.InOutSine)
                .AsyncWaitForCompletion()
                .AsUniTask()
        );

        Player.Instance.AddToInventory(this);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
    }

    private void SetTextAlpha(TMPro.TMP_Text text, float alpha)
    {
        Color color = text.color;
        color.a = alpha;
        text.color = color;
    }
}
