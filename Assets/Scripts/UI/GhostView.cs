using Data;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public sealed class GhostView : MonoBehaviour
{
    [SerializeField] private ItemView itemView;

    RectTransform rectTransform;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (itemView == null)
            itemView = GetComponentInChildren<ItemView>(true);
    }

    public void SetIcon(Sprite sprite)
    {
        itemView?.SetIcon(sprite);
    }

    public void SetRarity(ItemRarity rarity)
    {
        itemView?.SetRarity(rarity);
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
