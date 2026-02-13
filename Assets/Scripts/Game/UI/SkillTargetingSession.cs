using System;

public static class SkillTargetingSession
{
    public static GameManager ActiveOrchestrator { get; private set; }
    public static int ActiveSkillSlotIndex { get; private set; } = -1;

    public static bool IsActive => ActiveOrchestrator != null && ActiveSkillSlotIndex >= 0;

    public static event Action SessionChanged;

    public static void Begin(GameManager orchestrator, int skillSlotIndex)
    {
        if (orchestrator == null || skillSlotIndex < 0)
        {
            Cancel();
            return;
        }

        if (ReferenceEquals(ActiveOrchestrator, orchestrator) &&
            ActiveSkillSlotIndex == skillSlotIndex)
        {
            return;
        }

        ActiveOrchestrator = orchestrator;
        ActiveSkillSlotIndex = skillSlotIndex;
        SessionChanged?.Invoke();
    }

    public static void Cancel()
    {
        if (!IsActive)
            return;

        ActiveOrchestrator = null;
        ActiveSkillSlotIndex = -1;
        SessionChanged?.Invoke();
    }

    public static bool IsFor(GameManager orchestrator, int skillSlotIndex)
    {
        return ReferenceEquals(ActiveOrchestrator, orchestrator) &&
               ActiveSkillSlotIndex == skillSlotIndex;
    }

    public static bool IsFor(GameManager orchestrator)
    {
        return ReferenceEquals(ActiveOrchestrator, orchestrator) && IsActive;
    }

    public static bool TryConsumeSituationTarget(string situationInstanceId)
    {
        if (!IsActive)
            return false;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return false;

        bool used = ActiveOrchestrator.TryUseSkillBySlotIndex(
            ActiveSkillSlotIndex,
            selectedAgentInstanceId: null,
            selectedSituationInstanceId: situationInstanceId,
            selectedDieIndex: -1);
        if (used)
            Cancel();

        return used;
    }

    public static bool TryConsumeAgentDieTarget(string agentInstanceId, int dieIndex)
    {
        if (!IsActive)
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;
        if (dieIndex < 0)
            return false;

        bool used = ActiveOrchestrator.TryUseSkillBySlotIndex(
            ActiveSkillSlotIndex,
            selectedAgentInstanceId: agentInstanceId,
            selectedSituationInstanceId: null,
            selectedDieIndex: dieIndex);
        if (used)
            Cancel();

        return used;
    }
}

