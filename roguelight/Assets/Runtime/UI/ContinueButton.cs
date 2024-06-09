using UnityEngine;
using UnityEngine.UI;

public class ContinueButton : MonoBehaviour
{
    public Button Button;
    public void Awake()
    {
        Button = GetComponent<Button>();
        Button.onClick.AddListener(OnClick);
    }

    void OnClick()
    {
        Player.Instance.OnClick(this.transform);
        TweenTools.Shake(this.GetComponent<RectTransform>(), 0.666f, 1.5f);
        Game.Instance.RestartCycle();
    }
}
