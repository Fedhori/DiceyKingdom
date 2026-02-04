using System.Collections.Generic;
using UnityEngine;
using Data;
using GameStats;

public sealed class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [SerializeField] private string defaultItemId = "item.default";
    [SerializeField] private Transform attachTarget; // 플레이어 transform 등
    [SerializeField] private GameObject objectItemPrefab;
    [SerializeField] private float tickIntervalSeconds = 1f;
    [SerializeField] private float orbitRadius = 64f;
    [SerializeField] private float orbitPeriodSeconds = 4f;

    readonly List<ItemController> controllers = new();
    readonly Dictionary<ItemInstance, ItemController> controllerMap = new();
    readonly HashSet<ItemInstance> effectSources = new();
    readonly ItemInventory inventory = new();
    PlayerInstance subscribedPlayer;
    BlockController currentTriggerBlock;
    int lastCurrency;
    bool isPlayActive;
    float tickTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (!ItemRepository.IsInitialized)
        {
            Debug.LogWarning("[ItemManager] ItemRepository not initialized.");
        }

        inventory.OnSlotChanged += HandleSlotChanged;
    }

    void Start()
    {
        SubscribePlayer();
    }

    public ItemInventory Inventory => inventory;
    public BlockController CurrentTriggerBlock => currentTriggerBlock;

    public int GetPierceBonus()
    {
        var player = PlayerManager.Instance?.Current;
        return player != null ? player.PierceBonus : 0;
    }

    public void BeginPlay()
    {
        isPlayActive = true;
        tickTimer = 0f;
        ResetItemRuntimeState();
        ClearControllers();
        BuildControllersFromInventory();
    }

    public void EndPlay()
    {
        isPlayActive = false;
        tickTimer = 0f;
        ResetItemRuntimeState();
        ClearControllers();
    }

    public void TriggerAll(ItemTriggerType trigger)
    {
        TriggerAll(trigger, null);
    }

    public void TriggerAll(ItemTriggerType trigger, BlockController targetBlock)
    {
        var previous = currentTriggerBlock;
        currentTriggerBlock = targetBlock;

        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var inst = slots[i];
            if (inst == null)
                continue;

            int repeat = inst.GetTriggerRepeat(trigger);
            for (int r = 0; r < repeat; r++)
                inst.HandleTrigger(trigger);
        }

        currentTriggerBlock = previous;
    }

    public void TriggerItem(ItemInstance item, ItemTriggerType trigger)
    {
        TriggerItem(item, trigger, null);
    }

    public void TriggerItem(ItemInstance item, ItemTriggerType trigger, BlockController targetBlock)
    {
        if (item == null)
            return;

        var previous = currentTriggerBlock;
        currentTriggerBlock = targetBlock;

        int repeat = item.GetTriggerRepeat(trigger);
        for (int r = 0; r < repeat; r++)
            item.HandleTrigger(trigger);

        currentTriggerBlock = previous;
    }


    public void InitializeFromPlayer(PlayerInstance player)
    {
        ClearControllers();
        ClearEffectSubscriptions();
        inventory.Clear();

        if (!ItemRepository.IsInitialized)
        {
            Debug.LogError("[ItemManager] cannot init items; repository not initialized.");
            return;
        }

        var ids = player?.ItemIds;
        if (ids == null || ids.Count == 0)
            ids = new List<string> { defaultItemId };

        foreach (var id in ids)
        {
            if (string.IsNullOrEmpty(id))
                continue;

            if (!ItemRepository.TryGet(id, out var dto) || dto == null)
            {
                Debug.LogWarning($"[ItemManager] item id not found: {id}");
                continue;
            }

            var inst = new ItemInstance(dto);
            if (!inventory.TryGetFirstEmptySlot(out var index))
            {
                Debug.LogWarning("[ItemManager] Inventory full. Skipping item add.");
                continue;
            }

            if (!inventory.TrySetSlot(index, inst))
            {
                Debug.LogWarning("[ItemManager] Failed to set item slot.");
                continue;
            }
        }

        SubscribePlayer();
        TriggerAll(ItemTriggerType.OnCurrencyChanged);
    }

    void BuildControllersFromInventory()
    {
        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var inst = slots[i];
            if (inst == null || !inst.IsObject)
                continue;

            if (controllerMap.ContainsKey(inst))
                continue;

            SpawnController(inst);
        }

        ArrangeOrbitControllers();
    }

    ItemController SpawnController(ItemInstance inst)
    {
        if (inst == null || attachTarget == null)
        {
            Debug.LogWarning("[ItemManager] Missing instance/attachTarget");
            return null;
        }

        if (objectItemPrefab == null)
        {
            Debug.LogWarning("[ItemManager] Object item prefab not assigned.");
            return null;
        }

        var go = Instantiate(objectItemPrefab, attachTarget.position, Quaternion.identity, attachTarget);
        var ctrl = go.GetComponent<ItemController>();
        if (ctrl == null)
        {
            Debug.LogWarning("[ItemManager] ItemController missing on object prefab.");
            Destroy(go);
            return null;
        }

        ctrl.BindItem(inst, attachTarget);
        controllers.Add(ctrl);
        controllerMap[inst] = ctrl;
        InitializeOrbit(ctrl, controllers.Count - 1, controllers.Count);
        return ctrl;
    }

    void HandleSlotChanged(int slotIndex, ItemInstance previous, ItemInstance current, ItemInventory.SlotChangeType changeType)
    {
        _ = slotIndex;

        switch (changeType)
        {
            case ItemInventory.SlotChangeType.Add:
                if (current == null)
                    return;

                SubscribeEffects(current);
                current.HandleTrigger(ItemTriggerType.OnAcquire);

                if (isPlayActive && current.IsObject)
                    SpawnController(current);

                TriggerAll(ItemTriggerType.OnItemChanged);
                // 아이템 구매보다 재화 사용이 타이밍이 빠르므로, 이에 대응하기 위해 여기서 한번 트리거
                TriggerAll(ItemTriggerType.OnCurrencyChanged);
                break;
            case ItemInventory.SlotChangeType.Remove:
                if (previous == null)
                    return;

                RemoveController(previous);
                UnsubscribeEffects(previous);
                RemoveOwnedModifiers(previous);

                TriggerAll(ItemTriggerType.OnItemChanged);
                break;
            case ItemInventory.SlotChangeType.Move:
            case ItemInventory.SlotChangeType.Swap:
                TriggerAll(ItemTriggerType.OnItemChanged);
                break;
        }
    }

    bool SubscribeEffects(ItemInstance inst)
    {
        if (inst == null)
            return false;

        if (!effectSources.Add(inst))
            return false;

        inst.OnEffectTriggered += HandleItemEffect;
        return true;
    }

    void UnsubscribeEffects(ItemInstance inst)
    {
        if (inst == null)
            return;

        if (!effectSources.Remove(inst))
            return;

        inst.OnEffectTriggered -= HandleItemEffect;
    }

    void ClearEffectSubscriptions()
    {
        foreach (var inst in effectSources)
        {
            if (inst != null)
                inst.OnEffectTriggered -= HandleItemEffect;
        }
        effectSources.Clear();
    }

    void RemoveOwnedModifiers(ItemInstance inst)
    {
        if (inst == null)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        player.Stats.RemoveModifiers(layer: StatLayer.Owned, source: inst.UniqueId);
    }

    void HandleItemEffect(ItemEffectDto effect, ItemInstance source, string sourceUid)
    {
        var mgr = ItemEffectManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[ItemManager] ItemEffectManager is null.");
            return;
        }

        mgr.ApplyEffect(effect, source, sourceUid ?? source.UniqueId);
    }

    void Update()
    {
        if (!isPlayActive)
            return;

        float delta = Time.deltaTime;
        if (delta > 0f)
            HandleTime(delta);

        if (tickIntervalSeconds <= 0f)
            return;

        tickTimer += delta;
        while (tickTimer >= tickIntervalSeconds)
        {
            tickTimer -= tickIntervalSeconds;
            TriggerAll(ItemTriggerType.OnTick);
        }
    }

    void HandleTime(float deltaSeconds)
    {
        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
            slots[i]?.HandleTime(deltaSeconds);
    }

    void RemoveController(ItemInstance inst)
    {
        if (inst == null)
            return;

        if (!controllerMap.TryGetValue(inst, out var ctrl) || ctrl == null)
            return;

        controllerMap.Remove(inst);
        controllers.Remove(ctrl);
        Destroy(ctrl.gameObject);
    }

    void ClearControllers()
    {
        for (int i = controllers.Count - 1; i >= 0; i--)
        {
            var c = controllers[i];
            if (c != null)
                Destroy(c.gameObject);
        }
        controllers.Clear();
        controllerMap.Clear();
    }

    void ArrangeOrbitControllers()
    {
        int total = controllers.Count;
        for (int i = 0; i < total; i++)
        {
            var ctrl = controllers[i];
            if (ctrl == null)
                continue;

            InitializeOrbit(ctrl, i, total);
        }
    }

    void InitializeOrbit(ItemController controller, int index, int total)
    {
        if (controller == null || attachTarget == null)
            return;

        var orbit = controller.GetComponent<ItemOrbitController>();
        if (orbit == null)
            return;

        orbit.Initialize(attachTarget, index, total, orbitRadius, orbitPeriodSeconds);
    }

    void OnDisable()
    {
        UnsubscribePlayer();
        ClearControllers();
        ClearEffectSubscriptions();
    }

    void ResetItemRuntimeState()
    {
        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
            slots[i]?.ResetRuntimeState();
    }

    void SubscribePlayer()
    {
        UnsubscribePlayer();

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        subscribedPlayer = player;
        lastCurrency = subscribedPlayer.Currency;
        subscribedPlayer.OnCurrencyChanged += HandleCurrencyChanged;
    }

    void UnsubscribePlayer()
    {
        if (subscribedPlayer == null)
            return;

        subscribedPlayer.OnCurrencyChanged -= HandleCurrencyChanged;
        subscribedPlayer = null;
        lastCurrency = 0;
    }

    void HandleCurrencyChanged()
    {
        int current = subscribedPlayer?.Currency ?? 0;
        bool spent = current < lastCurrency;
        lastCurrency = current;

        if (spent)
            TriggerAll(ItemTriggerType.OnCurrencySpent);

        TriggerAll(ItemTriggerType.OnCurrencyChanged);
    }

    public bool RemoveItemInstance(ItemInstance item, bool storeUpgrades)
    {
        if (item == null || inventory == null)
            return false;

        int index = FindItemIndex(item);
        if (index < 0)
            return false;

        if (storeUpgrades)
            StoreUpgrades(item);

        return inventory.TryRemoveAt(index, out _);
    }

    public bool SellItemInstance(ItemInstance item, bool storeUpgrades)
    {
        if (item == null || inventory == null)
            return false;

        if (!ItemRepository.TryGet(item.Id, out var dto) || dto == null)
            return false;

        int basePrice = ShopManager.CalculateSellPrice(dto.price);
        int price = basePrice + item.SellValueBonus;
        if (price < 0)
            price = 0;

        CurrencyManager.Instance?.AddCurrency(price);
        AudioManager.Instance?.Play("Buy");
        return RemoveItemInstance(item, storeUpgrades);
    }

    int FindItemIndex(ItemInstance item)
    {
        if (item == null)
            return -1;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (ReferenceEquals(inventory.GetSlot(i), item))
                return i;
        }

        return -1;
    }

    void StoreUpgrades(ItemInstance item)
    {
        if (item == null)
            return;

        var upgrades = item.Upgrades;
        if (upgrades == null || upgrades.Count == 0)
            return;

        var upgradeManager = UpgradeManager.Instance;
        var inventoryManager = UpgradeInventoryManager.Instance;
        if (upgradeManager == null || inventoryManager == null)
            return;

        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgrade = upgrades[i];
            if (upgrade != null)
                inventoryManager.Add(upgrade);
        }

        upgradeManager.RemoveUpgrade(item);
    }
}
