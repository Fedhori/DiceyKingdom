using UnityEngine;
using UnityEngine.EventSystems;

public sealed class EnemyDropTarget : MonoBehaviour, IDropHandler
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string enemyInstanceId = string.Empty;

    public void SetEnemyInstanceId(string instanceId)
    {
        enemyInstanceId = instanceId ?? string.Empty;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!AssignmentDragSession.IsActive)
            return;

        AssignmentDragSession.MarkDropHandled();

        if (orchestrator == null)
            return;
        if (string.IsNullOrWhiteSpace(enemyInstanceId))
            return;

        orchestrator.TryAssignAdventurer(AssignmentDragSession.AdventurerInstanceId, enemyInstanceId);
    }
}
