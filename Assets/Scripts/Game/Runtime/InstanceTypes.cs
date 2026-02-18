using System;
using System.Collections.Generic;

[Serializable]



public sealed class RunState
{
    public string uid = string.Empty;
    public int turn;
    public int barracksCapacity;
    public int gold;
    public int stability;
    public int stabilityMax;

    
    public List<AdventurerInstance> candidates = new();
    public List<AdventurerInstance> adventurers = new();
    public List<AdventurerInstance> graveyard = new();
    public List<MissionInstance> missions = new();
    public List<TraitInstance> traits = new();
    public List<ModifierInstance> modifiers = new();

    public string activeMissionUid = string.Empty;
}

[Serializable]



public sealed class AdventurerInstance
{
    public string uid = string.Empty;
    public string adventurerId = string.Empty;
    public string displayName = string.Empty;
    public int portraitIndex = -1;

    public int level = 1;
    public int xp;

    public int hp;
    public int maxHp;
    public int stamina;
    public int maxStamina;
    public float heroism;

    public int strength;
    public int agility;
    public int intelligence;

    public float growthStrength;
    public float growthAgility;
    public float growthIntelligence;

    public int equipmentSlotCount = 2;

    public bool assignedThisTurn;
    public string assignedMissionUid = string.Empty;
    public List<string> traitUids = new();
    public List<string> equipmentUids = new();
    public List<string> heroismUsedMissionUids = new();
}

[Serializable]



public sealed class MissionInstance
{
    public string uid = string.Empty;
    public string missionId = string.Empty;
    public int remainingDeadlineTurns;

    public bool isPartyLocked;
    public bool isExpeditionInProgress;
    public int currentAbilityTestIndex;

    public List<string> assignedAdventurerUids = new();
    public List<AbilityTestProgressInstance> abilityTestProgresses = new();
}

[Serializable]



public sealed class AbilityTestProgressInstance
{
    public int testIndex;
    public int attemptCount;
    public bool isCleared;
}

[Serializable]



public sealed class TraitInstance
{
    public string uid = string.Empty;
    public string traitId = string.Empty;
    public string ownerAdventurerUid = string.Empty;
    public bool isLocked;
}

[Serializable]



public sealed class ModifierInstance
{
    public string uid = string.Empty;
    public string ownerUid = string.Empty;
    public string sourceUid = string.Empty;
    public string missionUid = string.Empty;

    public StatId statId = StatId.None;
    public ModifierOpType opType = ModifierOpType.Add;
    public float value;
    public int priority;
    public ModifierLayer layer = ModifierLayer.Normal;
    public ModifierStackPolicy stackPolicy = ModifierStackPolicy.Stack;
}

public enum StatId
{
    None = 0,
    Strength = 1,
    Agility = 2,
    Intelligence = 3,
    Hp = 4,
    MaxHp = 5,
    Stamina = 6,
    MaxStamina = 7,
    Heroism = 8,
    Gold = 9,
    Stability = 10
}

public enum ModifierOpType
{
    Add = 0,
    Mul = 1,
    Set = 2
}

public enum ModifierLayer
{
    Normal = 0,
    Mission = 1
}

public enum ModifierStackPolicy
{
    Stack = 0,
    Replace = 1,
    IgnoreIfExists = 2
}

