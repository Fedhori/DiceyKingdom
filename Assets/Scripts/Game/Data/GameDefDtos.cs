using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[Serializable]
public sealed class EffectBundle
{
    [JsonProperty("effects")] public List<EffectSpec> effects = new();
}

[Serializable]
public sealed class EffectSpec
{
    [JsonProperty("effectType")] public string effectType = string.Empty;
    [JsonProperty("value")] public double? value;
    [JsonProperty("params")] public JObject effectParams = new();
}

[Serializable]
public sealed class SituationDef
{
    [JsonProperty("situationId")] public string situationId = string.Empty;
    [JsonProperty("nameKey")] public string nameKey = string.Empty;
    [JsonProperty("tags")] public List<string> tags = new();
    [JsonProperty("baseRequirement")] public int baseRequirement = 1;
    [JsonProperty("baseDeadlineTurns")] public int baseDeadlineTurns = 1;
    [JsonProperty("successReward")] public EffectBundle successReward = new();
    [JsonProperty("failureEffect")] public EffectBundle failureEffect = new();
    [JsonProperty("failurePersistMode")] public string failurePersistMode = "remove";
}

[Serializable]
public sealed class AgentDef
{
    [JsonProperty("agentId")] public string agentId = string.Empty;
    [JsonProperty("nameKey")] public string nameKey = string.Empty;
    [JsonProperty("diceCount")] public int diceCount = 1;
    [JsonProperty("gearSlotCount")] public int gearSlotCount;
    [JsonProperty("rules")] public List<AgentRuleDef> rules = new();
}

[Serializable]
public sealed class AgentRuleDef
{
    [JsonProperty("trigger")] public AgentRuleTriggerDef trigger = new();
    [JsonProperty("condition")] public AgentRuleConditionDef condition = new();
    [JsonProperty("effect")] public EffectSpec effect = new();
}

[Serializable]
public sealed class AgentRuleTriggerDef
{
    [JsonProperty("type")] public string type = string.Empty;
    [JsonProperty("params")] public JObject triggerParams = new();
}

[Serializable]
public sealed class AgentRuleConditionDef
{
    [JsonProperty("type")] public string type = "always";
    [JsonProperty("params")] public JObject conditionParams = new();
}

[Serializable]
public sealed class SkillDef
{
    [JsonProperty("skillId")] public string skillId = string.Empty;
    [JsonProperty("nameKey")] public string nameKey = string.Empty;
    [JsonProperty("cooldownTurns")] public int cooldownTurns;
    [JsonProperty("maxUsesPerTurn")] public int maxUsesPerTurn = 1;
    [JsonProperty("effectBundle")] public EffectBundle effectBundle = new();
}

[Serializable]
public sealed class SituationDefCatalog
{
    [JsonProperty("situations")] public List<SituationDef> situations = new();
}

[Serializable]
public sealed class AgentDefCatalog
{
    [JsonProperty("agents")] public List<AgentDef> agents = new();
}

[Serializable]
public sealed class SkillDefCatalog
{
    [JsonProperty("skills")] public List<SkillDef> skills = new();
}


