using UnityEngine;

public static class RectTransformExtensions
{
    public static void SetPivotAndAnchors(this RectTransform rectTransform, Vector2[] corners)
    {
        if (corners.Length != 4)
        {
            Debug.LogError("Corners array must have a length of 4.");
            return;
        }

        Vector2 size = rectTransform.rect.size;

        Vector2 newPivot = new Vector2(
            Mathf.InverseLerp(corners[0].x, corners[2].x, 0),
            Mathf.InverseLerp(corners[0].y, corners[2].y, 0)
        );

        rectTransform.pivot = newPivot;

        rectTransform.offsetMin = new Vector2(
            Mathf.Min(corners[0].x, corners[2].x),
            Mathf.Min(corners[0].y, corners[2].y)
        );

        rectTransform.offsetMax = new Vector2(
            Mathf.Max(corners[1].x, corners[3].x),
            Mathf.Max(corners[1].y, corners[3].y)
        );

        rectTransform.sizeDelta = size;
    }
}
