using System;
using System.Collections.Generic;

[Serializable]
public sealed class AdventurerDefList
{
    public List<AdventurerDef> adventurerDefs = new();
}

[Serializable]
public sealed class MissionDefList
{
    public List<MissionDef> missionDefs = new();
}

[Serializable]
public sealed class TraitDefList
{
    public List<TraitDef> traitDefs = new();
}

[Serializable]
public sealed class AdventurerDef
{
    public string id = string.Empty;
    public int recruitWeight = 1;
    public int baseHpMin = 1;
    public int baseHpMax = 1;
    public int baseStaminaMin = 1;
    public int baseStaminaMax = 1;
    public float baseHeroismMin;
    public float baseHeroismMax;
    public int strengthMin = 1;
    public int strengthMax = 1;
    public int agilityMin = 1;
    public int agilityMax = 1;
    public int intelligenceMin = 1;
    public int intelligenceMax = 1;
    public float growthStrengthMin;
    public float growthStrengthMax;
    public float growthAgilityMin;
    public float growthAgilityMax;
    public float growthIntelligenceMin;
    public float growthIntelligenceMax;
    public int equipmentSlotCount = 2;
    public List<RuleDef> rules = new();
}

[Serializable]
public sealed class MissionDef
{
    public string id = string.Empty;
    public int spawnWeight = 1;
    public int partyLimit = 1;
    public int baseDeadlineTurns = 1;
    public List<string> tags = new();
    public List<AbilityTestDef> abilityTests = new();
    public List<RuleDef> rules = new();
}

[Serializable]
public sealed class TraitDef
{
    public string id = string.Empty;
    public string polarity = "positive";
    public int acquireWeight = 1;
    public List<RuleDef> rules = new();
}

[Serializable]
public sealed class AbilityTestDef
{
    public List<string> requiredAbilities = new();
    public int difficulty = 2;
}

[Serializable]
public sealed class RuleDef
{
    public string trigger = string.Empty;
    public ConditionDef condition = new();
    public List<EffectDef> effects = new();
}

[Serializable]
public sealed class ConditionDef
{
    public string conditionId = "always";
    public List<float> @params = new();
}

[Serializable]
public sealed class EffectDef
{
    public string effectId = string.Empty;
    public string targetId = string.Empty;
    public List<float> @params = new();
    public int priority;
    public string layer = "normal";
    public string stackPolicy = "stack";
}
