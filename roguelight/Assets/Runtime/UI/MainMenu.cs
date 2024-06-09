using UnityEngine;
using UnityEngine.UI;
using Utilities.Extensions;

public class MainMenu : MonoBehaviour
{
    public Transform? PlayArrow;

    public void Show()
    {
        foreach (var g in transform.EnumerateHierarchy())
            g.SetActive(true);

        PlayArrow!.SetActive(true);
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

        this.SetActive(true);

        foreach (var g in PlayArrow!.EnumerateHierarchy())
            g.SetActive(true);
    }
}
