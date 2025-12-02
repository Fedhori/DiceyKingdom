using UnityEngine;

public enum TooltipAnchorType
{
    World,
    Screen
}

public readonly struct TooltipAnchor
{
    public TooltipAnchorType Type { get; }
    public Vector3 WorldPosition { get; }
    public Vector2 ScreenPosition { get; }

    public TooltipAnchor(TooltipAnchorType type, Vector3 worldPosition, Vector2 screenPosition)
    {
        Type = type;
        WorldPosition = worldPosition;
        ScreenPosition = screenPosition;
    }

    public static TooltipAnchor FromWorld(Vector3 worldPosition)
    {
        return new TooltipAnchor(TooltipAnchorType.World, worldPosition, default);
    }

    public static TooltipAnchor FromScreen(Vector2 screenPosition)
    {
        return new TooltipAnchor(TooltipAnchorType.Screen, default, screenPosition);
    }
}