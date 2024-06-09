using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class UIPerspectiveEffect : MonoBehaviour
{
    public float perspectiveAmount = 0.1f; // Amount of perspective effect

    private Image image;
    private RectTransform rectTransform;
    private Vector3[] originalCorners = new Vector3[4];
    private Vector3[] modifiedCorners = new Vector3[4];

    void Start()
    {
        image = GetComponent<Image>();
        rectTransform = GetComponent<RectTransform>();

        // Store the original corners of the RectTransform
        rectTransform.GetLocalCorners(originalCorners);
    }

    void Update()
    {
        ApplyPerspective();
    }

    void ApplyPerspective()
    {
        // Apply perspective effect to the corners
        for (int i = 0; i < 4; i++)
        {
            modifiedCorners[i] = originalCorners[i];

            if (i == 0 || i == 3) // Modify the top corners
            {
                modifiedCorners[i].x += modifiedCorners[i].y * perspectiveAmount;
            }
        }

        // Update the RectTransform corners
        rectTransform.SetPivotAndAnchors(modifiedCorners.Select(x => new Vector2(x.x, x.y)).ToArray());
    }
}
