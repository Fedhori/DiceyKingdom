using System;
using System.Collections.Generic;
using Data;
using UnityEngine;
using Random = System.Random;

public sealed class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] ShopView shopView;
    [SerializeField] private GameObject notEnoughBallText;
    [SerializeField] private GameObject ballItemsLayout;

    [SerializeField] int itemsPerShop = 3;
    [SerializeField] int ballItemsPerShop = 2;
    [SerializeField] int baseRerollCost = 1;
    [SerializeField] int rerollCostIncrement = 1;

    bool isOpen;
    ShopOpenContext context;

    readonly List<PinDto> sellablePins = new();
    readonly List<int> tempPinIndices = new();

    readonly List<BallDto> sellableBalls = new();
    readonly List<int> tempBallIndices = new();

    PinItemData[] currentItems;
    BallItemData[] currentBallItems;
    int currentRerollCost;

    public event Action<int> OnSelectionChanged;

    public int CurrentSelectionIndex { get; private set; } = -1;

    bool IsMainStoreContext(ShopOpenContext ctx)
    {
        return ctx == ShopOpenContext.BetweenRounds
               || ctx == ShopOpenContext.AfterStage;
    }

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
            shopView.SetBallCallbacks(OnClickBallItem);
            OnSelectionChanged += shopView.HandleSelectionChanged;
        }
    }

    System.Random Rng =>
        GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();

    public void Open(StageInstance stage, ShopOpenContext context, int nextRoundIndex)
    {
        this.context = context;
        isOpen = true;

        ClearSelection();

        BuildSellablePins();
        BuildSellableBalls();
        EnsureItemArray();
        EnsureBallItemArray();
        currentRerollCost = Mathf.Max(1, baseRerollCost);

        RollPinItems();
        RollBallItems();
        RefreshView();

        if (shopView != null)
            shopView.Show();

        if (stage != null)
        {
            switch (context)
            {
                case ShopOpenContext.BetweenRounds:
                    Debug.Log(
                        $"[ShopManager] Open shop for stage {stage.StageIndex + 1}, before round {nextRoundIndex + 1}");
                    break;
                case ShopOpenContext.AfterStage:
                    Debug.Log($"[ShopManager] Open shop after stage {stage.StageIndex + 1}");
                    break;
                default:
                    Debug.Log($"[ShopManager] Open shop (context: {context})");
                    break;
            }
        }
        else
        {
            Debug.Log("[ShopManager] Open shop (stage is null)");
        }
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

    void BuildSellableBalls()
    {
        sellableBalls.Clear();

        if (!BallRepository.IsInitialized)
        {
            Debug.LogWarning("[ShopManager] BallRepository not initialized.");
            return;
        }

        foreach (var dto in BallRepository.All)
        {
            if (dto == null)
                continue;

            if (dto.isNotSell)
                continue;

            if (!string.IsNullOrEmpty(GameConfig.BasicBallId) && dto.id == GameConfig.BasicBallId)
                continue;

            sellableBalls.Add(dto);
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
            currentItems[i].pin = null; // ← 추가
            currentItems[i].price = 0;
            currentItems[i].sold = false;
        }
    }

    void EnsureBallItemArray()
    {
        if (ballItemsPerShop <= 0)
            ballItemsPerShop = 2;

        if (currentBallItems == null || currentBallItems.Length != ballItemsPerShop)
            currentBallItems = new BallItemData[ballItemsPerShop];

        for (int i = 0; i < currentBallItems.Length; i++)
        {
            currentBallItems[i].hasItem = false;
            currentBallItems[i].ball = null;
            currentBallItems[i].ballCount = 0;
            currentBallItems[i].price = 0;
            currentBallItems[i].sold = false;
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
            currentItems[slot].pin = previewInstance; // ← PinInstance 캐싱
            currentItems[slot].price = previewInstance.Price;
            currentItems[slot].sold = false;
        }
    }

    void RollBallItems()
    {
        var player = PlayerManager.Instance?.Current;

        var deck = player?.BallDeck;
        if (deck == null)
            return;

        int basicCount = deck.GetCount(GameConfig.BasicBallId);
        
        if(!notEnoughBallText)
            notEnoughBallText?.SetActive(basicCount <= 0);
        if(!ballItemsLayout)
            ballItemsLayout?.SetActive(basicCount > 0);
        if (basicCount <= 0)
            return;

        EnsureBallItemArray();

        if (sellableBalls.Count == 0)
            return;

        tempBallIndices.Clear();
        for (int i = 0; i < sellableBalls.Count; i++)
            tempBallIndices.Add(i);

        int count = Mathf.Min(ballItemsPerShop, sellableBalls.Count);

        for (int slot = 0; slot < count; slot++)
        {
            if (tempBallIndices.Count == 0)
                break;

            int pick = Rng.Next(tempBallIndices.Count);
            int ballIndex = tempBallIndices[pick];
            tempBallIndices.RemoveAt(pick);

            var dto = sellableBalls[ballIndex];

            var maxBallCount = (int)Mathf.Min(Mathf.Max(1, GameConfig.MaxBallPrice / dto.price), basicCount);
            var minBallCount = (int)Mathf.Min(Mathf.Max(1, 1 / dto.price), basicCount);

            var ballCount = UnityEngine.Random.Range(minBallCount, maxBallCount + 1);

            var floatPrice = ballCount * dto.price;
            int floor = Mathf.FloorToInt(floatPrice);
            float frac = floatPrice - floor;
            var finalPrice = (UnityEngine.Random.Range(0, frac) < frac) ? floor + 1 : floor;


            currentBallItems[slot].hasItem = true;
            currentBallItems[slot].ball = dto;
            currentBallItems[slot].ballCount = ballCount;
            currentBallItems[slot].price = finalPrice;
            currentBallItems[slot].sold = false;
        }
    }

    public void SetSelection(int itemIndex)
    {
        if (!IsMainStoreContext(context))
            return;

        PinInstance selection = null;

        if (currentItems != null && itemIndex >= 0 && itemIndex < currentItems.Length)
        {
            ref var item = ref currentItems[itemIndex];
            if (item.hasItem && !item.sold && item.pin != null)
            {
                selection = item.pin;
            }
        }

        ApplySelection(selection, itemIndex);
    }

    public void ClearSelection()
    {
        if (!IsMainStoreContext(context))
            return;

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
        if (!isOpen || !IsMainStoreContext(context))
            return false;

        if (CurrentSelectionIndex < 0 || currentItems == null)
            return false;

        if (CurrentSelectionIndex >= currentItems.Length)
            return false;

        ref PinItemData item = ref currentItems[CurrentSelectionIndex];
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

        var pinId = item.pin.Id;
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

        bool hasEmptySlot = PinManager.Instance != null && PinManager.Instance.GetBasicPinSlot(out var x, out var y);

        shopView.SetPinItems(currentItems, currency, hasEmptySlot, currentRerollCost);
        shopView.SetBallItems(currentBallItems, currency);
        shopView.RefreshAll();
    }

    void OnClickItem(int index)
    {
        if (!isOpen)
            return;

        if (!IsMainStoreContext(context))
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

    void OnClickBallItem(int index)
    {
        if (!isOpen)
            return;

        if (!IsMainStoreContext(context))
            return;

        TryPurchaseBallAt(index);
    }

    public bool HasBallDeckSpace(int space)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return false;

        var deck = player.BallDeck;
        if (deck == null)
            return false;

        int basicCount = deck.GetCount(GameConfig.BasicBallId);
        return basicCount >= space;
    }

    void TryPurchaseBallAt(int index)
    {
        if (currentBallItems == null || index < 0 || index >= currentBallItems.Length)
        {
            Debug.LogError("ShopManager: currentBallItems is invalid");
            return;
        }


        ref BallItemData item = ref currentBallItems[index];
        if (!item.hasItem || item.sold || item.ball == null)
        {
            Debug.LogError($"ShopManager: item is invalid. index: {index}");
            return;
        }

        var currencyMgr = CurrencyManager.Instance;
        var player = PlayerManager.Instance?.Current;

        if (currencyMgr == null || player == null)
        {
            Debug.LogError("ShopManager: currencyMgr, player is null");
            return;
        }

        var deck = player.BallDeck;
        if (deck == null)
        {
            Debug.LogError("ShopManager: deck is null");
            return;
        }

        int price = item.price;

        if (currencyMgr.CurrentCurrency < price)
        {
            Debug.LogError("ShopManager: currency unavailable.");
            return;
        }

        if (!HasBallDeckSpace(item.ballCount))
        {
            Debug.LogError("ShopManager: HasBallDeckSpace failed.");
            return;
        }

        if (!currencyMgr.TrySpend(price))
        {
            Debug.LogError("ShopManager: TrySpend failed after spending currency.");
            return;
        }

        if (!deck.TryReplace(item.ball.id, item.ballCount))
        {
            currencyMgr.AddCurrency(price);
            Debug.LogError("ShopManager: TrySpend failed after spending currency. Refunding.");
            return;
        }

        item.sold = true;
        RefreshView();
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
        RollBallItems();
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

        if (shopView != null)
            shopView.Hide();

        ClearSelection();

        Debug.Log("[ShopManager] Close shop");

        FlowManager.Instance?.OnShopClosed(context);
    }

    void OnDisable()
    {
        if (shopView != null)
            OnSelectionChanged -= shopView.HandleSelectionChanged;
    }
}