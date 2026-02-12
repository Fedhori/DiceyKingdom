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

    public static List<SituationDef> LoadSituationDefs(string relativePath = SituationsPath)
    {
        var situations = ParseCatalogList<SituationDef>(relativePath, "situations");
        ValidateSituationDefs(situations, relativePath);
        return situations;
    }

    public static List<AdventurerDef> LoadAdventurerDefs(string relativePath = AdventurersPath)
    {
        return ParseCatalogList<AdventurerDef>(relativePath, "adventurers");
    }

    public static List<SkillDef> LoadSkillDefs(string relativePath = SkillsPath)
    {
        return ParseCatalogList<SkillDef>(relativePath, "skills");
    }

    public static GameStaticDataSet LoadAll()
    {
        return new GameStaticDataSet
        {
            situationDefs = LoadSituationDefs(),
            adventurerDefs = LoadAdventurerDefs(),
            skillDefs = LoadSkillDefs()
        };
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
}

[Serializable]
public sealed class GameStaticDataSet
{
    public List<SituationDef> situationDefs = new();
    public List<AdventurerDef> adventurerDefs = new();
    public List<SkillDef> skillDefs = new();
}
