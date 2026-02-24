using System;
using System.Collections.Generic;
using Game.Application.Duel.Effects;

namespace Game.Infrastructure.Data
{
    public sealed class GameDataValidator
    {
        const double probabilityEpsilon = 0.000001d;

        static readonly HashSet<string> allowedOpCodes = new(StringComparer.Ordinal)
        {
            nameof(DuelEffectOpCode.ModifyPowerResult),
            nameof(DuelEffectOpCode.MoveAbility),
            nameof(DuelEffectOpCode.MoveOpponentAbility),
            nameof(DuelEffectOpCode.ModifyTotalPower),
            nameof(DuelEffectOpCode.ModifyHealth),
            nameof(DuelEffectOpCode.AddPowerModifier)
        };

        static readonly HashSet<string> allowedConditionTypes = new(StringComparer.Ordinal)
        {
            "Always",
            "IsInLoadout",
            "OpponentCountEquals",
            "HasTag"
        };

        public void Validate(GameDatabase database, DataIndexDef dataIndex, GameDataValidationReport report)
        {
            if (database == null)
            {
                throw new ArgumentNullException(nameof(database));
            }

            if (dataIndex == null)
            {
                throw new ArgumentNullException(nameof(dataIndex));
            }

            if (report == null)
            {
                throw new ArgumentNullException(nameof(report));
            }

            ValidateRequiredConfigs(database, report);
            ValidateConfigValues(database, report);
            ValidateAbilityDefs(database, report);
            ValidateEncounterDefs(database, report);
            ValidateEffectOps(database, report);
        }

        void ValidateRequiredConfigs(GameDatabase database, GameDataValidationReport report)
        {
            if (database.duelConfig == null)
            {
                report.AddError(
                    GameDataErrorCode.MissingRequiredConfig,
                    GameDataConstants.DefaultDataIndexPath,
                    "duel.config",
                    "Required config 'duel.config' is missing.");
            }

            if (database.runConfig == null)
            {
                report.AddError(
                    GameDataErrorCode.MissingRequiredConfig,
                    GameDataConstants.DefaultDataIndexPath,
                    "run.config",
                    "Required config 'run.config' is missing.");
            }

            if (database.playerStart == null)
            {
                report.AddError(
                    GameDataErrorCode.MissingRequiredConfig,
                    GameDataConstants.DefaultDataIndexPath,
                    "player.start",
                    "Required config 'player.start' is missing.");
            }
        }

        void ValidateConfigValues(GameDatabase database, GameDataValidationReport report)
        {
            if (database.duelConfig != null)
            {
                if (database.duelConfig.powerResultMin < 1)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.duelConfigSourcePath,
                        database.duelConfig.id,
                        "powerResultMin must be greater than or equal to 1.");
                }
            }

            if (database.runConfig != null)
            {
                if (database.runConfig.startingHonor < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.runConfigSourcePath,
                        database.runConfig.id,
                        "startingHonor must be greater than or equal to 0.");
                }

                if (database.runConfig.capacity <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.runConfigSourcePath,
                        database.runConfig.id,
                        "capacity must be greater than zero.");
                }
            }

            if (database.playerStart != null)
            {
                if (database.playerStart.startingHonor < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingHonor must be greater than or equal to 0.");
                }

                if (database.playerStart.startingPlayerHealth <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingPlayerHealth must be greater than zero.");
                }

                List<string> startingLoadoutAbilityIds = database.playerStart.startingLoadoutAbilityIds;
                if (startingLoadoutAbilityIds == null || startingLoadoutAbilityIds.Count <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingLoadoutAbilityIds must contain at least one ability id.");
                }
                else
                {
                    for (int i = 0; i < startingLoadoutAbilityIds.Count; i++)
                    {
                        string abilityId = startingLoadoutAbilityIds[i];
                        if (string.IsNullOrWhiteSpace(abilityId))
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingLoadoutAbilityIds[{i}] must not be empty.");
                            continue;
                        }

                        if (!database.abilitiesById.ContainsKey(abilityId))
                        {
                            report.AddError(
                                GameDataErrorCode.MissingReference,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingLoadoutAbilityIds[{i}]('{abilityId}') does not exist.");
                        }
                    }
                }
            }
        }

        void ValidateAbilityDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, AbilityDef> pair in database.abilitiesById)
            {
                string id = pair.Key;
                AbilityDef def = pair.Value;
                string path = database.abilitySourcePathById.TryGetValue(id, out string foundPath)
                    ? foundPath
                    : string.Empty;

                if (!def.TryGetAbilityType(out AbilityType abilityType))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidEnum,
                        path,
                        id,
                        $"type '{def.type}' is invalid. Allowed: {AbilityType.Attack}, {AbilityType.Skill}, {AbilityType.Passive}.");
                    continue;
                }

                if (def.buildCost < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "buildCost must be greater than or equal to 0.");
                }

                if (def.cooldown < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "cooldown must be greater than or equal to 0.");
                }

                int resolvedPower = def.ResolvePower();
                if (abilityType == AbilityType.Attack && resolvedPower <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "Attack type ability must define power greater than 0.");
                }

                if (abilityType != AbilityType.Attack && resolvedPower != 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "Only Attack type ability can define power.");
                }

                ValidateTimedEffectConditions(def, path, id, report);
            }
        }

        void ValidateTimedEffectConditions(
            AbilityDef abilityDef,
            string path,
            string ownerId,
            GameDataValidationReport report)
        {
            if (abilityDef.effects == null)
            {
                return;
            }

            for (int effectIndex = 0; effectIndex < abilityDef.effects.Count; effectIndex++)
            {
                TimedEffectDef timedEffect = abilityDef.effects[effectIndex];
                if (timedEffect?.condition == null)
                {
                    continue;
                }

                ConditionDef condition = timedEffect.condition;
                if (!allowedConditionTypes.Contains(condition.type))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidEnum,
                        path,
                        ownerId,
                        $"effects[{effectIndex}].condition.type '{condition.type}' is invalid.");
                    continue;
                }

                if (string.Equals(condition.type, "HasTag", StringComparison.Ordinal) &&
                    string.IsNullOrWhiteSpace(condition.tag))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        ownerId,
                        $"effects[{effectIndex}].condition.tag is required for HasTag.");
                }
            }
        }

        void ValidateEncounterDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, EncounterDef> pair in database.encountersById)
            {
                string id = pair.Key;
                EncounterDef encounterDef = pair.Value;
                string path = database.encounterSourcePathById.TryGetValue(id, out string foundPath)
                    ? foundPath
                    : string.Empty;

                if (encounterDef.enemy == null)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "enemy must not be null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(encounterDef.enemy.id))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "enemy.id must not be empty.");
                }

                if (encounterDef.enemy.health <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "enemy.health must be greater than zero.");
                }

                if (encounterDef.enemy.patterns == null || encounterDef.enemy.patterns.Count <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "enemy.patterns must contain at least one entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(encounterDef.enemy.startPatternId))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "enemy.startPatternId must not be empty.");
                }

                var patternIds = new HashSet<string>(StringComparer.Ordinal);

                for (int patternIndex = 0; patternIndex < encounterDef.enemy.patterns.Count; patternIndex++)
                {
                    EncounterEnemyPatternDef pattern = encounterDef.enemy.patterns[patternIndex];
                    if (pattern == null)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"enemy.patterns[{patternIndex}] is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(pattern.patternId))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"enemy.patterns[{patternIndex}].patternId must not be empty.");
                    }
                    else if (!patternIds.Add(pattern.patternId))
                    {
                        report.AddError(
                            GameDataErrorCode.DuplicateId,
                            path,
                            id,
                            $"enemy.patterns[{patternIndex}].patternId('{pattern.patternId}') is duplicated.");
                    }

                    ValidatePatternClashes(database, report, path, id, patternIndex, pattern);
                }

                if (!string.IsNullOrWhiteSpace(encounterDef.enemy.startPatternId) &&
                    !patternIds.Contains(encounterDef.enemy.startPatternId))
                {
                    report.AddError(
                        GameDataErrorCode.MissingReference,
                        path,
                        id,
                        $"enemy.startPatternId('{encounterDef.enemy.startPatternId}') does not exist.");
                }

                ValidatePatternTransitions(report, path, id, encounterDef.enemy.patterns, patternIds);
            }
        }

        void ValidatePatternClashes(
            GameDatabase database,
            GameDataValidationReport report,
            string path,
            string encounterId,
            int patternIndex,
            EncounterEnemyPatternDef pattern)
        {
            if (pattern.clashes == null || pattern.clashes.Count <= 0)
            {
                report.AddError(
                    GameDataErrorCode.InvalidValue,
                    path,
                    encounterId,
                    $"enemy.patterns[{patternIndex}].clashes must contain at least one entry.");
                return;
            }

            for (int clashIndex = 0; clashIndex < pattern.clashes.Count; clashIndex++)
            {
                EncounterEnemyClashDef clash = pattern.clashes[clashIndex];
                if (clash == null)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        encounterId,
                        $"enemy.patterns[{patternIndex}].clashes[{clashIndex}] is null.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(clash.clashId))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        encounterId,
                        $"enemy.patterns[{patternIndex}].clashes[{clashIndex}].clashId must not be empty.");
                }

                if (clash.maxPlayerAssignments.HasValue && clash.maxPlayerAssignments.Value < 1)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        encounterId,
                        $"enemy.patterns[{patternIndex}].clashes[{clashIndex}].maxPlayerAssignments must be greater than zero when specified.");
                }

                if (clash.abilityLoadout == null)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        encounterId,
                        $"enemy.patterns[{patternIndex}].clashes[{clashIndex}].abilityLoadout must not be null.");
                    continue;
                }

                for (int abilityIndex = 0; abilityIndex < clash.abilityLoadout.Count; abilityIndex++)
                {
                    SummonAbilityRefDef abilityRef = clash.abilityLoadout[abilityIndex];
                    if (abilityRef == null)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].clashes[{clashIndex}].abilityLoadout[{abilityIndex}] is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(abilityRef.abilityId))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].clashes[{clashIndex}].abilityLoadout[{abilityIndex}].abilityId must not be empty.");
                    }
                    else if (!database.abilitiesById.ContainsKey(abilityRef.abilityId))
                    {
                        report.AddError(
                            GameDataErrorCode.MissingReference,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].clashes[{clashIndex}].abilityLoadout[{abilityIndex}].abilityId('{abilityRef.abilityId}') does not exist.");
                    }

                    if (abilityRef.count < 0)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].clashes[{clashIndex}].abilityLoadout[{abilityIndex}].count must be greater than or equal to 0.");
                    }
                }
            }
        }

        void ValidatePatternTransitions(
            GameDataValidationReport report,
            string path,
            string encounterId,
            IReadOnlyList<EncounterEnemyPatternDef> patterns,
            HashSet<string> patternIds)
        {
            if (patterns == null)
            {
                return;
            }

            for (int patternIndex = 0; patternIndex < patterns.Count; patternIndex++)
            {
                EncounterEnemyPatternDef pattern = patterns[patternIndex];
                if (pattern == null)
                {
                    continue;
                }

                if (pattern.nextPatterns == null || pattern.nextPatterns.Count <= 0)
                {
                    continue;
                }

                double probabilitySum = 0.0d;
                for (int transitionIndex = 0; transitionIndex < pattern.nextPatterns.Count; transitionIndex++)
                {
                    EncounterEnemyPatternTransitionDef transition = pattern.nextPatterns[transitionIndex];
                    if (transition == null)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].nextPatterns[{transitionIndex}] is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(transition.patternId))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].nextPatterns[{transitionIndex}].patternId must not be empty.");
                    }
                    else if (!patternIds.Contains(transition.patternId))
                    {
                        report.AddError(
                            GameDataErrorCode.MissingReference,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].nextPatterns[{transitionIndex}].patternId('{transition.patternId}') does not exist.");
                    }

                    if (transition.probability <= 0.0d)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            encounterId,
                            $"enemy.patterns[{patternIndex}].nextPatterns[{transitionIndex}].probability must be greater than 0.");
                        continue;
                    }

                    probabilitySum += transition.probability;
                }

                if (Math.Abs(probabilitySum - 1.0d) > probabilityEpsilon)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        encounterId,
                        $"enemy.patterns[{patternIndex}].nextPatterns probability sum must be 1.0. current={probabilitySum:0.######}");
                }
            }
        }

        void ValidateEffectOps(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, AbilityDef> pair in database.abilitiesById)
            {
                string ownerId = pair.Key;
                string path = database.abilitySourcePathById.TryGetValue(ownerId, out string foundPath)
                    ? foundPath
                    : string.Empty;
                AbilityDef abilityDef = pair.Value;

                for (int effectIndex = 0; effectIndex < abilityDef.effects.Count; effectIndex++)
                {
                    TimedEffectDef timedEffect = abilityDef.effects[effectIndex];
                    for (int opIndex = 0; opIndex < timedEffect.ops.Count; opIndex++)
                    {
                        ValidateOp(
                            timedEffect.ops[opIndex],
                            path,
                            ownerId,
                            $"effects[{effectIndex}].ops[{opIndex}]",
                            report);
                    }
                }
            }
        }

        void ValidateOp(EffectOpDef opDef, string path, string ownerId, string context, GameDataValidationReport report)
        {
            if (!allowedOpCodes.Contains(opDef.op))
            {
                report.AddError(
                    GameDataErrorCode.UnsupportedOpCode,
                    path,
                    ownerId,
                    $"{context}: op '{opDef.op}' is not supported in P0.");
                return;
            }

            switch (opDef.op)
            {
                case nameof(DuelEffectOpCode.ModifyPowerResult):
                    ValidateModeAndAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.MoveAbility):
                case nameof(DuelEffectOpCode.MoveOpponentAbility):
                    if (!opDef.keeppowerResult.HasValue || !opDef.keeppowerResult.Value)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            ownerId,
                            $"{context}: keeppowerResult must be true.");
                    }

                    break;
                case nameof(DuelEffectOpCode.ModifyTotalPower):
                    ValidateSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.ModifyHealth):
                    ValidateSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.AddPowerModifier):
                    if (!string.Equals(opDef.target, "Power", StringComparison.Ordinal) &&
                        !string.Equals(opDef.target, "PowerResult", StringComparison.Ordinal))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidEnum,
                            path,
                            ownerId,
                            $"{context}: target must be Power or PowerResult.");
                    }

                    if (!string.Equals(opDef.layer, "Duel", StringComparison.Ordinal) &&
                        !string.Equals(opDef.layer, "Permanent", StringComparison.Ordinal))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidEnum,
                            path,
                            ownerId,
                            $"{context}: layer must be Duel or Permanent (case-sensitive).");
                    }

                    ValidateModeAndAmount(opDef, path, ownerId, context, report);
                    break;
            }
        }

        void ValidateModeAndAmount(
            EffectOpDef opDef,
            string path,
            string ownerId,
            string context,
            GameDataValidationReport report)
        {
            if (!string.Equals(opDef.mode, "Add", StringComparison.Ordinal) &&
                !string.Equals(opDef.mode, "PercentBonus", StringComparison.Ordinal))
            {
                report.AddError(
                    GameDataErrorCode.InvalidEnum,
                    path,
                    ownerId,
                    $"{context}: mode must be Add or PercentBonus.");
            }

            ValidateAmount(opDef, path, ownerId, context, report);
        }

        void ValidateSide(EffectOpDef opDef, string path, string ownerId, string context, GameDataValidationReport report)
        {
            if (!string.Equals(opDef.side, "Player", StringComparison.Ordinal) &&
                !string.Equals(opDef.side, "Opponent", StringComparison.Ordinal))
            {
                report.AddError(
                    GameDataErrorCode.InvalidEnum,
                    path,
                    ownerId,
                    $"{context}: side must be Player or Opponent.");
            }
        }

        void ValidateAmount(EffectOpDef opDef, string path, string ownerId, string context, GameDataValidationReport report)
        {
            if (!opDef.TryGetAmount(out _))
            {
                report.AddError(
                    GameDataErrorCode.MissingReference,
                    path,
                    ownerId,
                    $"{context}: numeric amount is required (value/amount/delta).");
            }
        }
    }
}


