using System;
using System.Collections.Generic;

public enum TurnPhase
{
    TurnStart = 0,
    BoardUpdate = 1,
    AdventurerRoll = 2,
    Adjustment = 3,
    TargetAndAttack = 4,
    Settlement = 5,
    EndTurn = 6
}

[Serializable]
public sealed class TurnRuntimeState
{
    public int turnNumber = 1;
    public TurnPhase phase = TurnPhase.TurnStart;
    public string processingAdventurerInstanceId = string.Empty;
}

[Serializable]
public sealed class StageRuntimeState
{
    public int stageNumber;
    public string activePresetId = string.Empty;
}

[Serializable]
public sealed class SkillCooldownState
{
    public string skillDefId = string.Empty;
    public int cooldownRemainingTurns;
    public bool usedThisTurn;
}

[Serializable]
public sealed class SituationState
{
    public string instanceId = string.Empty;
    public string situationDefId = string.Empty;
    public int currentRequirement;
    public int deadlineTurnsLeft;
    public List<string> assignedAdventurerIds = new();
}

[Serializable]
public sealed class AdventurerState
{
    public string instanceId = string.Empty;
    public string adventurerDefId = string.Empty;
    public List<int> rolledDiceValues = new();
    public string assignedSituationInstanceId;
    public bool actionConsumed;
}

[Serializable]
public sealed class GameRunState
{
    public TurnRuntimeState turn = new();
    public StageRuntimeState stage = new();
    public int stability;
    public int maxStability;
    public int gold;
    public int rngSeed;
    public int nextSituationInstanceSequence = 1;
    public int nextAdventurerInstanceSequence = 1;
    public List<SituationState> situations = new();
    public List<AdventurerState> adventurers = new();
    public List<SkillCooldownState> skillCooldowns = new();

    public void ResetTurnTransientState()
    {
        turn.processingAdventurerInstanceId = string.Empty;

        for (int i = 0; i < situations.Count; i++)
        {
            var situation = situations[i];
            if (situation == null)
                continue;

            situation.assignedAdventurerIds.Clear();
        }

        for (int i = 0; i < adventurers.Count; i++)
        {
            var adventurer = adventurers[i];
            if (adventurer == null)
                continue;

            adventurer.rolledDiceValues.Clear();
            adventurer.assignedSituationInstanceId = null;
            adventurer.actionConsumed = false;
        }

        for (int i = 0; i < skillCooldowns.Count; i++)
        {
            var cooldownState = skillCooldowns[i];
            if (cooldownState == null)
                continue;

            cooldownState.usedThisTurn = false;
        }
    }
}

public static class GameRunBootstrap
{
    public const int InitialStability = 20;
    public const int InitialGold = 0;
    public const int AdventurerSlotCount = 4;
    public const int InitialSituationSpawnCount = 3;
    public const int PeriodicSituationSpawnCount = 3;
    public const int PeriodicSituationSpawnIntervalTurns = 4;

    public static GameRunState CreateNewRun(GameStaticDataSet staticData, int rngSeed)
    {
        if (staticData == null)
            throw new ArgumentNullException(nameof(staticData));
        if (staticData.adventurerDefs == null || staticData.adventurerDefs.Count != AdventurerSlotCount)
        {
            throw new InvalidOperationException(
                $"v0 requires exactly {AdventurerSlotCount} adventurer defs (actual={staticData.adventurerDefs?.Count ?? 0})");
        }

        if (staticData.situationDefs == null || staticData.situationDefs.Count == 0)
            throw new InvalidOperationException("Situation defs are empty.");

        var runState = new GameRunState
        {
            stability = InitialStability,
            maxStability = InitialStability,
            gold = InitialGold,
            rngSeed = rngSeed,
            turn = new TurnRuntimeState
            {
                turnNumber = 1,
                phase = TurnPhase.TurnStart
            }
        };

        for (int i = 0; i < staticData.adventurerDefs.Count; i++)
        {
            var def = staticData.adventurerDefs[i];
            runState.adventurers.Add(new AdventurerState
            {
                instanceId = $"adventurer_{runState.nextAdventurerInstanceSequence++}",
                adventurerDefId = def.adventurerId
            });
        }

        for (int i = 0; i < staticData.skillDefs.Count; i++)
        {
            var def = staticData.skillDefs[i];
            runState.skillCooldowns.Add(new SkillCooldownState
            {
                skillDefId = def.skillId,
                cooldownRemainingTurns = 0,
                usedThisTurn = false
            });
        }

        var rng = new Random(rngSeed);
        SpawnRandomSituations(runState, staticData, rng, InitialSituationSpawnCount, "initial");
        return runState;
    }

    public static bool TrySpawnPeriodicSituations(
        GameRunState runState,
        GameStaticDataSet staticData,
        Random rng)
    {
        if (runState == null)
            throw new ArgumentNullException(nameof(runState));
        if (staticData == null)
            throw new ArgumentNullException(nameof(staticData));
        if (rng == null)
            throw new ArgumentNullException(nameof(rng));
        if (!IsPeriodicSpawnTurn(runState.turn.turnNumber))
            return false;

        SpawnRandomSituations(runState, staticData, rng, PeriodicSituationSpawnCount, "periodic");
        return true;
    }

    public static bool IsPeriodicSpawnTurn(int turnNumber)
    {
        if (turnNumber <= 1)
            return false;

        // Turn 1 starts with the initial spawn. Periodic spawns occur after each full 4-turn block.
        return (turnNumber - 1) % PeriodicSituationSpawnIntervalTurns == 0;
    }

    static void SpawnRandomSituations(
        GameRunState runState,
        GameStaticDataSet staticData,
        Random rng,
        int count,
        string stageLabel)
    {
        if (count <= 0)
            return;
        if (staticData.situationDefs == null || staticData.situationDefs.Count == 0)
            throw new InvalidOperationException("Situation defs are empty.");

        var pool = new List<SituationDef>(staticData.situationDefs.Count);
        for (int i = 0; i < staticData.situationDefs.Count; i++)
        {
            var def = staticData.situationDefs[i];
            if (def == null || string.IsNullOrWhiteSpace(def.situationId))
                continue;

            pool.Add(def);
        }

        if (pool.Count == 0)
            throw new InvalidOperationException("Situation defs do not contain valid entries.");

        runState.stage.stageNumber += 1;
        runState.stage.activePresetId = stageLabel ?? string.Empty;

        for (int i = 0; i < count; i++)
        {
            var def = pool[rng.Next(0, pool.Count)];
            runState.situations.Add(new SituationState
            {
                instanceId = $"situation_{runState.nextSituationInstanceSequence++}",
                situationDefId = def.situationId,
                currentRequirement = Math.Max(1, def.baseRequirement),
                deadlineTurnsLeft = Math.Max(1, def.baseDeadlineTurns)
            });
        }
    }
}
