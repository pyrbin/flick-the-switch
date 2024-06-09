using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class HUD : MonoBehaviour
{
    public Oil? Oil;
    public Slider? OilSlider;
    public Image? OilSliderFill;
    public TMPro.TMP_Text? OilText;

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

    private Color _defaultColor;
    public void Awake()
    {
        OilSlider!.value = Oil!.Percentage();
    }

    public void Start()
    {
        Player.Instance.GoldChanged += OnGoldChanged;
        _defaultColor = OilSliderFill!.color;
    }

    public void OnGoldChanged(int amount, GoldChangedMode mode)
    {
        TweenTools.Shake2(this.Gold!.GetComponent<RectTransform>(), 0.666f, 1.5f);
    }

    public void Update()
    {
        OilSlider!.value = Oil!.Percentage();
        OilSliderFill!.color = Oil.IsLow ? Color.red : _defaultColor;
        OilText!.text = (Oil.Percentage() * 100f).CeilToInt().ToString() + "%";

        if (Gold is not null && Gold!.gameObject.activeSelf && GoldText is not null && Player.Instance is not null)
        {
            GoldText!.text = Player.Instance.Gold.ToString();
        }
    }
}
