using UnityEngine;
using UnityEngine.EventSystems;

public sealed class EnemyDropTarget : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] string situationInstanceId = string.Empty;

    public void SetSituationInstanceId(string instanceId)
    {
        situationInstanceId = instanceId ?? string.Empty;
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!AssignmentDragSession.IsActive)
            return;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return;

        bool processed = SituationManager.Instance.TryTestAgainstSituationDie(situationInstanceId, 0);
        if (!processed)
            return;

        AssignmentDragSession.MarkDropHandled();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            return;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return;

        SituationManager.Instance.TryTestAgainstSituationDie(situationInstanceId, 0);
    }
}

