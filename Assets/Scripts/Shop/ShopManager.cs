using System;
using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class ShopManager : MonoBehaviour
{
    public static ShopManager Instance { get; private set; }

    [SerializeField] private ShopView shopView;

    [SerializeField] private int itemsPerShop = 6;
    [SerializeField] private int baseRerollCost = 1;
    [SerializeField] private int rerollCostIncrement = 1;

    [Header("Mixed Item Probabilities (weight-based)")]
    [SerializeField] private ProductProbability[] itemProbabilities =
    {
        new ProductProbability { type = ProductType.Item, weight = 100 }
    };

    bool isOpen;

    readonly List<IProduct> rosterItems = new();
    readonly List<ItemDto> sellableItems = new();
    readonly HashSet<string> rosterItemIds = new();
    readonly HashSet<string> ownedItemIds = new();

    IProduct[] currentShopItems;
    int currentRerollCost;

    public event Action<int> OnSelectionChanged;

    public int CurrentSelectionIndex { get; private set; } = -1;

    int draggingItemIndex = -1;

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
            shopView.SetCallbacks(
                onClickItem: OnClickItem,
                onClickReroll: OnClickReroll,
                onClickClose: OnClickCloseButton,
                onBeginDragItem: BeginItemDrag,
                onDragItem: UpdateItemDrag,
                onEndDragItem: EndItemDrag
            );
            OnSelectionChanged += shopView.HandleSelectionChanged;
            shopView.Close();  
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

    public static int CalculateSellPrice(int price)
    {
        if (price <= 0)
            return 0;

        return Mathf.FloorToInt(price * 0.5f);
    }

    public void Open()
    {
        isOpen = true;

        ClearSelection();

        BuildSellableItems();
        CollectOwnedItems();
        EnsureArrays();
        currentRerollCost = Mathf.Max(1, baseRerollCost);

        BuildRoster();
        RefreshView();

        shopView.Open();   
    }

    void CollectOwnedItems()
    {
        ownedItemIds.Clear();
        rosterItemIds.Clear();

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var inst = inventory.GetSlot(i);
            if (inst == null || string.IsNullOrEmpty(inst.Id))
                continue;

            ownedItemIds.Add(inst.Id);
        }
    }

    void BuildSellableItems()
    {
        sellableItems.Clear();

        if (!ItemRepository.IsInitialized)
        {
            Debug.LogWarning("[ShopManager] ItemRepository not initialized.");
            return;
        }

        foreach (var entry in ItemRepository.All)
        {
            var dto = entry.Value;
            if (dto == null)
                continue;

            // 추후 isNotSell 같은 플래그가 생기면 필터 추가
            sellableItems.Add(dto);
        }
    }

    void EnsureArrays()
    {
        if (itemsPerShop <= 0)
            itemsPerShop = 3;

        if (currentShopItems == null || currentShopItems.Length != itemsPerShop)
            currentShopItems = new IProduct[itemsPerShop];

        for (int i = 0; i < itemsPerShop; i++)
            currentShopItems[i] = null;
    }

    void BuildRoster()
    {
        rosterItems.Clear();
        rosterItemIds.Clear();

        var factory = ShopItemFactory.Instance;
        if (factory == null)
        {
            Debug.LogError("[ShopManager] ShopItemFactory.Instance is null.");
            return;
        }

        var itemPool = BuildItemPool();

        for (int slot = 0; slot < itemsPerShop; slot++)
        {
            var type = factory.RollType(itemProbabilities);
            if (type != ProductType.Item)
                continue;

            if (itemPool.Count == 0)
                continue;

            var dto = PopItem(itemPool);
            var item = factory.CreateItem(dto);
            if (item != null)
            {
                rosterItems.Add(item);
                rosterItemIds.Add(dto.id);
            }
        }

        rosterItems.Sort((a, b) => a.ProductType.CompareTo(b.ProductType));

        // currentShopItems에 복사
        EnsureArrays();
        for (int i = 0; i < currentShopItems.Length; i++)
            currentShopItems[i] = null;
        int copyCount = Mathf.Min(itemsPerShop, rosterItems.Count);
        for (int i = 0; i < copyCount; i++)
            currentShopItems[i] = rosterItems[i];

        for (int i = 0; i < itemsPerShop; i++)
        {
            if (currentShopItems[i] != null)
                currentShopItems[i].Sold = false;
        }
    }

    List<ItemDto> BuildItemPool()
    {
        var pool = new List<ItemDto>();

        if (sellableItems == null || sellableItems.Count == 0)
            return pool;

        for (int i = 0; i < sellableItems.Count; i++)
        {
            var dto = sellableItems[i];
            if (dto == null)
                continue;

            if (!IsItemAllowed(dto.id))
                continue;

            pool.Add(dto);
        }

        // 셔플
        for (int i = pool.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        return pool;
    }

    ItemDto PopItem(List<ItemDto> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        int last = pool.Count - 1;
        var dto = pool[last];
        pool.RemoveAt(last);
        return dto;
    }

    bool IsItemAllowed(string itemId)
    {
        if (string.IsNullOrEmpty(itemId))
            return false;

        if (ownedItemIds.Contains(itemId))
            return false;

        if (rosterItemIds.Contains(itemId))
            return false;

        return true;
    }

    public void SetSelection(int itemIndex)
    {
        IProduct selection = null;

        if (currentShopItems != null && itemIndex >= 0 && itemIndex < currentShopItems.Length)
        {
            var item = GetShopItem(itemIndex);
            if (item != null && !IsSold(itemIndex))
                selection = item;
        }

        ApplySelection(selection, itemIndex);
    }

    public void ClearSelection()
    {
        ApplySelection(null, -1);
        shopView?.ClearSelectionVisuals();
        ItemSlotManager.Instance?.ClearHighlights();
    }

    void ApplySelection(IProduct selection, int itemIndex)
    {
        CurrentSelectionIndex = selection != null ? itemIndex : -1;
        OnSelectionChanged?.Invoke(CurrentSelectionIndex);

        if (selection is { ProductType: ProductType.Item })
            ItemSlotManager.Instance?.HighlightEmptySlots();
        else
            ItemSlotManager.Instance?.ClearHighlights();
    }

    public bool TryPurchaseSelectedAt(int row, int col)
    {
        _ = row;
        _ = col;
        return false;
    }

    public bool TryPurchaseSelectedItemAt(int slotIndex)
    {
        if (!isOpen)
            return false;

        if (CurrentSelectionIndex < 0 || currentShopItems == null)
            return false;

        if (CurrentSelectionIndex >= currentShopItems.Length)
            return false;

        var item = GetShopItem(CurrentSelectionIndex);
        if (item == null || item.ProductType != ProductType.Item || IsSold(CurrentSelectionIndex))
            return false;

        shopView?.ClearSelectionVisuals();
        ItemSlotManager.Instance?.ClearHighlights();
        return TryPurchaseItemAt(CurrentSelectionIndex, slotIndex);
    }

    bool TryPurchaseItemAt(int itemIndex, int overrideSlot = -1)
    {
        if (!isOpen)
            return false;

        if (currentShopItems == null || itemIndex < 0 || itemIndex >= currentShopItems.Length)
            return false;

        var item = currentShopItems[itemIndex] as ItemProduct;
        if (item == null || IsSold(itemIndex))
            return false;

        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
            return false;

        int price = item.Price;
        if (!currencyMgr.TrySpend(price))
        {
            RefreshView();
            return false;
        }

        int slotIndex = overrideSlot >= 0 ? overrideSlot : FindFirstEmptyItemSlot();
        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
        {
            currencyMgr.AddCurrency(price);
            RefreshView();
            return false;
        }

        if (slotIndex < 0)
        {
            currencyMgr.AddCurrency(price);
            RefreshView();
            return false;
        }

        if (!ItemRepository.TryGet(item.Id, out var dto) || dto == null)
        {
            currencyMgr.AddCurrency(price);
            RefreshView();
            return false;
        }

        if (!inventory.TrySetSlot(slotIndex, new ItemInstance(dto)))
        {
            currencyMgr.AddCurrency(price);
            RefreshView();
            return false;
        }

        MarkSold(itemIndex);
        ClearSelection();
        RefreshView();
        return true;
    }

    int FindFirstEmptyItemSlot()
    {
        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return -1;

        if (inventory.TryGetFirstEmptySlot(out int idx))
            return idx;

        return -1;
    }

    void MarkSold(int index)
    {
        var item = GetShopItem(index);
        if (item != null)
            item.Sold = true;
    }

    void RefreshView()
    {
        if (shopView == null)
            return;

        int currency = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.CurrentCurrency
            : 0;

        bool hasEmptyItemSlot = ItemManager.Instance?.Inventory != null
            && ItemManager.Instance.Inventory.TryGetFirstEmptySlot(out _);

        shopView.SetItems(currentShopItems, currency, hasEmptyItemSlot, currentRerollCost);
        shopView.RefreshAll();
    }

    IProduct GetShopItem(int index)
    {
        if (currentShopItems == null || index < 0 || index >= currentShopItems.Length)
            return null;
        return currentShopItems[index];
    }

    bool IsSold(int index)
    {
        var item = GetShopItem(index);
        return item?.Sold ?? true;
    }

    public IProduct GetSelectedItem()
    {
        if (CurrentSelectionIndex < 0 || currentShopItems == null)
            return null;

        if (CurrentSelectionIndex >= currentShopItems.Length)
            return null;

        return currentShopItems[CurrentSelectionIndex];
    }

    void OnClickItem(int index)
    {
        if (!isOpen)
            return;

        if (currentShopItems == null || index < 0 || index >= currentShopItems.Length)
            return;

        if (CurrentSelectionIndex == index)
            ClearSelection();
        else
            SetSelection(index);

        var item = GetShopItem(index);
        if (item == null)
            return;

        if (item.ProductType == ProductType.Item)
        {
            shopView?.ClearSelectionVisuals();
        }
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

        BuildSellableItems();
        CollectOwnedItems();
        BuildRoster();
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

        draggingItemIndex = -1;

        if (shopView != null)
        {
            shopView.Close();
            shopView.HideItemDragGhost();
        }

        ClearSelection();

        StageManager.Instance?.OnShopClosed();
    }

    void HandleCurrencyChanged(int value)
    {
        RefreshView();
    }

    // ======================
    // Item drag (토큰)
    // ======================

    public void BeginItemDrag(int itemIndex, Vector2 screenPos)
    {
        if (!isOpen || shopView == null)
            return;

        var item = GetShopItem(itemIndex);
        if (item == null || IsSold(itemIndex))
            return;

        if (StageManager.Instance.CurrentPhase != StagePhase.Shop)
            return;

        SetSelection(itemIndex);

        if (item.ProductType == ProductType.Item)
        {
            draggingItemIndex = itemIndex;
            ItemSlotManager.Instance?.HighlightEmptySlots();
            shopView.ShowItemDragGhost(item, screenPos);
        }
        else
        {
            draggingItemIndex = -1;
        }
    }

    public void UpdateItemDrag(int itemIndex, Vector2 screenPos)
    {
        if (!isOpen || shopView == null)
            return;

        if (itemIndex == draggingItemIndex)
        {
            shopView.UpdateItemDragGhostPosition(screenPos);
            ItemSlotManager.Instance?.UpdatePurchaseHover(screenPos);
        }
    }

    public void EndItemDrag(int itemIndex, Vector2 screenPos)
    {
        if (shopView == null)
        {
            draggingItemIndex = -1;
            return;
        }

        if (!isOpen)
        {
            shopView.HideItemDragGhost();
            draggingItemIndex = -1;
            return;
        }

        if (itemIndex == draggingItemIndex)
        {
            var slotManager = ItemSlotManager.Instance;
            int targetSlot = -1;
            if (slotManager != null && slotManager.TryGetEmptySlotFromScreenPos(screenPos, out targetSlot))
                TryPurchaseItemAt(itemIndex, targetSlot);
        }

        shopView.HideItemDragGhost();
        draggingItemIndex = -1;
        ItemSlotManager.Instance?.ClearHighlights();
    }

}
