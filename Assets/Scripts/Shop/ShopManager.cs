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

    [Header("Item Rarity Probabilities (weight-based)")]
    [SerializeField] private int[] rarityWeights = new int[] { 60, 30, 10, 0 };

    [Header("Upgrade Rarity Probabilities (weight-based)")]
    [SerializeField] private int[] upgradeRarityWeights = new int[] { 60, 30, 10, 0 };

    [Header("Product Probabilities (weight-based)")]
    [SerializeField] private int itemProductWeight = 80;
    [SerializeField] private int upgradeProductWeight = 20;

    bool isOpen;

    readonly List<IProduct> rosterItems = new();
    readonly List<ItemDto> sellableItems = new();
    readonly List<UpgradeDto> sellableUpgrades = new();
    readonly HashSet<string> rosterItemIds = new();
    readonly HashSet<string> ownedItemIds = new();

    IProduct[] currentShopItems;
    int currentRerollCost;

    public event Action OnShopItemChanged;

    public event Action<int> OnSelectionChanged;

    public int CurrentSelectionIndex { get; private set; } = -1;

    int draggingItemIndex = -1;
    ItemInventory subscribedInventory;

    public bool IsUpgradeSelectionActive => GetSelectedItem()?.ProductType == ProductType.Upgrade;

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
                onBeginDragItem: BeginItemDrag,
                onDragItem: UpdateItemDrag,
                onEndDragItem: EndItemDrag
            );
            OnSelectionChanged += shopView.HandleSelectionChanged;
            shopView.Close();  
        }
        OnShopItemChanged += RefreshView;
    }

    void Start()
    {
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            player.OnCurrencyChanged += HandleCurrencyChanged;

        UiSelectionEvents.OnSelectionCleared += HandleSelectionCleared;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory != null)
        {
            subscribedInventory = inventory;
            subscribedInventory.OnInventoryChanged += RefreshView;
        }
    }

    void OnDisable()
    {
        if (shopView != null)
            OnSelectionChanged -= shopView.HandleSelectionChanged;

        var player = PlayerManager.Instance?.Current;
        if (player != null)
            player.OnCurrencyChanged -= HandleCurrencyChanged;

        UiSelectionEvents.OnSelectionCleared -= HandleSelectionCleared;
        OnShopItemChanged -= RefreshView;
        if (subscribedInventory != null)
        {
            subscribedInventory.OnInventoryChanged -= RefreshView;
            subscribedInventory = null;
        }
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

        UiSelectionEvents.RaiseSelectionCleared();

        BuildSellableItems();
        BuildSellableUpgrades();
        CollectOwnedItems();
        EnsureArrays();
        currentRerollCost = Mathf.Max(1, baseRerollCost);

        BuildRoster();
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
            if (dto.isNotSell)
                continue;

            sellableItems.Add(dto);
        }
    }

    void BuildSellableUpgrades()
    {
        sellableUpgrades.Clear();

        if (!UpgradeRepository.IsInitialized)
        {
            Debug.LogWarning("[ShopManager] UpgradeRepository not initialized.");
            return;
        }

        foreach (var entry in UpgradeRepository.All)
        {
            var dto = entry.Value;
            if (dto == null)
                continue;

            sellableUpgrades.Add(dto);
        }
    }

    void EnsureArrays()
    {
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

        var itemPools = BuildItemPoolsByRarity();
        var upgradePools = BuildUpgradePoolsByRarity();

        for (int slot = 0; slot < itemsPerShop; slot++)
        {
            var type = RollProductType(itemPools, upgradePools);
            if (type == ProductType.Item)
            {
                if (!TryRollRarity(itemPools, out var rarity))
                    continue;

                if (!TryPopItem(itemPools, rarity, out var dto))
                    continue;

                var item = factory.CreateItem(dto);
                if (item != null)
                {
                    rosterItems.Add(item);
                    rosterItemIds.Add(dto.id);
                }
            }
            else if (type == ProductType.Upgrade)
            {
                if (!TryRollUpgradeRarity(upgradePools, out var rarity))
                    continue;

                if (!TryPopUpgrade(upgradePools, rarity, out var upgrade))
                    continue;

                var product = factory.CreateUpgrade(upgrade);
                if (product != null)
                    rosterItems.Add(product);
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

        NotifyShopItemChanged();
    }

    Dictionary<ItemRarity, List<ItemDto>> BuildItemPoolsByRarity()
    {
        var pools = new Dictionary<ItemRarity, List<ItemDto>>();

        if (sellableItems == null || sellableItems.Count == 0)
            return pools;

        for (int i = 0; i < sellableItems.Count; i++)
        {
            var dto = sellableItems[i];
            if (dto == null)
                continue;

            if (!IsItemAllowed(dto.id))
                continue;

            if (!pools.TryGetValue(dto.rarity, out var list))
            {
                list = new List<ItemDto>();
                pools[dto.rarity] = list;
            }

            list.Add(dto);
        }

        foreach (var entry in pools)
            ShuffleList(entry.Value);

        return pools;
    }

    Dictionary<ItemRarity, List<UpgradeDto>> BuildUpgradePoolsByRarity()
    {
        var pools = new Dictionary<ItemRarity, List<UpgradeDto>>();

        if (sellableUpgrades == null || sellableUpgrades.Count == 0)
            return pools;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return pools;

        for (int i = 0; i < sellableUpgrades.Count; i++)
        {
            var dto = sellableUpgrades[i];
            if (dto == null)
                continue;

            if (!IsUpgradeApplicableToInventory(dto, inventory))
                continue;

            if (!pools.TryGetValue(dto.rarity, out var list))
            {
                list = new List<UpgradeDto>();
                pools[dto.rarity] = list;
            }

            list.Add(dto);
        }

        foreach (var entry in pools)
            ShuffleList(entry.Value);

        return pools;
    }

    bool IsUpgradeApplicableToInventory(UpgradeDto dto, ItemInventory inventory)
    {
        if (dto == null || inventory == null)
            return false;

        var preview = new UpgradeInstance(dto);
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var inst = inventory.GetSlot(i);
            if (inst == null)
                continue;

            if (preview.IsApplicable(inst))
                return true;
        }

        return false;
    }

    bool TryPopItem(Dictionary<ItemRarity, List<ItemDto>> pools, ItemRarity rarity, out ItemDto dto)
    {
        dto = null;
        if (pools == null)
            return false;

        if (!pools.TryGetValue(rarity, out var list) || list == null || list.Count == 0)
            return false;

        int last = list.Count - 1;
        dto = list[last];
        list.RemoveAt(last);

        if (list.Count == 0)
            pools.Remove(rarity);

        return dto != null;
    }

    bool TryPopUpgrade(Dictionary<ItemRarity, List<UpgradeDto>> pools, ItemRarity rarity, out UpgradeDto dto)
    {
        dto = null;
        if (pools == null)
            return false;

        if (!pools.TryGetValue(rarity, out var list) || list == null || list.Count == 0)
            return false;

        int last = list.Count - 1;
        dto = list[last];
        list.RemoveAt(last);

        if (list.Count == 0)
            pools.Remove(rarity);

        return dto != null;
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

    bool TryRollRarity(Dictionary<ItemRarity, List<ItemDto>> pools, out ItemRarity rarity)
    {
        rarity = ItemRarity.Common;
        if (pools == null || pools.Count == 0)
            return false;

        int totalWeight = 0;
        var available = new List<ItemRarity>();

        foreach (var entry in pools)
        {
            if (entry.Value == null || entry.Value.Count == 0)
                continue;

            available.Add(entry.Key);
            totalWeight += Mathf.Max(0, GetRarityWeight(entry.Key));
        }

        if (available.Count == 0)
            return false;

        if (totalWeight <= 0)
            return false;

        int roll = Rng.Next(0, totalWeight);
        int acc = 0;
        for (int i = 0; i < available.Count; i++)
        {
            int weight = Mathf.Max(0, GetRarityWeight(available[i]));
            acc += weight;
            if (roll < acc)
            {
                rarity = available[i];
                return true;
            }
        }

        rarity = available[available.Count - 1];
        return true;
    }

    int GetRarityWeight(ItemRarity rarity)
    {
        if (rarityWeights == null || rarityWeights.Length == 0)
            return 0;

        int index = (int)rarity;
        if (index < 0 || index >= rarityWeights.Length)
            return 0;

        return rarityWeights[index];
    }

    bool TryRollUpgradeRarity(Dictionary<ItemRarity, List<UpgradeDto>> pools, out ItemRarity rarity)
    {
        rarity = ItemRarity.Common;
        if (pools == null || pools.Count == 0)
            return false;

        int totalWeight = 0;
        var available = new List<ItemRarity>();

        foreach (var entry in pools)
        {
            if (entry.Value == null || entry.Value.Count == 0)
                continue;

            available.Add(entry.Key);
            totalWeight += Mathf.Max(0, GetUpgradeRarityWeight(entry.Key));
        }

        if (available.Count == 0)
            return false;

        if (totalWeight <= 0)
            return false;

        int roll = Rng.Next(0, totalWeight);
        int acc = 0;
        for (int i = 0; i < available.Count; i++)
        {
            int weight = Mathf.Max(0, GetUpgradeRarityWeight(available[i]));
            acc += weight;
            if (roll < acc)
            {
                rarity = available[i];
                return true;
            }
        }

        rarity = available[available.Count - 1];
        return true;
    }

    int GetUpgradeRarityWeight(ItemRarity rarity)
    {
        if (upgradeRarityWeights == null || upgradeRarityWeights.Length == 0)
            return 0;

        int index = (int)rarity;
        if (index < 0 || index >= upgradeRarityWeights.Length)
            return 0;

        return upgradeRarityWeights[index];
    }

    bool HasAvailableItems(Dictionary<ItemRarity, List<ItemDto>> pools)
    {
        if (pools == null)
            return false;

        foreach (var entry in pools)
        {
            if (entry.Value != null && entry.Value.Count > 0)
                return true;
        }

        return false;
    }

    bool HasAvailableUpgrades(Dictionary<ItemRarity, List<UpgradeDto>> pools)
    {
        if (pools == null)
            return false;

        foreach (var entry in pools)
        {
            if (entry.Value != null && entry.Value.Count > 0)
                return true;
        }

        return false;
    }

    ProductType RollProductType(Dictionary<ItemRarity, List<ItemDto>> itemPools, Dictionary<ItemRarity, List<UpgradeDto>> upgradePools)
    {
        bool hasItems = HasAvailableItems(itemPools);
        bool hasUpgrades = HasAvailableUpgrades(upgradePools);

        if (hasItems && !hasUpgrades)
            return ProductType.Item;

        if (!hasItems && hasUpgrades)
            return ProductType.Upgrade;

        if (!hasItems && !hasUpgrades)
            return ProductType.Item;

        int itemWeight = Mathf.Max(0, itemProductWeight);
        int upgradeWeight = Mathf.Max(0, upgradeProductWeight);
        int total = itemWeight + upgradeWeight;
        if (total <= 0)
            return ProductType.Item;

        int roll = Rng.Next(0, total);
        return roll < itemWeight ? ProductType.Item : ProductType.Upgrade;
    }

    void ShuffleList<T>(List<T> list)
    {
        if (list == null || list.Count <= 1)
            return;

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void SetSelection(int itemIndex)
    {
        UiSelectionEvents.RaiseSelectionCleared();

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

        UpdateSlotHighlightsForSelection();

        if (selection != null)
            shopView?.PinTooltipForSelection(itemIndex);
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

    public bool TryApplySelectedUpgradeAt(int slotIndex)
    {
        if (!isOpen)
            return false;

        if (CurrentSelectionIndex < 0 || currentShopItems == null)
            return false;

        if (CurrentSelectionIndex >= currentShopItems.Length)
            return false;

        var upgrade = GetShopItem(CurrentSelectionIndex) as UpgradeProduct;
        if (upgrade == null || upgrade.Sold)
            return false;

        return TryApplyUpgradeAt(slotIndex, upgrade, confirmReplace: true);
    }

    bool TryApplyUpgradeAt(int slotIndex, UpgradeProduct upgrade, bool confirmReplace)
    {
        if (!isOpen || upgrade == null || upgrade.Sold)
            return false;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return false;

        if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            return false;

        var targetItem = inventory.GetSlot(slotIndex);
        if (targetItem == null)
            return false;

        var preview = upgrade.PreviewInstance;
        if (preview == null || !preview.IsApplicable(targetItem))
            return false;

        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
            return false;

        if (currencyMgr.CurrentCurrency < upgrade.Price)
            return false;

        if (confirmReplace && NeedsUpgradeReplaceConfirm(targetItem, preview))
        {
            ShowUpgradeReplaceModal(targetItem, upgrade, slotIndex);
            return false;
        }

        return ApplyUpgrade(targetItem, upgrade, preview);
    }

    bool NeedsUpgradeReplaceConfirm(ItemInstance targetItem, UpgradeInstance newUpgrade)
    {
        if (targetItem?.Upgrade == null || newUpgrade == null)
            return false;

        return targetItem.Upgrade.Id != newUpgrade.Id;
    }

    void ShowUpgradeReplaceModal(ItemInstance targetItem, UpgradeProduct upgrade, int slotIndex)
    {
        var modal = ModalManager.Instance;
        if (modal == null)
            return;

        string itemName = LocalizationUtil.GetItemName(targetItem.Id);
        if (string.IsNullOrEmpty(itemName))
            itemName = targetItem.Id;

        string currentUpgradeName = LocalizationUtil.GetUpgradeName(targetItem.Upgrade.Id);
        if (string.IsNullOrEmpty(currentUpgradeName))
            currentUpgradeName = targetItem.Upgrade.Id;

        string newUpgradeName = LocalizationUtil.GetUpgradeName(upgrade.Id);
        if (string.IsNullOrEmpty(newUpgradeName))
            newUpgradeName = upgrade.Id;

        var args = new Dictionary<string, object>
        {
            ["itemName"] = itemName,
            ["currentUpgrade"] = currentUpgradeName,
            ["newUpgrade"] = newUpgradeName
        };

        modal.ShowConfirmation(
            "modal",
            "modal.upgradeReplace.title",
            "modal",
            "modal.upgradeReplace.message",
            () => TryApplyUpgradeAt(slotIndex, upgrade, confirmReplace: false),
            () => { },
            args);
    }

    bool ApplyUpgrade(ItemInstance targetItem, UpgradeProduct upgrade, UpgradeInstance preview)
    {
        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
            return false;

        if (currencyMgr.CurrentCurrency < upgrade.Price)
            return false;

        if (!currencyMgr.TrySpend(upgrade.Price))
            return false;

        var upgradeManager = UpgradeManager.Instance;
        if (upgradeManager == null || !upgradeManager.ApplyUpgrade(targetItem, preview))
            return false;
        MarkSold(upgrade);
        UiSelectionEvents.RaiseSelectionCleared();
        return true;
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
            return false;

        int slotIndex = overrideSlot >= 0 ? overrideSlot : FindFirstEmptyItemSlot();
        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
        {
            currencyMgr.AddCurrency(price);
            return false;
        }

        if (slotIndex < 0)
        {
            currencyMgr.AddCurrency(price);
            return false;
        }

        if (!ItemRepository.TryGet(item.Id, out var dto) || dto == null)
        {
            currencyMgr.AddCurrency(price);
            return false;
        }

        if (!inventory.TrySetSlot(slotIndex, new ItemInstance(dto)))
        {
            currencyMgr.AddCurrency(price);
            return false;
        }

        MarkSold(itemIndex);
        UiSelectionEvents.RaiseSelectionCleared();
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
        MarkSold(GetShopItem(index));
    }

    void MarkSold(IProduct item)
    {
        if (item != null)
            item.Sold = true;

        NotifyShopItemChanged();
    }

    void RefreshView()
    {
        if (!isOpen || shopView == null)
            return;

        int currency = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.CurrentCurrency
            : 0;

        bool[] canBuyFlags = BuildCanBuyFlags(currentShopItems, currency);

        shopView.SetItems(currentShopItems, canBuyFlags, currency, currentRerollCost);
        shopView.RefreshAll();

        if (IsSelectedItemInvalid())
            UiSelectionEvents.RaiseSelectionCleared();
        else
            UpdateSlotHighlightsForSelection();
    }

    bool[] BuildCanBuyFlags(IProduct[] items, int currency)
    {
        if (items == null)
            return null;

        var flags = new bool[items.Length];
        bool hasEmptyItemSlot = ItemManager.Instance?.Inventory != null
            && ItemManager.Instance.Inventory.TryGetFirstEmptySlot(out _);

        for (int i = 0; i < items.Length; i++)
        {
            var product = items[i];
            if (product == null || product.Sold)
                continue;

            if (currency < product.Price)
                continue;

            if (product.ProductType == ProductType.Item)
            {
                flags[i] = hasEmptyItemSlot;
            }
            else if (product.ProductType == ProductType.Upgrade)
            {
                var upgrade = product as UpgradeProduct;
                flags[i] = upgrade != null && HasApplicableUpgradeSlot(upgrade);
            }
        }

        return flags;
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
        {
            UiSelectionEvents.RaiseSelectionCleared();
            return;
        }
        else
            SetSelection(index);
    }

    public void OnClickReroll()
    {
        if (!isOpen)
            return;

        UiSelectionEvents.RaiseSelectionCleared();

        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
            return;

        int cost = Mathf.Max(1, currentRerollCost);

        if (!currencyMgr.TrySpend(cost))
            return;

        currentRerollCost = Mathf.Max(1, currentRerollCost + Mathf.Max(1, rerollCostIncrement));

        BuildSellableItems();
        BuildSellableUpgrades();
        CollectOwnedItems();
        BuildRoster();
    }

    public void OnClickCloseButton()
    {
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        UiSelectionEvents.RaiseSelectionCleared();

        isOpen = false;

        draggingItemIndex = -1;

        if (shopView != null)
        {
            shopView.Close();
            shopView.HideItemDragGhost();
        }

        StageManager.Instance?.OnShopClosed();
    }

    void HandleCurrencyChanged(int value)
    {
        RefreshView();
    }

    void NotifyShopItemChanged()
    {
        OnShopItemChanged?.Invoke();
    }

    void HandleSelectionCleared()
    {
        if (!isOpen)
            return;

        ClearSelection();
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
        TooltipManager.Instance?.HideForDrag();

        if (item.ProductType == ProductType.Item || item.ProductType == ProductType.Upgrade)
        {
            draggingItemIndex = itemIndex;
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
            bool hasValidTarget = false;
            var selected = GetShopItem(itemIndex);
            if (selected is ItemProduct itemProduct)
            {
                hasValidTarget = ItemSlotManager.Instance != null
                    && ItemSlotManager.Instance.UpdatePurchaseHover(screenPos, itemProduct.PreviewInstance);
            }
            else if (selected is UpgradeProduct upgradeProduct)
            {
                hasValidTarget = ItemSlotManager.Instance != null
                    && ItemSlotManager.Instance.UpdateUpgradeHover(screenPos, upgradeProduct.PreviewInstance);
            }

            if (hasValidTarget && selected is ItemProduct)
            {
                shopView.HideItemDragGhost();
            }
            else
            {
                shopView.ShowItemDragGhost(GetShopItem(itemIndex), screenPos);
                shopView.UpdateItemDragGhostPosition(screenPos);
            }
        }
    }

    public void EndItemDrag(int itemIndex, Vector2 screenPos)
    {
        if (shopView == null)
        {
            draggingItemIndex = -1;
            TooltipManager.Instance?.RestoreAfterDrag();
            return;
        }

        if (!isOpen)
        {
            shopView.HideItemDragGhost();
            draggingItemIndex = -1;
            TooltipManager.Instance?.RestoreAfterDrag();
            return;
        }

        if (itemIndex == draggingItemIndex)
        {
            var slotManager = ItemSlotManager.Instance;
            var product = GetShopItem(itemIndex);
            if (product is ItemProduct)
            {
                int targetSlot = -1;
                if (slotManager != null && slotManager.TryGetEmptySlotFromScreenPos(screenPos, out targetSlot))
                    TryPurchaseItemAt(itemIndex, targetSlot);
            }
            else if (product is UpgradeProduct upgradeProduct)
            {
                int targetSlot = -1;
                if (slotManager != null && slotManager.TryGetUpgradeSlotFromScreenPos(screenPos, upgradeProduct.PreviewInstance, out targetSlot))
                    TryApplySelectedUpgradeAt(targetSlot);
            }
        }

        shopView.HideItemDragGhost();
        draggingItemIndex = -1;
        ItemSlotManager.Instance?.ClearHighlights();
        ItemSlotManager.Instance?.ClearPreviews();
        UpdateSlotHighlightsForSelection();
        TooltipManager.Instance?.RestoreAfterDrag();
    }

    void UpdateSlotHighlightsForSelection()
    {
        if (CurrentSelectionIndex < 0)
        {
            ItemSlotManager.Instance?.ClearHighlights();
            return;
        }

        var selected = GetShopItem(CurrentSelectionIndex);
        if (selected == null)
        {
            ItemSlotManager.Instance?.ClearHighlights();
            return;
        }

        if (selected.ProductType == ProductType.Item)
        {
            if (CanPurchaseSelectedItem(selected))
                ItemSlotManager.Instance?.HighlightEmptySlots();
            else
                ItemSlotManager.Instance?.ClearHighlights();
            return;
        }

        if (selected.ProductType == ProductType.Upgrade)
        {
            var upgrade = selected as UpgradeProduct;
            if (CanApplySelectedUpgrade(upgrade))
                ItemSlotManager.Instance?.HighlightUpgradeSlots(upgrade?.PreviewInstance);
            else
                ItemSlotManager.Instance?.ClearHighlights();
        }
    }

    bool CanPurchaseSelectedItem(IProduct selected)
    {
        if (selected == null || selected.Sold || selected.ProductType != ProductType.Item)
            return false;

        var currency = CurrencyManager.Instance;
        if (currency == null || currency.CurrentCurrency < selected.Price)
            return false;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return false;

        return inventory.TryGetFirstEmptySlot(out _);
    }

    bool CanApplySelectedUpgrade(UpgradeProduct upgrade)
    {
        if (upgrade == null || upgrade.Sold)
            return false;

        var currency = CurrencyManager.Instance;
        if (currency == null || currency.CurrentCurrency < upgrade.Price)
            return false;

        return HasApplicableUpgradeSlot(upgrade);
    }

    bool HasApplicableUpgradeSlot(UpgradeProduct upgrade)
    {
        if (upgrade == null)
            return false;

        return ItemSlotManager.Instance != null
            && ItemSlotManager.Instance.HasApplicableUpgradeSlot(upgrade.PreviewInstance);
    }

    bool IsSelectedItemInvalid()
    {
        if (CurrentSelectionIndex < 0)
            return false;

        var selected = GetShopItem(CurrentSelectionIndex);
        return selected == null || selected.Sold;
    }

}
