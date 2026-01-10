using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ItemTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TooltipAnchorType anchorType = TooltipAnchorType.Screen;
    [SerializeField] RectTransform anchorRect;

    ItemInstance instance;
    readonly Vector3[] corners = new Vector3[4];

    void Awake()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    public void Bind(ItemInstance boundInstance)
    {
        instance = boundInstance;
        if (instance == null)
            TooltipManager.Instance?.ClearOwner(this);
    }

    public void Clear()
    {
        instance = null;
        TooltipManager.Instance?.ClearOwner(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (instance == null)
            return;

        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        if (!TryBuildTooltip(out var model, out var anchor))
            return;

        manager.BeginHover(this, model, anchor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        manager.EndHover(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (instance == null)
            return;

        var manager = TooltipManager.Instance;
        if (manager == null)
            return;

        if (!TryBuildTooltip(out var model, out var anchor))
            return;

        manager.TogglePin(this, model, anchor);
    }

    bool TryBuildTooltip(out TooltipModel model, out TooltipAnchor anchor)
    {
        model = default;
        anchor = default;

        if (instance == null)
            return false;

        model = ItemTooltipUtil.BuildModel(instance);

        if (anchorType == TooltipAnchorType.World)
        {
            anchor = TooltipAnchor.FromWorld(transform.position);
            return true;
        }

        var rect = anchorRect != null ? anchorRect : transform as RectTransform;
        if (rect == null)
            return false;

        rect.GetWorldCorners(corners);
        Vector3 topLeftWorld = corners[1];
        Vector3 topRightWorld = corners[2];

        Vector2 screenRightTop = RectTransformUtility.WorldToScreenPoint(null, topRightWorld);
        Vector2 screenLeftTop = RectTransformUtility.WorldToScreenPoint(null, topLeftWorld);
        anchor = TooltipAnchor.FromScreen(screenRightTop, screenLeftTop);
        return true;
    }
}
