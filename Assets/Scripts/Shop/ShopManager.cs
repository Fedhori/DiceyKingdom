using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private ShopView shopView;

    [SerializeField] private int itemsPerShop = 3;
    [SerializeField] private int baseRerollCost = 1;
    [SerializeField] private int rerollCostIncrement = 1;

    [Header("Pin Drag Settings")]
    [SerializeField] private LayerMask pinDropLayerMask = ~0; // 월드 핀 콜라이더 레이어

    bool isOpen;

    readonly List<PinDto> sellablePins = new();
    readonly List<int> tempPinIndices = new();

    PinItemData[] currentItems;
    int currentRerollCost;

    public event Action<int> OnSelectionChanged;

    public int CurrentSelectionIndex { get; private set; } = -1;

    int draggingPinIndex = -1;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (shopView != null)
        {
            shopView.SetCallbacks(OnClickItem, OnClickReroll, OnClickCloseButton);
            OnSelectionChanged += shopView.HandleSelectionChanged;
        }
    }

    void Start()
    {
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            player.OnCurrencyChanged += HandleCurrencyChanged;
    }

    void OnDisable()
    {
        if (shopView != null)
            OnSelectionChanged -= shopView.HandleSelectionChanged;

        var player = PlayerManager.Instance?.Current;
        if (player != null)
            player.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    System.Random Rng =>
        GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();

    public void Open(StageInstance stage, int nextRoundIndex)
    {
        isOpen = true;

        ClearSelection();

        BuildSellablePins();
        EnsureItemArray();
        currentRerollCost = Mathf.Max(1, baseRerollCost);

        RollPinItems();
        RefreshView();

        if (shopView != null)
            shopView.Show();
    }

    void BuildSellablePins()
    {
        sellablePins.Clear();

        if (!PinRepository.IsInitialized)
        {
            Debug.LogWarning("[ShopManager] PinRepository not initialized.");
            return;
        }

        string basicId = PinManager.Instance != null ? PinManager.Instance.DefaultPinId : null;

        foreach (var dto in PinRepository.All)
        {
            if (dto == null)
                continue;

            if (dto.isNotSell)
                continue;

            if (!string.IsNullOrEmpty(basicId) && dto.id == basicId)
                continue;

            sellablePins.Add(dto);
        }
    }

    void EnsureItemArray()
    {
        if (itemsPerShop <= 0)
            itemsPerShop = 3;

        if (currentItems == null || currentItems.Length != itemsPerShop)
            currentItems = new PinItemData[itemsPerShop];

        for (int i = 0; i < currentItems.Length; i++)
        {
            currentItems[i].hasItem = false;
            currentItems[i].pin = null;
            currentItems[i].price = 0;
            currentItems[i].sold = false;
        }
    }

    void RollPinItems()
    {
        EnsureItemArray();

        if (sellablePins.Count == 0)
            return;

        tempPinIndices.Clear();
        for (int i = 0; i < sellablePins.Count; i++)
            tempPinIndices.Add(i);

        int count = Mathf.Min(itemsPerShop, sellablePins.Count);

        for (int slot = 0; slot < count; slot++)
        {
            if (tempPinIndices.Count == 0)
                break;

            int pick = Rng.Next(tempPinIndices.Count);
            int pinIndex = tempPinIndices[pick];
            tempPinIndices.RemoveAt(pick);

            var dto = sellablePins[pinIndex];
            var previewInstance = new PinInstance(dto, -1, -1, registerEventEffects: false);

            currentItems[slot].hasItem = true;
            currentItems[slot].pin = previewInstance;
            currentItems[slot].price = previewInstance.Price;
            currentItems[slot].sold = false;
        }
    }

    public void SetSelection(int itemIndex)
    {
        PinInstance selection = null;

        if (currentItems != null && itemIndex >= 0 && itemIndex < currentItems.Length)
        {
            ref var item = ref currentItems[itemIndex];
            if (item.hasItem && !item.sold && item.pin != null)
                selection = item.pin;
        }

        ApplySelection(selection, itemIndex);
    }

    public void ClearSelection()
    {
        ApplySelection(null, -1);
        shopView?.ClearPinSelectionVisuals();
    }

    void ApplySelection(PinInstance selection, int itemIndex)
    {
        CurrentSelectionIndex = selection != null ? itemIndex : -1;
        OnSelectionChanged?.Invoke(CurrentSelectionIndex);
    }

    public bool TryPurchaseSelectedAt(int row, int col)
    {
        if (!isOpen)
            return false;

        if (CurrentSelectionIndex < 0 || currentItems == null)
            return false;

        if (CurrentSelectionIndex >= currentItems.Length)
            return false;

        return TryPurchasePinItemAt(CurrentSelectionIndex, row, col);
    }

    bool TryPurchasePinItemAt(int itemIndex, int row, int col)
    {
        if (!isOpen)
            return false;

        if (currentItems == null || itemIndex < 0 || itemIndex >= currentItems.Length)
            return false;

        ref PinItemData item = ref currentItems[itemIndex];
        if (!item.hasItem || item.sold || item.pin == null)
            return false;

        var currencyMgr = CurrencyManager.Instance;
        var pinMgr = PinManager.Instance;

        if (currencyMgr == null || pinMgr == null)
            return false;

        var rows = pinMgr.PinsByRow;
        if (row < 0 || row >= rows.Count)
            return false;

        var rowList = rows[row];
        if (rowList == null || col < 0 || col >= rowList.Count)
            return false;

        var targetPin = rowList[col];
        if (targetPin == null || targetPin.Instance == null)
            return false;

        // 기본 핀만 교체
        if (targetPin.Instance.Id != pinMgr.DefaultPinId)
            return false;

        int price = item.price;
        if (currencyMgr.CurrentCurrency < price)
        {
            RefreshView();
            return false;
        }

        if (!currencyMgr.TrySpend(price))
        {
            RefreshView();
            return false;
        }

        string pinId = item.pin.Id;
        if (string.IsNullOrEmpty(pinId) || !pinMgr.TryReplace(pinId, row, col))
        {
            Debug.LogError("[ShopManager] TryReplace failed after spending currency. Refunding.");
            currencyMgr.AddCurrency(price);
            RefreshView();
            return false;
        }

        item.sold = true;
        ClearSelection();
        RefreshView();
        return true;
    }

    void RefreshView()
    {
        if (shopView == null)
            return;

        int currency = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.CurrentCurrency
            : 0;

        bool hasEmptySlot = PinManager.Instance != null &&
                            PinManager.Instance.GetBasicPinSlot(out _, out _);

        shopView.SetPinItems(currentItems, currency, hasEmptySlot, currentRerollCost);
        shopView.RefreshAll();
    }

    void OnClickItem(int index)
    {
        if (!isOpen)
            return;

        if (currentItems == null || index < 0 || index >= currentItems.Length)
            return;

        ref PinItemData item = ref currentItems[index];

        if (!item.hasItem || item.sold || item.pin == null)
            return;

        if (CurrentSelectionIndex == index)
            ClearSelection();
        else
            SetSelection(index);
    }

    void OnClickReroll()
    {
        if (!isOpen)
            return;

        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
        {
            RefreshView();
            return;
        }

        int cost = Mathf.Max(1, currentRerollCost);

        if (!currencyMgr.TrySpend(cost))
        {
            RefreshView();
            return;
        }

        currentRerollCost = Mathf.Max(1, currentRerollCost + Mathf.Max(1, rerollCostIncrement));

        RollPinItems();
        RefreshView();
    }

    void OnClickCloseButton()
    {
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;

        draggingPinIndex = -1;

        if (shopView != null)
        {
            shopView.Hide();
            shopView.HidePinDragGhost();
        }

        ClearSelection();

        FlowManager.Instance?.OnShopClosed();
    }

    void HandleCurrencyChanged(int value)
    {
        RefreshView();
    }

    // ======================
    // Pin drag (신규)
    // ======================

    public void BeginPinDrag(int itemIndex, Vector2 screenPos)
    {
        if (!isOpen || shopView == null)
            return;

        if (currentItems == null || itemIndex < 0 || itemIndex >= currentItems.Length)
            return;

        ref PinItemData item = ref currentItems[itemIndex];
        if (!item.hasItem || item.sold || item.pin == null)
            return;

        var flow = FlowManager.Instance;
        if (flow != null && flow.CurrentPhase != FlowPhase.Shop)
            return;

        // 드래그 시작 시 해당 상품 선택 상태로 만들어 기본 핀 하이라이트 유지
        SetSelection(itemIndex);

        draggingPinIndex = itemIndex;
        shopView.ShowPinDragGhost(item.pin, screenPos);
    }

    public void UpdatePinDrag(int itemIndex, Vector2 screenPos)
    {
        if (!isOpen || shopView == null)
            return;

        if (itemIndex != draggingPinIndex)
            return;

        shopView.UpdatePinDragGhostPosition(screenPos);
    }

    public void EndPinDrag(int itemIndex, Vector2 screenPos)
    {
        if (shopView == null)
        {
            draggingPinIndex = -1;
            return;
        }

        if (!isOpen || itemIndex != draggingPinIndex)
        {
            shopView.HidePinDragGhost();
            draggingPinIndex = -1;
            return;
        }

        if (TryGetTargetPinFromScreenPos(screenPos, out var targetPin))
        {
            // 기본 핀만 교체 가능 → 실제 체크는 TryPurchasePinItemAt 내부에서도 한 번 더 한다.
            TryPurchasePinItemAt(itemIndex, targetPin.RowIndex, targetPin.ColumnIndex);
        }

        shopView.HidePinDragGhost();
        draggingPinIndex = -1;
    }

    bool TryGetTargetPinFromScreenPos(Vector2 screenPos, out PinController pin)
    {
        pin = null;

        var cam = Camera.main;
        if (cam == null)
            return false;

        Vector3 worldPos3 = cam.ScreenToWorldPoint(screenPos);
        Vector2 worldPos = worldPos3;

        var hit = Physics2D.OverlapPoint(worldPos, pinDropLayerMask);
        if (hit == null)
            return false;

        pin = hit.GetComponentInParent<PinController>();
        return pin != null;
    }
}
