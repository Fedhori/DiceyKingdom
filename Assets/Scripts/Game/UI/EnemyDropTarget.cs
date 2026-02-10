using UnityEngine;
using UnityEngine.EventSystems;

public sealed class EnemyDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string enemyInstanceId = string.Empty;

    public void SetOrchestrator(GameTurnOrchestrator value)
    {
        orchestrator = value;
    }

    public void SetEnemyInstanceId(string instanceId)
    {
        enemyInstanceId = instanceId ?? string.Empty;
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
        if (string.IsNullOrWhiteSpace(enemyInstanceId))
            return;

        bool assigned = orchestrator.TryAssignAdventurer(AssignmentDragSession.AdventurerInstanceId, enemyInstanceId);
        if (!assigned)
            return;

        AssignmentDragSession.MarkDropHandled();
    }

    void TryResolveOrchestrator()
    {
        if (orchestrator != null)
            return;

        orchestrator = FindFirstObjectByType<GameTurnOrchestrator>();
    }
}
