using System;
using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public sealed class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    public bool IsLoadMode { get; private set; }
    public int CurrentRunSeed { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void BeginLoadMode()
    {
        IsLoadMode = true;
    }

    public void EndLoadMode()
    {
        IsLoadMode = false;
    }

    public void SetRunSeed(int seed)
    {
        CurrentRunSeed = seed;
    }

    public SaveData BuildSaveData()
    {
        var data = new SaveData();
        FillMeta(data.meta);
        FillRun(data.run);
        FillPlayer(data.player);
        FillInventory(data.inventory);
        FillUpgradeInventory(data.upgradeInventory);
        return data;
    }

    public bool SaveOnStageStart()
    {
        var data = BuildSaveData();
        var result = SaveService.WriteSave(data);
        if (result.IsSuccess)
            return true;

        SaveLogger.LogError($"Save failed: {result.Message}");
        var modal = ModalManager.Instance;
        if (modal != null)
        {
            modal.ShowInfo(
                "modal",
                "modal.saveFailed.title",
                "modal",
                "modal.saveFailed.message",
                () => { });
        }

        return false;
    }

    public bool ApplySaveData(SaveData data)
    {
        if (data == null)
            return false;

        SetRunSeed(data.meta?.runSeed ?? 0);
        GameManager.Instance?.ResetRng(CurrentRunSeed);
        ApplyPlayer(data.player);
        ApplyInventory(data.inventory);
        ApplyUpgradeInventory(data.upgradeInventory);
        TriggerPostLoadItemEvents();
        return true;
    }

    void FillMeta(SaveMeta meta)
    {
        if (meta == null)
            return;

        meta.schemaVersion = SaveMeta.CurrentSchemaVersion;
        meta.appVersion = Application.version;
        meta.timestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        meta.runSeed = CurrentRunSeed;
    }

    void FillRun(SaveRun run)
    {
        if (run == null)
            return;

        run.stageIndex = StageManager.Instance?.CurrentStage?.StageIndex ?? -1;
    }

    void FillPlayer(SavePlayer player)
    {
        if (player == null)
            return;

        var instance = PlayerManager.Instance?.Current;
        if (instance == null)
            return;

        player.playerId = instance.Id;
        player.currency = instance.Currency;
        player.permanentStatModifiers = CollectPermanentModifiers(instance.Stats);
    }

    void FillInventory(SaveInventory inventory)
    {
        if (inventory == null)
            return;

        inventory.slots.Clear();

        var itemInventory = ItemManager.Instance?.Inventory;
        if (itemInventory == null)
            return;

        for (int i = 0; i < itemInventory.SlotCount; i++)
        {
            var item = itemInventory.GetSlot(i);
            var slot = new SaveItemSlot();
            if (item != null)
            {
                slot.itemId = item.Id;
                slot.itemUniqueId = item.UniqueId;
                slot.permanentStatModifiers = CollectPermanentModifiers(item.Stats);
                slot.upgrades = CollectUpgrades(item.Upgrades);
            }

            inventory.slots.Add(slot);
        }
    }

    void FillUpgradeInventory(SaveUpgradeInventory inventory)
    {
        if (inventory == null)
            return;

        inventory.upgrades.Clear();

        var upgradeInventory = UpgradeInventoryManager.Instance;
        if (upgradeInventory == null)
            return;

        var upgrades = upgradeInventory.Upgrades;
        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgrade = upgrades[i];
            if (upgrade == null)
                continue;

            inventory.upgrades.Add(new SaveUpgrade
            {
                upgradeId = upgrade.Id,
                upgradeUniqueId = upgrade.UniqueId
            });
        }
    }

    static List<SaveUpgrade> CollectUpgrades(IReadOnlyList<UpgradeInstance> upgrades)
    {
        var list = new List<SaveUpgrade>();
        if (upgrades == null)
            return list;

        for (int i = 0; i < upgrades.Count; i++)
        {
            var upgrade = upgrades[i];
            if (upgrade == null)
                continue;

            list.Add(new SaveUpgrade
            {
                upgradeId = upgrade.Id,
                upgradeUniqueId = upgrade.UniqueId
            });
        }

        return list;
    }

    static List<SaveStatModifier> CollectPermanentModifiers(StatSet stats)
    {
        var list = new List<SaveStatModifier>();
        if (stats == null)
            return list;

        foreach (var slot in stats.AllSlots)
        {
            if (slot == null)
                continue;

            var modifiers = slot.Modifiers;
            if (modifiers == null || modifiers.Count == 0)
                continue;

            for (int i = 0; i < modifiers.Count; i++)
            {
                var modifier = modifiers[i];
                if (modifier == null)
                    continue;

                if (modifier.Layer != StatLayer.Permanent)
                    continue;

                list.Add(new SaveStatModifier
                {
                    statId = modifier.StatId,
                    op = modifier.OpKind,
                    value = modifier.Value,
                    layer = modifier.Layer,
                    source = modifier.Source,
                    priority = modifier.Priority
                });
            }
        }

        return list;
    }

    void ApplyPlayer(SavePlayer player)
    {
        if (player == null)
            return;

        var manager = PlayerManager.Instance;
        if (manager == null)
            return;

        if (!string.IsNullOrEmpty(player.playerId))
            manager.CreatePlayer(player.playerId);

        var instance = manager.Current;
        if (instance == null)
            return;

        instance.SetCurrency(player.currency);
        ApplyPermanentModifiers(instance.Stats, player.permanentStatModifiers);
    }

    void ApplyInventory(SaveInventory inventory)
    {
        if (inventory == null)
            return;

        var manager = ItemManager.Instance;
        if (manager == null)
            return;

        var itemInventory = manager.Inventory;
        if (itemInventory == null)
            return;

        itemInventory.Clear();

        var slots = inventory.slots;
        if (slots == null)
            return;

        int count = Mathf.Min(itemInventory.SlotCount, slots.Count);
        for (int i = 0; i < count; i++)
        {
            var slot = slots[i];
            if (slot == null || string.IsNullOrEmpty(slot.itemId))
                continue;

            if (!ItemRepository.TryGet(slot.itemId, out var dto) || dto == null)
                continue;

            var item = new ItemInstance(dto, slot.itemUniqueId);
            ApplyPermanentModifiers(item.Stats, slot.permanentStatModifiers);

            var upgrades = BuildUpgrades(slot.upgrades);
            if (upgrades.Count > 0)
            {
                var upgradeManager = UpgradeManager.Instance;
                upgradeManager?.ApplyUpgrades(item, upgrades);
            }

            itemInventory.TrySetSlot(i, item);
        }
    }

    void ApplyUpgradeInventory(SaveUpgradeInventory inventory)
    {
        if (inventory == null)
            return;

        var manager = UpgradeInventoryManager.Instance;
        if (manager == null)
            return;

        manager.Clear();

        var upgrades = BuildUpgrades(inventory.upgrades);
        for (int i = 0; i < upgrades.Count; i++)
            manager.Add(upgrades[i]);
    }

    static List<UpgradeInstance> BuildUpgrades(IReadOnlyList<SaveUpgrade> upgrades)
    {
        var list = new List<UpgradeInstance>();
        if (upgrades == null)
            return list;

        for (int i = 0; i < upgrades.Count; i++)
        {
            var saved = upgrades[i];
            if (saved == null || string.IsNullOrEmpty(saved.upgradeId))
                continue;

            if (!UpgradeRepository.TryGet(saved.upgradeId, out var dto) || dto == null)
                continue;

            list.Add(new UpgradeInstance(dto, saved.upgradeUniqueId));
        }

        return list;
    }

    static void ApplyPermanentModifiers(StatSet stats, IReadOnlyList<SaveStatModifier> modifiers)
    {
        if (stats == null || modifiers == null)
            return;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var modifier = modifiers[i];
            if (modifier == null)
                continue;

            if (modifier.layer != StatLayer.Permanent)
                continue;

            stats.AddModifier(new StatModifier(
                modifier.statId,
                modifier.op,
                modifier.value,
                modifier.layer,
                modifier.source,
                modifier.priority));
        }
    }

    static void TriggerPostLoadItemEvents()
    {
        var manager = ItemManager.Instance;
        if (manager == null)
            return;

        manager.TriggerAll(ItemTriggerType.OnItemChanged);
        manager.TriggerAll(ItemTriggerType.OnCurrencyChanged);
    }
}
