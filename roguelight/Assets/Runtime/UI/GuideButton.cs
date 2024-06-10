
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GuideButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject Guide;
    public Button Button;

    bool _hover = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Guide.SetActive(true);
        _hover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Guide.SetActive(false);
        _hover = false;
    }

    void Update()
    {
        Guide.SetActive(_hover);
    }
}
