using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ItemTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TooltipAnchorType anchorType = TooltipAnchorType.Screen;
    [SerializeField] RectTransform anchorRect;

    ItemInstance instance;
    UpgradeInstance upgrade;
    readonly Vector3[] corners = new Vector3[4];

    public bool HasUpgradeToggle => instance != null && instance.Upgrade != null;

    void Awake()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    public void Bind(ItemInstance boundInstance)
    {
        instance = boundInstance;
        upgrade = null;
        if (instance == null)
            TooltipManager.Instance?.ClearOwner(this);
    }

    public void Clear()
    {
        instance = null;
        upgrade = null;
        TooltipManager.Instance?.ClearOwner(this);
    }

    public void BindUpgrade(UpgradeInstance boundUpgrade)
    {
        upgrade = boundUpgrade;
        instance = null;
        if (upgrade == null)
            TooltipManager.Instance?.ClearOwner(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
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

    public bool TryBuildTooltip(out TooltipModel model, out TooltipAnchor anchor)
    {
        model = default;
        anchor = default;

        if (instance == null && upgrade == null)
            return false;

        if (instance != null)
        {
            if (!TryBuildTooltip(TooltipDisplayMode.Item, out model, out anchor))
                return false;
        }
        else
        {
            if (!TryBuildTooltip(TooltipDisplayMode.Upgrade, out model, out anchor))
                return false;
        }

        return true;
    }

    public bool TryBuildTooltip(TooltipDisplayMode mode, out TooltipModel model, out TooltipAnchor anchor)
    {
        model = default;
        anchor = default;

        switch (mode)
        {
            case TooltipDisplayMode.Item:
                if (instance == null)
                    return false;
                model = ItemTooltipUtil.BuildModel(instance);
                break;
            case TooltipDisplayMode.Upgrade:
                var upgradeToShow = upgrade ?? instance?.Upgrade;
                if (upgradeToShow == null)
                    return false;
                model = UpgradeTooltipUtil.BuildModel(upgradeToShow);
                break;
            default:
                return false;
        }

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

    public void Pin()
    {
        if (!TryBuildTooltip(out var model, out var anchor))
            return;

        TooltipManager.Instance?.Pin(this, model, anchor);
    }
}
