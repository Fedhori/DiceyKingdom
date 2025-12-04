using UnityEngine;

public enum TooltipKind
{
    Pin,
    Ball,
    Simple
}

public readonly struct TooltipModel
{
    public readonly string Title;
    public readonly string Body;
    public readonly Sprite Icon;
    public readonly TooltipKind Kind;
    public readonly float scoreMultiplier;

    public TooltipModel(string title, string body, Sprite icon, TooltipKind kind, float scoreMultiplier)
    {
        Title = title;
        Body = body;
        Icon = icon;
        Kind = kind;
        this.scoreMultiplier = scoreMultiplier;
    }
}