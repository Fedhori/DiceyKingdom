using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

public static class GameStaticDataLoader
{
    static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
    {
        MissingMemberHandling = MissingMemberHandling.Ignore,
        NullValueHandling = NullValueHandling.Ignore
    };

    static readonly HashSet<string> situationResourceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "defense",
        "stability"
    };

    static readonly HashSet<string> demandTargetModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "self",
        "selected_situation",
        "by_tag",
        "all_other_situations"
    };

    static readonly HashSet<string> deadlineTargetModes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "self",
        "selected_situation",
        "by_tag",
        "random_other_situation"
    };

    static readonly HashSet<string> diceUpgradeConditionWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "assigned_situation_deadline_lte",
        "die_face_lte",
        "die_face_eq",
        "die_face_gte",
        "assigned_dice_count_in_situation_gte",
        "assigned_dice_count_in_situation_lte",
        "player_defense_lte",
        "player_defense_gte",
        "player_stability_lte",
        "player_stability_gte",
        "board_order_first",
        "board_order_last",
        "on_resolve_success"
    };

    static readonly HashSet<string> diceUpgradeTriggerWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "roll_once_after_roll",
        "recheck_on_die_face_change"
    };

    public static bool TryLoadDefault(out GameStaticDataCatalog catalog, out string errorMessage)
    {
        catalog = null;
        errorMessage = string.Empty;

        var errors = new List<string>();

        var situationDataSet = TryLoadDataSet<GameSituationDataSet>(GameDataPaths.situations, "situations", errors);
        var advisorDataSet = TryLoadDataSet<GameAdvisorDataSet>(GameDataPaths.advisors, "advisors", errors);
        var decreeDataSet = TryLoadDataSet<GameDecreeDataSet>(GameDataPaths.decrees, "decrees", errors);
        var diceUpgradeDataSet = TryLoadDataSet<GameDiceUpgradeDataSet>(GameDataPaths.diceUpgrades, "dice_upgrades", errors);

        if (situationDataSet != null)
            ValidateSituationDataSet(situationDataSet, errors);
        if (advisorDataSet != null)
            ValidateAdvisorDataSet(advisorDataSet, errors);
        if (decreeDataSet != null)
            ValidateDecreeDataSet(decreeDataSet, errors);
        if (diceUpgradeDataSet != null)
            ValidateDiceUpgradeDataSet(diceUpgradeDataSet, errors);

        if (errors.Count > 0)
        {
            errorMessage = string.Join("\n", errors);
            return false;
        }

        catalog = new GameStaticDataCatalog(
            situationDataSet.situations,
            advisorDataSet.advisors,
            decreeDataSet.decrees,
            diceUpgradeDataSet.diceUpgrades);

        return true;
    }

    static TDataSet TryLoadDataSet<TDataSet>(string relativePath, string label, List<string> errors)
        where TDataSet : class
    {
        try
        {
            var json = SaCache.ReadText(relativePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                errors.Add($"[{label}] empty json: {relativePath}");
                return null;
            }

            var dataSet = JsonConvert.DeserializeObject<TDataSet>(json, jsonSettings);
            if (dataSet == null)
                errors.Add($"[{label}] deserialize failed: {relativePath}");

            return dataSet;
        }
        catch (IOException e)
        {
            errors.Add($"[{label}] read failed: {relativePath} ({e.Message})");
            return null;
        }
        catch (Exception e)
        {
            errors.Add($"[{label}] parse failed: {relativePath} ({e.Message})");
            return null;
        }
    }

    static void ValidateSituationDataSet(GameSituationDataSet dataSet, List<string> errors)
    {
        if (dataSet.situations == null || dataSet.situations.Count == 0)
        {
            errors.Add("[situations] no entries");
            return;
        }

        ValidateUniqueIds(dataSet.situations, data => data.situationId, "situations", errors);

        for (int i = 0; i < dataSet.situations.Count; i++)
        {
            var data = dataSet.situations[i];
            var prefix = $"[situations:{data.situationId}]";
            if (string.IsNullOrWhiteSpace(data.situationId))
                errors.Add("[situations] missing situation_id");
            if (data.demand <= 0)
                errors.Add($"{prefix} demand must be > 0");
            if (data.deadline <= 0)
                errors.Add($"{prefix} deadline must be > 0");
            if (data.riskValue <= 0f)
                errors.Add($"{prefix} risk_value must be > 0");

            NormalizeCollections(data);

            ValidateEffects(data.onTurnStartEffects, prefix, errors);
            ValidateEffects(data.onSuccess, prefix, errors);
            ValidateEffects(data.onFail, prefix, errors);
        }
    }

    static void ValidateAdvisorDataSet(GameAdvisorDataSet dataSet, List<string> errors)
    {
        if (dataSet.advisors == null || dataSet.advisors.Count == 0)
        {
            errors.Add("[advisors] no entries");
            return;
        }

        ValidateUniqueIds(dataSet.advisors, data => data.advisorId, "advisors", errors);

        for (int i = 0; i < dataSet.advisors.Count; i++)
        {
            var data = dataSet.advisors[i];
            var prefix = $"[advisors:{data.advisorId}]";
            if (string.IsNullOrWhiteSpace(data.advisorId))
                errors.Add("[advisors] missing advisor_id");
            if (data.cooldown < 0)
                errors.Add($"{prefix} cooldown must be >= 0");
            if (string.IsNullOrWhiteSpace(data.targetType))
                errors.Add($"{prefix} target_type is required");

            data.conditions ??= new List<GameConditionDefinition>();
            data.effects ??= new List<GameEffectDefinition>();

            if (data.effects.Count == 0)
                errors.Add($"{prefix} effects must contain at least one entry");

            ValidateEffects(data.effects, prefix, errors);
        }
    }

    static void ValidateDecreeDataSet(GameDecreeDataSet dataSet, List<string> errors)
    {
        if (dataSet.decrees == null || dataSet.decrees.Count == 0)
        {
            errors.Add("[decrees] no entries");
            return;
        }

        ValidateUniqueIds(dataSet.decrees, data => data.decreeId, "decrees", errors);

        for (int i = 0; i < dataSet.decrees.Count; i++)
        {
            var data = dataSet.decrees[i];
            var prefix = $"[decrees:{data.decreeId}]";
            if (string.IsNullOrWhiteSpace(data.decreeId))
                errors.Add("[decrees] missing decree_id");

            data.conditions ??= new List<GameConditionDefinition>();
            data.effects ??= new List<GameEffectDefinition>();

            if (data.effects.Count == 0)
                errors.Add($"{prefix} effects must contain at least one entry");

            ValidateEffects(data.effects, prefix, errors);
        }
    }

    static void ValidateDiceUpgradeDataSet(GameDiceUpgradeDataSet dataSet, List<string> errors)
    {
        if (dataSet.diceUpgrades == null || dataSet.diceUpgrades.Count == 0)
        {
            errors.Add("[dice_upgrades] no entries");
            return;
        }

        ValidateUniqueIds(dataSet.diceUpgrades, data => data.upgradeId, "dice_upgrades", errors);

        for (int i = 0; i < dataSet.diceUpgrades.Count; i++)
        {
            var data = dataSet.diceUpgrades[i];
            var prefix = $"[dice_upgrades:{data.upgradeId}]";
            if (string.IsNullOrWhiteSpace(data.upgradeId))
                errors.Add("[dice_upgrades] missing upgrade_id");
            if (string.IsNullOrWhiteSpace(data.triggerType))
                errors.Add($"{prefix} trigger_type is required");
            else if (!diceUpgradeTriggerWhitelist.Contains(data.triggerType))
                errors.Add($"{prefix} trigger_type is not allowed: {data.triggerType}");

            data.conditions ??= new List<GameConditionDefinition>();
            data.effects ??= new List<GameEffectDefinition>();

            if (data.conditions.Count > 1)
                errors.Add($"{prefix} conditions count must be <= 1");

            for (int conditionIndex = 0; conditionIndex < data.conditions.Count; conditionIndex++)
            {
                var condition = data.conditions[conditionIndex];
                if (string.IsNullOrWhiteSpace(condition.conditionType))
                {
                    errors.Add($"{prefix} condition_type is required");
                    continue;
                }

                if (!diceUpgradeConditionWhitelist.Contains(condition.conditionType))
                    errors.Add($"{prefix} condition_type is not allowed: {condition.conditionType}");
            }

            if (data.effects.Count == 0)
                errors.Add($"{prefix} effects must contain at least one entry");

            ValidateEffects(data.effects, prefix, errors);
        }
    }

    static void ValidateEffects(List<GameEffectDefinition> effects, string prefix, List<string> errors)
    {
        if (effects == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
            {
                errors.Add($"{prefix} effect[{i}] is null");
                continue;
            }

            if (string.IsNullOrWhiteSpace(effect.effectType))
            {
                errors.Add($"{prefix} effect[{i}] missing effect_type");
                continue;
            }

            switch (effect.effectType)
            {
                case "resource_delta":
                    ValidateRequiredValue(effect, prefix, i, errors);
                    if (!situationResourceTypes.Contains(effect.targetResource))
                        errors.Add($"{prefix} effect[{i}] resource_delta requires target_resource(defense|stability)");
                    break;

                case "gold_delta":
                    ValidateRequiredValue(effect, prefix, i, errors);
                    break;

                case "demand_delta":
                    ValidateRequiredValue(effect, prefix, i, errors);
                    if (!demandTargetModes.Contains(effect.targetMode))
                        errors.Add($"{prefix} effect[{i}] demand_delta target_mode is invalid: {effect.targetMode}");
                    if (effect.targetMode == "by_tag" && string.IsNullOrWhiteSpace(effect.targetTag))
                        errors.Add($"{prefix} effect[{i}] demand_delta by_tag requires target_tag");
                    break;

                case "deadline_delta":
                    ValidateRequiredValue(effect, prefix, i, errors);
                    if (!deadlineTargetModes.Contains(effect.targetMode))
                        errors.Add($"{prefix} effect[{i}] deadline_delta target_mode is invalid: {effect.targetMode}");
                    if (effect.targetMode == "by_tag" && string.IsNullOrWhiteSpace(effect.targetTag))
                        errors.Add($"{prefix} effect[{i}] deadline_delta by_tag requires target_tag");
                    break;

                case "resource_guard":
                    if (!situationResourceTypes.Contains(effect.targetResource))
                        errors.Add($"{prefix} effect[{i}] resource_guard requires target_resource(defense|stability)");
                    if (!effect.duration.HasValue || effect.duration.Value <= 0)
                        errors.Add($"{prefix} effect[{i}] resource_guard requires duration > 0");
                    break;

                case "die_face_delta":
                case "die_face_set":
                case "die_face_min":
                case "die_face_mult":
                    ValidateRequiredValue(effect, prefix, i, errors);
                    break;

                case "reroll_assigned_dice":
                    break;

                default:
                    errors.Add($"{prefix} effect[{i}] unknown effect_type: {effect.effectType}");
                    break;
            }
        }
    }

    static void ValidateRequiredValue(GameEffectDefinition effect, string prefix, int effectIndex, List<string> errors)
    {
        if (!effect.value.HasValue)
            errors.Add($"{prefix} effect[{effectIndex}] {effect.effectType} requires value");
    }

    static void ValidateUniqueIds<TData>(IReadOnlyList<TData> source, Func<TData, string> idSelector, string label, List<string> errors)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < source.Count; i++)
        {
            var id = idSelector(source[i]) ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (!set.Add(id))
                errors.Add($"[{label}] duplicate id: {id}");
        }
    }

    static void NormalizeCollections(GameSituationDefinition data)
    {
        data.tags ??= new List<string>();
        data.onTurnStartEffects ??= new List<GameEffectDefinition>();
        data.onSuccess ??= new List<GameEffectDefinition>();
        data.onFail ??= new List<GameEffectDefinition>();
    }
}

