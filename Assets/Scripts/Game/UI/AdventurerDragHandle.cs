using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AdventurerDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string adventurerInstanceId = string.Empty;

    public string AdventurerInstanceId => adventurerInstanceId;

    public void SetAdventurerInstanceId(string instanceId)
    {
        adventurerInstanceId = instanceId ?? string.Empty;
    }

    public void SetOrchestrator(GameTurnOrchestrator value)
    {
        orchestrator = value;
    }

    void Awake()
    {
        TryResolveOrchestrator();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;
        if (orchestrator == null)
            return;
        if (!orchestrator.TryBeginAdventurerTargeting(adventurerInstanceId))
            return;

        AssignmentDragSession.Begin(adventurerInstanceId, eventData.position);
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

        bool shouldCancelTargeting = !AssignmentDragSession.DropHandled;
        AssignmentDragSession.End();

        if (!shouldCancelTargeting)
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

    void TryResolveOrchestrator()
    {
        if (orchestrator != null)
            return;

        orchestrator = FindFirstObjectByType<GameTurnOrchestrator>();
    }
}
