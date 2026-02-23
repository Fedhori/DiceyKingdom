using System;
using System.Collections.Generic;
using Game.Application.Duel.Effects;

namespace Game.Infrastructure.Data
{
    public sealed class GameDataValidator
    {
        static readonly HashSet<string> allowedOpCodes = new(StringComparer.Ordinal)
        {
            nameof(DuelEffectOpCode.ModifyAttackResult),
            nameof(DuelEffectOpCode.MoveAbility),
            nameof(DuelEffectOpCode.MoveOpponentAbility),
            nameof(DuelEffectOpCode.ModifyTotalAttack),
            nameof(DuelEffectOpCode.ModifyHealth),
            nameof(DuelEffectOpCode.AddAttackModifier)
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
            ValidateClashDefs(database, report);
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
                if (database.duelConfig.clashCount <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.duelConfigSourcePath,
                        database.duelConfig.id,
                        "clashCount must be greater than zero.");
                }

                if (database.duelConfig.attackResultMin < 1)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.duelConfigSourcePath,
                        database.duelConfig.id,
                        "attackResultMin must be greater than or equal to 1.");
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

                List<string> startingBagAbilityIds = database.playerStart.startingBagAbilityIds;
                if (startingBagAbilityIds == null || startingBagAbilityIds.Count <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingBagAbilityIds must contain at least one ability id.");
                }
                else
                {
                    for (int i = 0; i < startingBagAbilityIds.Count; i++)
                    {
                        string abilityId = startingBagAbilityIds[i];
                        if (string.IsNullOrWhiteSpace(abilityId))
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingBagAbilityIds[{i}] must not be empty.");
                            continue;
                        }

                        if (!database.abilitiesById.ContainsKey(abilityId))
                        {
                            report.AddError(
                                GameDataErrorCode.MissingReference,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingBagAbilityIds[{i}]('{abilityId}') does not exist.");
                        }
                    }
                }
            }
        }

        void ValidateClashDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, ClashDef> pair in database.clashesById)
            {
                string id = pair.Key;
                ClashDef def = pair.Value;
                string path = database.clashSourcePathById.TryGetValue(id, out string foundPath)
                    ? foundPath
                    : string.Empty;

                if (def.slotLimit.HasValue && def.slotLimit.Value < 1)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "slotLimit must be greater than zero when specified.");
                }

                if (def.damage < 1)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "damage must be greater than or equal to 1.");
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

                int resolvedDamage = def.ResolveDamage();
                if (abilityType == AbilityType.Attack && resolvedDamage <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "Attack type ability must define damage greater than 0.");
                }

                if (abilityType != AbilityType.Attack && resolvedDamage != 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "Only Attack type ability can define damage.");
                }
            }
        }

        void ValidateEncounterDefs(GameDatabase database, GameDataValidationReport report)
        {
            int clashCount = database.duelConfig?.clashCount ?? 0;

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

                if (encounterDef.enemy.clashes == null || encounterDef.enemy.clashes.Count <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "enemy.clashes must contain at least one entry.");
                    continue;
                }

                for (int clashIndex = 0; clashIndex < encounterDef.enemy.clashes.Count; clashIndex++)
                {
                    EncounterEnemyClashDef enemyClash = encounterDef.enemy.clashes[clashIndex];
                    if (enemyClash == null)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"enemy.clashes[{clashIndex}] is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(enemyClash.clashId))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"enemy.clashes[{clashIndex}].clashId must not be empty.");
                    }
                    else if (!database.clashesById.ContainsKey(enemyClash.clashId))
                    {
                        report.AddError(
                            GameDataErrorCode.MissingReference,
                            path,
                            id,
                            $"enemy.clashes[{clashIndex}].clashId('{enemyClash.clashId}') does not exist.");
                    }

                    if (clashIndex >= clashCount)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidIndex,
                            path,
                            id,
                            $"enemy.clashes[{clashIndex}] exceeds duel.config.clashCount({clashCount}).");
                    }

                    if (enemyClash.abilityLoadout == null)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"enemy.clashes[{clashIndex}].abilityLoadout must not be null.");
                        continue;
                    }

                    for (int abilityIndex = 0; abilityIndex < enemyClash.abilityLoadout.Count; abilityIndex++)
                    {
                        SummonAbilityRefDef abilityRef = enemyClash.abilityLoadout[abilityIndex];
                        if (abilityRef == null)
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                path,
                                id,
                                $"enemy.clashes[{clashIndex}].abilityLoadout[{abilityIndex}] is null.");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(abilityRef.abilityId))
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                path,
                                id,
                                $"enemy.clashes[{clashIndex}].abilityLoadout[{abilityIndex}].abilityId must not be empty.");
                        }
                        else if (!database.abilitiesById.ContainsKey(abilityRef.abilityId))
                        {
                            report.AddError(
                                GameDataErrorCode.MissingReference,
                                path,
                                id,
                                $"enemy.clashes[{clashIndex}].abilityLoadout[{abilityIndex}].abilityId('{abilityRef.abilityId}') does not exist.");
                        }

                        if (abilityRef.count < 0)
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                path,
                                id,
                                $"enemy.clashes[{clashIndex}].abilityLoadout[{abilityIndex}].count must be greater than or equal to 0.");
                        }
                    }
                }
            }
        }

        void ValidateEffectOps(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, ClashDef> pair in database.clashesById)
            {
                string ownerId = pair.Key;
                string path = database.clashSourcePathById.TryGetValue(ownerId, out string foundPath)
                    ? foundPath
                    : string.Empty;
                ClashDef clashDef = pair.Value;

                foreach (KeyValuePair<string, List<EffectBlockDef>> outcomePair in clashDef.outcomeEffects)
                {
                    string outcome = outcomePair.Key;
                    List<EffectBlockDef> blocks = outcomePair.Value;
                    for (int i = 0; i < blocks.Count; i++)
                    {
                        EffectBlockDef block = blocks[i];
                        for (int opIndex = 0; opIndex < block.ops.Count; opIndex++)
                        {
                            ValidateOp(
                                block.ops[opIndex],
                                path,
                                ownerId,
                                $"outcomeEffects.{outcome}[{i}].ops[{opIndex}]",
                                report);
                        }
                    }
                }
            }

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
                case nameof(DuelEffectOpCode.ModifyAttackResult):
                    ValidateModeAndAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.MoveAbility):
                case nameof(DuelEffectOpCode.MoveOpponentAbility):
                    if (!opDef.keepAttackResult.HasValue || !opDef.keepAttackResult.Value)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            ownerId,
                            $"{context}: keepAttackResult must be true.");
                    }

                    break;
                case nameof(DuelEffectOpCode.ModifyTotalAttack):
                    ValidateSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.ModifyHealth):
                    ValidateSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.AddAttackModifier):
                    if (!string.Equals(opDef.target, "Attack", StringComparison.Ordinal) &&
                        !string.Equals(opDef.target, "AttackResult", StringComparison.Ordinal))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidEnum,
                            path,
                            ownerId,
                            $"{context}: target must be Attack or AttackResult.");
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
