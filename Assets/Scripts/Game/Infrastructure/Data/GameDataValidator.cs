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
            nameof(DuelEffectOpCode.MoveAction),
            nameof(DuelEffectOpCode.MoveOpponentAction),
            nameof(DuelEffectOpCode.ModifyTotalAttack),
            nameof(DuelEffectOpCode.TransformOutcome),
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
            ValidateActionDefs(database, report);
            ValidateCardDefs(database, report);
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

                if (database.duelConfig.greatVictoryMultiplier < 2)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.duelConfigSourcePath,
                        database.duelConfig.id,
                        "greatVictoryMultiplier must be greater than or equal to 2.");
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
                if (database.playerStart.startingHonor < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingHonor must be greater than or equal to 0.");
                }

                if (database.playerStart.startingFocus < 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingFocus must be greater than or equal to 0.");
                }

                if (database.duelConfig != null &&
                    database.playerStart.startingFocus > database.duelConfig.focusMax)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        $"startingFocus must be less than or equal to duel.config.focusMax({database.duelConfig.focusMax}).");
                }

                if (database.playerStart.startingPlayerHealth <= 0)
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        database.playerStartSourcePath,
                        database.playerStart.id,
                        "startingPlayerHealth must be greater than zero.");
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
                        if (cardDef.duelStart != null && cardDef.duelStart.summonActions != null)
                        {
                            for (int summonIndex = 0; summonIndex < cardDef.duelStart.summonActions.Count; summonIndex++)
                            {
                                SummonActionRefDef summon = cardDef.duelStart.summonActions[summonIndex];
                                if (summon != null && summon.count > 0 && !string.IsNullOrWhiteSpace(summon.actionId))
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
                                $"startingSquadCardIds[{i}]('{cardId}') must summon at least one action.");
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
            }
        }

        void ValidateActionDefs(GameDatabase database, GameDataValidationReport report)
        {
            foreach (KeyValuePair<string, ActionDef> pair in database.actionsById)
            {
                string id = pair.Key;
                ActionDef def = pair.Value;
                string path = database.actionSourcePathById.TryGetValue(id, out string foundPath)
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

                for (int i = 0; i < cardDef.duelStart.summonActions.Count; i++)
                {
                    SummonActionRefDef summon = cardDef.duelStart.summonActions[i];
                    if (!database.actionsById.ContainsKey(summon.actionId))
                    {
                        report.AddError(
                            GameDataErrorCode.MissingReference,
                            path,
                            id,
                            $"duelStart.summonActions[{i}].actionId('{summon.actionId}') does not exist.");
                    }
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

                for (int planIndex = 0; planIndex < encounterDef.plans.Count; planIndex++)
                {
                    EncounterPlanDef plan = encounterDef.plans[planIndex];
                    if (plan.clashIndex < 0 || plan.clashIndex >= clashCount)
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidIndex,
                            path,
                            id,
                            $"plans[{planIndex}].clashIndex({plan.clashIndex}) is out of range.");
                    }

                    for (int actionIndex = 0; actionIndex < plan.actions.Count; actionIndex++)
                    {
                        SummonActionRefDef actionRef = plan.actions[actionIndex];
                        if (!database.actionsById.ContainsKey(actionRef.actionId))
                        {
                            report.AddError(
                                GameDataErrorCode.MissingReference,
                                path,
                                id,
                                $"plans[{planIndex}].actions[{actionIndex}].actionId('{actionRef.actionId}') does not exist.");
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

            foreach (KeyValuePair<string, ActionDef> pair in database.actionsById)
            {
                string ownerId = pair.Key;
                string path = database.actionSourcePathById.TryGetValue(ownerId, out string foundPath)
                    ? foundPath
                    : string.Empty;
                ActionDef actionDef = pair.Value;

                for (int effectIndex = 0; effectIndex < actionDef.effects.Count; effectIndex++)
                {
                    TimedEffectDef timedEffect = actionDef.effects[effectIndex];
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

                for (int opIndex = 0; opIndex < cardDef.duelStart.ops.Count; opIndex++)
                {
                    ValidateOp(
                        cardDef.duelStart.ops[opIndex],
                        path,
                        ownerId,
                        $"duelStart.ops[{opIndex}]",
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
                case nameof(DuelEffectOpCode.ModifyAttackResult):
                    ValidateModeAndAmount(opDef, path, ownerId, context, report);
                    break;
                case nameof(DuelEffectOpCode.MoveAction):
                case nameof(DuelEffectOpCode.MoveOpponentAction):
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
                case nameof(DuelEffectOpCode.TransformOutcome):
                    if (!string.Equals(opDef.transformKind, nameof(DuelOutcomeTransformKind.Risky), StringComparison.Ordinal) &&
                        !string.Equals(opDef.transformKind, nameof(DuelOutcomeTransformKind.Safe), StringComparison.Ordinal))
                    {
                        report.AddError(
                            GameDataErrorCode.InvalidEnum,
                            path,
                            ownerId,
                            $"{context}: transformKind must be Risky or Safe.");
                    }

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
