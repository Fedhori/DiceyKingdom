using UnityEngine;

public enum TooltipKind
{
    Simple,
    Item
}

public readonly struct TooltipModel
{
    public readonly string Title;
    public readonly string Body;
    public readonly Sprite Icon;
    public readonly TooltipKind Kind;
    public readonly float DamageMultiplier;

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        DamageMultiplier = 0f;
    }

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind, float damageMultiplier)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        DamageMultiplier = damageMultiplier;
    }
}
