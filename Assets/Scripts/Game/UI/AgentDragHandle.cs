using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AgentDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string agentInstanceId = string.Empty;

    public string AgentInstanceId => agentInstanceId;

    public void SetAgentInstanceId(string instanceId)
    {
        agentInstanceId = instanceId ?? string.Empty;
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
        if (!orchestrator.TryBeginAgentTargeting(agentInstanceId))
            return;

        AssignmentDragSession.Begin(agentInstanceId, eventData.position);
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

        orchestrator.TryClearAgentAssignment(agentInstanceId);
    }

    bool CanDrag()
    {
        if (orchestrator == null)
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        return orchestrator.CanAssignAgent(agentInstanceId);
    }

    void TryResolveOrchestrator()
    {
        if (orchestrator != null)
            return;

        orchestrator = FindFirstObjectByType<GameTurnOrchestrator>();
    }
}

