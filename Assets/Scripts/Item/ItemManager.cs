using System.Collections.Generic;
using UnityEngine;
using Data;

public sealed class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [SerializeField] private string defaultItemId = "item.default";
    [SerializeField] private Transform attachTarget; // 플레이어 transform 등
    [SerializeField] private ItemController itemControllerPrefab;

    readonly List<ItemInstance> items = new();
    readonly List<ItemController> controllers = new();

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
    }

    public IReadOnlyList<ItemInstance> Items => items;

    public void InitializeFromPlayer(PlayerInstance player)
    {
        ClearControllers();
        items.Clear();

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
            items.Add(inst);
            SpawnController(inst);
        }
    }

    void SpawnController(ItemInstance inst)
    {
        if (itemControllerPrefab == null || inst == null || attachTarget == null)
        {
            Debug.LogWarning("[ItemManager] Missing prefab/instance/attachTarget");
            return;
        }

        var ctrl = Instantiate(itemControllerPrefab, attachTarget.position, Quaternion.identity, attachTarget);
        ctrl.BindItem(inst, attachTarget);
        controllers.Add(ctrl);
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
    }

    void OnDisable()
    {
        ClearControllers();
    }

    public void ClearAll()
    {
        items.Clear();
        ClearControllers();
    }

    public Transform GetAttachTarget()
    {
        return attachTarget;
    }
}
