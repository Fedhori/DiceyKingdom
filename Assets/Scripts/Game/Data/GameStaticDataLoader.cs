using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class GameStaticDataLoader
{
    public const string EnemiesPath = "Data/Enemies.json";
    public const string AdventurersPath = "Data/Adventurers.json";
    public const string SkillsPath = "Data/Skills.json";
    public const string EnemyStagePresetsPath = "Data/EnemyStagePresets.json";

    public static List<EnemyDef> LoadEnemyDefs(string relativePath = EnemiesPath)
    {
        var enemies = ParseCatalogList<EnemyDef>(relativePath, "enemies");
        ValidateEnemyDefs(enemies, relativePath);
        return enemies;
    }

    public static List<AdventurerDef> LoadAdventurerDefs(string relativePath = AdventurersPath)
    {
        return ParseCatalogList<AdventurerDef>(relativePath, "adventurers");
    }

    public static List<SkillDef> LoadSkillDefs(string relativePath = SkillsPath)
    {
        return ParseCatalogList<SkillDef>(relativePath, "skills");
    }

    public static List<EnemyStagePresetDef> LoadEnemyStagePresetDefs(
        IReadOnlyList<EnemyDef> enemyDefs,
        string relativePath = EnemyStagePresetsPath)
    {
        var presets = ParseCatalogList<EnemyStagePresetDef>(relativePath, "stage_presets");
        ValidateEnemyStagePresetDefs(presets, enemyDefs, relativePath);
        return presets;
    }

    public static GameStaticDataSet LoadAll()
    {
        var enemyDefs = LoadEnemyDefs();
        return new GameStaticDataSet
        {
            enemyDefs = enemyDefs,
            adventurerDefs = LoadAdventurerDefs(),
            skillDefs = LoadSkillDefs(),
            stagePresetDefs = LoadEnemyStagePresetDefs(enemyDefs)
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

    static void ValidateEnemyDefs(IReadOnlyList<EnemyDef> enemyDefs, string sourcePath)
    {
        var errors = new List<string>();

        for (int i = 0; i < enemyDefs.Count; i++)
        {
            var def = enemyDefs[i];
            if (def == null)
            {
                errors.Add($"enemies[{i}] is null");
                continue;
            }

            if (def.actionPool == null)
            {
                errors.Add($"{def.enemyId}: action_pool is null");
                continue;
            }

            if (def.actionPool.Count != 2)
                errors.Add($"{def.enemyId}: action_pool count must be 2 (actual={def.actionPool.Count})");

            bool hasPrep1 = false;
            bool hasPrep2 = false;
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int j = 0; j < def.actionPool.Count; j++)
            {
                var action = def.actionPool[j];
                if (action == null)
                {
                    errors.Add($"{def.enemyId}: action_pool[{j}] is null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(action.actionId))
                    errors.Add($"{def.enemyId}: action_pool[{j}].action_id is empty");
                else if (!ids.Add(action.actionId))
                    errors.Add($"{def.enemyId}: duplicated action_id '{action.actionId}'");

                if (action.weight < 1 || action.weight > 5)
                    errors.Add($"{def.enemyId}:{action.actionId} weight out of range (1..5): {action.weight}");

                if (action.prepTurns < 1 || action.prepTurns > 2)
                    errors.Add($"{def.enemyId}:{action.actionId} prep_turns out of range (1..2): {action.prepTurns}");

                hasPrep1 |= action.prepTurns == 1;
                hasPrep2 |= action.prepTurns == 2;
            }

            if (!hasPrep1)
                errors.Add($"{def.enemyId}: requires at least one prep_turns=1 action");
            if (!hasPrep2)
                errors.Add($"{def.enemyId}: requires at least one prep_turns=2 action");
        }

        if (errors.Count == 0)
            return;

        var message = $"[GameStaticDataLoader] Validation failed ({sourcePath})\n- " +
                      string.Join("\n- ", errors);
        Debug.LogError(message);
        throw new InvalidDataException(message);
    }

    static void ValidateEnemyStagePresetDefs(
        IReadOnlyList<EnemyStagePresetDef> presetDefs,
        IReadOnlyList<EnemyDef> enemyDefs,
        string sourcePath)
    {
        var errors = new List<string>();

        if (presetDefs.Count != 3)
            errors.Add($"stage_presets count must be 3 (actual={presetDefs.Count})");

        var knownEnemyIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < enemyDefs.Count; i++)
        {
            var enemyDef = enemyDefs[i];
            if (enemyDef == null || string.IsNullOrWhiteSpace(enemyDef.enemyId))
                continue;

            knownEnemyIds.Add(enemyDef.enemyId);
        }

        var presetIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < presetDefs.Count; i++)
        {
            var preset = presetDefs[i];
            if (preset == null)
            {
                errors.Add($"stage_presets[{i}] is null");
                continue;
            }

            if (string.IsNullOrWhiteSpace(preset.presetId))
                errors.Add($"stage_presets[{i}].preset_id is empty");
            else if (!presetIds.Add(preset.presetId))
                errors.Add($"duplicated preset_id '{preset.presetId}'");

            if (preset.spawns == null || preset.spawns.Count == 0)
            {
                errors.Add($"{preset.presetId}: spawns must contain at least one entry");
                continue;
            }

            for (int j = 0; j < preset.spawns.Count; j++)
            {
                var spawn = preset.spawns[j];
                if (spawn == null)
                {
                    errors.Add($"{preset.presetId}: spawns[{j}] is null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(spawn.enemyId))
                {
                    errors.Add($"{preset.presetId}: spawns[{j}].enemy_id is empty");
                }
                else if (!knownEnemyIds.Contains(spawn.enemyId))
                {
                    errors.Add($"{preset.presetId}: unknown enemy_id '{spawn.enemyId}'");
                }

                if (spawn.count < 1)
                    errors.Add($"{preset.presetId}: spawns[{j}].count must be >= 1 (actual={spawn.count})");
            }
        }

        if (errors.Count == 0)
            return;

        var message = $"[GameStaticDataLoader] Validation failed ({sourcePath})\n- " +
                      string.Join("\n- ", errors);
        Debug.LogError(message);
        throw new InvalidDataException(message);
    }
}

[Serializable]
public sealed class GameStaticDataSet
{
    public List<EnemyDef> enemyDefs = new();
    public List<AdventurerDef> adventurerDefs = new();
    public List<SkillDef> skillDefs = new();
    public List<EnemyStagePresetDef> stagePresetDefs = new();
}

