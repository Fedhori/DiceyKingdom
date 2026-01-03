using System;
using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

public sealed class ShopView : MonoBehaviour
{
    [Header("Overlay Root")] [SerializeField]
    private GameObject shopOverlay;

    [Header("Item UI")] [SerializeField] private ProductController itemProductPrefab;
    [SerializeField] private Transform productsParent;

    [Header("Reroll / Close UI")] [SerializeField]
    private LocalizeStringEvent rerollCostText;

    [SerializeField] private Button rerollButton;
    [SerializeField] private Button closeButton;

    readonly List<ProductController> itemViews = new();

    int selectedItemIndex = -1;

    Action<int> onClickItem;
    Action<int, Vector2> onBeginDragItem;
    Action<int, Vector2> onDragItem;
    Action<int, Vector2> onEndDragItem;

    void Awake()
    {
        ClearEditorPlacedItems();

        if (shopOverlay != null)
            shopOverlay.SetActive(false);
    }

    void ClearEditorPlacedItems()
    {
        itemViews.Clear();

        if (productsParent != null)
        {
            for (int i = productsParent.childCount - 1; i >= 0; i--)
            {
                var child = productsParent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }

    public void SetCallbacks(Action<int> onClickItem,
        Action<int, Vector2> onBeginDragItem, Action<int, Vector2> onDragItem, Action<int, Vector2> onEndDragItem)
    {
        this.onClickItem = onClickItem;
        this.onBeginDragItem = onBeginDragItem;
        this.onDragItem = onDragItem;
        this.onEndDragItem = onEndDragItem;
    }

    void EnsureItemViews(IProduct[] items)
    {
        if (productsParent == null)
            return;

        int count = items != null ? items.Length : 0;

        // Ensure list size
        while (itemViews.Count < count)
            itemViews.Add(null);

        for (int i = 0; i < count; i++)
        {
            var desiredType = items?[i]?.ProductType ?? ProductType.Item;
            var currentView = itemViews[i];

            bool needNew = currentView == null || currentView.ViewType != desiredType;
            if (needNew)
            {
                if (currentView != null)
                    Destroy(currentView.gameObject);

                var prefab = GetPrefab(desiredType);
                if (prefab == null)
                    continue;

                var view = Instantiate(prefab, productsParent);
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

    ProductController GetPrefab(ProductType type)
    {
        return type switch
        {
            ProductType.Item => itemProductPrefab,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public void SetItems(IProduct[] items, int currentCurrency, bool hasEmptyItemSlot, int rerollCost)
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

            var canBuy = false;
            if (item.ProductType == ProductType.Item)
                canBuy = !sold && hasEmptyItemSlot && currentCurrency >= item.Price;

            view.gameObject.SetActive(true);
            view.SetData(item, item.Price, canBuy, sold);
            view.SetSelected(i == selectedItemIndex);
        }

        if (rerollCostText != null)
        {
            bool canReroll = currentCurrency >= rerollCost;
            if (rerollCostText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
                sv.Value = rerollCost.ToString();
            rerollCostText.GetComponent<TMP_Text>().color = canReroll ? Colors.White : Colors.Red;
        }
    }

    public void Open()
    {
        if (shopOverlay != null)
            shopOverlay.SetActive(true);
    }

    public void Close()
    {
        if (shopOverlay != null)
            shopOverlay.SetActive(false);
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

    public void ShowItemDragGhost(IProduct item, Vector2 screenPos)
    {
        var rarity = ItemRarity.Common;
        if (item is ItemProduct itemProduct)
            rarity = itemProduct.Rarity;

        GhostManager.Instance?.ShowGhost(item?.Icon, screenPos,
            item?.ProductType == ProductType.Item ? GhostKind.Item : GhostKind.Pin, rarity);
    }

    public void UpdateItemDragGhostPosition(Vector2 screenPos)
    {
        GhostManager.Instance?.UpdateGhostPosition(screenPos);
    }

    public void HideItemDragGhost()
    {
        GhostManager.Instance?.HideGhost();
    }
}
