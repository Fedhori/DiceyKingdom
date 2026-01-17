using UnityEngine;
using UnityEngine.EventSystems;

public sealed class SimpleTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Text")]
    [SerializeField, TextArea] string title;
    [SerializeField, TextArea] string body;

    [Header("Anchor Rect (optional)")]
    [SerializeField] RectTransform anchorRect;

    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        var rect = anchorRect != null ? anchorRect : transform as RectTransform;
        if (rect == null)
            return;

        rect.GetWorldCorners(corners);
        Vector3 topLeftWorld = corners[1];
        Vector3 topRightWorld = corners[2];

        Vector2 screenRightTop = RectTransformUtility.WorldToScreenPoint(null, topRightWorld);
        Vector2 screenLeftTop = RectTransformUtility.WorldToScreenPoint(null, topLeftWorld);

        var model = new TooltipModel(
            title,
            body,
            TooltipKind.Simple
        );

        var anchor = TooltipAnchor.FromScreen(screenRightTop, screenLeftTop);

        manager.BeginHover(this, model, anchor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        manager.EndHover(this);
    }

    void OnDisable()
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        manager.EndHover(this);
    }
}