using System.Collections.Generic;
using UnityEngine;

public sealed class AssignmentCommitController : MonoBehaviour
{
    [SerializeField] string titleTable = "UI";
    [SerializeField] string titleKey = "assignment.unassigned.title";
    [SerializeField] string messageTable = "UI";
    [SerializeField] string messageKey = "assignment.unassigned.message";

    public void OnCommitPressed()
    {
        int pendingCount = PhaseManager.Instance.RequestCommitAssignmentPhase();
        if (pendingCount <= 0)
            return;

        var modal = ModalManager.Instance;
        var messageArgs = new Dictionary<string, object>
        {
            { "count", pendingCount }
        };

        modal.ShowConfirmation(
            titleTable,
            titleKey,
            messageTable,
            messageKey,
            onConfirm: () => { PhaseManager.Instance.ConfirmCommitAssignmentPhase(); },
            onCancel: null,
            messageArgs: messageArgs);
    }
}
