using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

public sealed class ShopView : MonoBehaviour
{
    [Header("Overlay Root")]
    [SerializeField] private GameObject shopClosedOverlay;

    [Header("Item UI")]
    [SerializeField] private ShopItemView pinItemPrefab;
    [SerializeField] private ShopItemView tokenItemPrefab;
    [SerializeField] private Transform itemsParent;

    [Header("Reroll / Close UI")]
    [SerializeField] private LocalizeStringEvent rerollCostText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button closeButton;

    [Header("Pin Drag UI")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Image dragGhostImage;

    readonly List<ShopItemView> itemViews = new();

    int selectedItemIndex = -1;

    Action<int> onClickItem;
    Action<int, Vector2> onBeginDragItem;
    Action<int, Vector2> onDragItem;
    Action<int, Vector2> onEndDragItem;
    Action onClickReroll;
    Action onClickClose;

    void Awake()
    {
        ClearEditorPlacedItems();

        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(() => onClickReroll?.Invoke());
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() => onClickClose?.Invoke());
        }

        if (shopClosedOverlay != null)
            shopClosedOverlay.SetActive(true);

        if (dragGhostImage != null)
            dragGhostImage.gameObject.SetActive(false);
    }

    void ClearEditorPlacedItems()
    {
        itemViews.Clear();

        if (itemsParent != null)
        {
            for (int i = itemsParent.childCount - 1; i >= 0; i--)
            {
                var child = itemsParent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }

    public void SetCallbacks(Action<int> onClickItem, Action onClickReroll, Action onClickClose,
        Action<int, Vector2> onBeginDragItem, Action<int, Vector2> onDragItem, Action<int, Vector2> onEndDragItem)
    {
        this.onClickItem = onClickItem;
        this.onClickReroll = onClickReroll;
        this.onClickClose = onClickClose;
        this.onBeginDragItem = onBeginDragItem;
        this.onDragItem = onDragItem;
        this.onEndDragItem = onEndDragItem;
    }

    void EnsureItemViews(IShopItem[] items)
    {
        if (itemsParent == null)
            return;

        int count = items != null ? items.Length : 0;

        // Ensure list size
        while (itemViews.Count < count)
            itemViews.Add(null);

        for (int i = 0; i < count; i++)
        {
            var desiredType = items != null && items[i] != null ? items[i].ItemType : ShopItemType.Pin;
            var currentView = itemViews[i];

            bool needNew = currentView == null || currentView.ViewType != desiredType;
            if (needNew)
            {
                if (currentView != null)
                    Destroy(currentView.gameObject);

                var prefab = GetPrefab(desiredType);
                if (prefab == null)
                    continue;

                var view = Instantiate(prefab, itemsParent);
                view.SetViewType(desiredType);
                view.SetIndex(i);
                view.SetHandlers(
                    click: idx => onClickItem?.Invoke(idx),
                    beginDrag: (idx, pos) => onBeginDragItem?.Invoke(idx, pos),
                    drag: (idx, pos) => onDragItem?.Invoke(idx, pos),
                    endDrag: (idx, pos) => onEndDragItem?.Invoke(idx, pos)
                );
                view.transform.SetSiblingIndex(i);
                itemViews[i] = view;
            }
            else
            {
                currentView.SetIndex(i);
                currentView.transform.SetSiblingIndex(i);
            }

            if (itemViews[i] != null)
                itemViews[i].gameObject.SetActive(true);
        }

        // Deactivate extra views
        for (int i = count; i < itemViews.Count; i++)
        {
            if (itemViews[i] != null)
                itemViews[i].gameObject.SetActive(false);
        }
    }

    ShopItemView GetPrefab(ShopItemType type)
    {
        return type switch
        {
            ShopItemType.Token => tokenItemPrefab != null ? tokenItemPrefab : pinItemPrefab,
            _ => pinItemPrefab
        };
    }

    public void SetItems(IShopItem[] items, int currentCurrency, bool hasEmptyPinSlot, bool hasEmptyTokenSlot, int rerollCost)
    {
        int count = (items != null) ? items.Length : 0;

        EnsureItemViews(items);

        for (int i = 0; i < count; i++)
        {
            var view = i < itemViews.Count ? itemViews[i] : null;
            if (view == null)
                continue;

            var item = items[i];
            bool sold = item != null && item.Sold;
            if (item == null)
            {
                view.gameObject.SetActive(false);
                continue;
            }

            bool canBuy;
            if (item.ItemType == ShopItemType.Pin)
                canBuy = !sold && hasEmptyPinSlot && currentCurrency >= item.Price;
            else
                canBuy = !sold && hasEmptyTokenSlot && currentCurrency >= item.Price;

            view.gameObject.SetActive(true);
            view.SetData(item, item.Price, canBuy, sold);
            view.SetSelected(i == selectedItemIndex);
        }

        if (rerollCostText != null)
        {
            bool canReroll = currentCurrency >= rerollCost;
            if (rerollCostText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
                sv.Value = rerollCost.ToString();
            rerollCostText.GetComponent<TMP_Text>().color = canReroll ? Colors.Black : Colors.Red;
        }
    }

    public void Open()
    {
        if (shopClosedOverlay != null)
            shopClosedOverlay.SetActive(false);
    }

    public void Close()
    {
        if (shopClosedOverlay != null)
            shopClosedOverlay.SetActive(true);
    }

    public void HandleSelectionChanged(int selectedIndex)
    {
        selectedItemIndex = selectedIndex;
        RefreshItemSelectionVisuals();
    }

    void RefreshItemSelectionVisuals()
    {
        for (int i = 0; i < itemViews.Count; i++)
        {
            var view = itemViews[i];
            if (view == null)
                continue;

            bool shouldSelect = i == selectedItemIndex && view.gameObject.activeSelf;
            view.SetSelected(shouldSelect);
        }
    }

    public void ClearSelectionVisuals()
    {
        selectedItemIndex = -1;
        RefreshItemSelectionVisuals();
    }

    public void RefreshAll()
    {
        RefreshItemSelectionVisuals();
    }

    // ======================
    // Pin drag UI
    // ======================

    public void ShowItemDragGhost(IShopItem item, Vector2 screenPos)
    {
        if (dragGhostImage == null || item == null)
            return;

        dragGhostImage.sprite = item.Icon;
        dragGhostImage.gameObject.SetActive(true);
        UpdateDragGhostPosition(screenPos);
    }

    public void UpdateItemDragGhostPosition(Vector2 screenPos)
    {
        UpdateDragGhostPosition(screenPos);
    }

    public void HideItemDragGhost()
    {
        if (dragGhostImage != null)
            dragGhostImage.gameObject.SetActive(false);
    }

    void UpdateDragGhostPosition(Vector2 screenPos)
    {
        if (dragGhostImage == null)
            return;

        var rectTransform = dragGhostImage.rectTransform;

        Canvas canvas = rootCanvas != null ? rootCanvas : rectTransform.GetComponentInParent<Canvas>();
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
