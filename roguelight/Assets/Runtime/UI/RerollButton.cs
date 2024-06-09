
using UnityEngine;
using UnityEngine.UI;

public class RerollButton : MonoBehaviour
{
    public Button Button;
    public TMPro.TMP_Text? GoldText;

    public void Awake()
    {
        Button = GetComponent<Button>();
        Button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        if (Game.Instance is not null && GoldText is not null)
        {
            GoldText.text = Game.Instance.CurrentRerollCost.ToString();
            GoldText.color = Game.Instance.HasMoneyForReroll() ? Color.white : Color.red;
            Button.interactable = Game.Instance.HasMoneyForReroll();
        }
    }

    void OnClick()
    {
        Player.Instance.OnClick(this.transform);
        if (Game.Instance is null || !Game.Instance.HasMoneyForReroll()) return;
        TweenTools.Shake(this.GetComponent<RectTransform>(), 0.666f, 1.5f);
        Game.Instance.RerollShop();
    }
}
