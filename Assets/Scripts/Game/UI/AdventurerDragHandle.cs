using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AdventurerDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string adventurerInstanceId = string.Empty;

    public void SetAdventurerInstanceId(string instanceId)
    {
        adventurerInstanceId = instanceId ?? string.Empty;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;

        AssignmentDragSession.Begin(adventurerInstanceId);
        AssignmentDragSession.Move(eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!AssignmentDragSession.IsActive)
            return;

        AssignmentDragSession.Move(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!AssignmentDragSession.IsActive)
            return;

        bool shouldClearAssignment = !AssignmentDragSession.DropHandled;
        AssignmentDragSession.End();

        if (!shouldClearAssignment)
            return;
        if (orchestrator == null)
            return;

        orchestrator.TryClearAdventurerAssignment(adventurerInstanceId);
    }

    bool CanDrag()
    {
        if (orchestrator == null)
            return false;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return false;

        return orchestrator.CanAssignAdventurer(adventurerInstanceId);
    }
}
