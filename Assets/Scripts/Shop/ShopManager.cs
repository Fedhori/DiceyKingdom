using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] ShopView shopView;

    [SerializeField] int itemsPerShop = 3;
    [SerializeField] int baseRerollCost = 1;
    [SerializeField] int rerollCostIncrement = 1;

    bool isOpen;
    StageInstance currentStage;
    int nextRoundIndex;
    ShopOpenContext context;

    readonly List<PinDto> sellablePins = new();
    readonly List<int> tempIndices = new();

    ShopItemData[] currentItems;
    int currentRerollCost;

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
        }
    }

    System.Random Rng =>
        GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();

    public void Open(StageInstance stage, ShopOpenContext context, int nextRoundIndex)
    {
        currentStage = stage;
        this.context = context;
        this.nextRoundIndex = nextRoundIndex;
        isOpen = true;

        BuildSellablePins();
        EnsureItemArray();
        currentRerollCost = Mathf.Max(1, baseRerollCost);

        RollItems();
        RefreshView();

        if (shopView != null)
            shopView.Show();

        if (stage != null)
        {
            switch (context)
            {
                case ShopOpenContext.BetweenRounds:
                    Debug.Log($"[ShopManager] Open shop for stage {stage.StageIndex + 1}, before round {nextRoundIndex + 1}");
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

    void EnsureItemArray()
    {
        if (itemsPerShop <= 0)
            itemsPerShop = 3;

        if (currentItems == null || currentItems.Length != itemsPerShop)
            currentItems = new ShopItemData[itemsPerShop];

        for (int i = 0; i < currentItems.Length; i++)
        {
            currentItems[i].hasItem = false;
            currentItems[i].pinId = null;
            currentItems[i].price = 0;
            currentItems[i].sold = false;
        }
    }

    void RollItems()
    {
        EnsureItemArray();

        if (sellablePins.Count == 0)
            return;

        tempIndices.Clear();
        for (int i = 0; i < sellablePins.Count; i++)
            tempIndices.Add(i);

        int count = Mathf.Min(itemsPerShop, sellablePins.Count);

        for (int slot = 0; slot < count; slot++)
        {
            if (tempIndices.Count == 0)
                break;

            int pick = Rng.Next(tempIndices.Count);
            int pinIndex = tempIndices[pick];
            tempIndices.RemoveAt(pick);

            var dto = sellablePins[pinIndex];

            currentItems[slot].hasItem = true;
            currentItems[slot].pinId = dto.id;
            currentItems[slot].price = dto.price;
            currentItems[slot].sold = false;
        }
    }

    void RefreshView()
    {
        if (shopView == null)
            return;

        int currency = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.CurrentCurrency
            : 0;

        bool hasEmptySlot = PinManager.Instance != null && PinManager.Instance.HasBasicPinSlot();

        shopView.SetItems(currentItems, currency, hasEmptySlot, currentRerollCost);
    }

    void OnClickItem(int index)
    {
        if (!isOpen)
            return;

        if (currentItems == null || index < 0 || index >= currentItems.Length)
            return;

        ref ShopItemData item = ref currentItems[index];

        if (!item.hasItem || item.sold)
            return;

        var currencyMgr = CurrencyManager.Instance;
        var pinMgr = PinManager.Instance;

        if (currencyMgr == null || pinMgr == null)
            return;

        int currency = currencyMgr.CurrentCurrency;
        bool hasEmptySlot = pinMgr.HasBasicPinSlot();

        if (!hasEmptySlot || currency < item.price)
        {
            RefreshView();
            return;
        }

        if (!currencyMgr.TrySpend(item.price))
        {
            RefreshView();
            return;
        }

        if (!pinMgr.TryReplaceBasicPin(item.pinId))
        {
            Debug.LogError("[ShopManager] TryReplaceBasicPin failed after spending currency. Refunding.");
            currencyMgr.AddCurrency(item.price);
            RefreshView();
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

        RollItems();
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

        Debug.Log("[ShopManager] Close shop");

        FlowManager.Instance?.OnShopClosed(context);
    }
}
