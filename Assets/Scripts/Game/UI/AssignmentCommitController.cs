using System.Collections.Generic;
using UnityEngine;

public sealed class AssignmentCommitController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string titleTable = "UI";
    [SerializeField] string titleKey = "assignment.unassigned.title";
    [SerializeField] string messageTable = "UI";
    [SerializeField] string messageKey = "assignment.unassigned.message";

    void OnEnable()
    {
        if (orchestrator == null)
            return;

        orchestrator.AssignmentCommitConfirmationRequested += OnAssignmentCommitConfirmationRequested;
    }

    void OnDisable()
    {
        if (orchestrator == null)
            return;

        orchestrator.AssignmentCommitConfirmationRequested -= OnAssignmentCommitConfirmationRequested;
    }

    public void OnCommitPressed()
    {
        if (orchestrator == null)
            return;

        orchestrator.RequestCommitAssignmentPhase();
    }

    void OnAssignmentCommitConfirmationRequested(int unassignedCount)
    {
        if (orchestrator == null)
            return;

        var modal = ModalManager.Instance;
        if (modal == null)
        {
            Debug.LogWarning(
                "[AssignmentCommitController] ModalManager is missing. Assignment confirmation dialog could not be shown.");
            return;
        }

        var messageArgs = new Dictionary<string, object>
        {
            { "count", unassignedCount }
        };

        modal.ShowConfirmation(
            titleTable,
            titleKey,
            messageTable,
            messageKey,
            onConfirm: () => { orchestrator.ConfirmCommitAssignmentPhase(); },
            onCancel: null,
            messageArgs: messageArgs);
    }
}
