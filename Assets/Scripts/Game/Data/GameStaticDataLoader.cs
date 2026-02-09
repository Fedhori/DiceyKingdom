using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class GameStaticDataLoader
{
    public const string MonstersPath = "Data/Monsters.json";
    public const string AdventurersPath = "Data/Adventurers.json";
    public const string SkillsPath = "Data/Skills.json";

    public static List<MonsterDef> LoadMonsterDefs(string relativePath = MonstersPath)
    {
        var monsters = ParseCatalogList<MonsterDef>(relativePath, "monsters");
        ValidateMonsterDefs(monsters, relativePath);
        return monsters;
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
            monsterDefs = LoadMonsterDefs(),
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

    static void ValidateMonsterDefs(IReadOnlyList<MonsterDef> monsterDefs, string sourcePath)
    {
        var errors = new List<string>();

        for (int i = 0; i < monsterDefs.Count; i++)
        {
            var def = monsterDefs[i];
            if (def == null)
            {
                errors.Add($"monsters[{i}] is null");
                continue;
            }

            if (def.actionPool == null)
            {
                errors.Add($"{def.monsterId}: action_pool is null");
                continue;
            }

            if (def.actionPool.Count != 2)
                errors.Add($"{def.monsterId}: action_pool count must be 2 (actual={def.actionPool.Count})");

            bool hasPrep1 = false;
            bool hasPrep2 = false;
            var ids = new HashSet<string>(StringComparer.Ordinal);

            for (int j = 0; j < def.actionPool.Count; j++)
            {
                var action = def.actionPool[j];
                if (action == null)
                {
                    errors.Add($"{def.monsterId}: action_pool[{j}] is null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(action.actionId))
                    errors.Add($"{def.monsterId}: action_pool[{j}].action_id is empty");
                else if (!ids.Add(action.actionId))
                    errors.Add($"{def.monsterId}: duplicated action_id '{action.actionId}'");

                if (action.weight < 1 || action.weight > 5)
                    errors.Add($"{def.monsterId}:{action.actionId} weight out of range (1..5): {action.weight}");

                if (action.prepTurns < 1 || action.prepTurns > 2)
                    errors.Add($"{def.monsterId}:{action.actionId} prep_turns out of range (1..2): {action.prepTurns}");

                hasPrep1 |= action.prepTurns == 1;
                hasPrep2 |= action.prepTurns == 2;
            }

            if (!hasPrep1)
                errors.Add($"{def.monsterId}: requires at least one prep_turns=1 action");
            if (!hasPrep2)
                errors.Add($"{def.monsterId}: requires at least one prep_turns=2 action");
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
    public List<MonsterDef> monsterDefs = new();
    public List<AdventurerDef> adventurerDefs = new();
    public List<SkillDef> skillDefs = new();
}
