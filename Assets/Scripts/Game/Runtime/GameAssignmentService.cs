using System;

public enum AssignmentResult
{
    Success = 0,
    AdventurerNotFound = 1,
    SituationNotFound = 2,
    AdventurerUnavailable = 3
}

public static class GameAssignmentService
{
    public static AssignmentResult AssignAdventurerToSituation(
        GameRunState runState,
        string adventurerInstanceId,
        string situationInstanceId)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return AssignmentResult.AdventurerNotFound;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return AssignmentResult.SituationNotFound;

        var adventurer = FindAdventurer(runState, adventurerInstanceId);
        if (adventurer == null)
            return AssignmentResult.AdventurerNotFound;
        if (adventurer.actionConsumed)
            return AssignmentResult.AdventurerUnavailable;

        var situation = FindSituation(runState, situationInstanceId);
        if (situation == null)
            return AssignmentResult.SituationNotFound;

        RemoveFromAssignedSituation(runState, adventurer.instanceId, adventurer.assignedSituationInstanceId);

        if (!ContainsAssignedAdventurer(situation, adventurer.instanceId))
            situation.assignedAdventurerIds.Add(adventurer.instanceId);

        adventurer.assignedSituationInstanceId = situation.instanceId;
        return AssignmentResult.Success;
    }

    public static AssignmentResult ClearAdventurerAssignment(GameRunState runState, string adventurerInstanceId)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return AssignmentResult.AdventurerNotFound;

        var adventurer = FindAdventurer(runState, adventurerInstanceId);
        if (adventurer == null)
            return AssignmentResult.AdventurerNotFound;

        RemoveFromAssignedSituation(runState, adventurer.instanceId, adventurer.assignedSituationInstanceId);
        adventurer.assignedSituationInstanceId = null;
        return AssignmentResult.Success;
    }

    public static int CountUnassignedAdventurers(GameRunState runState)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));

        int count = 0;
        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            var adventurer = runState.adventurers[i];
            if (adventurer == null)
                continue;
            if (adventurer.actionConsumed)
                continue;
            if (!string.IsNullOrWhiteSpace(adventurer.assignedSituationInstanceId))
                continue;

            count += 1;
        }

        return count;
    }

    public static int CountPendingAdventurers(GameRunState runState)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));

        int count = 0;
        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            var adventurer = runState.adventurers[i];
            if (adventurer == null)
                continue;
            if (adventurer.actionConsumed)
                continue;

            count += 1;
        }

        return count;
    }

    static AdventurerState FindAdventurer(GameRunState runState, string adventurerInstanceId)
    {
        for (int i = 0; i < runState.adventurers.Count; i++)
        {
            var adventurer = runState.adventurers[i];
            if (adventurer == null)
                continue;
            if (!string.Equals(adventurer.instanceId, adventurerInstanceId, StringComparison.Ordinal))
                continue;

            return adventurer;
        }

        return null;
    }

    static SituationState FindSituation(GameRunState runState, string situationInstanceId)
    {
        for (int i = 0; i < runState.situations.Count; i++)
        {
            var situation = runState.situations[i];
            if (situation == null)
                continue;
            if (!string.Equals(situation.instanceId, situationInstanceId, StringComparison.Ordinal))
                continue;

            return situation;
        }

        return null;
    }

    static bool ContainsAssignedAdventurer(SituationState situation, string adventurerInstanceId)
    {
        if (situation?.assignedAdventurerIds == null)
            return false;

        for (int i = 0; i < situation.assignedAdventurerIds.Count; i++)
        {
            if (string.Equals(situation.assignedAdventurerIds[i], adventurerInstanceId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static void RemoveFromAssignedSituation(
        GameRunState runState,
        string adventurerInstanceId,
        string assignedSituationInstanceId)
    {
        if (string.IsNullOrWhiteSpace(assignedSituationInstanceId))
            return;

        var situation = FindSituation(runState, assignedSituationInstanceId);
        if (situation?.assignedAdventurerIds == null)
            return;

        for (int i = situation.assignedAdventurerIds.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(situation.assignedAdventurerIds[i], adventurerInstanceId, StringComparison.Ordinal))
                continue;

            situation.assignedAdventurerIds.RemoveAt(i);
        }
    }

    public static AssignmentResult AssignAdventurerToEnemy(
        GameRunState runState,
        string adventurerInstanceId,
        string enemyInstanceId)
    {
        return AssignAdventurerToSituation(runState, adventurerInstanceId, enemyInstanceId);
    }
}
