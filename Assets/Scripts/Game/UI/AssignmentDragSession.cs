using System;
using UnityEngine;

public static class AssignmentDragSession
{
    public static string AdventurerInstanceId { get; private set; } = string.Empty;
    public static bool DropHandled { get; private set; }

    public static bool IsActive => !string.IsNullOrWhiteSpace(AdventurerInstanceId);

    public static event Action<string> DragStarted;
    public static event Action<string, Vector2> DragMoved;
    public static event Action<string, bool> DragEnded;

    public static void Begin(string adventurerInstanceId)
    {
        AdventurerInstanceId = adventurerInstanceId ?? string.Empty;
        DropHandled = false;
        DragStarted?.Invoke(AdventurerInstanceId);
    }

    public static void Move(Vector2 screenPosition)
    {
        if (!IsActive)
            return;

        DragMoved?.Invoke(AdventurerInstanceId, screenPosition);
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

        var adventurerInstanceId = AdventurerInstanceId;
        var dropHandled = DropHandled;

        AdventurerInstanceId = string.Empty;
        DropHandled = false;

        DragEnded?.Invoke(adventurerInstanceId, dropHandled);
    }
}
