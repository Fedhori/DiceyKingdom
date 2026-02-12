using UnityEngine;
using UnityEngine.InputSystem;

public sealed class GameTurnHotkeyController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;

    void Update()
    {
        if (orchestrator == null)
            return;

        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (IsRollKeyPressed(keyboard, 0))
            orchestrator.TryRollAgentBySlotIndex(0);
        if (IsRollKeyPressed(keyboard, 1))
            orchestrator.TryRollAgentBySlotIndex(1);
        if (IsRollKeyPressed(keyboard, 2))
            orchestrator.TryRollAgentBySlotIndex(2);
        if (IsRollKeyPressed(keyboard, 3))
            orchestrator.TryRollAgentBySlotIndex(3);

        if (keyboard.qKey.wasPressedThisFrame)
            HandleSkillHotkey(0);
        if (keyboard.wKey.wasPressedThisFrame)
            HandleSkillHotkey(1);
        if (keyboard.eKey.wasPressedThisFrame)
            HandleSkillHotkey(2);
        if (keyboard.rKey.wasPressedThisFrame)
            HandleSkillHotkey(3);

        if (keyboard.spaceKey.wasPressedThisFrame)
            orchestrator.RequestCommitAssignmentPhase();

        if (SkillTargetingSession.IsFor(orchestrator) &&
            !orchestrator.CanUseSkillBySlotIndex(SkillTargetingSession.ActiveSkillSlotIndex))
        {
            SkillTargetingSession.Cancel();
        }
    }

    void HandleSkillHotkey(int skillSlotIndex)
    {
        if (orchestrator == null)
            return;
        if (!orchestrator.CanUseSkillBySlotIndex(skillSlotIndex))
            return;

        bool needsSituationTarget = orchestrator.SkillRequiresSituationTargetBySlotIndex(skillSlotIndex);
        if (!needsSituationTarget)
        {
            bool used = orchestrator.TryUseSkillBySlotIndex(skillSlotIndex);
            if (used && SkillTargetingSession.IsFor(orchestrator, skillSlotIndex))
                SkillTargetingSession.Cancel();
            return;
        }

        if (SkillTargetingSession.IsFor(orchestrator, skillSlotIndex))
        {
            SkillTargetingSession.Cancel();
            return;
        }

        SkillTargetingSession.Begin(orchestrator, skillSlotIndex);
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

