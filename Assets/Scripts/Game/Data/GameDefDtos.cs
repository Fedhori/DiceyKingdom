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
public sealed class ActionDef
{
    [JsonProperty("action_id")] public string actionId = string.Empty;
    [JsonProperty("weight")] public int weight = 1;
    [JsonProperty("prep_turns")] public int prepTurns = 1;
    [JsonProperty("on_resolve")] public EffectBundle onResolve = new();
}

[Serializable]
public sealed class MonsterDef
{
    [JsonProperty("monster_id")] public string monsterId = string.Empty;
    [JsonProperty("name_key")] public string nameKey = string.Empty;
    [JsonProperty("tags")] public List<string> tags = new();
    [JsonProperty("base_health")] public int baseHealth = 1;
    [JsonProperty("on_kill_reward")] public EffectBundle onKillReward = new();
    [JsonProperty("action_pool")] public List<ActionDef> actionPool = new();
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
public sealed class MonsterDefCatalog
{
    [JsonProperty("monsters")] public List<MonsterDef> monsters = new();
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
