using System;
using System.Collections.Generic;
using Game.Application.Battle.Effects;

namespace Game.Infrastructure.Data
{
    public sealed class GameDataValidator
    {
        static readonly HashSet<string> allowedOpCodes = new(StringComparer.Ordinal)
        {
            nameof(BattleEffectOpCode.ModifyAttackResult),
            nameof(BattleEffectOpCode.MoveTroop),
            nameof(BattleEffectOpCode.MoveEnemyTroop),
            nameof(BattleEffectOpCode.ModifyTotalAttack),
            nameof(BattleEffectOpCode.TransformOutcome),
            nameof(BattleEffectOpCode.ModifyMorale),
            nameof(BattleEffectOpCode.AddAttackModifier)
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
            ValidateBattlefieldDefs(database, report);
            ValidateTroopDefs(database, report);
            ValidateCardDefs(database, report);
            ValidateEncounterDefs(database, report);
            ValidateEffectOps(database, report);
        }

        void ValidateRequiredConfigs(GameDatabase database, GameDataValidationReport report)
        {
            if (database.battleConfig == null)
            {
                report.AddError(
                    GameDataErrorCode.MissingRequiredConfig,
                    GameDataConstants.DefaultDataIndexPath,
                    "battle_config",
                    "Required config 'battle_config' is missing.");
            }

            if (database.runConfig == null)
            {
                report.AddError(
                    GameDataErrorCode.MissingRequiredConfig,
                    GameDataConstants.DefaultDataIndexPath,
                    "run_config",
                    "Required config 'run_config' is missing.");
            }

            if (database.playerStart == null)
            {
                report.AddError(
                    GameDataErrorCode.MissingRequiredConfig,
                    GameDataConstants.DefaultDataIndexPath,
                    "player_start",
                    "Required config 'player_start' is missing.");
            }
        }

        void ValidateConfigValues(GameDatabase database, GameDataValidationReport report)
        {
            if (database.battleConfig != null)
            {
                if (database.battleConfig.battlefieldCount <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.battleConfigSourcePath,
                        database.battleConfig.id,
                        "battlefieldCount must be greater than zero.");
                }

                if (database.battleConfig.attackResultMin < 1)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.battleConfigSourcePath,
                        database.battleConfig.id,
                        "attackResultMin must be greater than or equal to 1.");
                }

                if (database.battleConfig.greatVictoryMultiplier < 2)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.battleConfigSourcePath,
                        database.battleConfig.id,
                        "greatVictoryMultiplier must be greater than or equal to 2.");
                }
            }

            if (database.runConfig != null)
            {
                if (database.runConfig.startingStability < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.runConfigSourcePath,
                        database.runConfig.id,
                        "startingStability must be greater than or equal to 0.");
                }

                if (database.runConfig.supplyLimit <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.runConfigSourcePath,
                        database.runConfig.id,
                        "supplyLimit must be greater than zero.");
                }
            }

            if (database.playerStart != null)
            {
                if (database.playerStart.startingStability < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingStability must be greater than or equal to 0.");
                }

                if (database.playerStart.startingMana < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingMana must be greater than or equal to 0.");
                }

                if (database.battleConfig != null &&
                    database.playerStart.startingMana > database.battleConfig.manaMax)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        $"startingMana must be less than or equal to battle_config.manaMax({database.battleConfig.manaMax}).");
                }

                if (database.playerStart.startingPlayerMorale <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingPlayerMorale must be greater than zero.");
                }

                if (database.playerStart.startingSquadCardIds == null ||
                    database.playerStart.startingSquadCardIds.Count <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingSquadCardIds must contain at least one Squad card id.");
                }
                else
                {
                    for (int i = 0; i < database.playerStart.startingSquadCardIds.Count; i++)
                    {
                        string cardId = database.playerStart.startingSquadCardIds[i];
                        if (string.IsNullOrWhiteSpace(cardId))
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingSquadCardIds[{i}] must not be empty.");
                            continue;
                        }

                        if (!database.cardsById.TryGetValue(cardId, out CardDef cardDef) || cardDef == null)
                        {
                            report.AddError(
                                GameDataErrorCode.MissingReference,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingSquadCardIds[{i}]('{cardId}') does not exist.");
                            continue;
                        }

                        if (!string.Equals(cardDef.type, "Squad", StringComparison.Ordinal))
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidEnum,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingSquadCardIds[{i}]('{cardId}') must reference a Squad card.");
                            continue;
                        }

                        bool hasPositiveSummon = false;
                        if (cardDef.battleStart != null && cardDef.battleStart.summonTroops != null)
                        {
                            for (int summonIndex = 0; summonIndex < cardDef.battleStart.summonTroops.Count; summonIndex++)
                            {
                                SummonTroopRefDef summon = cardDef.battleStart.summonTroops[summonIndex];
                                if (summon != null && summon.count > 0 && !string.IsNullOrWhiteSpace(summon.troopId))
                                {
                                    hasPositiveSummon = true;
                                    break;
                                }
                            }
                        }

                        if (!hasPositiveSummon)
                        {
                            report.AddError(
                                GameDataErrorCode.InvalidValue,
                                database.playerStartSourcePath,
                                database.playerStart.id,
                                $"startingSquadCardIds[{i}]('{cardId}') must summon at least one troop.");
                        }
                    }
                }
            }
        }

        void ValidateBattlefieldDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, BattlefieldDef> pair in database.battlefieldsById)
            {
                string id = pair.Key;
                BattlefieldDef def = pair.Value;
                string path = database.battlefieldSourcePathById.TryGetValue(id, out string foundPath)
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
            }
        }

        void ValidateTroopDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, TroopDef> pair in database.troopsById)
            {
                string id = pair.Key;
                TroopDef def = pair.Value;
                string path = database.troopSourcePathById.TryGetValue(id, out string foundPath)
                    ? foundPath
                    : string.Empty;

                if (def.attack <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "attack must be greater than zero.");
                }
            }
        }

        void ValidateCardDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, CardDef> pair in database.cardsById)
            {
                string id = pair.Key;
                CardDef cardDef = pair.Value;
                string path = database.cardSourcePathById.TryGetValue(id, out string foundPath)
                    ? foundPath
                    : string.Empty;

                if (!string.Equals(cardDef.type, "Squad", StringComparison.Ordinal) &&
                    !string.Equals(cardDef.type, "Support", StringComparison.Ordinal))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidEnum,
                        path,
                        id,
                        $"Card type '{cardDef.type}' is invalid. Allowed: Squad, Support.");
                }

                if (cardDef.supplyCost < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        id,
                        "supplyCost must be greater than or equal to 0.");
                }

                for (int i = 0; i < cardDef.battleStart.summonTroops.Count; i++)
                {
                    SummonTroopRefDef summon = cardDef.battleStart.summonTroops[i];
                    if (!database.troopsById.ContainsKey(summon.troopId))
                    {
                        report.AddError(
                            GameDataErrorCode.MissingReference,
                            path,
                            id,
                            $"battleStart.summonTroops[{i}].troopId('{summon.troopId}') does not exist.");
                    }
                }
            }
        }

        void ValidateEncounterDefs(GameDatabase database, GameDataValidationReport report)
        {
            int battlefieldCount = database.battleConfig?.battlefieldCount ?? 0;

            foreach (KeyValuePair<string, EncounterDef> pair in database.encountersById)
            {
                string id = pair.Key;
                EncounterDef encounterDef = pair.Value;
                string path = database.encounterSourcePathById.TryGetValue(id, out string foundPath)
                    ? foundPath
                    : string.Empty;

                for (int planIndex = 0; planIndex < encounterDef.plans.Count; planIndex++)
                {
                    EncounterPlanDef plan = encounterDef.plans[planIndex];
                    if (plan.battlefieldIndex < 0 || plan.battlefieldIndex >= battlefieldCount)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidIndex,
                            path,
                            id,
                            $"plans[{planIndex}].battlefieldIndex({plan.battlefieldIndex}) is out of range.");
                    }

                    for (int troopIndex = 0; troopIndex < plan.troops.Count; troopIndex++)
                    {
                        SummonTroopRefDef troopRef = plan.troops[troopIndex];
                        if (!database.troopsById.ContainsKey(troopRef.troopId))
                        {
                            report.AddError(
                                GameDataErrorCode.MissingReference,
                                path,
                                id,
                                $"plans[{planIndex}].troops[{troopIndex}].troopId('{troopRef.troopId}') does not exist.");
                        }
                    }
                }
            }
        }

        void ValidateEffectOps(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, BattlefieldDef> pair in database.battlefieldsById)
            {
                string ownerId = pair.Key;
                string path = database.battlefieldSourcePathById.TryGetValue(ownerId, out string foundPath)
                    ? foundPath
                    : string.Empty;
                BattlefieldDef battlefieldDef = pair.Value;

                foreach (KeyValuePair<string, List<EffectBlockDef>> outcomePair in battlefieldDef.outcomeEffects)
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

            foreach (KeyValuePair<string, TroopDef> pair in database.troopsById)
            {
                string ownerId = pair.Key;
                string path = database.troopSourcePathById.TryGetValue(ownerId, out string foundPath)
                    ? foundPath
                    : string.Empty;
                TroopDef troopDef = pair.Value;

                for (int effectIndex = 0; effectIndex < troopDef.effects.Count; effectIndex++)
                {
                    TimedEffectDef timedEffect = troopDef.effects[effectIndex];
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

            foreach (KeyValuePair<string, CardDef> pair in database.cardsById)
            {
                string ownerId = pair.Key;
                string path = database.cardSourcePathById.TryGetValue(ownerId, out string foundPath)
                    ? foundPath
                    : string.Empty;
                CardDef cardDef = pair.Value;

                for (int opIndex = 0; opIndex < cardDef.battleStart.ops.Count; opIndex++)
                {
                    ValidateOp(
                        cardDef.battleStart.ops[opIndex],
                        path,
                        ownerId,
                        $"battleStart.ops[{opIndex}]",
                        report);
                }
            }

            foreach (KeyValuePair<string, SkillDef> pair in database.skillsById)
            {
                string ownerId = pair.Key;
                string path = database.skillSourcePathById.TryGetValue(ownerId, out string foundPath)
                    ? foundPath
                    : string.Empty;
                SkillDef skillDef = pair.Value;

                for (int opIndex = 0; opIndex < skillDef.ops.Count; opIndex++)
                {
                    ValidateOp(
                        skillDef.ops[opIndex],
                        path,
                        ownerId,
                        $"ops[{opIndex}]",
                        report);
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
                case nameof(BattleEffectOpCode.ModifyAttackResult):
                    ValidateModeAndAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(BattleEffectOpCode.MoveTroop):
                case nameof(BattleEffectOpCode.MoveEnemyTroop):
                    if (!opDef.keepAttackResult.HasValue || !opDef.keepAttackResult.Value)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidValue,
                            path,
                            ownerId,
                            $"{context}: keepAttackResult must be true.");
                    }

                    break;
                case nameof(BattleEffectOpCode.ModifyTotalAttack):
                    ValidateSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(BattleEffectOpCode.TransformOutcome):
                    if (!string.Equals(opDef.transformKind, nameof(BattleOutcomeTransformKind.Risky), StringComparison.Ordinal) &&
                        !string.Equals(opDef.transformKind, nameof(BattleOutcomeTransformKind.Safe), StringComparison.Ordinal))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidEnum,
                            path,
                            ownerId,
                            $"{context}: transformKind must be Risky or Safe.");
                    }

                    break;
                case nameof(BattleEffectOpCode.ModifyMorale):
                    ValidateSide(opDef, path, ownerId, context, report);
                    ValidateAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(BattleEffectOpCode.AddAttackModifier):
                    if (!string.Equals(opDef.target, "Attack", StringComparison.Ordinal) &&
                        !string.Equals(opDef.target, "AttackResult", StringComparison.Ordinal))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidEnum,
                            path,
                            ownerId,
                            $"{context}: target must be Attack or AttackResult.");
                    }

                    if (!string.Equals(opDef.layer, "Battle", StringComparison.Ordinal) &&
                        !string.Equals(opDef.layer, "Permanent", StringComparison.Ordinal))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidEnum,
                            path,
                            ownerId,
                            $"{context}: layer must be Battle or Permanent (case-sensitive).");
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
                !string.Equals(opDef.side, "Enemy", StringComparison.Ordinal))
            {
                report.AddError(
                    GameDataErrorCode.InvalidEnum,
                    path,
                    ownerId,
                    $"{context}: side must be Player or Enemy.");
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
