using System;

public enum AssignmentResult
{
    Success = 0,
    AdventurerNotFound = 1,
    EnemyNotFound = 2,
    AdventurerUnavailable = 3
}

public static class GameAssignmentService
{
    public static AssignmentResult AssignAdventurerToEnemy(
        GameRunState runState,
        string adventurerInstanceId,
        string enemyInstanceId)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return AssignmentResult.AdventurerNotFound;
        if (string.IsNullOrWhiteSpace(enemyInstanceId))
            return AssignmentResult.EnemyNotFound;

        var adventurer = FindAdventurer(runState, adventurerInstanceId);
        if (adventurer == null)
            return AssignmentResult.AdventurerNotFound;
        if (adventurer.actionConsumed)
            return AssignmentResult.AdventurerUnavailable;

        var enemy = FindEnemy(runState, enemyInstanceId);
        if (enemy == null)
            return AssignmentResult.EnemyNotFound;

        RemoveFromAssignedEnemy(runState, adventurer.instanceId, adventurer.assignedEnemyInstanceId);

        if (!ContainsAssignedAdventurer(enemy, adventurer.instanceId))
            enemy.assignedAdventurerIds.Add(adventurer.instanceId);

        adventurer.assignedEnemyInstanceId = enemy.instanceId;
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

        RemoveFromAssignedEnemy(runState, adventurer.instanceId, adventurer.assignedEnemyInstanceId);
        adventurer.assignedEnemyInstanceId = null;
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
            if (!string.IsNullOrWhiteSpace(adventurer.assignedEnemyInstanceId))
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

    static EnemyState FindEnemy(GameRunState runState, string enemyInstanceId)
    {
        for (int i = 0; i < runState.enemies.Count; i++)
        {
            var enemy = runState.enemies[i];
            if (enemy == null)
                continue;
            if (!string.Equals(enemy.instanceId, enemyInstanceId, StringComparison.Ordinal))
                continue;

            return enemy;
        }

        return null;
    }

    static bool ContainsAssignedAdventurer(EnemyState enemy, string adventurerInstanceId)
    {
        if (enemy?.assignedAdventurerIds == null)
            return false;

        for (int i = 0; i < enemy.assignedAdventurerIds.Count; i++)
        {
            if (string.Equals(enemy.assignedAdventurerIds[i], adventurerInstanceId, StringComparison.Ordinal))
                return true;
        }

        return false;
    }

    static void RemoveFromAssignedEnemy(
        GameRunState runState,
        string adventurerInstanceId,
        string assignedEnemyInstanceId)
    {
        if (string.IsNullOrWhiteSpace(assignedEnemyInstanceId))
            return;

        var enemy = FindEnemy(runState, assignedEnemyInstanceId);
        if (enemy?.assignedAdventurerIds == null)
            return;

        for (int i = enemy.assignedAdventurerIds.Count - 1; i >= 0; i--)
        {
            if (!string.Equals(enemy.assignedAdventurerIds[i], adventurerInstanceId, StringComparison.Ordinal))
                continue;

            enemy.assignedAdventurerIds.RemoveAt(i);
        }
    }
}
