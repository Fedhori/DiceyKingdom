using System;
using System.Collections.Generic;

public enum TurnPhase
{
    P0TurnStart = 0,
    P1BoardUpdate = 1,
    P2Assignment = 2,
    P3Roll = 3,
    P4Adjustment = 4,
    P5Settlement = 5,
    P6EndTurn = 6
}

[Serializable]
public sealed class TurnRuntimeState
{
    public int turnNumber = 1;
    public TurnPhase phase = TurnPhase.P0TurnStart;
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
public sealed class MonsterState
{
    public string instanceId = string.Empty;
    public string monsterDefId = string.Empty;
    public int currentHealth;
    public string currentActionId = string.Empty;
    public int actionTurnsLeft;
    public List<string> assignedAdventurerIds = new();
}

[Serializable]
public sealed class AdventurerState
{
    public string instanceId = string.Empty;
    public string adventurerDefId = string.Empty;
    public List<int> rolledDiceValues = new();
    public string assignedMonsterInstanceId;
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
    public int nextMonsterInstanceSequence = 1;
    public int nextAdventurerInstanceSequence = 1;
    public List<MonsterState> monsters = new();
    public List<AdventurerState> adventurers = new();
    public List<SkillCooldownState> skillCooldowns = new();

    public void ResetTurnTransientState()
    {
        for (int i = 0; i < adventurers.Count; i++)
        {
            var adventurer = adventurers[i];
            if (adventurer == null)
                continue;

            adventurer.rolledDiceValues.Clear();
            adventurer.assignedMonsterInstanceId = null;
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
    public const int RequiredStagePresetCount = 3;

    public static GameRunState CreateNewRun(GameStaticDataSet staticData, int rngSeed)
    {
        if (staticData == null)
            throw new ArgumentNullException(nameof(staticData));
        if (staticData.adventurerDefs == null || staticData.adventurerDefs.Count != AdventurerSlotCount)
            throw new InvalidOperationException(
                $"v0 requires exactly {AdventurerSlotCount} adventurer defs (actual={staticData.adventurerDefs?.Count ?? 0})");
        if (staticData.monsterDefs == null || staticData.monsterDefs.Count == 0)
            throw new InvalidOperationException("Monster defs are empty.");
        if (staticData.stagePresetDefs == null || staticData.stagePresetDefs.Count != RequiredStagePresetCount)
            throw new InvalidOperationException(
                $"v0 requires exactly {RequiredStagePresetCount} stage presets (actual={staticData.stagePresetDefs?.Count ?? 0})");

        var runState = new GameRunState
        {
            stability = InitialStability,
            maxStability = InitialStability,
            gold = InitialGold,
            rngSeed = rngSeed,
            turn = new TurnRuntimeState
            {
                turnNumber = 1,
                phase = TurnPhase.P0TurnStart
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
        SpawnRandomStagePreset(runState, staticData, rng);
        return runState;
    }

    public static bool TrySpawnStagePresetIfBoardCleared(
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
        if (runState.monsters.Count > 0)
            return false;

        SpawnRandomStagePreset(runState, staticData, rng);
        return true;
    }

    static void SpawnRandomStagePreset(GameRunState runState, GameStaticDataSet staticData, Random rng)
    {
        if (staticData.stagePresetDefs == null || staticData.stagePresetDefs.Count == 0)
            throw new InvalidOperationException("Stage presets are empty.");

        var monsterDefById = new Dictionary<string, MonsterDef>(StringComparer.Ordinal);
        for (int i = 0; i < staticData.monsterDefs.Count; i++)
        {
            var monsterDef = staticData.monsterDefs[i];
            if (monsterDef == null || string.IsNullOrWhiteSpace(monsterDef.monsterId))
                continue;

            monsterDefById[monsterDef.monsterId] = monsterDef;
        }

        var presetIndex = rng.Next(0, staticData.stagePresetDefs.Count);
        var preset = staticData.stagePresetDefs[presetIndex];
        if (preset == null)
            throw new InvalidOperationException($"Stage preset at index {presetIndex} is null.");

        runState.stage.stageNumber += 1;
        runState.stage.activePresetId = preset.presetId ?? string.Empty;

        for (int i = 0; i < preset.spawns.Count; i++)
        {
            var spawn = preset.spawns[i];
            if (spawn == null)
                continue;
            if (!monsterDefById.TryGetValue(spawn.monsterId, out var monsterDef))
                throw new InvalidOperationException(
                    $"Preset '{preset.presetId}' references unknown monster '{spawn.monsterId}'.");

            for (int count = 0; count < spawn.count; count++)
            {
                var actionDef = PickWeightedAction(monsterDef.actionPool, rng);
                runState.monsters.Add(new MonsterState
                {
                    instanceId = $"monster_{runState.nextMonsterInstanceSequence++}",
                    monsterDefId = monsterDef.monsterId,
                    currentHealth = monsterDef.baseHealth,
                    currentActionId = actionDef.actionId,
                    actionTurnsLeft = actionDef.prepTurns
                });
            }
        }
    }

    static ActionDef PickWeightedAction(IReadOnlyList<ActionDef> actionPool, Random rng)
    {
        if (actionPool == null || actionPool.Count == 0)
            throw new InvalidOperationException("action_pool is empty.");

        int totalWeight = 0;
        for (int i = 0; i < actionPool.Count; i++)
        {
            var action = actionPool[i];
            if (action == null)
                continue;

            totalWeight += Math.Max(0, action.weight);
        }

        if (totalWeight <= 0)
            throw new InvalidOperationException("action_pool total weight must be > 0.");

        int roll = rng.Next(1, totalWeight + 1);
        int cumulative = 0;

        for (int i = 0; i < actionPool.Count; i++)
        {
            var action = actionPool[i];
            if (action == null)
                continue;

            cumulative += Math.Max(0, action.weight);
            if (roll <= cumulative)
                return action;
        }

        throw new InvalidOperationException("Failed to pick weighted action.");
    }
}
