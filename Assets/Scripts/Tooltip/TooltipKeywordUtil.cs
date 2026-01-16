using System.Collections.Generic;
using Data;

public static class TooltipKeywordUtil
{
    public static IReadOnlyList<TooltipKeywordEntry> BuildForItem(ItemInstance item)
    {
        if (item == null)
            return null;

        List<TooltipKeywordEntry> entries = null;

        var keys = StatusUtil.Keys;
        for (int i = 0; i < keys.Count; i++)
        {
            string key = keys[i];
            if (StatusUtil.GetItemStatusValue(item, key) <= 0)
                continue;

            AppendKeyword(StatusUtil.GetKeywordId(key), ref entries);
        }

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

            if (StatusUtil.IsStatus(effect.statId))
                AppendKeyword(StatusUtil.GetKeywordId(effect.statId), ref entries);

            if (effect.effectType == ItemEffectType.SetItemStatus
                || effect.effectType == ItemEffectType.ApplyStatusToRandomBlocks)
            {
                if (StatusUtil.TryGetStatusKey(effect.statusType, out var statId))
                    AppendKeyword(StatusUtil.GetKeywordId(statId), ref entries);
            }
        }

        return entries;
    }

    static void AppendKeyword(string keywordId, ref List<TooltipKeywordEntry> entries)
    {
        if (string.IsNullOrEmpty(keywordId))
            return;

        entries ??= new List<TooltipKeywordEntry>();
        entries.Add(new TooltipKeywordEntry(
            $"tooltip.keyword.{keywordId}.title",
            $"tooltip.keyword.{keywordId}.body"));
    }
}
