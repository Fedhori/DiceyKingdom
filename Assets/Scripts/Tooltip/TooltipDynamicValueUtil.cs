using System;
using System.Collections.Generic;
using Data;
using GameStats;

public static class TooltipDynamicValueUtil
{
    public static void TryAddCurrentValueArgs(
        ItemEffectDto effect,
        int effectIndex,
        ItemInstance item,
        UpgradeInstance upgrade,
        Dictionary<string, object> args)
    {
        if (args == null || effect == null || !effect.showCurrentValue)
            return;

        string statId = effect.statId;
        if (string.IsNullOrEmpty(statId))
        {
            AppendCurrentValueArgs(args, effectIndex, 0d);
            return;
        }

        if (!TryResolveStats(effect, item, upgrade, out var stats, out var sourceUid, out var fallbackUid))
        {
            AppendCurrentValueArgs(args, effectIndex, 0d);
            return;
        }

        double value = SumModifiers(stats, statId, effect.duration, effect.effectMode, sourceUid);
        if (Math.Abs(value) <= 0d && !string.IsNullOrEmpty(fallbackUid) && !string.Equals(fallbackUid, sourceUid, StringComparison.Ordinal))
            value = SumModifiers(stats, statId, effect.duration, effect.effectMode, fallbackUid);
        AppendCurrentValueArgs(args, effectIndex, value);
    }

    public static void TryAddEstimatedValueArgs(
        ItemEffectDto effect,
        int effectIndex,
        ItemInstance item,
        UpgradeInstance upgrade,
        Dictionary<string, object> args)
    {
        if (args == null || effect == null || !effect.showCurrentValue)
            return;

        if (string.IsNullOrEmpty(effect.multiplier))
            return;

        if (!TryResolveStats(effect, item, upgrade, out var stats, out var sourceUid, out var fallbackUid))
            return;

        if (HasSourceModifiers(stats, effect, sourceUid))
            return;

        if (!string.IsNullOrEmpty(fallbackUid) && HasSourceModifiers(stats, effect, fallbackUid))
            return;

        double estimated = EstimateValue(effect, item, upgrade);
        AppendCurrentValueArgs(args, effectIndex, estimated);
    }

    static bool TryResolveStats(ItemEffectDto effect, ItemInstance item, UpgradeInstance upgrade, out StatSet stats, out string sourceUid, out string fallbackUid)
    {
        stats = null;
        sourceUid = null;
        fallbackUid = null;

        switch (effect.effectType)
        {
            case ItemEffectType.ModifyStat:
            case ItemEffectType.SetStat:
            {
                var player = PlayerManager.Instance?.Current;
                if (player == null)
                    return false;

                stats = player.Stats;
                sourceUid = upgrade?.UniqueId ?? item?.UniqueId;
                fallbackUid = item?.UniqueId;
                return true;
            }
            case ItemEffectType.ModifyItemStat:
            case ItemEffectType.SetItemStat:
            {
                var targetItem = upgrade != null ? FindTargetItem(upgrade) : item;
                if (targetItem == null)
                    return false;

                stats = targetItem.Stats;
                sourceUid = upgrade?.UniqueId ?? item?.UniqueId;
                fallbackUid = targetItem.UniqueId;
                return true;
            }
            default:
                return false;
        }
    }

    static bool HasSourceModifiers(StatSet stats, ItemEffectDto effect, string sourceUid)
    {
        if (stats == null || effect == null || string.IsNullOrEmpty(effect.statId) || string.IsNullOrEmpty(sourceUid))
            return false;

        var slot = stats.GetOrCreateSlot(effect.statId);
        var modifiers = slot.Modifiers;
        for (int i = 0; i < modifiers.Count; i++)
        {
            var mod = modifiers[i];
            if (mod == null)
                continue;

            if (mod.Layer != effect.duration || mod.OpKind != effect.effectMode)
                continue;

            if (!string.Equals(mod.Source, sourceUid, StringComparison.Ordinal))
                continue;

            return true;
        }

        return false;
    }

    static double EstimateValue(ItemEffectDto effect, ItemInstance item, UpgradeInstance upgrade)
    {
        if (effect == null)
            return 0d;

        double multiplier = ItemEffectMultiplierResolver.Resolve(effect, item, useFirstEmptySlotForAdjacent: true);
        return effect.value * multiplier;
    }

    static double SumModifiers(StatSet stats, string statId, StatLayer layer, StatOpKind opKind, string source)
    {
        if (stats == null || string.IsNullOrEmpty(statId))
            return 0d;

        var slot = stats.GetOrCreateSlot(statId);
        var modifiers = slot.Modifiers;
        double sum = 0d;

        for (int i = 0; i < modifiers.Count; i++)
        {
            var mod = modifiers[i];
            if (mod == null)
                continue;

            if (mod.Layer != layer || mod.OpKind != opKind)
                continue;

            if (source != null && !string.Equals(mod.Source, source, StringComparison.Ordinal))
                continue;

            sum += mod.Value;
        }

        return sum;
    }

    static void AppendCurrentValueArgs(Dictionary<string, object> args, int effectIndex, double value)
    {
        if (args == null)
            return;

        string valueKey = $"currentValue{effectIndex}";
        if (!args.ContainsKey(valueKey))
            args[valueKey] = value.ToString("+0.##;-0.##;0");

        string percentKey = $"currentValuePercent{effectIndex}";
        if (!args.ContainsKey(percentKey))
            args[percentKey] = (value * 100d).ToString("+0.##;-0.##;0");
    }

    static ItemInstance FindTargetItem(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return null;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return null;

        var slots = inventory.Slots;
        for (int i = 0; i < slots.Count; i++)
        {
            var item = slots[i];
            var upgrades = item?.Upgrades;
            if (upgrades == null)
                continue;

            for (int u = 0; u < upgrades.Count; u++)
            {
                if (ReferenceEquals(upgrades[u], upgrade))
                    return item;
            }
        }

        return null;
    }
}
