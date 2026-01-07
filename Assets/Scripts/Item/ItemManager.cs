using System.Collections.Generic;
using UnityEngine;
using Data;
using GameStats;

public sealed class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [SerializeField] private string defaultItemId = "item.default";
    [SerializeField] private Transform attachTarget; // 플레이어 transform 등
    [SerializeField] private float tickIntervalSeconds = 1f;

    readonly List<ItemController> controllers = new();
    readonly Dictionary<ItemInstance, ItemController> controllerMap = new();
    readonly HashSet<ItemInstance> effectSources = new();
    readonly ItemInventory inventory = new();
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

    public ItemInventory Inventory => inventory;

    public int GetPierceBonus()
    {
        int total = 0;
        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var inst = slots[i];
            if (inst == null)
                continue;

            int bonus = inst.PierceBonus;
            if (bonus > 0)
                total += bonus;
        }

        return total;
    }

    public bool HasSideWallCollisionItem()
    {
        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var inst = slots[i];
            if (inst != null && inst.EnableSideWallCollision)
                return true;
        }

        return false;
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
        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
            slots[i]?.HandleTrigger(trigger);
    }

    public void TriggerOverflowDamage(int overflow)
    {
        if (overflow <= 0)
            return;

        if (!IsOverflowDamageEnabled())
            return;

        BlockManager.Instance?.ApplyOverflowDamage(overflow);
    }

    bool IsOverflowDamageEnabled()
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return false;

        return player.IsOverflowDamageEnabled;
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

            // OnItemAdded handles object attach.
        }
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
    }

    void SpawnController(ItemInstance inst)
    {
        if (inst == null || attachTarget == null)
        {
            Debug.LogWarning("[ItemManager] Missing instance/attachTarget");
            return;
        }

        var prefab = ResolveObjectPrefab(inst);
        if (prefab == null)
        {
            Debug.LogWarning($"[ItemManager] Object prefab not found for item '{inst.Id}'.");
            return;
        }

        var go = Instantiate(prefab, attachTarget.position, Quaternion.identity, attachTarget);
        var ctrl = go.GetComponent<ItemController>();
        if (ctrl == null)
        {
            Debug.LogWarning("[ItemManager] ItemController missing on object prefab.");
            Destroy(go);
            return;
        }

        ctrl.BindItem(inst, attachTarget);
        controllers.Add(ctrl);
        controllerMap[inst] = ctrl;
    }

    GameObject ResolveObjectPrefab(ItemInstance inst)
    {
        if (inst == null || string.IsNullOrEmpty(inst.Id))
            return null;

        var registry = ItemPrefabRegistry.Instance;
        if (registry == null)
            return null;

        return registry.GetOrDefault(inst.Id);
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
                current.HandleTrigger(ItemTriggerType.OnItemAdded);

                if (isPlayActive && current.IsObject)
                    SpawnController(current);

                if (HasSideWallCollisionFlag(current))
                    RefreshSideWallCollision();
                break;
            case ItemInventory.SlotChangeType.Remove:
                if (previous == null)
                    return;

                RemoveController(previous);
                UnsubscribeEffects(previous);
                RemoveOwnedModifiers(previous);

                if (HasSideWallCollisionFlag(previous))
                    RefreshSideWallCollision();
                break;
        }
    }

    void RefreshSideWallCollision()
    {
        var factory = ProjectileFactory.Instance;
        if (factory == null)
            return;

        factory.SetSideWallCollisionEnabled(HasSideWallCollisionItem());
    }

    static bool HasSideWallCollisionFlag(ItemInstance inst)
    {
        return inst != null && inst.EnableSideWallCollision;
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

        player.Stats.RemoveModifiers(layer: StatLayer.Owned, source: inst);
    }

    void HandleItemEffect(ItemEffectDto effect, ItemInstance source)
    {
        var mgr = ItemEffectManager.Instance;
        if (mgr == null)
        {
            Debug.LogWarning("[ItemManager] ItemEffectManager is null.");
            return;
        }

        mgr.ApplyEffect(effect, source);
    }

    void Update()
    {
        if (!isPlayActive)
            return;

        if (tickIntervalSeconds <= 0f)
            return;

        tickTimer += Time.deltaTime;
        while (tickTimer >= tickIntervalSeconds)
        {
            tickTimer -= tickIntervalSeconds;
            TriggerAll(ItemTriggerType.OnTick);
        }
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

    void OnDisable()
    {
        ClearControllers();
        ClearEffectSubscriptions();
    }

    void ResetItemRuntimeState()
    {
        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
            slots[i]?.ResetRuntimeState();
    }
}
