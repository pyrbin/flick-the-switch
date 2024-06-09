using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class HUD : MonoBehaviour
{
    public Oil? Oil;
    public Slider? OilSlider;
    public Image? OilSliderFill;

    public Transform? Gold;
    public TMPro.TMP_Text? GoldText;

    public void Show()
    {
        foreach (var g in transform.EnumerateHierarchy())
            g.SetActive(true);
        OilSlider!.SetActive(true);
        this.SetActive(true);
    }

    public void Hide()
    {
        this.SetActive(false);
    }

    public void SetForShop()
    {
        foreach (var g in transform.EnumerateHierarchy())
            g.SetActive(false);

        foreach (var g in Gold!.transform.EnumerateHierarchy())
            g.SetActive(true);

        this.SetActive(true);
    }

    public void Awake()
    {
        OilSlider!.value = Oil!.Percentage();
    }

    public void OnStart()
    {
        Player.Instance.GoldChanged += OnGoldChanged;
    }

    public void OnGoldChanged(int amount, GoldChangedMode mode)
    {
        TweenTools.Press(this.Gold!.GetComponent<RectTransform>(), 0.7f);
    }

    public void Update()
    {
        OilSlider!.value = Oil!.Percentage();
        OilSliderFill!.color = Oil.IsLow ? Color.red : Color.white;

        if (Gold is not null && Gold!.gameObject.activeSelf && GoldText is not null && Player.Instance is not null)
        {
            GoldText!.text = Player.Instance.Gold.ToString();
        }
    }
}
