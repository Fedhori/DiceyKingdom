using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public sealed class GhostView : MonoBehaviour
{
    [SerializeField] private Image iconImage;

    RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);
    }

    public void SetIcon(Sprite sprite)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = sprite;
        iconImage.enabled = sprite != null;
    }

    public void SetScreenPosition(Vector2 screenPos, Canvas canvas)
    {
        if (rectTransform == null)
            return;

        if (canvas == null)
            canvas = rectTransform.GetComponentInParent<Canvas>();

        if (canvas == null)
            return;

        Camera cam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera ||
            canvas.renderMode == RenderMode.WorldSpace)
        {
            cam = canvas.worldCamera;
        }

        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rectTransform, screenPos, cam, out var worldPos))
        {
            rectTransform.position = worldPos;
        }
    }
}
