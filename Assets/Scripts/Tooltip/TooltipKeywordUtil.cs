using System.Collections.Generic;
using Data;

public static class TooltipKeywordUtil
{
    public static IReadOnlyList<TooltipKeywordEntry> BuildForItem(ItemInstance item)
    {
        if (item == null)
            return null;

        if (item.StatusType == BlockStatusType.Unknown || item.StatusStack <= 0)
            return null;

        List<TooltipKeywordEntry> entries = null;
        AppendStatusKeyword(item.StatusType, ref entries);
        return entries;
    }

    public static IReadOnlyList<TooltipKeywordEntry> BuildForUpgrade(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return null;

        var effects = upgrade.Effects;
        if (effects == null || effects.Count == 0)
            return null;

        List<TooltipKeywordEntry> entries = null;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            if (effect.effectType != ItemEffectType.SetItemStatus
                && effect.effectType != ItemEffectType.ApplyStatusToRandomBlocks)
            {
                continue;
            }

            AppendStatusKeyword(effect.statusType, ref entries);
        }

        return entries;
    }

    static void AppendStatusKeyword(BlockStatusType type, ref List<TooltipKeywordEntry> entries)
    {
        if (!TryGetStatusKeyword(type, out var entry))
            return;

        entries ??= new List<TooltipKeywordEntry>();
        entries.Add(entry);
    }

    static bool TryGetStatusKeyword(BlockStatusType type, out TooltipKeywordEntry entry)
    {
        switch (type)
        {
            case BlockStatusType.Freeze:
                entry = new TooltipKeywordEntry(
                    "tooltip.keyword.freeze.title",
                    "tooltip.keyword.freeze.body");
                return true;
            default:
                entry = default;
                return false;
        }
    }
}
