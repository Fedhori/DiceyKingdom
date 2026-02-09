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
    public int stability;
    public int maxStability;
    public int gold;
    public int rngSeed;
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
