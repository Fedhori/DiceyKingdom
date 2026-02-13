using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameTurnHotkeyController : MonoBehaviour
{
    [SerializeField] string titleTable = "UI";
    [SerializeField] string titleKey = "assignment.unassigned.title";
    [SerializeField] string messageTable = "UI";
    [SerializeField] string messageKey = "assignment.unassigned.message";

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (IsRollKeyPressed(keyboard, 0))
            AgentManager.Instance.TryRollAgentBySlotIndex(0);
        if (IsRollKeyPressed(keyboard, 1))
            AgentManager.Instance.TryRollAgentBySlotIndex(1);
        if (IsRollKeyPressed(keyboard, 2))
            AgentManager.Instance.TryRollAgentBySlotIndex(2);
        if (IsRollKeyPressed(keyboard, 3))
            AgentManager.Instance.TryRollAgentBySlotIndex(3);

        if (keyboard.qKey.wasPressedThisFrame)
            HandleSkillHotkey(0);
        if (keyboard.wKey.wasPressedThisFrame)
            HandleSkillHotkey(1);
        if (keyboard.eKey.wasPressedThisFrame)
            HandleSkillHotkey(2);
        if (keyboard.rKey.wasPressedThisFrame)
            HandleSkillHotkey(3);

        if (keyboard.spaceKey.wasPressedThisFrame)
            RequestCommitWithConfirmation();

        if (SkillTargetingSession.IsFor(GameManager.Instance) &&
            !GameManager.Instance.CanUseSkillBySlotIndex(SkillTargetingSession.ActiveSkillSlotIndex))
        {
            SkillTargetingSession.Cancel();
        }
    }

    void HandleSkillHotkey(int skillSlotIndex)
    {
        if (!GameManager.Instance.CanUseSkillBySlotIndex(skillSlotIndex))
            return;

        bool needsSituationTarget = GameManager.Instance.SkillRequiresSituationTargetBySlotIndex(skillSlotIndex);
        if (!needsSituationTarget)
        {
            bool used = GameManager.Instance.TryUseSkillBySlotIndex(skillSlotIndex);
            if (used && SkillTargetingSession.IsFor(GameManager.Instance, skillSlotIndex))
                SkillTargetingSession.Cancel();
            return;
        }

        if (SkillTargetingSession.IsFor(GameManager.Instance, skillSlotIndex))
        {
            SkillTargetingSession.Cancel();
            return;
        }

        SkillTargetingSession.Begin(GameManager.Instance, skillSlotIndex);
    }

    void RequestCommitWithConfirmation()
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

    static bool IsRollKeyPressed(Keyboard keyboard, int slotIndex)
    {
        return slotIndex switch
        {
            0 => keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame,
            1 => keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame,
            2 => keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame,
            3 => keyboard.digit4Key.wasPressedThisFrame || keyboard.numpad4Key.wasPressedThisFrame,
            _ => false
        };
    }
}
