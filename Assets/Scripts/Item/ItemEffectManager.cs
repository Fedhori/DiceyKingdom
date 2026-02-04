using System;
using Data;
using GameStats;
using UnityEngine;

public sealed class ItemEffectManager : MonoBehaviour
{
    public static ItemEffectManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ApplyEffect(ItemEffectDto dto, ItemInstance item, string sourceUid)
    {
        if (dto == null || item == null)
            return;

        if (ShouldFilterPermanent(dto))
            return;

        switch (dto.effectType)
        {
            case ItemEffectType.ModifyStat:
                ApplyPlayerStat(dto, item, sourceUid);
                break;
            case ItemEffectType.AddCurrency:
                ApplyCurrency(dto);
                break;
            case ItemEffectType.SpawnProjectile:
                SpawnProjectiles(dto, item);
                break;
            case ItemEffectType.ApplyStatusToRandomBlocks:
                ApplyStatusToRandomBlocks(dto, item);
                break;
            case ItemEffectType.ModifyItemStat:
                ModifyItemStat(dto, item, sourceUid);
                break;
            case ItemEffectType.SetItemStat:
                SetItemStat(dto, item, sourceUid);
                break;
            case ItemEffectType.SetStat:
                ApplySetStat(dto, item, sourceUid);
                break;
            case ItemEffectType.ApplyDamageToAllBlocks:
                ApplyDamageToAllBlocks(dto, item);
                break;
            case ItemEffectType.SetItemStatus:
                SetItemStatus(dto, item, sourceUid);
                break;
            case ItemEffectType.ModifyTriggerRepeat:
                ModifyTriggerRepeat(dto, item, sourceUid);
                break;
            case ItemEffectType.ChargeNextProjectileDamage:
                ChargeNextProjectileDamage(item);
                break;
            case ItemEffectType.AddGuaranteedCriticalHits:
                AddGuaranteedCriticalHits(dto, item);
                break;
            case ItemEffectType.SetGuaranteedCriticalHits:
                SetGuaranteedCriticalHits(dto, item);
                break;
            case ItemEffectType.RemoveSelf:
                RemoveSelf(item);
                break;
            case ItemEffectType.SellSelf:
                SellSelf(item);
                break;
            default:
                Debug.LogWarning($"[ItemEffectManager] Unsupported effect type: {dto.effectType}");
                break;
        }
    }

    void ApplyPlayerStat(ItemEffectDto dto, ItemInstance item, string sourceUid)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[ItemEffectManager] ModifyStat with empty statId.");
            return;
        }

        var layer = dto.duration;

        double multiplier = ItemEffectMultiplierResolver.Resolve(dto, item);
        double value = dto.value * multiplier;
        if (Math.Abs(value) <= 0d)
            return;

        if (layer == StatLayer.Temporary && dto.durationSeconds > 0f)
        {
            player.AddTimedModifier(dto.statId, dto.effectMode, value, dto.durationSeconds);
            return;
        }

        string source = sourceUid ?? item.UniqueId;
        player.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: dto.effectMode,
            value: value,
            layer: layer,
            source: source
        ));
    }

    void ApplyCurrency(ItemEffectDto dto)
    {
        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
            return;

        currencyMgr.AddCurrency(Mathf.RoundToInt(dto.value));
    }

    void SpawnProjectiles(ItemEffectDto dto, ItemInstance item)
    {
        if (item == null || dto == null)
            return;

        var factory = ProjectileFactory.Instance;
        if (factory == null)
            return;

        var playArea = BlockManager.Instance;
        if (playArea == null)
            return;

        int count = Mathf.Max(0, Mathf.FloorToInt(dto.value));
        if (count <= 0)
            return;

        var rng = GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();
        for (int i = 0; i < count; i++)
        {
            var pos = playArea.GetRandomPositionInPlayArea();
            double angle = rng.NextDouble() * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos((float)angle), Mathf.Sin((float)angle));

            factory.SpawnProjectile(pos, dir, item);
        }
    }

    void ApplyStatusToRandomBlocks(ItemEffectDto dto, ItemInstance item)
    {
        if (item == null || dto == null)
            return;

        if (dto.statusType == BlockStatusType.Unknown)
            return;

        if (!StatusUtil.TryGetStatusKey(dto.statusType, out var statId))
        {
            Debug.LogWarning($"[ItemEffectManager] ApplyStatusToRandomBlocks has no statId for {dto.statusType}.");
            return;
        }

        int stack = StatusUtil.GetItemStatusValue(item, statId);
        if (stack <= 0)
            return;

        int count = Mathf.Max(0, Mathf.FloorToInt(dto.value));
        if (count <= 0)
            return;

        var manager = BlockManager.Instance;
        if (manager == null)
            return;

        manager.ApplyStatusToRandomBlocks(dto.statusType, count, stack);
    }

    void ModifyItemStat(ItemEffectDto dto, ItemInstance sourceItem, string sourceUid)
    {
        if (dto == null || sourceItem == null)
            return;

        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[ItemEffectManager] ModifyItemStat with empty statId.");
            return;
        }

        var targetItem = ResolveTargetItem(dto.target, sourceItem);
        if (targetItem == null)
            return;

        if (dto.statId == ItemStatIds.DamageMultiplier && targetItem.DamageMultiplier <= 0f)
            return;

        string source = sourceUid ?? sourceItem.UniqueId;
        var modifier = new StatModifier(
            dto.statId,
            dto.effectMode,
            dto.value,
            dto.duration,
            source);
        targetItem.Stats.AddModifier(modifier);
    }

    void SetItemStat(ItemEffectDto dto, ItemInstance sourceItem, string sourceUid)
    {
        if (dto == null || sourceItem == null)
            return;

        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[ItemEffectManager] SetItemStat with empty statId.");
            return;
        }

        var targetItem = ResolveTargetItem(dto.target, sourceItem);
        if (targetItem == null)
            return;

        double multiplier = ItemEffectMultiplierResolver.Resolve(dto, sourceItem);
        double value = dto.value * multiplier;

        string source = sourceUid ?? sourceItem.UniqueId;
        targetItem.Stats.RemoveModifiers(dto.statId, dto.duration, source);

        if (Math.Abs(value) <= 0d)
            return;

        targetItem.Stats.AddModifier(new StatModifier(
            dto.statId,
            dto.effectMode,
            value,
            dto.duration,
            source));
    }

    ItemInstance ResolveTargetItem(ItemEffectTarget target, ItemInstance sourceItem)
    {
        switch (target)
        {
            case ItemEffectTarget.Self:
                return sourceItem;
            case ItemEffectTarget.Right:
                return GetRightItem(sourceItem);
            default:
                Debug.LogWarning($"[ItemEffectManager] Unsupported target '{target}'.");
                return null;
        }
    }

    ItemInstance GetRightItem(ItemInstance sourceItem)
    {
        if (sourceItem == null)
            return null;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return null;

        int sourceIndex = FindItemIndex(inventory, sourceItem);
        if (sourceIndex < 0)
            return null;

        int slotsPerRow = Mathf.Max(1, GameConfig.ItemSlotsPerRow);
        if ((sourceIndex + 1) % slotsPerRow == 0)
            return null;

        int rightIndex = sourceIndex + 1;
        if (rightIndex >= inventory.SlotCount)
            return null;

        var targetItem = inventory.GetSlot(rightIndex);
        return targetItem;
    }

    int FindItemIndex(ItemInventory inventory, ItemInstance item)
    {
        if (inventory == null || item == null)
            return -1;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (ReferenceEquals(inventory.GetSlot(i), item))
                return i;
        }

        return -1;
    }

    void ApplySetStat(ItemEffectDto dto, ItemInstance item, string sourceUid)
    {
        if (dto == null || item == null)
            return;

        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[ItemEffectManager] SetStat with empty statId.");
            return;
        }

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        double multiplier = ItemEffectMultiplierResolver.Resolve(dto, item);
        double value = dto.value * multiplier;

        string source = sourceUid ?? item.UniqueId;
        player.Stats.RemoveModifiers(dto.statId, dto.duration, source);

        if (Math.Abs(value) <= 0d)
            return;

        player.Stats.AddModifier(new StatModifier(
            dto.statId,
            dto.effectMode,
            value,
            dto.duration,
            source));
    }

    void ApplyDamageToAllBlocks(ItemEffectDto dto, ItemInstance item)
    {
        if (dto == null || item == null)
            return;

        float damageScale = dto.value;
        if (damageScale <= 0f)
            return;

        BlockManager.Instance?.ApplyDamageToAllBlocks(damageScale, item);
    }

    void SetItemStatus(ItemEffectDto dto, ItemInstance sourceItem, string sourceUid)
    {
        if (dto == null || sourceItem == null)
            return;

        var targetItem = ResolveTargetItem(dto.target, sourceItem);
        if (targetItem == null)
            return;

        if (dto.statusType == BlockStatusType.Unknown)
        {
            Debug.LogWarning("[ItemEffectManager] SetItemStatus with Unknown statusType.");
            return;
        }

        if (!StatusUtil.TryGetStatusKey(dto.statusType, out var statId))
        {
            Debug.LogWarning($"[ItemEffectManager] SetItemStatus has no statId for {dto.statusType}.");
            return;
        }

        int stack = Mathf.Max(1, Mathf.FloorToInt(dto.value));
        if (stack <= 0)
            return;

        string source = sourceUid ?? sourceItem.UniqueId;
        targetItem.Stats.AddModifier(new StatModifier(
            statId,
            StatOpKind.Add,
            stack,
            StatLayer.Upgrade,
            source));
    }

    void ModifyTriggerRepeat(ItemEffectDto dto, ItemInstance sourceItem, string sourceUid)
    {
        if (dto == null || sourceItem == null)
            return;

        if (dto.triggerType == ItemTriggerType.Unknown)
        {
            Debug.LogWarning("[ItemEffectManager] ModifyTriggerRepeat with Unknown triggerType.");
            return;
        }

        var targetItem = ResolveTargetItem(dto.target, sourceItem);
        if (targetItem == null)
            return;

        string source = sourceUid ?? sourceItem.UniqueId;
        targetItem.AddTriggerRepeatModifier(dto.triggerType, dto.effectMode, dto.value, dto.duration, source);
    }

    static bool ShouldFilterPermanent(ItemEffectDto dto)
    {
        if (dto == null)
            return false;

        if (dto.duration != StatLayer.Permanent)
            return false;

        if (!UsesDuration(dto.effectType))
            return false;

        var save = SaveManager.Instance;
        return save != null && save.IsLoadMode;
    }

    static bool UsesDuration(ItemEffectType effectType)
    {
        switch (effectType)
        {
            case ItemEffectType.ModifyStat:
            case ItemEffectType.ModifyItemStat:
            case ItemEffectType.SetItemStat:
            case ItemEffectType.SetStat:
            case ItemEffectType.ModifyTriggerRepeat:
                return true;
            default:
                return false;
        }
    }

    void ChargeNextProjectileDamage(ItemInstance item)
    {
        if (item == null)
            return;

        item.TryChargeNextProjectileDamage();
    }

    void AddGuaranteedCriticalHits(ItemEffectDto dto, ItemInstance sourceItem)
    {
        if (dto == null || sourceItem == null)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        int count = Mathf.FloorToInt(dto.value);
        if (count <= 0)
            return;

        player.AddGuaranteedCriticalHits(count);
    }

    void SetGuaranteedCriticalHits(ItemEffectDto dto, ItemInstance sourceItem)
    {
        if (dto == null || sourceItem == null)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        int count = Mathf.FloorToInt(dto.value);
        player.SetGuaranteedCriticalHits(Mathf.Max(0, count));
    }

    void RemoveSelf(ItemInstance item)
    {
        if (item == null)
            return;

        var manager = ItemManager.Instance;
        if (manager == null)
            return;

        manager.RemoveItemInstance(item, storeUpgrades: true);
    }

    void SellSelf(ItemInstance item)
    {
        if (item == null)
            return;

        var manager = ItemManager.Instance;
        if (manager == null)
            return;

        manager.SellItemInstance(item, storeUpgrades: true);
    }
}
