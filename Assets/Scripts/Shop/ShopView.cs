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

    [Header("Pin Item UI")] [SerializeField] private PinItemView pinItemPrefab;
    [SerializeField] private Transform pinItemsParent;

    [Header("Ball Item UI")] [SerializeField] private BallItemView ballItemPrefab;
    [SerializeField] private Transform ballItemsParent;

    [Header("Reroll / Close UI")] [SerializeField]
    private LocalizeStringEvent rerollCostText;

    [SerializeField] private Button rerollButton;
    [SerializeField] private Button closeButton;

    readonly List<PinItemView> pinItemViews = new();
    readonly List<BallItemView> ballItemViews = new();

    int selectedPinItemIndex = -1;

    Action<int> onClickPinItem;
    Action<int> onClickBallItem;
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
        pinItemViews.Clear();
        ballItemViews.Clear();

        if (ballItemsParent != null)
        {
            for (int i = ballItemsParent.childCount - 1; i >= 0; i--)
            {
                var child = ballItemsParent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }

        if (pinItemsParent != null)
        {
            for (int i = pinItemsParent.childCount - 1; i >= 0; i--)
            {
                var child = pinItemsParent.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }

    public void SetCallbacks(Action<int> onClickItem, Action onClickReroll, Action onClickClose)
    {
        this.onClickPinItem = onClickItem;
        this.onClickReroll = onClickReroll;
        this.onClickClose = onClickClose;
    }

    public void SetBallCallbacks(Action<int> onClickItem)
    {
        this.onClickBallItem = onClickItem;
    }

    void EnsurePinItemViews(int count)
    {
        if (pinItemPrefab == null || pinItemsParent == null)
        {
            Debug.LogError("[ShopView] pinItemPrefab or pinItemsParent is null.");
            return;
        }

        while (pinItemViews.Count < count)
        {
            var view = Instantiate(pinItemPrefab, pinItemsParent);
            int index = pinItemViews.Count;

            view.SetClickHandler(() =>
            {
                if (onClickPinItem != null)
                    onClickPinItem(index);
            });

            pinItemViews.Add(view);
        }

        for (int i = 0; i < pinItemViews.Count; i++)
        {
            bool active = i < count;
            if (pinItemViews[i] != null)
                pinItemViews[i].gameObject.SetActive(active);
        }
    }

    void EnsureBallItemViews(int count)
    {
        if (ballItemPrefab == null || ballItemsParent == null)
        {
            Debug.LogError("[ShopView] ballItemPrefab or ballItemsParent is null.");
            return;
        }

        while (ballItemViews.Count < count)
        {
            var view = Instantiate(ballItemPrefab, ballItemsParent);
            int index = ballItemViews.Count;

            view.SetClickHandler(() =>
            {
                if (onClickBallItem != null)
                    onClickBallItem(index);
            });

            ballItemViews.Add(view);
        }

        for (int i = 0; i < ballItemViews.Count; i++)
        {
            bool active = i < count;
            if (ballItemViews[i] != null)
                ballItemViews[i].gameObject.SetActive(active);
        }
    }

    public void SetPinItems(PinItemData[] items, int currentCurrency, bool hasEmptySlot, int rerollCost)
    {
        int count = (items != null) ? items.Length : 0;

        EnsurePinItemViews(count);

        if (count != pinItemViews.Count)
            return;

        for (int i = 0; i < count; i++)
        {
            var view = pinItemViews[i];
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
            view.SetSelected(i == selectedPinItemIndex);
        }

        if (rerollCostText != null)
        {
            bool canReroll = currentCurrency >= rerollCost;
            if (rerollCostText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
                sv.Value = rerollCost.ToString();
            rerollCostText.GetComponent<TMP_Text>().color = canReroll ? Colors.Black : Colors.Red;
        }
    }

    public void SetBallItems(BallItemData[] items, int currentCurrency, bool hasDeckSpace)
    {
        int count = (items != null) ? items.Length : 0;

        EnsureBallItemViews(count);

        for (int i = 0; i < count; i++)
        {
            var view = ballItemViews[i];
            if (view == null)
                continue;

            var data = items[i];

            if (!data.hasItem || data.ball == null)
            {
                view.gameObject.SetActive(false);
                continue;
            }

            bool canBuy = !data.sold && hasDeckSpace && currentCurrency >= data.price;

            view.gameObject.SetActive(true);
            view.SetData(data.ball, data.price, canBuy, data.sold);
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
        selectedPinItemIndex = selectedIndex;

        RefreshPinSelectionVisuals();
    }

    void RefreshPinSelectionVisuals()
    {
        for (int i = 0; i < pinItemViews.Count; i++)
        {
            var view = pinItemViews[i];
            if (view == null)
                continue;

            bool shouldSelect = i == selectedPinItemIndex
                                && view.gameObject.activeSelf;
            view.SetSelected(shouldSelect);
        }
    }

    public void ClearPinSelectionVisuals()
    {
        selectedPinItemIndex = -1;
        RefreshPinSelectionVisuals();
    }

    public void RefreshAll()
    {
        RefreshPinSelectionVisuals();
    }
}
