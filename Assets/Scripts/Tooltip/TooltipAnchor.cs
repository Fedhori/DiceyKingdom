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

    // Screen Space 기준: 우상단 / 좌상단
    public Vector2 ScreenRightTop { get; }
    public Vector2 ScreenLeftTop { get; }

    TooltipAnchor(TooltipAnchorType type, Vector3 worldPosition, Vector2 screenRightTop, Vector2 screenLeftTop)
    {
        Type = type;
        WorldPosition = worldPosition;
        ScreenRightTop = screenRightTop;
        ScreenLeftTop = screenLeftTop;
    }

    public static TooltipAnchor FromWorld(Vector3 worldPosition)
    {
        return new TooltipAnchor(TooltipAnchorType.World, worldPosition, default, default);
    }

    /// <summary>
    /// Screen Space 기준 앵커.
    /// screenRightTop: 대상의 우상단 스크린 좌표
    /// screenLeftTop : 대상의 좌상단 스크린 좌표
    /// </summary>
    public static TooltipAnchor FromScreen(Vector2 screenRightTop, Vector2 screenLeftTop)
    {
        return new TooltipAnchor(TooltipAnchorType.Screen, default, screenRightTop, screenLeftTop);
    }
}