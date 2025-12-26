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
    [SerializeField] private ShopItemProbability[] itemProbabilities =
    {
        new ShopItemProbability { type = ShopItemType.Token, weight = 100 }
    };

    bool isOpen;

    readonly List<IShopItem> rosterItems = new();
    readonly List<TokenDto> sellableTokens = new();
    readonly HashSet<string> rosterTokenIds = new();
    readonly HashSet<string> ownedTokenIds = new();

    IShopItem[] currentShopItems;
    int currentRerollCost;

    public event Action<int> OnSelectionChanged;

    public int CurrentSelectionIndex { get; private set; } = -1;

    int draggingTokenIndex = -1;

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

    public void Open()
    {
        isOpen = true;

        ClearSelection();

        BuildSellableTokens();
        CollectOwnedTokens();
        EnsureArrays();
        currentRerollCost = Mathf.Max(1, baseRerollCost);

        BuildRoster();
        RefreshView();

        shopView.Open();   
    }

    void CollectOwnedTokens()
    {
        ownedTokenIds.Clear();
        rosterTokenIds.Clear();

        if (TokenManager.Instance != null)
            TokenManager.Instance.CollectOwnedTokenIds(ownedTokenIds);
    }

    void BuildSellableTokens()
    {
        sellableTokens.Clear();

        if (!TokenRepository.IsInitialized)
        {
            Debug.LogWarning("[ShopManager] TokenRepository not initialized.");
            return;
        }

        foreach (var dto in TokenRepository.All)
        {
            if (dto == null)
                continue;

            // 추후 isNotSell 같은 플래그가 생기면 필터 추가
            sellableTokens.Add(dto);
        }
    }

    void EnsureArrays()
    {
        if (itemsPerShop <= 0)
            itemsPerShop = 3;

        if (currentShopItems == null || currentShopItems.Length != itemsPerShop)
            currentShopItems = new IShopItem[itemsPerShop];

        for (int i = 0; i < itemsPerShop; i++)
            currentShopItems[i] = null;
    }

    void BuildRoster()
    {
        rosterItems.Clear();
        rosterTokenIds.Clear();

        var factory = ShopItemFactory.Instance;
        if (factory == null)
        {
            Debug.LogError("[ShopManager] ShopItemFactory.Instance is null.");
            return;
        }

        var tokenPool = BuildTokenPool();

        for (int slot = 0; slot < itemsPerShop; slot++)
        {
            var type = factory.RollType(itemProbabilities);
            if (type != ShopItemType.Token)
                continue;

            if (tokenPool.Count == 0)
                continue;

            var dto = PopToken(tokenPool);
            var item = factory.CreateToken(dto);
            if (item != null)
            {
                rosterItems.Add(item);
                rosterTokenIds.Add(dto.id);
            }
        }

        rosterItems.Sort((a, b) => a.ItemType.CompareTo(b.ItemType));

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

    List<TokenDto> BuildTokenPool()
    {
        var pool = new List<TokenDto>();

        if (sellableTokens == null || sellableTokens.Count == 0)
            return pool;

        for (int i = 0; i < sellableTokens.Count; i++)
        {
            var dto = sellableTokens[i];
            if (dto == null)
                continue;

            if (!IsTokenAllowed(dto.id))
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

    TokenDto PopToken(List<TokenDto> pool)
    {
        if (pool == null || pool.Count == 0)
            return null;

        int last = pool.Count - 1;
        var dto = pool[last];
        pool.RemoveAt(last);
        return dto;
    }

    bool IsTokenAllowed(string tokenId)
    {
        if (string.IsNullOrEmpty(tokenId))
            return false;

        if (ownedTokenIds.Contains(tokenId))
            return false;

        if (rosterTokenIds.Contains(tokenId))
            return false;

        return true;
    }

    public void SetSelection(int itemIndex)
    {
        IShopItem selection = null;

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
        TokenManager.Instance?.ClearHighlights();
    }

    void ApplySelection(IShopItem selection, int itemIndex)
    {
        CurrentSelectionIndex = selection != null ? itemIndex : -1;
        OnSelectionChanged?.Invoke(CurrentSelectionIndex);
    }

    public bool TryPurchaseSelectedAt(int row, int col)
    {
        _ = row;
        _ = col;
        return false;
    }

    public bool TryPurchaseSelectedTokenAt(int slotIndex)
    {
        if (!isOpen)
            return false;

        if (CurrentSelectionIndex < 0 || currentShopItems == null)
            return false;

        if (CurrentSelectionIndex >= currentShopItems.Length)
            return false;

        var item = GetShopItem(CurrentSelectionIndex);
        if (item == null || item.ItemType != ShopItemType.Token || IsSold(CurrentSelectionIndex))
            return false;

        shopView?.ClearSelectionVisuals();
        TokenManager.Instance?.ClearHighlights();
        return TryPurchaseTokenItemAt(CurrentSelectionIndex, slotIndex);
    }

    bool TryPurchaseTokenItemAt(int itemIndex, int overrideSlot = -1)
    {
        if (!isOpen)
            return false;

        if (currentShopItems == null || itemIndex < 0 || itemIndex >= currentShopItems.Length)
            return false;

        var item = currentShopItems[itemIndex] as TokenShopItem;
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

        int slotIndex = overrideSlot >= 0 ? overrideSlot : FindFirstEmptyTokenSlot();
        if (slotIndex < 0 || !TokenManager.Instance.TryAddTokenAt(item.Id, slotIndex, out _))
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

    int FindFirstEmptyTokenSlot()
    {
        if (TokenManager.Instance == null)
            return -1;

        if (TokenManager.Instance.TryGetFirstEmptySlot(out int idx))
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

        bool hasEmptyTokenSlot = TokenManager.Instance != null && TokenManager.Instance.TryGetFirstEmptySlot(out _);

        shopView.SetItems(currentShopItems, currency, false, hasEmptyTokenSlot, currentRerollCost);
        shopView.RefreshAll();
    }

    IShopItem GetShopItem(int index)
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

    public IShopItem GetSelectedItem()
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

        if (item.ItemType == ShopItemType.Token)
        {
            // 토큰 슬롯 하이라이트, 핀 선택 해제
            TokenManager.Instance?.HighlightEmptySlots();
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

        BuildSellableTokens();
        CollectOwnedTokens();
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

        draggingTokenIndex = -1;

        if (shopView != null)
        {
            shopView.Close();
            shopView.HideItemDragGhost();
        }

        ClearSelection();

        FlowManager.Instance?.OnShopClosed();
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

        var flow = FlowManager.Instance;
        if (flow != null && flow.CurrentPhase != FlowPhase.Shop)
            return;

        SetSelection(itemIndex);

        if (item.ItemType == ShopItemType.Token)
        {
            draggingTokenIndex = itemIndex;
            TokenManager.Instance?.HighlightEmptySlots();
            shopView.ShowItemDragGhost(item, screenPos);
        }
        else
        {
            draggingTokenIndex = -1;
        }
    }

    public void UpdateItemDrag(int itemIndex, Vector2 screenPos)
    {
        if (!isOpen || shopView == null)
            return;

        if (itemIndex == draggingTokenIndex)
            shopView.UpdateItemDragGhostPosition(screenPos);
    }

    public void EndItemDrag(int itemIndex, Vector2 screenPos)
    {
        if (shopView == null)
        {
            draggingTokenIndex = -1;
            return;
        }

        if (!isOpen)
        {
            shopView.HideItemDragGhost();
            draggingTokenIndex = -1;
            return;
        }

        if (itemIndex == draggingTokenIndex)
        {
            int targetSlot = -1;
            if (TokenManager.Instance != null && TokenManager.Instance.TryGetSlotFromScreenPos(screenPos, out var idx))
                targetSlot = idx;

            if (targetSlot >= 0)
                TryPurchaseTokenItemAt(itemIndex, targetSlot);
        }

        shopView.HideItemDragGhost();
        draggingTokenIndex = -1;
        TokenManager.Instance?.ClearHighlights();
    }

}
