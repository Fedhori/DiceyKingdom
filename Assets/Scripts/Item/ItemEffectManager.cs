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

    public void ApplyEffect(ItemEffectDto dto, ItemInstance item)
    {
        if (dto == null || item == null)
            return;

        switch (dto.effectType)
        {
            case ItemEffectType.ModifyStat:
                ApplyPlayerStat(dto, item);
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
            case ItemEffectType.AddSellValue:
                ApplySellValue(dto, item);
                break;
            case ItemEffectType.ModifyItemStat:
                ModifyItemStat(dto, item);
                break;
            case ItemEffectType.SetItemStat:
                SetItemStat(dto, item);
                break;
            case ItemEffectType.SetStat:
                ApplySetStat(dto, item);
                break;
            case ItemEffectType.ModifyBaseIncome:
                ApplyBaseIncome(dto, item);
                break;
            case ItemEffectType.ApplyDamageToAllBlocks:
                ApplyDamageToAllBlocks(dto, item);
                break;
            case ItemEffectType.SetItemStatus:
                SetItemStatus(dto, item);
                break;
            case ItemEffectType.ModifyTriggerRepeat:
                ModifyTriggerRepeat(dto, item);
                break;
            default:
                Debug.LogWarning($"[ItemEffectManager] Unsupported effect type: {dto.effectType}");
                break;
        }
    }

    void ApplyPlayerStat(ItemEffectDto dto, ItemInstance item)
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

        double multiplier = ResolveMultiplier(dto, item);
        double value = dto.value * multiplier;
        if (Math.Abs(value) <= 0d)
            return;

        player.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: dto.effectMode,
            value: value,
            layer: layer,
            source: item
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

        for (int i = 0; i < count; i++)
        {
            var pos = playArea.GetRandomPositionInPlayArea();
            Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude <= 0.001f)
                dir = Vector2.up;

            factory.SpawnProjectile(pos, dir, item);
        }
    }

    void ApplyStatusToRandomBlocks(ItemEffectDto dto, ItemInstance item)
    {
        if (item == null || dto == null)
            return;

        if (item.StatusType == BlockStatusType.Unknown)
            return;

        int stack = item.StatusStack;
        if (stack <= 0)
            return;

        int count = Mathf.Max(0, Mathf.FloorToInt(dto.value));
        if (count <= 0)
            return;

        var manager = BlockManager.Instance;
        if (manager == null)
            return;

        manager.ApplyStatusToRandomBlocks(item.StatusType, count, stack);
    }

    void ApplySellValue(ItemEffectDto dto, ItemInstance item)
    {
        if (item == null || dto == null)
            return;

        int amount = Mathf.FloorToInt(dto.value);
        if (amount <= 0)
            return;

        item.AddSellValueBonus(amount);
    }

    void ModifyItemStat(ItemEffectDto dto, ItemInstance sourceItem)
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

        var modifier = new StatModifier(
            dto.statId,
            dto.effectMode,
            dto.value,
            dto.duration,
            targetItem);
        targetItem.Stats.AddModifier(modifier);
    }

    void SetItemStat(ItemEffectDto dto, ItemInstance sourceItem)
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

        double multiplier = ResolveMultiplier(dto, sourceItem);
        double value = dto.value * multiplier;

        targetItem.Stats.RemoveModifiers(dto.statId, dto.duration, sourceItem);

        if (Math.Abs(value) <= 0d)
            return;

        targetItem.Stats.AddModifier(new StatModifier(
            dto.statId,
            dto.effectMode,
            value,
            dto.duration,
            sourceItem));
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

    void ApplySetStat(ItemEffectDto dto, ItemInstance item)
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

        double multiplier = ResolveMultiplier(dto, item);
        double value = dto.value * multiplier;

        player.Stats.RemoveModifiers(dto.statId, dto.duration, item);

        if (Math.Abs(value) <= 0d)
            return;

        player.Stats.AddModifier(new StatModifier(
            dto.statId,
            dto.effectMode,
            value,
            dto.duration,
            item));
    }

    double ResolveMultiplier(ItemEffectDto dto, ItemInstance sourceItem)
    {
        if (dto == null || string.IsNullOrEmpty(dto.multiplier))
            return 1d;

        switch (dto.multiplier)
        {
            case "normalItemCount":
                return GetNormalItemCount();
            case "currencyAtMost":
                return GetCurrencyAtMostMultiplier(dto.threshold);
            case "adjacentEmptySlotCount":
                return GetAdjacentEmptySlotCount(sourceItem);
            case "weaponCount":
                return GetWeaponCount();
            default:
                Debug.LogWarning($"[ItemEffectManager] Unknown multiplier '{dto.multiplier}'.");
                return 1d;
        }
    }

    int GetAdjacentEmptySlotCount(ItemInstance sourceItem)
    {
        if (sourceItem == null)
            return 0;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return 0;

        int sourceIndex = FindItemIndex(inventory, sourceItem);
        if (sourceIndex < 0)
            return 0;

        int slotsPerRow = Mathf.Max(1, GameConfig.ItemSlotsPerRow);
        int count = 0;

        if (sourceIndex % slotsPerRow != 0)
        {
            int left = sourceIndex - 1;
            if (left >= 0 && inventory.IsSlotEmpty(left))
                count++;
        }

        if ((sourceIndex + 1) % slotsPerRow != 0)
        {
            int right = sourceIndex + 1;
            if (right < inventory.SlotCount && inventory.IsSlotEmpty(right))
                count++;
        }

        int up = sourceIndex - slotsPerRow;
        if (up >= 0 && inventory.IsSlotEmpty(up))
            count++;

        int down = sourceIndex + slotsPerRow;
        if (down < inventory.SlotCount && inventory.IsSlotEmpty(down))
            count++;

        return count;
    }

    int GetWeaponCount()
    {
        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return 0;

        int count = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var inst = inventory.GetSlot(i);
            if (inst == null)
                continue;

            if (inst.IsWeapon())
                count++;
        }

        return count;
    }

    int GetNormalItemCount()
    {
        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return 0;

        int count = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var inst = inventory.GetSlot(i);
            if (inst != null && inst.Rarity == ItemRarity.Common)
                count++;
        }

        return count;
    }

    double GetCurrencyAtMostMultiplier(int threshold)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return 0d;

        if (threshold < 0)
        {
            Debug.LogWarning($"[ItemEffectManager] currencyAtMost threshold < 0: {threshold}");
            threshold = 0;
        }

        return player.Currency <= threshold ? 1d : 0d;
    }

    void ApplyBaseIncome(ItemEffectDto dto, ItemInstance item)
    {
        if (dto == null || item == null)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        player.Stats.AddModifier(new StatModifier(
            PlayerStatIds.BaseIncomeBonus,
            dto.effectMode,
            dto.value,
            dto.duration,
            item));
    }

    void ApplyDamageToAllBlocks(ItemEffectDto dto, ItemInstance item)
    {
        if (dto == null || item == null)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        float multiplier = item.DamageMultiplier;
        if (dto.value > 0f)
            multiplier *= dto.value;

        int damage = Mathf.Max(1, Mathf.FloorToInt((float)(multiplier * player.Power)));
        if (damage <= 0)
            return;

        BlockManager.Instance?.ApplyDamageToAllBlocks(damage, item);
    }

    void SetItemStatus(ItemEffectDto dto, ItemInstance sourceItem)
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

        int stack = Mathf.Max(1, Mathf.FloorToInt(dto.value));
        targetItem.SetStatusOverride(dto.statusType, stack);
    }

    void ModifyTriggerRepeat(ItemEffectDto dto, ItemInstance sourceItem)
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

        targetItem.AddTriggerRepeatModifier(dto.triggerType, dto.effectMode, dto.value, dto.duration, targetItem);
    }
}
