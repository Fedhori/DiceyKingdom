using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;




public static class StaticDataLoader
{
    public const string AdventurersPath = "Data/Adventurers.json";
    public const string MissionsPath = "Data/Missions.json";
    public const string TraitsPath = "Data/Traits.json";

    static readonly HashSet<string> ValidAbilities = new(StringComparer.Ordinal)
    {
        "strength",
        "agility",
        "intelligence"
    };

    static readonly HashSet<string> ValidPolarity = new(StringComparer.Ordinal)
    {
        "positive",
        "negative"
    };

    static readonly HashSet<string> ValidTrigger = new(StringComparer.Ordinal)
    {
        "onAbilityValueCalculation",
        "onExpeditionResolved",
        "onMissionFailed",
        "onTurnSettlement",
        "onHpChanged"
    };

    static readonly HashSet<string> ValidCondition = new(StringComparer.Ordinal)
    {
        "always",
        "hpBelowMax",
        "hpDeltaNegative",
        "expeditionSucceeded",
        "expeditionFailed"
    };

    static readonly HashSet<string> ValidLayer = new(StringComparer.Ordinal)
    {
        "normal",
        "mission"
    };

    static readonly HashSet<string> ValidStackPolicy = new(StringComparer.Ordinal)
    {
        "stack",
        "replace",
        "ignoreIfExists"
    };

    static readonly Dictionary<string, int> EffectParamCountById = new(StringComparer.Ordinal)
    {
        { "addStability", 1 },
        { "addGold", 1 },
        { "addHpSelf", 1 },
        { "addHpAssignedParty", 1 },
        { "addHpAllAdventurers", 1 },
        { "addStaminaSelf", 1 },
        { "addStaminaAssignedParty", 1 },
        { "addStaminaAllAdventurers", 1 },
        { "addXpSelf", 1 },
        { "addXpAssignedParty", 1 },
        { "addAbilitySelf", 3 },
        { "addAbilityAssignedParty", 3 },
        { "addAbilityAllAdventurers", 3 }
    };

    public static StaticDataSet Current { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        Current = null;
    }

    public static StaticDataSet LoadAll()
    {
        List<AdventurerDef> adventurers = LoadAdventurerDefs();
        List<MissionDef> missions = LoadMissionDefs();
        List<TraitDef> traits = LoadTraitDefs();

        ValidateAdventurerDefs(adventurers, AdventurersPath);
        ValidateMissionDefs(missions, MissionsPath);
        ValidateTraitDefs(traits, TraitsPath);

        var set = new StaticDataSet(adventurers, missions, traits);
        Current = set;
        return set;
    }

    public static List<AdventurerDef> LoadAdventurerDefs(string relativePath = AdventurersPath)
    {
        var root = ParseJson<AdventurerDefList>(relativePath);
        return root?.adventurerDefs ?? new List<AdventurerDef>();
    }

    public static List<MissionDef> LoadMissionDefs(string relativePath = MissionsPath)
    {
        var root = ParseJson<MissionDefList>(relativePath);
        return root?.missionDefs ?? new List<MissionDef>();
    }

    public static List<TraitDef> LoadTraitDefs(string relativePath = TraitsPath)
    {
        var root = ParseJson<TraitDefList>(relativePath);
        return root?.traitDefs ?? new List<TraitDef>();
    }

    static T ParseJson<T>(string relativePath) where T : class
    {
        string json = SaCache.ReadText(relativePath);
        if (string.IsNullOrWhiteSpace(json))
            throw new InvalidDataException($"[StaticDataLoader] Empty json: {relativePath}");

        T parsed = JsonConvert.DeserializeObject<T>(json);
        if (parsed == null)
            throw new InvalidDataException($"[StaticDataLoader] Invalid json shape: {relativePath}");

        return parsed;
    }

    static void ValidateAdventurerDefs(IReadOnlyList<AdventurerDef> defs, string sourcePath)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < defs.Count; i++)
        {
            AdventurerDef def = defs[i];
            if (def == null)
            {
                errors.Add($"adventurerDefs[{i}] is null");
                continue;
            }

            ValidateId(def.id, $"adventurerDefs[{i}].id", ids, errors);

            if (def.recruitWeight < 1)
                errors.Add($"adventurerDefs[{i}].recruitWeight must be >= 1");
            if (def.equipmentSlotCount < 0)
                errors.Add($"adventurerDefs[{i}].equipmentSlotCount must be >= 0");

            ValidateRange(def.baseHpMin, def.baseHpMax, $"adventurerDefs[{i}].baseHp", errors);
            ValidateRange(def.baseStaminaMin, def.baseStaminaMax, $"adventurerDefs[{i}].baseStamina", errors);
            ValidateRange(def.baseHeroismMin, def.baseHeroismMax, $"adventurerDefs[{i}].baseHeroism", errors);
            ValidateRange(def.strengthMin, def.strengthMax, $"adventurerDefs[{i}].strength", errors);
            ValidateRange(def.agilityMin, def.agilityMax, $"adventurerDefs[{i}].agility", errors);
            ValidateRange(def.intelligenceMin, def.intelligenceMax, $"adventurerDefs[{i}].intelligence", errors);
            ValidateRange(def.growthStrengthMin, def.growthStrengthMax, $"adventurerDefs[{i}].growthStrength", errors);
            ValidateRange(def.growthAgilityMin, def.growthAgilityMax, $"adventurerDefs[{i}].growthAgility", errors);
            ValidateRange(def.growthIntelligenceMin, def.growthIntelligenceMax, $"adventurerDefs[{i}].growthIntelligence", errors);

            NormalizeRules(def.rules);
            ValidateRules(def.rules, $"adventurerDefs[{i}].rules", errors);
        }

        ThrowIfInvalid(errors, sourcePath);
    }

    static void ValidateMissionDefs(IReadOnlyList<MissionDef> defs, string sourcePath)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < defs.Count; i++)
        {
            MissionDef def = defs[i];
            if (def == null)
            {
                errors.Add($"missionDefs[{i}] is null");
                continue;
            }

            ValidateId(def.id, $"missionDefs[{i}].id", ids, errors);

            if (def.spawnWeight < 1)
                errors.Add($"missionDefs[{i}].spawnWeight must be >= 1");
            if (def.partyLimit < 1)
                errors.Add($"missionDefs[{i}].partyLimit must be >= 1");
            if (def.baseDeadlineTurns < 1)
                errors.Add($"missionDefs[{i}].baseDeadlineTurns must be >= 1");

            if (def.abilityTests == null || def.abilityTests.Count == 0)
            {
                errors.Add($"missionDefs[{i}].abilityTests must contain at least 1 entry");
            }
            else
            {
                for (int testIndex = 0; testIndex < def.abilityTests.Count; testIndex++)
                {
                    AbilityTestDef test = def.abilityTests[testIndex];
                    if (test == null)
                    {
                        errors.Add($"missionDefs[{i}].abilityTests[{testIndex}] is null");
                        continue;
                    }

                    if (test.requiredAbilities == null || test.requiredAbilities.Count == 0)
                        errors.Add($"missionDefs[{i}].abilityTests[{testIndex}].requiredAbilities must contain at least 1 entry");
                    else
                        ValidateAbilities(test.requiredAbilities, $"missionDefs[{i}].abilityTests[{testIndex}].requiredAbilities", errors);

                    if (test.difficulty < 2)
                        errors.Add($"missionDefs[{i}].abilityTests[{testIndex}].difficulty must be >= 2");
                }
            }

            NormalizeRules(def.rules);
            ValidateRules(def.rules, $"missionDefs[{i}].rules", errors);
        }

        ThrowIfInvalid(errors, sourcePath);
    }

    static void ValidateTraitDefs(IReadOnlyList<TraitDef> defs, string sourcePath)
    {
        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.Ordinal);

        for (int i = 0; i < defs.Count; i++)
        {
            TraitDef def = defs[i];
            if (def == null)
            {
                errors.Add($"traitDefs[{i}] is null");
                continue;
            }

            ValidateId(def.id, $"traitDefs[{i}].id", ids, errors);

            if (!ValidPolarity.Contains(def.polarity ?? string.Empty))
                errors.Add($"traitDefs[{i}].polarity is invalid: {def.polarity}");
            if (def.acquireWeight < 1)
                errors.Add($"traitDefs[{i}].acquireWeight must be >= 1");

            NormalizeRules(def.rules);
            ValidateRules(def.rules, $"traitDefs[{i}].rules", errors);
        }

        ThrowIfInvalid(errors, sourcePath);
    }

    static void NormalizeRules(List<RuleDef> rules)
    {
        if (rules == null)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            RuleDef rule = rules[i];
            if (rule == null)
                continue;

            rule.condition ??= new ConditionDef();
            rule.condition.@params ??= new List<float>();
            rule.effects ??= new List<EffectDef>();

            for (int effectIndex = 0; effectIndex < rule.effects.Count; effectIndex++)
            {
                EffectDef effect = rule.effects[effectIndex];
                if (effect == null)
                    continue;

                effect.@params ??= new List<float>();
            }
        }
    }

    static void ValidateRules(IReadOnlyList<RuleDef> rules, string path, List<string> errors)
    {
        if (rules == null)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            RuleDef rule = rules[i];
            if (rule == null)
            {
                errors.Add($"{path}[{i}] is null");
                continue;
            }

            if (!ValidTrigger.Contains(rule.trigger ?? string.Empty))
                errors.Add($"{path}[{i}].trigger is invalid: {rule.trigger}");

            if (rule.condition == null)
            {
                errors.Add($"{path}[{i}].condition is null");
            }
            else
            {
                if (!ValidCondition.Contains(rule.condition.conditionId ?? string.Empty))
                    errors.Add($"{path}[{i}].condition.conditionId is invalid: {rule.condition.conditionId}");
            }

            if (rule.effects == null || rule.effects.Count == 0)
            {
                errors.Add($"{path}[{i}].effects must contain at least 1 entry");
                continue;
            }

            for (int effectIndex = 0; effectIndex < rule.effects.Count; effectIndex++)
            {
                EffectDef effect = rule.effects[effectIndex];
                if (effect == null)
                {
                    errors.Add($"{path}[{i}].effects[{effectIndex}] is null");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(effect.effectId))
                    errors.Add($"{path}[{i}].effects[{effectIndex}].effectId is required");
                else if (!EffectParamCountById.TryGetValue(effect.effectId, out int paramCount))
                    errors.Add($"{path}[{i}].effects[{effectIndex}].effectId is unsupported: {effect.effectId}");
                else if (effect.@params == null || effect.@params.Count != paramCount)
                    errors.Add($"{path}[{i}].effects[{effectIndex}].params count must be {paramCount}");

                if (!ValidLayer.Contains(effect.layer ?? string.Empty))
                    errors.Add($"{path}[{i}].effects[{effectIndex}].layer is invalid: {effect.layer}");
                if (!ValidStackPolicy.Contains(effect.stackPolicy ?? string.Empty))
                    errors.Add($"{path}[{i}].effects[{effectIndex}].stackPolicy is invalid: {effect.stackPolicy}");
            }
        }
    }

    static void ValidateAbilities(IReadOnlyList<string> abilities, string path, List<string> errors)
    {
        for (int i = 0; i < abilities.Count; i++)
        {
            string ability = abilities[i];
            if (!ValidAbilities.Contains(ability ?? string.Empty))
                errors.Add($"{path}[{i}] is invalid: {ability}");
        }
    }

    static void ValidateId(string id, string path, ISet<string> idSet, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            errors.Add($"{path} is required");
            return;
        }

        if (!idSet.Add(id))
            errors.Add($"{path} is duplicated: {id}");
    }

    static void ValidateRange(int min, int max, string path, ICollection<string> errors)
    {
        if (min > max)
            errors.Add($"{path}: min({min}) must be <= max({max})");
    }

    static void ValidateRange(float min, float max, string path, ICollection<string> errors)
    {
        if (min > max)
            errors.Add($"{path}: min({min}) must be <= max({max})");
    }

    static void ThrowIfInvalid(IReadOnlyList<string> errors, string sourcePath)
    {
        if (errors.Count == 0)
            return;

        string message = "[StaticDataLoader] Validation failed (" + sourcePath + ")\n- " +
                         string.Join("\n- ", errors);
        throw new InvalidDataException(message);
    }
}

