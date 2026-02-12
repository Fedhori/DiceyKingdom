using UnityEngine;
using UnityEngine.EventSystems;

public sealed class EnemyDropTarget : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string situationInstanceId = string.Empty;

    public void SetOrchestrator(GameTurnOrchestrator value)
    {
        orchestrator = value;
    }

    public void SetSituationInstanceId(string instanceId)
    {
        situationInstanceId = instanceId ?? string.Empty;
    }

    void Awake()
    {
        TryResolveOrchestrator();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!AssignmentDragSession.IsActive)
            return;

        if (orchestrator == null)
            return;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return;

        bool assigned = orchestrator.TryAssignAgent(AssignmentDragSession.AgentInstanceId, situationInstanceId);
        if (!assigned)
            return;

        AssignmentDragSession.MarkDropHandled();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;
        if (AssignmentDragSession.IsActive)
            return;
        if (!SkillTargetingSession.IsActive)
            return;
        if (orchestrator == null)
            return;
        if (!SkillTargetingSession.IsFor(orchestrator))
            return;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return;

        SkillTargetingSession.TryConsumeSituationTarget(situationInstanceId);
    }

    void TryResolveOrchestrator()
    {
        if (orchestrator != null)
            return;

        orchestrator = FindFirstObjectByType<GameTurnOrchestrator>();
    }
}

