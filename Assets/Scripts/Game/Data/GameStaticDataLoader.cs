using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class GameStaticDataLoader
{
    public const string SituationsPath = "Data/Situations.json";
    public const string AdventurersPath = "Data/Adventurers.json";
    public const string SkillsPath = "Data/Skills.json";

    const string AdventurerLocalizationTable = "adventurer";
    const string RuleTriggerOnRoll = "onRoll";
    const string RuleTriggerOnCalculation = "onCalculation";
    const string RuleConditionAlways = "always";
    const string RuleConditionDiceAtLeastCount = "diceAtLeastCount";
    const string RuleEffectDieFaceDelta = "dieFaceDelta";
    const string RuleEffectStabilityDelta = "stabilityDelta";
    const string RuleEffectGoldDelta = "goldDelta";
    const string RuleEffectAttackBonusByThreshold = "attackBonusByThreshold";
    const string RuleEffectFlatAttackBonus = "flatAttackBonus";

    static bool hasLoggedAdventurerLocalizationWarnings;

    public static List<SituationDef> LoadSituationDefs(string relativePath = SituationsPath)
    {
        var situations = ParseCatalogList<SituationDef>(relativePath, "situations");
        ValidateSituationDefs(situations, relativePath);
        return situations;
    }

    public static List<AdventurerDef> LoadAdventurerDefs(string relativePath = AdventurersPath)
    {
        var adventurers = ParseCatalogList<AdventurerDef>(relativePath, "adventurers");
        ValidateAdventurerDefs(adventurers, relativePath);
        return adventurers;
    }

    public static List<SkillDef> LoadSkillDefs(string relativePath = SkillsPath)
    {
        var skills = ParseCatalogList<SkillDef>(relativePath, "skills");
        ValidateSkillDefs(skills, relativePath);
        return skills;
    }

    public static GameStaticDataSet LoadAll(bool logWarningsOnce = true)
    {
        var dataSet = new GameStaticDataSet
        {
            situationDefs = LoadSituationDefs(),
            adventurerDefs = LoadAdventurerDefs(),
            skillDefs = LoadSkillDefs()
        };

        if (logWarningsOnce)
            LogAdventurerLocalizationWarningsOnce(dataSet.adventurerDefs);

        return dataSet;
    }

    public static List<string> CollectAdventurerLocalizationWarnings(IReadOnlyList<AdventurerDef> adventurerDefs)
    {
        var warnings = new List<string>();
        if (adventurerDefs == null)
            return warnings;

        var expectedKeys = BuildExpectedAdventurerLocalizationKeys(adventurerDefs);
        string sharedDataPath = GetLocalizationSharedDataPath(AdventurerLocalizationTable);
        if (!File.Exists(sharedDataPath))
        {
            warnings.Add($"Localization table asset missing: {sharedDataPath}");
            return warnings;
        }

        var actualKeys = ReadSharedTableKeys(sharedDataPath, warnings);
        var expectedOrdered = new List<string>(expectedKeys);
        expectedOrdered.Sort(StringComparer.Ordinal);
        for (int i = 0; i < expectedOrdered.Count; i++)
        {
            string key = expectedOrdered[i];
            if (!actualKeys.Contains(key))
                warnings.Add($"Missing localization key: {AdventurerLocalizationTable}/{key}");
        }

        var actualOrdered = new List<string>(actualKeys);
        actualOrdered.Sort(StringComparer.Ordinal);
        for (int i = 0; i < actualOrdered.Count; i++)
        {
            string key = actualOrdered[i];
            if (!expectedKeys.Contains(key))
                warnings.Add($"Orphan localization key: {AdventurerLocalizationTable}/{key}");
        }

        return warnings;
    }

    static List<T> ParseCatalogList<T>(string relativePath, string listKey)
    {
        string json = SaCache.ReadText(relativePath);
        if (string.IsNullOrWhiteSpace(json))
            return new List<T>();

        var token = JToken.Parse(json);
        if (token.Type == JTokenType.Array)
            return token.ToObject<List<T>>() ?? new List<T>();

        if (token.Type == JTokenType.Object)
        {
            var objectToken = (JObject)token;
            var listToken = objectToken[listKey];
            if (listToken != null && listToken.Type == JTokenType.Array)
                return listToken.ToObject<List<T>>() ?? new List<T>();
        }

        throw new InvalidDataException($"[GameStaticDataLoader] Invalid json shape: {relativePath}");
    }

    static void ValidateAdventurerDefs(IReadOnlyList<AdventurerDef> adventurerDefs, string sourcePath)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < adventurerDefs.Count; i++)
        {
            var def = adventurerDefs[i];
            if (def == null)
            {
                errors.Add($"adventurers[{i}] is null");
                continue;
            }

            if (string.IsNullOrWhiteSpace(def.adventurerId))
            {
                errors.Add($"adventurers[{i}].adventurerId is empty");
            }
            else if (!ids.Add(def.adventurerId))
            {
                errors.Add($"duplicated adventurerId '{def.adventurerId}'");
            }

            if (def.diceCount < 1)
                errors.Add($"{def.adventurerId}: diceCount must be >= 1 (actual={def.diceCount})");

            if (def.gearSlotCount < 0)
                errors.Add($"{def.adventurerId}: gearSlotCount must be >= 0 (actual={def.gearSlotCount})");

            if (def.rules == null)
            {
                def.rules = new List<AdventurerRuleDef>();
                continue;
            }

            for (int ruleIndex = 0; ruleIndex < def.rules.Count; ruleIndex++)
            {
                var rule = def.rules[ruleIndex];
                if (rule == null)
                {
                    errors.Add($"{def.adventurerId}.rules[{ruleIndex}] is null");
                    continue;
                }

                string triggerType = rule.trigger?.type?.Trim();
                if (!string.Equals(triggerType, RuleTriggerOnRoll, StringComparison.Ordinal) &&
                    !string.Equals(triggerType, RuleTriggerOnCalculation, StringComparison.Ordinal))
                {
                    errors.Add(
                        $"{def.adventurerId}.rules[{ruleIndex}].trigger.type must be '{RuleTriggerOnRoll}' or '{RuleTriggerOnCalculation}' (actual='{triggerType}')");
                    continue;
                }

                if (rule.condition == null)
                    rule.condition = new AdventurerRuleConditionDef();

                string conditionType = rule.condition.type?.Trim();
                if (string.IsNullOrWhiteSpace(conditionType))
                {
                    rule.condition.type = RuleConditionAlways;
                    conditionType = RuleConditionAlways;
                }

                if (!string.Equals(conditionType, RuleConditionAlways, StringComparison.Ordinal) &&
                    !string.Equals(conditionType, RuleConditionDiceAtLeastCount, StringComparison.Ordinal))
                {
                    errors.Add(
                        $"{def.adventurerId}.rules[{ruleIndex}].condition.type must be '{RuleConditionAlways}' or '{RuleConditionDiceAtLeastCount}' (actual='{conditionType}')");
                }
                else if (string.Equals(conditionType, RuleConditionDiceAtLeastCount, StringComparison.Ordinal))
                {
                    if (!TryGetIntParam(rule.condition.conditionParams, "threshold", out int threshold) || threshold < 1)
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].condition.params.threshold must be >= 1 for '{RuleConditionDiceAtLeastCount}'");
                    }

                    if (!TryGetIntParam(rule.condition.conditionParams, "count", out int count) || count < 1)
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].condition.params.count must be >= 1 for '{RuleConditionDiceAtLeastCount}'");
                    }
                }

                if (rule.effect == null || string.IsNullOrWhiteSpace(rule.effect.effectType))
                {
                    errors.Add($"{def.adventurerId}.rules[{ruleIndex}].effect.effectType is empty");
                    continue;
                }

                string effectType = rule.effect.effectType.Trim();
                if (string.Equals(triggerType, RuleTriggerOnRoll, StringComparison.Ordinal))
                {
                    bool allowed =
                        string.Equals(effectType, RuleEffectDieFaceDelta, StringComparison.Ordinal) ||
                        string.Equals(effectType, RuleEffectStabilityDelta, StringComparison.Ordinal) ||
                        string.Equals(effectType, RuleEffectGoldDelta, StringComparison.Ordinal);
                    if (!allowed)
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].effect.effectType '{effectType}' is not allowed for trigger '{RuleTriggerOnRoll}'");
                    }
                }
                else if (string.Equals(triggerType, RuleTriggerOnCalculation, StringComparison.Ordinal))
                {
                    bool allowed =
                        string.Equals(effectType, RuleEffectAttackBonusByThreshold, StringComparison.Ordinal) ||
                        string.Equals(effectType, RuleEffectFlatAttackBonus, StringComparison.Ordinal);
                    if (!allowed)
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].effect.effectType '{effectType}' is not allowed for trigger '{RuleTriggerOnCalculation}'");
                    }
                }

                if (string.Equals(effectType, RuleEffectDieFaceDelta, StringComparison.Ordinal))
                {
                    string diePickRule = GetParamString(rule.effect.effectParams, "diePickRule");
                    bool validPickRule =
                        string.Equals(diePickRule, "selected", StringComparison.Ordinal) ||
                        string.Equals(diePickRule, "lowest", StringComparison.Ordinal) ||
                        string.Equals(diePickRule, "highest", StringComparison.Ordinal) ||
                        string.Equals(diePickRule, "all", StringComparison.Ordinal);
                    if (!validPickRule)
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].effect.params.diePickRule must be selected|lowest|highest|all (actual='{diePickRule}')");
                    }

                    if ((string.Equals(diePickRule, "lowest", StringComparison.Ordinal) ||
                         string.Equals(diePickRule, "highest", StringComparison.Ordinal)) &&
                        TryGetIntParam(rule.effect.effectParams, "count", out int applyCount) &&
                        applyCount < 1)
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].effect.params.count must be >= 1 when diePickRule is lowest/highest");
                    }
                }
                else if (string.Equals(effectType, RuleEffectAttackBonusByThreshold, StringComparison.Ordinal))
                {
                    if (!TryGetIntParam(rule.effect.effectParams, "threshold", out int threshold) || threshold < 1)
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].effect.params.threshold must be >= 1 for '{RuleEffectAttackBonusByThreshold}'");
                    }

                    if (!TryGetIntParam(rule.effect.effectParams, "bonusPerMatch", out _))
                    {
                        errors.Add(
                            $"{def.adventurerId}.rules[{ruleIndex}].effect.params.bonusPerMatch is required for '{RuleEffectAttackBonusByThreshold}'");
                    }
                }
            }
        }

        if (errors.Count == 0)
            return;

        var message = $"[GameStaticDataLoader] Validation failed ({sourcePath})\n- " +
                      string.Join("\n- ", errors);
        Debug.LogError(message);
        throw new InvalidDataException(message);
    }

    static void ValidateSituationDefs(IReadOnlyList<SituationDef> situationDefs, string sourcePath)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < situationDefs.Count; i++)
        {
            var def = situationDefs[i];
            if (def == null)
            {
                errors.Add($"situations[{i}] is null");
                continue;
            }

            if (string.IsNullOrWhiteSpace(def.situationId))
            {
                errors.Add($"situations[{i}].situationId is empty");
            }
            else if (!ids.Add(def.situationId))
            {
                errors.Add($"duplicated situationId '{def.situationId}'");
            }

            if (def.baseRequirement < 1)
                errors.Add($"{def.situationId}: baseRequirement must be >= 1 (actual={def.baseRequirement})");

            if (def.baseDeadlineTurns < 1)
                errors.Add($"{def.situationId}: baseDeadlineTurns must be >= 1 (actual={def.baseDeadlineTurns})");

            if (def.successReward == null)
                def.successReward = new EffectBundle();
            if (def.failureEffect == null)
                def.failureEffect = new EffectBundle();

            string persistMode = NormalizePersistMode(def.failurePersistMode);
            if (!string.Equals(persistMode, "remove", StringComparison.Ordinal) &&
                !string.Equals(persistMode, "resetDeadline", StringComparison.Ordinal))
            {
                errors.Add(
                    $"{def.situationId}: failurePersistMode must be 'remove' or 'resetDeadline' (actual='{def.failurePersistMode}')");
            }

            def.failurePersistMode = persistMode;
        }

        if (errors.Count == 0)
            return;

        var message = $"[GameStaticDataLoader] Validation failed ({sourcePath})\n- " +
                      string.Join("\n- ", errors);
        Debug.LogError(message);
        throw new InvalidDataException(message);
    }

    static void ValidateSkillDefs(IReadOnlyList<SkillDef> skillDefs, string sourcePath)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < skillDefs.Count; i++)
        {
            var def = skillDefs[i];
            if (def == null)
            {
                errors.Add($"skills[{i}] is null");
                continue;
            }

            if (string.IsNullOrWhiteSpace(def.skillId))
            {
                errors.Add($"skills[{i}].skillId is empty");
            }
            else if (!ids.Add(def.skillId))
            {
                errors.Add($"duplicated skillId '{def.skillId}'");
            }

            if (def.maxUsesPerTurn < 1)
                errors.Add($"{def.skillId}: maxUsesPerTurn must be >= 1 (actual={def.maxUsesPerTurn})");

            if (def.effectBundle == null)
                def.effectBundle = new EffectBundle();
        }

        if (errors.Count == 0)
            return;

        var message = $"[GameStaticDataLoader] Validation failed ({sourcePath})\n- " +
                      string.Join("\n- ", errors);
        Debug.LogError(message);
        throw new InvalidDataException(message);
    }

    static string NormalizePersistMode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "remove";

        string trimmed = raw.Trim();
        if (string.Equals(trimmed, "remove", StringComparison.OrdinalIgnoreCase))
            return "remove";
        if (string.Equals(trimmed, "resetDeadline", StringComparison.OrdinalIgnoreCase))
            return "resetDeadline";

        return trimmed.ToLowerInvariant();
    }

    static void LogAdventurerLocalizationWarningsOnce(IReadOnlyList<AdventurerDef> adventurerDefs)
    {
        if (hasLoggedAdventurerLocalizationWarnings)
            return;

        hasLoggedAdventurerLocalizationWarnings = true;
        var warnings = CollectAdventurerLocalizationWarnings(adventurerDefs);
        if (warnings.Count == 0)
        {
            Debug.Log("[GameStaticDataLoader] Adventurer localization key validation passed.");
            return;
        }

        Debug.LogWarning(
            "[GameStaticDataLoader] Adventurer localization key warnings\n- " +
            string.Join("\n- ", warnings));
    }

    static HashSet<string> BuildExpectedAdventurerLocalizationKeys(IReadOnlyList<AdventurerDef> adventurerDefs)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < adventurerDefs.Count; i++)
        {
            var def = adventurerDefs[i];
            if (def == null)
                continue;

            if (!string.IsNullOrWhiteSpace(def.nameKey))
                keys.Add(def.nameKey.Trim());

            if (string.IsNullOrWhiteSpace(def.adventurerId) || def.rules == null)
                continue;

            for (int ruleIndex = 0; ruleIndex < def.rules.Count; ruleIndex++)
                keys.Add($"{def.adventurerId}.rule.{ruleIndex}");
        }

        return keys;
    }

    static string GetLocalizationSharedDataPath(string tableName)
    {
        return Path.Combine(Application.dataPath, "Localization", $"{tableName} Shared Data.asset");
    }

    static HashSet<string> ReadSharedTableKeys(string path, List<string> warnings)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        try
        {
            foreach (string rawLine in File.ReadLines(path))
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string line = rawLine.Trim();
                if (!line.StartsWith("m_Key:", StringComparison.Ordinal))
                    continue;

                string key = line.Substring("m_Key:".Length).Trim();
                key = TrimOuterQuotes(key);
                if (!string.IsNullOrWhiteSpace(key))
                    keys.Add(key);
            }
        }
        catch (Exception exception)
        {
            warnings.Add($"Failed to read localization table asset '{path}': {exception.Message}");
        }

        return keys;
    }

    static string TrimOuterQuotes(string raw)
    {
        if (string.IsNullOrEmpty(raw) || raw.Length < 2)
            return raw;

        if ((raw[0] == '"' && raw[^1] == '"') || (raw[0] == '\'' && raw[^1] == '\''))
            return raw.Substring(1, raw.Length - 2);

        return raw;
    }

    static bool TryGetIntParam(JObject source, string key, out int value)
    {
        value = 0;
        if (source == null || string.IsNullOrWhiteSpace(key))
            return false;

        var token = source[key];
        if (token == null || token.Type == JTokenType.Null)
            return false;

        if (token.Type == JTokenType.Integer || token.Type == JTokenType.Float)
        {
            value = (int)Math.Round(token.Value<double>(), MidpointRounding.AwayFromZero);
            return true;
        }

        return int.TryParse(token.ToString().Trim(), out value);
    }

    static string GetParamString(JObject source, string key)
    {
        if (source == null || string.IsNullOrWhiteSpace(key))
            return string.Empty;

        var token = source[key];
        if (token == null || token.Type == JTokenType.Null)
            return string.Empty;

        return token.ToString().Trim();
    }
}

[Serializable]
public sealed class GameStaticDataSet
{
    public List<SituationDef> situationDefs = new();
    public List<AdventurerDef> adventurerDefs = new();
    public List<SkillDef> skillDefs = new();
}
