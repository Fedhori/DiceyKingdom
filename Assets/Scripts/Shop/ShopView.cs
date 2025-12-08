using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

public sealed class ShopView : MonoBehaviour
{
    [Header("Overlay Root")] [SerializeField]
    private GameObject overlayRoot;

    [Header("Item UI")] [SerializeField] private ShopItemView itemPrefab;
    [SerializeField] private Transform itemsParent;

    [Header("Reroll / Close UI")] [SerializeField]
    private LocalizeStringEvent rerollCostText;

    [SerializeField] private Button rerollButton;
    [SerializeField] private Button closeButton;

    readonly List<ShopItemView> itemViews = new();

    int currentSelectedIndex = -1;

    Action<int> onClickItem;
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

        // ShopView GameObject는 항상 Active로 두고,
        // 실제로 열고 닫는 건 overlayRoot만 제어
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    void ClearEditorPlacedItems()
    {
        itemViews.Clear();

        if (itemsParent == null)
            return;

        for (int i = itemsParent.childCount - 1; i >= 0; i--)
        {
            var child = itemsParent.GetChild(i);
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public void SetCallbacks(Action<int> onClickItem, Action onClickReroll, Action onClickClose)
    {
        this.onClickItem = onClickItem;
        this.onClickReroll = onClickReroll;
        this.onClickClose = onClickClose;
    }

    void EnsureItemViews(int count)
    {
        if (itemPrefab == null || itemsParent == null)
        {
            Debug.LogError("[ShopView] itemPrefab or itemsParent is null.");
            return;
        }

        while (itemViews.Count < count)
        {
            var view = Instantiate(itemPrefab, itemsParent);
            int index = itemViews.Count;

            view.SetClickHandler(() =>
            {
                if (onClickItem != null)
                    onClickItem(index);
            });

            itemViews.Add(view);
        }

        for (int i = 0; i < itemViews.Count; i++)
        {
            bool active = i < count;
            if (itemViews[i] != null)
                itemViews[i].gameObject.SetActive(active);
        }
    }

    public void SetItems(ShopItemData[] items, int currentCurrency, bool hasEmptySlot, int rerollCost)
    {
        int count = (items != null) ? items.Length : 0;

        EnsureItemViews(count);

        for (int i = 0; i < count; i++)
        {
            var view = itemViews[i];
            if (view == null)
                continue;

            var data = items[i];

            if (!data.hasItem || data.pin == null)
            {
                view.gameObject.SetActive(false);
                continue;
            }

            bool canBuy = !data.sold && hasEmptySlot && currentCurrency >= data.price;

            view.gameObject.SetActive(true);
            view.SetData(data.pin, data.price, canBuy, data.sold);   // ← PinInstance 넘김
            view.SetSelected(i == currentSelectedIndex);
        }

        if (rerollCostText != null)
        {
            bool canReroll = currentCurrency >= rerollCost;
            if (rerollCostText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
                sv.Value = rerollCost.ToString();
            rerollCostText.GetComponent<TMP_Text>().color = canReroll ? Colors.Black : Colors.Red;
        }
    }

    public void Show()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    public void Hide()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    public void HandleSelectionChanged(int selectedIndex)
    {
        currentSelectedIndex = selectedIndex;

        RefreshSelectionVisuals();
    }

    void RefreshSelectionVisuals()
    {
        for (int i = 0; i < itemViews.Count; i++)
        {
            var view = itemViews[i];
            if (view == null)
                continue;

            bool shouldSelect = i == currentSelectedIndex
                                && view.gameObject.activeSelf;
            view.SetSelected(shouldSelect);
        }
    }

    public void ClearSelectionVisuals()
    {
        currentSelectedIndex = -1;
        RefreshSelectionVisuals();
    }

    public void RefreshAll()
    {
        RefreshSelectionVisuals();
    }
}
