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
    [SerializeField] private GameObject overlayRoot;

    [Header("Pin Item UI")]
    [SerializeField] private PinItemView pinItemPrefab;
    [SerializeField] private Transform pinItemsParent;

    [Header("Reroll / Close UI")]
    [SerializeField] private LocalizeStringEvent rerollCostText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private Button closeButton;

    [Header("Pin Drag UI")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private Image dragGhostImage;

    readonly List<PinItemView> pinItemViews = new();

    int selectedPinItemIndex = -1;

    Action<int> onClickPinItem;
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

        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        if (dragGhostImage != null)
            dragGhostImage.gameObject.SetActive(false);
    }

    void ClearEditorPlacedItems()
    {
        pinItemViews.Clear();

        if (pinItemsParent != null)
        {
            for (int i = pinItemsParent.childCount - 1; i >= 0; i--)
            {
                var child = pinItemsParent.GetChild(i);
                if (child != null)
                    Destroy(child.gameObject);
            }
        }
    }

    public void SetCallbacks(Action<int> onClickItem, Action onClickReroll, Action onClickClose)
    {
        this.onClickPinItem = onClickItem;
        this.onClickReroll = onClickReroll;
        this.onClickClose = onClickClose;
    }

    void EnsurePinItemViews(int count)
    {
        if (pinItemPrefab == null || pinItemsParent == null)
            return;

        while (pinItemViews.Count < count)
        {
            var view = Instantiate(pinItemPrefab, pinItemsParent);
            int index = pinItemViews.Count;

            view.SetIndex(index);

            view.SetClickHandler(() =>
            {
                onClickPinItem?.Invoke(index);
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
            view.SetData(data.pin, data.price, canBuy, data.sold);
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

            bool shouldSelect = i == selectedPinItemIndex && view.gameObject.activeSelf;
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

    // ======================
    // Pin drag UI
    // ======================

    public void ShowPinDragGhost(PinInstance pin, Vector2 screenPos)
    {
        if (dragGhostImage == null || pin == null)
            return;

        dragGhostImage.sprite = SpriteCache.GetPinSprite(pin.Id);
        dragGhostImage.gameObject.SetActive(true);
        UpdateDragGhostPosition(screenPos);
    }

    public void UpdatePinDragGhostPosition(Vector2 screenPos)
    {
        UpdateDragGhostPosition(screenPos);
    }

    public void HidePinDragGhost()
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
