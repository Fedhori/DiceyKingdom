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
    public readonly string TitleKey;
    public readonly string BodyKey;
    public readonly Dictionary<string, object> Args;

    public TooltipKeywordEntry(string titleKey, string bodyKey, Dictionary<string, object> args = null)
    {
        TitleKey = titleKey;
        BodyKey = bodyKey;
        Args = args;
    }
}

public readonly struct TooltipModel
{
    public readonly string Title;
    public readonly string Body;
    public readonly Sprite Icon;
    public readonly TooltipKind Kind;
    public readonly float Damage;
    public readonly ItemRarity Rarity;
    public readonly string RarityLabelOverride;
    public readonly IReadOnlyList<TooltipKeywordEntry> KeywordEntries;

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind)
        : this(title, body, icon, kind, 0f, ItemRarity.Common, null, null)
    { }

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind, float damage)
        : this(title, body, icon, kind, damage, ItemRarity.Common, null, null)
    { }

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind, float damage, ItemRarity rarity)
        : this(title, body, icon, kind, damage, rarity, null, null)
    { }

    public TooltipModel(
        string title,
        string body,
        Sprite icon,
        TooltipKind kind,
        float damage,
        ItemRarity rarity,
        string rarityLabelOverride)
        : this(title, body, icon, kind, damage, rarity, rarityLabelOverride, null)
    { }

    public TooltipModel(
        string title,
        string body,
        Sprite icon,
        TooltipKind kind,
        float damage,
        ItemRarity rarity,
        string rarityLabelOverride,
        IReadOnlyList<TooltipKeywordEntry> keywordEntries)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        Damage = damage;
        Rarity = rarity;
        RarityLabelOverride = rarityLabelOverride;
        KeywordEntries = keywordEntries;
    }
}
