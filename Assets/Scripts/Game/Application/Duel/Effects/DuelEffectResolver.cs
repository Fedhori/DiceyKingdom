using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Application.Duel.Effects
{
    public sealed class DuelEffectClashResolver
    {
        readonly Dictionary<DuelEffectOpCode, IDuelEffectHandler> handlers = new();

        public DuelEffectClashResolver()
        {
            Register(new ModifyAttackResultEffectHandler());
            Register(new AddAttackModifierEffectHandler());
            Register(new MoveActionEffectHandler());
            Register(new MoveOpponentActionEffectHandler());
            Register(new ModifyTotalAttackEffectHandler());
            Register(new TransformOutcomeEffectHandler());
            Register(new ModifyHealthEffectHandler());
        }

        public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            state.EnsureInitialized();
            context ??= new DuelEffectContext();

            if (state.isDuelEnded)
            {
                return FailWithWarning(
                    command.opCode,
                    DuelEffectFailureReason.DuelEnded,
                    "Effect rejected: duel already ended.");
            }

            if (!handlers.TryGetValue(command.opCode, out IDuelEffectHandler handler))
            {
                return FailWithWarning(
                    command.opCode,
                    DuelEffectFailureReason.UnsupportedOpCode,
                    $"Effect rejected: unsupported opCode({command.opCode}).");
            }

            DuelEffectResult result = handler.Apply(state, command, context);
            if (!result.isSuccess)
            {
                return FailWithWarning(command.opCode, result.failureReason, result.warningMessage);
            }

            return result;
        }

        public List<DuelEffectResult> ApplyAll(
            DuelState state,
            IReadOnlyList<DuelEffectCommand> commands,
            DuelEffectContext context = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            context ??= new DuelEffectContext();

            var results = new List<DuelEffectResult>(commands.Count);

            for (int i = 0; i < commands.Count; i++)
            {
                DuelEffectCommand command = commands[i];
                if (command == null)
                {
                    DuelEffectResult nullCommandResult = FailWithWarning(
                        DuelEffectOpCode.ModifyAttackResult,
                        DuelEffectFailureReason.MissingField,
                        $"Effect at index {i} was null.");
                    results.Add(nullCommandResult);
                    continue;
                }

                DuelEffectResult result = Apply(state, command, context);
                results.Add(result);
            }

            return results;
        }

        void Register(IDuelEffectHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            handlers[handler.opCode] = handler;
        }

        static DuelEffectResult FailWithWarning(
            DuelEffectOpCode opCode,
            DuelEffectFailureReason failureReason,
            string warningMessage)
        {
            Debug.LogWarning($"[EffectClashResolver] [{opCode}] {warningMessage}");
            return DuelEffectResult.Fail(failureReason, warningMessage);
        }

        static DuelEffectResult MoveActionInternal(
            DuelState state,
            DuelEffectCommand command,
            bool isPlayerSide)
        {
            if (string.IsNullOrWhiteSpace(command.actionId))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.MissingField,
                    "actionId is required.");
            }

            if (!state.actionsById.ContainsKey(command.actionId))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"actionId({command.actionId}) does not exist.");
            }

            if (!TryGetClashIndex(state, command.toClashIndex, out int toIndex))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidIndex,
                    $"toClashIndex({command.toClashIndex}) is out of range.");
            }

            if (!TryFindSourceClashIndex(
                    state,
                    command.actionId,
                    command.fromClashIndex,
                    isPlayerSide,
                    out int fromIndex))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"actionId({command.actionId}) is not deployed on the selected side.");
            }

            if (fromIndex == toIndex)
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"fromClashIndex({fromIndex}) and toClashIndex({toIndex}) are the same.");
            }

            ClashState toClash = state.clashes[toIndex];
            toClash.EnsureInitialized();

            if (toClash.slotLimit.HasValue && toClash.slotLimit.Value <= 0)
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"slotLimit({toClash.slotLimit.Value}) must be greater than zero when specified.");
            }

            List<string> toList = isPlayerSide ? toClash.playerActionIds : toClash.opponentActionIds;
            if (toClash.slotLimit.HasValue && toList.Count >= toClash.slotLimit.Value)
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.SlotLimitExceeded,
                    $"target clash({toIndex}) slotLimit exceeded.");
            }

            ClashState fromClash = state.clashes[fromIndex];
            fromClash.EnsureInitialized();

            List<string> fromList = isPlayerSide ? fromClash.playerActionIds : fromClash.opponentActionIds;
            if (!fromList.Remove(command.actionId))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"actionId({command.actionId}) was not found in source clash({fromIndex}).");
            }

            toList.Add(command.actionId);
            return DuelEffectResult.Success();
        }

        static bool TryGetClashIndex(DuelState state, int clashIndex, out int resolvedIndex)
        {
            resolvedIndex = clashIndex;

            if (clashIndex < 0 || clashIndex >= state.clashes.Count)
            {
                return false;
            }

            return true;
        }

        static bool TryFindSourceClashIndex(
            DuelState state,
            string actionId,
            int fromClashIndex,
            bool isPlayerSide,
            out int foundIndex)
        {
            foundIndex = -1;

            if (fromClashIndex >= 0)
            {
                if (fromClashIndex >= state.clashes.Count)
                {
                    return false;
                }

                ClashState explicitField = state.clashes[fromClashIndex];
                explicitField.EnsureInitialized();

                List<string> explicitList = isPlayerSide ? explicitField.playerActionIds : explicitField.opponentActionIds;
                if (!explicitList.Contains(actionId))
                {
                    return false;
                }

                foundIndex = fromClashIndex;
                return true;
            }

            for (int i = 0; i < state.clashes.Count; i++)
            {
                ClashState field = state.clashes[i];
                field.EnsureInitialized();

                List<string> list = isPlayerSide ? field.playerActionIds : field.opponentActionIds;
                if (!list.Contains(actionId))
                {
                    continue;
                }

                foundIndex = i;
                return true;
            }

            return false;
        }

        sealed class ModifyAttackResultEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.ModifyAttackResult;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                if (string.IsNullOrWhiteSpace(command.actionId))
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.MissingField,
                        "actionId is required.");
                }

                if (!state.actionsById.TryGetValue(command.actionId, out ActionInstance action) || action == null)
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.InvalidTarget,
                        $"actionId({command.actionId}) does not exist.");
                }

                action.EnsureInitialized();
                action.attackResultModifiers.Add(new NumericModifier
                {
                    operation = command.modifierOperation,
                    value = command.amount,
                    layer = command.modifierLayer,
                    sourceId = command.sourceId
                });

                if (action.baseRoll > 0)
                {
                    action.attackResult = DuelSimulator.ComputeAttackResult(
                        action.baseRoll,
                        action.attackResultModifiers);
                }

                return DuelEffectResult.Success();
            }
        }

        sealed class AddAttackModifierEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.AddAttackModifier;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                if (string.IsNullOrWhiteSpace(command.actionId))
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.MissingField,
                        "actionId is required.");
                }

                if (!state.actionsById.TryGetValue(command.actionId, out ActionInstance action) || action == null)
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.InvalidTarget,
                        $"actionId({command.actionId}) does not exist.");
                }

                action.EnsureInitialized();
                List<NumericModifier> targetModifiers = command.modifierTarget == DuelModifierTarget.AttackResult
                    ? action.attackResultModifiers
                    : action.attackModifiers;

                targetModifiers.Add(new NumericModifier
                {
                    operation = command.modifierOperation,
                    value = command.amount,
                    layer = command.modifierLayer,
                    sourceId = command.sourceId
                });

                if (command.modifierTarget == DuelModifierTarget.AttackResult && action.baseRoll > 0)
                {
                    action.attackResult = DuelSimulator.ComputeAttackResult(
                        action.baseRoll,
                        action.attackResultModifiers);
                }

                return DuelEffectResult.Success();
            }
        }

        sealed class MoveActionEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.MoveAction;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                return MoveActionInternal(state, command, true);
            }
        }

        sealed class MoveOpponentActionEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.MoveOpponentAction;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                return MoveActionInternal(state, command, false);
            }
        }

        sealed class ModifyTotalAttackEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.ModifyTotalAttack;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                if (!TryGetClashIndex(state, command.clashIndex, out int clashIndex))
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.InvalidIndex,
                        $"clashIndex({command.clashIndex}) is out of range.");
                }

                ClashState clash = state.clashes[clashIndex];
                clash.EnsureInitialized();

                if (command.isPlayerSide)
                {
                    clash.totalAttackBonusPlayer += command.amount;
                }
                else
                {
                    clash.totalAttackBonusOpponent += command.amount;
                }

                return DuelEffectResult.Success();
            }
        }

        sealed class TransformOutcomeEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.TransformOutcome;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                if (context == null || !context.hasOutcome)
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.MissingOutcomeContext,
                        "Outcome transform requires context.hasOutcome=true.");
                }

                switch (command.transformKind)
                {
                    case DuelOutcomeTransformKind.Risky:
                        if (context.outcome == DuelOutcome.Victory)
                        {
                            context.outcome = DuelOutcome.GreatVictory;
                        }

                        return DuelEffectResult.Success();
                    case DuelOutcomeTransformKind.Safe:
                        if (context.outcome == DuelOutcome.GreatDefeat)
                        {
                            context.outcome = DuelOutcome.Defeat;
                        }

                        return DuelEffectResult.Success();
                    default:
                        return DuelEffectResult.Fail(
                            DuelEffectFailureReason.MissingField,
                            $"transformKind({command.transformKind}) is invalid.");
                }
            }
        }

        sealed class ModifyHealthEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.ModifyHealth;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                if (command.isPlayerSide)
                {
                    state.playerHealth += command.amount;
                }
                else
                {
                    state.opponentHealth += command.amount;
                }

                if (state.playerHealth <= 0 || state.opponentHealth <= 0)
                {
                    state.isDuelEnded = true;
                    DuelSimulator.ClearModifierLayer(state, ModifierLayer.Duel);
                }

                return DuelEffectResult.Success();
            }
        }
    }
}
