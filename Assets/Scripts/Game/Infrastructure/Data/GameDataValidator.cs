using System;
using System.Collections.Generic;
using Game.Infrastructure.Data.Effects;

namespace Game.Infrastructure.Data
{
    public sealed class GameDataValidator
    {
        const int maxLoadoutAbilityCount = 16;

        static readonly HashSet<string> allowedOpCodes = new(StringComparer.Ordinal)
        {
            nameof(DuelEffectOpCode.ModifyPowerResult),
            nameof(DuelEffectOpCode.MoveAbility),
            nameof(DuelEffectOpCode.MoveOpponentAbility),
            nameof(DuelEffectOpCode.ModifyTotalPower),
            nameof(DuelEffectOpCode.ModifyHealth),
            nameof(DuelEffectOpCode.AddPowerModifier),
            nameof(DuelEffectOpCode.PreventOutgoingDamageOnWin),
            nameof(DuelEffectOpCode.DestroyAbility),
            nameof(DuelEffectOpCode.ModifyOutgoingDamageOnWin),
            nameof(DuelEffectOpCode.PowerMinPercent)
        };

        static readonly HashSet<string> allowedConditionTypes = new(StringComparer.Ordinal)
        {
            "Always",
            "IsInLoadout",
            "OpponentCountEquals",
            "OutcomeIsVictory",
            "OutcomeIsDefeat",
            "OutcomeIsDraw"
        };

        static readonly HashSet<string> allowedTimedEffectTimings = new(StringComparer.Ordinal)
        {
            nameof(DuelEffectTiming.DuelStart),
            nameof(DuelEffectTiming.Deploy),
            nameof(DuelEffectTiming.Roll),
            nameof(DuelEffectTiming.Skill),
            nameof(DuelEffectTiming.Resolve),
            nameof(DuelEffectTiming.AfterCombat),
            nameof(DuelEffectTiming.TurnEnd),
            nameof(DuelEffectTiming.HealthLost)
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
            ValidateEnemyDefs(database, report);
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
                if (database.duelConfig.cooldownTickPerTurn != 1)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.duelConfigSourcePath,
                        database.duelConfig.id,
                        "cooldownTickPerTurn must be exactly 1.");
                }

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
                    if (startingLoadoutAbilityIds.Count > maxLoadoutAbilityCount)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            database.playerStartSourcePath,
                            database.playerStart.id,
                            $"startingLoadoutAbilityIds count({startingLoadoutAbilityIds.Count}) exceeds max({maxLoadoutAbilityCount}).");
                    }

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
                            continue;
                        }

                        AbilityDef abilityDef = database.abilitiesById[abilityId];
                        if (abilityDef != null && !abilityDef.isPlayerObtainable)
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingLoadoutAbilityIds[{i}]('{abilityId}') must be isPlayerObtainable=true.");
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

                ValidateAbilityLocalizationKeys(def, path, id, report);

                if (def.buildCost < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "buildCost must be greater than or equal to 0.");
                }

                if (string.IsNullOrWhiteSpace(def.iconId))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "iconId must not be empty.");
                }
                else if (!AbilityIconPathPolicy.TryBuildPath(def.iconId, out _))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        $"iconId('{def.iconId}') contains invalid characters.");
                }

                int resolvedCooldown = def.ResolveCooldownTurns(abilityType);
                int minCooldown = AbilityDef.GetMinimumCooldownTurns(abilityType);
                if (resolvedCooldown < minCooldown)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        $"cooldown must be greater than or equal to {minCooldown} for type({abilityType}).");
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
                        "Only Attack type ability can define power greater than 0.");
                }

                ValidateTimedEffectConditions(def, path, id, report);
            }
        }

        static void ValidateAbilityLocalizationKeys(
            AbilityDef abilityDef,
            string path,
            string ownerId,
            GameDataValidationReport report)
        {
            string expectedNameLocKey = $"{ownerId}.name";
            if (string.IsNullOrWhiteSpace(abilityDef.nameLocKey))
            {
                report.AddError(
                    GameDataErrorCode.InvalidValue,
                    path,
                    ownerId,
                    "nameLocKey must not be empty.");
            }
            else if (!string.Equals(abilityDef.nameLocKey, expectedNameLocKey, StringComparison.Ordinal))
            {
                report.AddError(
                    GameDataErrorCode.InvalidValue,
                    path,
                    ownerId,
                    $"nameLocKey must be '{expectedNameLocKey}' (actual: '{abilityDef.nameLocKey}').");
            }

            string expectedDescLocKey = $"{ownerId}.desc";
            if (string.IsNullOrWhiteSpace(abilityDef.descLocKey))
            {
                report.AddError(
                    GameDataErrorCode.InvalidValue,
                    path,
                    ownerId,
                    "descLocKey must not be empty.");
            }
            else if (!string.Equals(abilityDef.descLocKey, expectedDescLocKey, StringComparison.Ordinal))
            {
                report.AddError(
                    GameDataErrorCode.InvalidValue,
                    path,
                    ownerId,
                    $"descLocKey must be '{expectedDescLocKey}' (actual: '{abilityDef.descLocKey}').");
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
                if (timedEffect == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(timedEffect.timing) ||
                    !allowedTimedEffectTimings.Contains(timedEffect.timing))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidEnum,
                        path,
                        ownerId,
                        $"effects[{effectIndex}].timing '{timedEffect.timing}' is invalid.");
                }

                if (timedEffect.condition == null)
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

            }
        }

        void ValidateEnemyDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, EnemyDef> pair in database.enemiesById)
            {
                string id = pair.Key;
                EnemyDef enemyDef = pair.Value;
                string path = database.enemySourcePathById.TryGetValue(id, out string foundPath)
                    ? foundPath
                    : string.Empty;

                if (enemyDef.health <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "health must be greater than zero.");
                }

                if (!enemyDef.TryGetTier(out _))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidEnum,
                        path,
                        id,
                        $"tier '{enemyDef.tier}' is invalid. Allowed: {EnemyTier.Normal}, {EnemyTier.Elite}, {EnemyTier.Boss}.");
                }

                if (enemyDef.abilityLoadout == null || enemyDef.abilityLoadout.Count <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "abilityLoadout must contain at least one entry.");
                    continue;
                }

                for (int loadoutIndex = 0; loadoutIndex < enemyDef.abilityLoadout.Count; loadoutIndex++)
                {
                    AbilityLoadoutEntryDef abilityRef = enemyDef.abilityLoadout[loadoutIndex];
                    if (abilityRef == null)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"abilityLoadout[{loadoutIndex}] is null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(abilityRef.abilityId))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"abilityLoadout[{loadoutIndex}].abilityId must not be empty.");
                    }
                    else if (!database.abilitiesById.ContainsKey(abilityRef.abilityId))
                    {
                        report.AddError(
                            GameDataErrorCode.MissingReference,
                            path,
                            id,
                            $"abilityLoadout[{loadoutIndex}].abilityId('{abilityRef.abilityId}') does not exist.");
                    }

                    if (abilityRef.count < 0)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            id,
                            $"abilityLoadout[{loadoutIndex}].count must be greater than or equal to 0.");
                    }

                    if (!database.abilitiesById.TryGetValue(abilityRef.abilityId, out AbilityDef abilityDef) ||
                        abilityDef == null ||
                        !abilityDef.TryGetAbilityType(out AbilityType abilityType))
                    {
                        continue;
                    }

                    if (abilityRef.power.HasValue)
                    {
                        int overridePower = abilityRef.power.Value;
                        if (abilityType == AbilityType.Attack && overridePower <= 0)
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                path,
                                id,
                                $"abilityLoadout[{loadoutIndex}].power must be greater than 0 for Attack.");
                        }

                        if (abilityType != AbilityType.Attack && overridePower != 0)
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                path,
                                id,
                                $"abilityLoadout[{loadoutIndex}].power must be 0 for {abilityType}.");
                        }
                    }

                    if (abilityRef.cooldown.HasValue)
                    {
                        int minCooldown = AbilityDef.GetMinimumCooldownTurns(abilityType);
                        if (abilityRef.cooldown.Value < minCooldown)
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                path,
                                id,
                                $"abilityLoadout[{loadoutIndex}].cooldown must be greater than or equal to {minCooldown} for {abilityType}.");
                        }
                    }
                }

                int totalCount = 0;
                for (int loadoutIndex = 0; loadoutIndex < enemyDef.abilityLoadout.Count; loadoutIndex++)
                {
                    AbilityLoadoutEntryDef abilityRef = enemyDef.abilityLoadout[loadoutIndex];
                    if (abilityRef == null || abilityRef.count <= 0)
                    {
                        continue;
                    }

                    totalCount += abilityRef.count;
                }

                if (totalCount > maxLoadoutAbilityCount)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        $"abilityLoadout total count({totalCount}) exceeds max({maxLoadoutAbilityCount}).");
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
                    if (timedEffect == null || timedEffect.ops == null)
                    {
                        continue;
                    }

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
                    ValidateOptionalSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.ModifyHealth):
                    ValidateOptionalSide(opDef, path, ownerId, context, report);
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
                case nameof(DuelEffectOpCode.PreventOutgoingDamageOnWin):
                    break;
                case nameof(DuelEffectOpCode.DestroyAbility):
                    break;
                case nameof(DuelEffectOpCode.ModifyOutgoingDamageOnWin):
                    ValidateOptionalSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.PowerMinPercent):
                    ValidateAmount(opDef, path, ownerId, context, report);
                    ValidateAmountMin(opDef, 0, path, ownerId, context, report);
                    ValidateAmountMax(opDef, 100, path, ownerId, context, report);
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

        void ValidateOptionalSide(EffectOpDef opDef, string path, string ownerId, string context, GameDataValidationReport report)
        {
            if (string.IsNullOrWhiteSpace(opDef.side))
            {
                return;
            }

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

        void ValidateAmountMin(
            EffectOpDef opDef,
            int minInclusive,
            string path,
            string ownerId,
            string context,
            GameDataValidationReport report)
        {
            if (!opDef.TryGetAmount(out int amount))
            {
                return;
            }

            if (amount >= minInclusive)
            {
                return;
            }

            report.AddError(
                GameDataErrorCode.InvalidValue,
                path,
                ownerId,
                $"{context}: amount must be greater than or equal to {minInclusive}.");
        }

        void ValidateAmountMax(
            EffectOpDef opDef,
            int maxInclusive,
            string path,
            string ownerId,
            string context,
            GameDataValidationReport report)
        {
            if (!opDef.TryGetAmount(out int amount))
            {
                return;
            }

            if (amount <= maxInclusive)
            {
                return;
            }

            report.AddError(
                GameDataErrorCode.InvalidValue,
                path,
                ownerId,
                $"{context}: amount must be less than or equal to {maxInclusive}.");
        }
    }
}


