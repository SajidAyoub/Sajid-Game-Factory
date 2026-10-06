using UnityEngine;

// Runtime-only inset of the generated HUD. Restores authored anchors on disable.
[DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
public sealed class Level1HUDSafeArea : MonoBehaviour
{
    private RectTransform rect;
    private Vector2 originalMin;
    private Vector2 originalMax;
    private Rect lastArea;
    private int lastWidth;
    private int lastHeight;

    private void OnEnable()
    {
        rect = GetComponent<RectTransform>();
        originalMin = rect.anchorMin; originalMax = rect.anchorMax;
        lastWidth = lastHeight = -1;
        Apply();
    }

    private void Update() => Apply();

    private void Apply()
    {
        if (rect == null) return;
        int width = Screen.width, height = Screen.height;
        Rect area = Screen.safeArea;
        if (width <= 0 || height <= 0 || area.width <= 0 || area.height <= 0 ||
            (width == lastWidth && height == lastHeight && area == lastArea)) return;
        lastWidth = width; lastHeight = height; lastArea = area;
        Vector2 min = new Vector2(Mathf.Clamp01(area.xMin / width), Mathf.Clamp01(area.yMin / height));
        Vector2 max = new Vector2(Mathf.Clamp01(area.xMax / width), Mathf.Clamp01(area.yMax / height));
        rect.anchorMin = originalMin + Vector2.Scale(originalMax - originalMin, min);
        rect.anchorMax = originalMin + Vector2.Scale(originalMax - originalMin, max);
    }

    private void OnDisable()
    {
        if (rect != null) { rect.anchorMin = originalMin; rect.anchorMax = originalMax; }
    }
}
