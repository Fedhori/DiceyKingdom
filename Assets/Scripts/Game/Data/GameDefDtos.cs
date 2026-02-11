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
    [JsonProperty("effect_type")] public string effectType = string.Empty;
    [JsonProperty("value")] public double? value;
    [JsonProperty("params")] public JObject effectParams = new();
}

[Serializable]
public sealed class SituationDef
{
    [JsonProperty("situation_id")] public string situationId = string.Empty;
    [JsonProperty("name_key")] public string nameKey = string.Empty;
    [JsonProperty("tags")] public List<string> tags = new();
    [JsonProperty("base_requirement")] public int baseRequirement = 1;
    [JsonProperty("base_deadline_turns")] public int baseDeadlineTurns = 1;
    [JsonProperty("success_reward")] public EffectBundle successReward = new();
    [JsonProperty("failure_effect")] public EffectBundle failureEffect = new();
    [JsonProperty("failure_persist_mode")] public string failurePersistMode = "remove";
}

[Serializable]
public sealed class AdventurerDef
{
    [JsonProperty("adventurer_id")] public string adventurerId = string.Empty;
    [JsonProperty("name_key")] public string nameKey = string.Empty;
    [JsonProperty("dice_count")] public int diceCount = 1;
    [JsonProperty("gear_slot_count")] public int gearSlotCount;
    [JsonProperty("innate_effect")] public EffectBundle innateEffect;
}

[Serializable]
public sealed class SkillDef
{
    [JsonProperty("skill_id")] public string skillId = string.Empty;
    [JsonProperty("name_key")] public string nameKey = string.Empty;
    [JsonProperty("cooldown_turns")] public int cooldownTurns;
    [JsonProperty("max_uses_per_turn")] public int maxUsesPerTurn = 1;
    [JsonProperty("effect_bundle")] public EffectBundle effectBundle = new();
}

[Serializable]
public sealed class SituationDefCatalog
{
    [JsonProperty("situations")] public List<SituationDef> situations = new();
}

[Serializable]
public sealed class AdventurerDefCatalog
{
    [JsonProperty("adventurers")] public List<AdventurerDef> adventurers = new();
}

[Serializable]
public sealed class SkillDefCatalog
{
    [JsonProperty("skills")] public List<SkillDef> skills = new();
}

