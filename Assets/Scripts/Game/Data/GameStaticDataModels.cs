using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public sealed class GameConditionDefinition
{
    [JsonProperty("condition_type")]
    public string conditionType { get; set; } = string.Empty;

    [JsonProperty("value")]
    public float? value { get; set; }
}

public sealed class GameEffectDefinition
{
    [JsonProperty("effect_type")]
    public string effectType { get; set; } = string.Empty;

    [JsonProperty("value")]
    public float? value { get; set; }

    [JsonProperty("target_resource")]
    public string targetResource { get; set; } = string.Empty;

    [JsonProperty("target_mode")]
    public string targetMode { get; set; } = string.Empty;

    [JsonProperty("target_tag")]
    public string targetTag { get; set; } = string.Empty;

    [JsonProperty("duration")]
    public int? duration { get; set; }
}

public sealed class GameSituationDefinition
{
    [JsonProperty("situation_id")]
    public string situationId { get; set; } = string.Empty;

    [JsonProperty("demand")]
    public int demand { get; set; }

    [JsonProperty("deadline")]
    public int deadline { get; set; }

    [JsonProperty("risk_value")]
    public float riskValue { get; set; }

    [JsonProperty("tags")]
    public List<string> tags { get; set; } = new();

    [JsonProperty("on_turn_start_effects")]
    public List<GameEffectDefinition> onTurnStartEffects { get; set; } = new();

    [JsonProperty("on_success")]
    public List<GameEffectDefinition> onSuccess { get; set; } = new();

    [JsonProperty("on_fail")]
    public List<GameEffectDefinition> onFail { get; set; } = new();
}

public sealed class GameAdvisorDefinition
{
    [JsonProperty("advisor_id")]
    public string advisorId { get; set; } = string.Empty;

    [JsonProperty("cooldown")]
    public int cooldown { get; set; }

    [JsonProperty("target_type")]
    public string targetType { get; set; } = string.Empty;

    [JsonProperty("max_uses_per_situation")]
    public int? maxUsesPerSituation { get; set; }

    [JsonProperty("conditions")]
    public List<GameConditionDefinition> conditions { get; set; } = new();

    [JsonProperty("effects")]
    public List<GameEffectDefinition> effects { get; set; } = new();
}

public sealed class GameDecreeDefinition
{
    [JsonProperty("decree_id")]
    public string decreeId { get; set; } = string.Empty;

    [JsonProperty("target_type")]
    public string targetType { get; set; } = string.Empty;

    [JsonProperty("conditions")]
    public List<GameConditionDefinition> conditions { get; set; } = new();

    [JsonProperty("effects")]
    public List<GameEffectDefinition> effects { get; set; } = new();
}

public sealed class GameDiceUpgradeDefinition
{
    [JsonProperty("upgrade_id")]
    public string upgradeId { get; set; } = string.Empty;

    [JsonProperty("trigger_type")]
    public string triggerType { get; set; } = string.Empty;

    [JsonProperty("conditions")]
    public List<GameConditionDefinition> conditions { get; set; } = new();

    [JsonProperty("effects")]
    public List<GameEffectDefinition> effects { get; set; } = new();
}

public sealed class GameSituationDataSet
{
    [JsonProperty("version")]
    public int version { get; set; } = 1;

    [JsonProperty("situations")]
    public List<GameSituationDefinition> situations { get; set; } = new();
}

public sealed class GameAdvisorDataSet
{
    [JsonProperty("version")]
    public int version { get; set; } = 1;

    [JsonProperty("advisors")]
    public List<GameAdvisorDefinition> advisors { get; set; } = new();
}

public sealed class GameDecreeDataSet
{
    [JsonProperty("version")]
    public int version { get; set; } = 1;

    [JsonProperty("decrees")]
    public List<GameDecreeDefinition> decrees { get; set; } = new();
}

public sealed class GameDiceUpgradeDataSet
{
    [JsonProperty("version")]
    public int version { get; set; } = 1;

    [JsonProperty("dice_upgrades")]
    public List<GameDiceUpgradeDefinition> diceUpgrades { get; set; } = new();
}

public sealed class GameStaticDataCatalog
{
    public IReadOnlyList<GameSituationDefinition> situations => situationList;
    public IReadOnlyList<GameAdvisorDefinition> advisors => advisorList;
    public IReadOnlyList<GameDecreeDefinition> decrees => decreeList;
    public IReadOnlyList<GameDiceUpgradeDefinition> diceUpgrades => diceUpgradeList;

    readonly List<GameSituationDefinition> situationList;
    readonly List<GameAdvisorDefinition> advisorList;
    readonly List<GameDecreeDefinition> decreeList;
    readonly List<GameDiceUpgradeDefinition> diceUpgradeList;

    readonly Dictionary<string, GameSituationDefinition> situationsById;
    readonly Dictionary<string, GameAdvisorDefinition> advisorsById;
    readonly Dictionary<string, GameDecreeDefinition> decreesById;
    readonly Dictionary<string, GameDiceUpgradeDefinition> diceUpgradesById;

    public GameStaticDataCatalog(
        List<GameSituationDefinition> situations,
        List<GameAdvisorDefinition> advisors,
        List<GameDecreeDefinition> decrees,
        List<GameDiceUpgradeDefinition> diceUpgrades)
    {
        situationList = situations ?? new List<GameSituationDefinition>();
        advisorList = advisors ?? new List<GameAdvisorDefinition>();
        decreeList = decrees ?? new List<GameDecreeDefinition>();
        diceUpgradeList = diceUpgrades ?? new List<GameDiceUpgradeDefinition>();

        situationsById = BuildDictionary(situationList, data => data.situationId);
        advisorsById = BuildDictionary(advisorList, data => data.advisorId);
        decreesById = BuildDictionary(decreeList, data => data.decreeId);
        diceUpgradesById = BuildDictionary(diceUpgradeList, data => data.upgradeId);
    }

    public bool TryGetSituation(string situationId, out GameSituationDefinition situationDefinition)
    {
        return situationsById.TryGetValue(situationId ?? string.Empty, out situationDefinition);
    }

    public bool TryGetAdvisor(string advisorId, out GameAdvisorDefinition advisorDefinition)
    {
        return advisorsById.TryGetValue(advisorId ?? string.Empty, out advisorDefinition);
    }

    public bool TryGetDecree(string decreeId, out GameDecreeDefinition decreeDefinition)
    {
        return decreesById.TryGetValue(decreeId ?? string.Empty, out decreeDefinition);
    }

    public bool TryGetDiceUpgrade(string upgradeId, out GameDiceUpgradeDefinition diceUpgradeDefinition)
    {
        return diceUpgradesById.TryGetValue(upgradeId ?? string.Empty, out diceUpgradeDefinition);
    }

    static Dictionary<string, T> BuildDictionary<T>(IReadOnlyList<T> source, Func<T, string> keySelector)
    {
        var dictionary = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);
        if (source == null || keySelector == null)
            return dictionary;

        for (int i = 0; i < source.Count; i++)
        {
            var item = source[i];
            var key = keySelector(item);
            if (string.IsNullOrWhiteSpace(key))
                continue;

            dictionary[key] = item;
        }

        return dictionary;
    }
}
