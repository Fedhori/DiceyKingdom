using System.Collections.Generic;
using Data;
using UnityEngine;

public enum TooltipKind
{
    Simple,
    Item,
    Upgrade
}

public enum TooltipDisplayMode
{
    Item,
    Upgrade
}

public readonly struct TooltipKeywordEntry
{
    public readonly string titleKey;
    public readonly string bodyKey;
    public readonly Dictionary<string, object> arguments;

    public TooltipKeywordEntry(string titleKey, string bodyKey, Dictionary<string, object> arguments = null)
    {
        this.titleKey = titleKey;
        this.bodyKey = bodyKey;
        this.arguments = arguments;
    }
}

public readonly struct TooltipModel
{
    public readonly string title;
    public readonly string body;
    public readonly TooltipKind kind;
    public readonly ItemRarity rarity;
    public readonly string rarityLabelOverride;
    public readonly IReadOnlyList<TooltipKeywordEntry> keywordEntries;

    public TooltipModel(
        string title,
        string body,
        TooltipKind kind,
        ItemRarity rarity = ItemRarity.Common,
        string rarityLabelOverride = null,
        IReadOnlyList<TooltipKeywordEntry> keywordEntries = null)
    {
        this.title = title;
        this.body = body;
        this.kind = kind;
        this.rarity = rarity;
        this.rarityLabelOverride = rarityLabelOverride;
        this.keywordEntries = keywordEntries;
    }
}
