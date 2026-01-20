using Data;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class ItemTooltipTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public enum TooltipActionKind
    {
        None = 0,
        BuyUpgrade,
        SellUpgrade
    }

    [SerializeField] private TooltipAnchorType anchorType = TooltipAnchorType.Screen;
    [SerializeField] RectTransform anchorRect;

    ItemInstance instance;
    UpgradeInstance upgrade;
    TooltipActionKind actionKind = TooltipActionKind.None;
    object actionSource;
    readonly Vector3[] corners = new Vector3[4];

    public bool HasUpgradeToggle => instance != null && instance.Upgrades.Count > 0;
    public ItemInstance BoundItem => instance;
    public TooltipActionKind ActionKind => actionKind;
    public object ActionSource => actionSource;

    void Awake()
    {
        if (anchorRect == null)
            anchorRect = transform as RectTransform;
    }

    public void Bind(ItemInstance boundInstance)
    {
        instance = boundInstance;
        upgrade = null;
        actionKind = TooltipActionKind.None;
        actionSource = null;
        if (instance == null)
            TooltipManager.Instance?.ClearOwner(this);
    }

    public void Clear()
    {
        instance = null;
        upgrade = null;
        actionKind = TooltipActionKind.None;
        actionSource = null;
        TooltipManager.Instance?.ClearOwner(this);
    }

    public void BindUpgrade(UpgradeInstance boundUpgrade, TooltipActionKind actionKind = TooltipActionKind.None, object actionSource = null)
    {
        upgrade = boundUpgrade;
        instance = null;
        this.actionKind = actionKind;
        this.actionSource = actionSource;
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
                model = ItemTooltipUtil.BuildModel(instance, BuildItemButtonConfig());
                break;
            case TooltipDisplayMode.Upgrade:
                var upgradeToShow = upgrade ?? instance?.Upgrade;
                if (upgradeToShow == null)
                    return false;
                model = UpgradeTooltipUtil.BuildModel(upgradeToShow, BuildUpgradeButtonConfig());
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

    TooltipButtonConfig BuildItemButtonConfig()
    {
        if (instance == null || instance.Upgrades.Count == 0)
            return null;

        return new TooltipButtonConfig(
            "tooltip.upgrade.view",
            Colors.Upgrade,
            true,
            () =>
            {
                UpgradePanelEvents.RaiseTooltipDismissRequested();
                UpgradePanelEvents.RaiseToggleRequested(instance);
            });
    }

    TooltipButtonConfig BuildUpgradeButtonConfig()
    {
        switch (actionKind)
        {
            case TooltipActionKind.BuyUpgrade:
                var shopUpgrade = actionSource as UpgradeProduct;
                if (shopUpgrade == null)
                    return null;

                var shop = ShopManager.Instance;
                bool canBuy = shop != null && shop.CanPurchaseUpgradeToInventory(shopUpgrade);
                return new TooltipButtonConfig(
                    "tooltip.buy.label",
                    Colors.Currency,
                    canBuy,
                    () => ShopManager.Instance?.TryPurchaseUpgradeToInventory(shopUpgrade));
            case TooltipActionKind.SellUpgrade:
                var ownedUpgrade = actionSource as UpgradeInstance;
                if (ownedUpgrade == null)
                    return null;

                return new TooltipButtonConfig(
                    "tooltip.sell.label",
                    Colors.Currency,
                    true,
                    () => UpgradeInventoryManager.Instance?.TrySellUpgrade(ownedUpgrade));
            default:
                return null;
        }
    }

    public void Pin()
    {
        if (!TryBuildTooltip(out var model, out var anchor))
            return;

        TooltipManager.Instance?.Pin(this, model, anchor);
    }
}
