using UnityEngine;
using UnityEngine.EventSystems;

public sealed class AgentDragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] string agentInstanceId = string.Empty;

    public string AgentInstanceId => agentInstanceId;

    public void SetAgentInstanceId(string instanceId)
    {
        agentInstanceId = instanceId ?? string.Empty;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!CanDrag())
            return;
        if (!AgentManager.Instance.TryBeginAgentTargeting(agentInstanceId))
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

        AgentManager.Instance.TryClearAgentAssignment(agentInstanceId);
    }

    bool CanDrag()
    {
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        return AgentManager.Instance.CanAssignAgent(agentInstanceId);
    }
}

