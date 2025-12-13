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
    [Header("Ball Drag UI")]
    [SerializeField] private Canvas rootCanvas;
    [SerializeField] private GameObject ballDragHintRoot;   // "이곳으로 드래그해 구매" 텍스트 루트
    [SerializeField] private RectTransform ballDropZoneArea; // 드롭 존 영역
    [SerializeField] private Image ballDragGhostImage;      // 마우스를 따라다닐 고스트 아이콘
    
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

        if (ballDragHintRoot != null)
            ballDragHintRoot.SetActive(false);
        if (ballDragGhostImage != null)
            ballDragGhostImage.gameObject.SetActive(false);
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
            view.SetIndex(index);
            ballItemViews.Add(view);
        }

        for (int i = 0; i < ballItemViews.Count; i++)
        {
            bool active = i < count;
            var view = ballItemViews[i];
            if (view != null)
            {
                view.gameObject.SetActive(active);
                view.SetIndex(i); // 재사용 시 인덱스 재설정
            }
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

    public void SetBallItems(BallItemData[] items, int currentCurrency)
    {
        if (items == null)
            return;

        int count = items.Length;

        EnsureBallItemViews(count);

        var shopManager = ShopManager.Instance;
        if (shopManager == null)
        {
            Debug.LogError("[ShopView] shopManager is null.");
            return;
        }
        
        for (int i = 0; i < count; i++)
        {
            var view = ballItemViews[i];
            if (view == null)
                continue;

            if (i >= items.Length)
                break;
            
            var data = items[i];

            if (!data.hasItem || data.ball == null)
            {
                view.gameObject.SetActive(false);
                continue;
            }
            
            bool canBuy = !data.sold && shopManager.HasBallDeckSpace(data.ballCount) && currentCurrency >= data.price;

            view.gameObject.SetActive(true);
            view.SetData(data.ball, data.ballCount, data.price, canBuy, data.sold);
        }
    }
    
    public void ShowBallDragHint(BallDto ball, Vector2 screenPos)
    {
        if (ballDragGhostImage != null)
        {
            ballDragGhostImage.sprite = SpriteCache.GetBallSprite(ball.id);
            ballDragGhostImage.gameObject.SetActive(true);
        }

        if (ballDragHintRoot != null)
            ballDragHintRoot.SetActive(true);

        UpdateBallDragGhostPosition(screenPos);
    }

    public void UpdateBallDragGhostPosition(Vector2 screenPos)
    {
        if (ballDragGhostImage == null)
            return;

        var rectTransform = ballDragGhostImage.rectTransform;

        // 이 고스트가 속한 Canvas 찾기
        Canvas canvas = rootCanvas != null 
            ? rootCanvas 
            : rectTransform.GetComponentInParent<Canvas>();

        if (canvas == null)
            return;

        Camera cam = null;
        if (canvas.renderMode == RenderMode.ScreenSpaceCamera ||
            canvas.renderMode == RenderMode.WorldSpace)
        {
            cam = canvas.worldCamera;
        }

        // 스크린 좌표 → 이 RectTransform 평면상의 월드 좌표
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                rectTransform, screenPos, cam, out var worldPos))
        {
            rectTransform.position = worldPos;
        }
    }

    public bool IsInBallDropZone(Vector2 screenPos)
    {
        if (ballDropZoneArea == null || rootCanvas == null)
            return false;

        var cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        return RectTransformUtility.RectangleContainsScreenPoint(
            ballDropZoneArea, screenPos, cam);
    }

    public void HideBallDragHint()
    {
        if (ballDragGhostImage != null)
            ballDragGhostImage.gameObject.SetActive(false);
        if (ballDragHintRoot != null)
            ballDragHintRoot.SetActive(false);
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

        HideBallDragHint();
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
