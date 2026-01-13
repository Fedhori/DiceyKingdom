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

public readonly struct TooltipModel
{
    public readonly string Title;
    public readonly string Body;
    public readonly Sprite Icon;
    public readonly TooltipKind Kind;
    public readonly float Damage;
    public readonly ItemRarity Rarity;
    public readonly string RarityLabelOverride;

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        Damage = 0f;
        Rarity = ItemRarity.Common;
        RarityLabelOverride = null;
    }

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind, float damage)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        Damage = damage;
        Rarity = ItemRarity.Common;
        RarityLabelOverride = null;
    }

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind, float damage, ItemRarity rarity)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        Damage = damage;
        Rarity = rarity;
        RarityLabelOverride = null;
    }

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind, float damage, ItemRarity rarity, string rarityLabelOverride)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        Damage = damage;
        Rarity = rarity;
        RarityLabelOverride = rarityLabelOverride;
    }
}
