using System.Collections.Generic;
using UnityEngine;
using Data;

public sealed class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [SerializeField] private string defaultItemId = "item.default";
    [SerializeField] private Transform attachTarget; // 플레이어 transform 등

    readonly List<ItemController> controllers = new();
    readonly Dictionary<ItemInstance, ItemController> controllerMap = new();
    readonly ItemInventory inventory = new();

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

    public void InitializeFromPlayer(PlayerInstance player)
    {
        ClearControllers();
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
            if (!inventory.TryAdd(inst, out _))
            {
                Debug.LogWarning("[ItemManager] Inventory full. Skipping item add.");
                continue;
            }

            // OnSlotChanged handles attach for object items.
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

        return registry.TryGet(inst.Id, out var prefab) ? prefab : null;
    }

    void HandleSlotChanged(int slotIndex, ItemInstance previous, ItemInstance current)
    {
        _ = slotIndex;

        if (previous != null && !ContainsInstance(previous))
            RemoveController(previous);

        if (current == null)
            return;

        if (!current.IsObject)
            return;

        if (controllerMap.ContainsKey(current))
            return;

        SpawnController(current);
    }

    bool ContainsInstance(ItemInstance inst)
    {
        if (inst == null)
            return false;

        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            if (ReferenceEquals(slots[i], inst))
                return true;
        }

        return false;
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
    }

    public void ClearAll()
    {
        inventory.Clear();
        ClearControllers();
    }

    public Transform GetAttachTarget()
    {
        return attachTarget;
    }
}
