using System;
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

public sealed class TooltipButtonConfig
{
    public string LabelKey { get; }
    public Color BackgroundColor { get; }
    public bool Interactable { get; }
    public Action OnClick { get; }

    public TooltipButtonConfig(string labelKey, Color backgroundColor, bool interactable, Action onClick)
    {
        LabelKey = labelKey;
        BackgroundColor = backgroundColor;
        Interactable = interactable;
        OnClick = onClick;
    }
}

public readonly struct TooltipModel
{
    public readonly string title;
    public readonly string body;
    public readonly TooltipKind kind;
    public readonly ItemRarity rarity;
    public readonly IReadOnlyList<TooltipKeywordEntry> keywordEntries;
    public readonly TooltipButtonConfig buttonConfig;

    public TooltipModel(
        string title,
        string body,
        TooltipKind kind,
        ItemRarity rarity = ItemRarity.Common,
        IReadOnlyList<TooltipKeywordEntry> keywordEntries = null,
        TooltipButtonConfig buttonConfig = null)
    {
        this.title = title;
        this.body = body;
        this.kind = kind;
        this.rarity = rarity;
        this.keywordEntries = keywordEntries;
        this.buttonConfig = buttonConfig;
    }
}
