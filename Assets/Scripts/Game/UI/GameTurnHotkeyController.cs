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
            orchestrator.TryRollAdventurerBySlotIndex(0);
        if (IsRollKeyPressed(keyboard, 1))
            orchestrator.TryRollAdventurerBySlotIndex(1);
        if (IsRollKeyPressed(keyboard, 2))
            orchestrator.TryRollAdventurerBySlotIndex(2);
        if (IsRollKeyPressed(keyboard, 3))
            orchestrator.TryRollAdventurerBySlotIndex(3);

        if (keyboard.qKey.wasPressedThisFrame)
            orchestrator.TryUseSkillBySlotIndex(0);
        if (keyboard.wKey.wasPressedThisFrame)
            orchestrator.TryUseSkillBySlotIndex(1);
        if (keyboard.eKey.wasPressedThisFrame)
            orchestrator.TryUseSkillBySlotIndex(2);
        if (keyboard.rKey.wasPressedThisFrame)
            orchestrator.TryUseSkillBySlotIndex(3);

        if (keyboard.spaceKey.wasPressedThisFrame)
            orchestrator.RequestCommitAssignmentPhase();
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
