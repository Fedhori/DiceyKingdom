using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Application.Duel
{
    public enum DuelUiAbilityType
    {
        Unknown = 0,
        Attack = 1,
        Skill = 2,
        Passive = 3
    }

    public readonly struct DuelUiEffectLineData
    {
        public int amount { get; }
        public string op { get; }

        public DuelUiEffectLineData(int amount, string op)
        {
            this.amount = amount;
            this.op = op ?? string.Empty;
        }
    }

    public readonly struct DuelUiAbilityData
    {
        public string instanceId { get; }
        public string abilityDefId { get; }
        public DuelUiAbilityType abilityType { get; }
        public int power { get; }
        public int cooldownTurns { get; }
        public int cooldownRemaining { get; }
        public string nameLocKey { get; }
        public string descLocKey { get; }
        public string iconId { get; }
        public string iconPath { get; }
        public IReadOnlyList<DuelUiEffectLineData> effects { get; }

        public bool hasEffects => effects != null && effects.Count > 0;

        public DuelUiAbilityData(
            string instanceId,
            string abilityDefId,
            DuelUiAbilityType abilityType,
            int power,
            int cooldownTurns,
            int cooldownRemaining,
            string nameLocKey,
            string descLocKey,
            string iconId,
            string iconPath,
            IReadOnlyList<DuelUiEffectLineData> effects)
        {
            this.instanceId = instanceId ?? string.Empty;
            this.abilityDefId = abilityDefId ?? string.Empty;
            this.abilityType = abilityType;
            this.power = Mathf.Max(0, power);
            this.cooldownTurns = Mathf.Max(0, cooldownTurns);
            this.cooldownRemaining = Mathf.Max(0, cooldownRemaining);
            this.nameLocKey = nameLocKey ?? string.Empty;
            this.descLocKey = descLocKey ?? string.Empty;
            this.iconId = iconId ?? string.Empty;
            this.iconPath = iconPath ?? string.Empty;
            this.effects = effects ?? Array.Empty<DuelUiEffectLineData>();
        }
    }

    public readonly struct DuelUiIconDefinition
    {
        public string iconId { get; }
        public string path { get; }

        public DuelUiIconDefinition(string iconId, string path)
        {
            this.iconId = iconId ?? string.Empty;
            this.path = path ?? string.Empty;
        }
    }

    public sealed class DuelUiQueryService
    {
        readonly List<DuelUiIconDefinition> iconDefinitions = new();
        GameDatabase database;

        public bool IsBound => database != null;

        public string DefaultIconId => AbilityIconPathPolicy.DefaultIconId;
        public string DefaultIconPath => AbilityIconPathPolicy.DefaultIconPath;

        public bool TryBindRuntimeData(out string failureMessage)
        {
            return TryBindDatabase(GameDataRuntime.CurrentDatabase, out failureMessage);
        }

        public bool TryBindDatabase(GameDatabase database, out string failureMessage)
        {
            failureMessage = string.Empty;
            if (database == null)
            {
                failureMessage = "database is null.";
                return false;
            }

            this.database = database;
            RebuildIconDefinitions();
            return true;
        }

        public bool TryCreateSessionSystems(
            out DuelSessionBuilder sessionBuilder,
            out DuelTurnProcessor turnProcessor,
            out string failureMessage)
        {
            sessionBuilder = null;
            turnProcessor = null;
            failureMessage = string.Empty;

            if (!IsBound)
            {
                failureMessage = "query service is not bound.";
                return false;
            }

            sessionBuilder = new DuelSessionBuilder(database);
            turnProcessor = new DuelTurnProcessor(database);
            return true;
        }

        public bool TryGetAbilityData(
            DuelState duelState,
            string abilityInstanceId,
            out DuelUiAbilityData abilityData,
            out string failureMessage)
        {
            abilityData = default;
            failureMessage = string.Empty;

            if (!TryResolveAbilityAndDef(
                    duelState,
                    abilityInstanceId,
                    out AbilityInstance ability,
                    out AbilityDef def,
                    out failureMessage))
            {
                return false;
            }

            string iconPath = AbilityIconPathPolicy.DefaultIconPath;
            if (!string.IsNullOrWhiteSpace(def.iconId) &&
                AbilityIconPathPolicy.TryBuildPath(def.iconId, out string resolvedPath))
            {
                iconPath = resolvedPath;
            }

            var effectLines = BuildEffectLineData(def);
            abilityData = new DuelUiAbilityData(
                abilityInstanceId,
                ability.abilityDefId,
                ToUiAbilityType(ability.abilityType),
                ResolveEffectivePower(ability),
                ability.cooldownTurns,
                ability.cooldownRemaining,
                def.nameLocKey,
                def.descLocKey,
                def.iconId,
                iconPath,
                effectLines);
            return true;
        }

        public bool TryGetAbilityType(DuelState duelState, string abilityInstanceId, out DuelUiAbilityType abilityType)
        {
            abilityType = DuelUiAbilityType.Unknown;
            if (duelState?.abilitiesById == null || string.IsNullOrWhiteSpace(abilityInstanceId))
            {
                return false;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityInstanceId, out AbilityInstance ability) || ability == null)
            {
                return false;
            }

            abilityType = ToUiAbilityType(ability.abilityType);
            return true;
        }

        public bool IsAttackAbility(AbilityInstance ability)
        {
            return ability != null && ToUiAbilityType(ability.abilityType) == DuelUiAbilityType.Attack;
        }

        public bool IsAttackAbility(DuelState duelState, string abilityInstanceId)
        {
            return TryGetAbilityType(duelState, abilityInstanceId, out DuelUiAbilityType abilityType) &&
                abilityType == DuelUiAbilityType.Attack;
        }

        public bool IsAttackDeployable(DuelState duelState, string abilityInstanceId)
        {
            if (!TryGetAbilityData(duelState, abilityInstanceId, out DuelUiAbilityData abilityData, out _))
            {
                return false;
            }

            return abilityData.abilityType == DuelUiAbilityType.Attack &&
                abilityData.cooldownRemaining <= 0;
        }

        public int ResolveEffectivePower(AbilityInstance ability)
        {
            if (ability == null)
            {
                return 0;
            }

            ability.EnsureInitialized();
            return Mathf.Max(
                0,
                NumericModifierCalculator.Apply(
                    ability.power,
                    ability.powerModifiers,
                    minValue: 0,
                    logContext: "DuelUiQueryService.ResolveEffectivePower"));
        }

        public bool TryGetIconDefinitions(out IReadOnlyList<DuelUiIconDefinition> definitions, out string failureMessage)
        {
            definitions = Array.Empty<DuelUiIconDefinition>();
            failureMessage = string.Empty;
            if (!IsBound)
            {
                failureMessage = "query service is not bound.";
                return false;
            }

            definitions = iconDefinitions;
            return true;
        }

        bool TryResolveAbilityAndDef(
            DuelState duelState,
            string abilityInstanceId,
            out AbilityInstance ability,
            out AbilityDef def,
            out string failureMessage)
        {
            ability = null;
            def = null;
            failureMessage = string.Empty;

            if (!IsBound)
            {
                failureMessage = "query service is not bound.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(abilityInstanceId))
            {
                failureMessage = "abilityInstanceId is empty.";
                return false;
            }

            if (duelState?.abilitiesById == null)
            {
                failureMessage = "duel state or abilitiesById is null.";
                return false;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityInstanceId, out ability) || ability == null)
            {
                failureMessage = $"ability instance does not exist ({abilityInstanceId}).";
                return false;
            }

            if (database.abilitiesById == null ||
                !database.abilitiesById.TryGetValue(ability.abilityDefId, out def) ||
                def == null)
            {
                failureMessage =
                    $"ability def does not exist for instance({abilityInstanceId}) defId({ability.abilityDefId}).";
                return false;
            }

            return true;
        }

        void RebuildIconDefinitions()
        {
            iconDefinitions.Clear();
            iconDefinitions.Add(new DuelUiIconDefinition(DefaultIconId, DefaultIconPath));

            if (database?.abilitiesById == null)
            {
                return;
            }

            var addedIconIds = new HashSet<string>(StringComparer.Ordinal)
            {
                DefaultIconId
            };
            foreach (KeyValuePair<string, AbilityDef> pair in database.abilitiesById)
            {
                AbilityDef abilityDef = pair.Value;
                if (abilityDef == null || string.IsNullOrWhiteSpace(abilityDef.iconId))
                {
                    continue;
                }

                string iconId = abilityDef.iconId;
                if (addedIconIds.Contains(iconId))
                {
                    continue;
                }

                string iconPath = DefaultIconPath;
                if (AbilityIconPathPolicy.TryBuildPath(iconId, out string resolvedPath))
                {
                    iconPath = resolvedPath;
                }

                iconDefinitions.Add(new DuelUiIconDefinition(iconId, iconPath));
                addedIconIds.Add(iconId);
            }
        }

        static DuelUiAbilityType ToUiAbilityType(AbilityType abilityType)
        {
            switch (abilityType)
            {
                case AbilityType.Attack:
                    return DuelUiAbilityType.Attack;
                case AbilityType.Skill:
                    return DuelUiAbilityType.Skill;
                case AbilityType.Passive:
                    return DuelUiAbilityType.Passive;
                default:
                    return DuelUiAbilityType.Unknown;
            }
        }

        static List<DuelUiEffectLineData> BuildEffectLineData(AbilityDef def)
        {
            if (def?.effects == null || def.effects.Count <= 0)
            {
                return new List<DuelUiEffectLineData>();
            }

            var lines = new List<DuelUiEffectLineData>(def.effects.Count);
            for (int effectIndex = 0; effectIndex < def.effects.Count; effectIndex++)
            {
                lines.Add(
                    new DuelUiEffectLineData(
                        ResolveAmount(def, effectIndex),
                        ResolveOp(def, effectIndex)));
            }

            return lines;
        }

        static int ResolveAmount(AbilityDef def, int effectIndex)
        {
            if (def?.effects == null || effectIndex < 0 || effectIndex >= def.effects.Count)
            {
                return 0;
            }

            TimedEffectDef effect = def.effects[effectIndex];
            if (effect?.ops == null || effect.ops.Count <= 0)
            {
                return 0;
            }

            for (int opIndex = 0; opIndex < effect.ops.Count; opIndex++)
            {
                EffectOpDef opDef = effect.ops[opIndex];
                if (opDef != null && opDef.TryGetAmount(out int amount))
                {
                    return amount;
                }
            }

            return 0;
        }

        static string ResolveOp(AbilityDef def, int effectIndex)
        {
            if (def?.effects == null || effectIndex < 0 || effectIndex >= def.effects.Count)
            {
                return string.Empty;
            }

            TimedEffectDef effect = def.effects[effectIndex];
            if (effect?.ops == null || effect.ops.Count <= 0)
            {
                return string.Empty;
            }

            for (int opIndex = 0; opIndex < effect.ops.Count; opIndex++)
            {
                EffectOpDef opDef = effect.ops[opIndex];
                if (opDef != null && !string.IsNullOrWhiteSpace(opDef.op))
                {
                    return opDef.op;
                }
            }

            return string.Empty;
        }
    }
}
