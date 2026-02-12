using System;
using UnityEngine;

public static class AssignmentDragSession
{
    public static string AgentInstanceId { get; private set; } = string.Empty;
    public static Vector2 DragStartScreenPosition { get; private set; }
    public static Vector2 CurrentScreenPosition { get; private set; }
    public static bool DropHandled { get; private set; }

    public static bool IsActive => !string.IsNullOrWhiteSpace(AgentInstanceId);

    public static event Action<string, Vector2> DragStarted;
    public static event Action<string, Vector2> DragMoved;
    public static event Action<string, bool> DragEnded;

    public static void Begin(string agentInstanceId, Vector2 startScreenPosition)
    {
        AgentInstanceId = agentInstanceId ?? string.Empty;
        DragStartScreenPosition = startScreenPosition;
        CurrentScreenPosition = startScreenPosition;
        DropHandled = false;
        DragStarted?.Invoke(AgentInstanceId, startScreenPosition);
    }

    public static void Move(Vector2 screenPosition)
    {
        if (!IsActive)
            return;

        CurrentScreenPosition = screenPosition;
        DragMoved?.Invoke(AgentInstanceId, screenPosition);
    }

    public static void MarkDropHandled()
    {
        if (!IsActive)
            return;

        DropHandled = true;
    }

    public static void End()
    {
        if (!IsActive)
            return;

        var agentInstanceId = AgentInstanceId;
        var dropHandled = DropHandled;

        AgentInstanceId = string.Empty;
        DragStartScreenPosition = default;
        CurrentScreenPosition = default;
        DropHandled = false;

        DragEnded?.Invoke(agentInstanceId, dropHandled);
    }
}

