using System;
using System.Collections.Generic;
using Game.Domain.Battle;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Application.Battle.Effects
{
    public sealed class BattleEffectResolver
    {
        readonly Dictionary<BattleEffectOpCode, IBattleEffectHandler> handlers = new();

        public BattleEffectResolver()
        {
            Register(new ModifyAttackResultEffectHandler());
            Register(new AddAttackModifierEffectHandler());
            Register(new MoveTroopEffectHandler());
            Register(new MoveEnemyTroopEffectHandler());
            Register(new ModifyTotalAttackEffectHandler());
            Register(new TransformOutcomeEffectHandler());
            Register(new ModifyMoraleEffectHandler());
        }

        public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context = null)
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
            context ??= new BattleEffectContext();

            if (state.isBattleEnded)
            {
                return FailWithWarning(
                    command.opCode,
                    BattleEffectFailureReason.BattleEnded,
                    "Effect rejected: battle already ended.");
            }

            if (!handlers.TryGetValue(command.opCode, out IBattleEffectHandler handler))
            {
                return FailWithWarning(
                    command.opCode,
                    BattleEffectFailureReason.UnsupportedOpCode,
                    $"Effect rejected: unsupported opCode({command.opCode}).");
            }

            BattleEffectResult result = handler.Apply(state, command, context);
            if (!result.isSuccess)
            {
                return FailWithWarning(command.opCode, result.failureReason, result.warningMessage);
            }

            return result;
        }

        public List<BattleEffectResult> ApplyAll(
            BattleState state,
            IReadOnlyList<BattleEffectCommand> commands,
            BattleEffectContext context = null)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            context ??= new BattleEffectContext();

            var results = new List<BattleEffectResult>(commands.Count);

            for (int i = 0; i < commands.Count; i++)
            {
                BattleEffectCommand command = commands[i];
                if (command == null)
                {
                    BattleEffectResult nullCommandResult = FailWithWarning(
                        BattleEffectOpCode.ModifyAttackResult,
                        BattleEffectFailureReason.MissingField,
                        $"Effect at index {i} was null.");
                    results.Add(nullCommandResult);
                    continue;
                }

                BattleEffectResult result = Apply(state, command, context);
                results.Add(result);
            }

            return results;
        }

        void Register(IBattleEffectHandler handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            handlers[handler.opCode] = handler;
        }

        static BattleEffectResult FailWithWarning(
            BattleEffectOpCode opCode,
            BattleEffectFailureReason failureReason,
            string warningMessage)
        {
            Debug.LogWarning($"[EffectResolver] [{opCode}] {warningMessage}");
            return BattleEffectResult.Fail(failureReason, warningMessage);
        }

        static BattleEffectResult MoveTroopInternal(
            BattleState state,
            BattleEffectCommand command,
            bool isPlayerSide)
        {
            if (string.IsNullOrWhiteSpace(command.troopId))
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.MissingField,
                    "troopId is required.");
            }

            if (!state.troopsById.ContainsKey(command.troopId))
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.InvalidTarget,
                    $"troopId({command.troopId}) does not exist.");
            }

            if (!TryGetBattlefieldIndex(state, command.toBattlefieldIndex, out int toIndex))
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.InvalidIndex,
                    $"toBattlefieldIndex({command.toBattlefieldIndex}) is out of range.");
            }

            if (!TryFindSourceBattlefieldIndex(
                    state,
                    command.troopId,
                    command.fromBattlefieldIndex,
                    isPlayerSide,
                    out int fromIndex))
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.InvalidTarget,
                    $"troopId({command.troopId}) is not deployed on the selected side.");
            }

            if (fromIndex == toIndex)
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.InvalidTarget,
                    $"fromBattlefieldIndex({fromIndex}) and toBattlefieldIndex({toIndex}) are the same.");
            }

            BattlefieldState toBattlefield = state.battlefields[toIndex];
            toBattlefield.EnsureInitialized();

            if (toBattlefield.slotLimit.HasValue && toBattlefield.slotLimit.Value <= 0)
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.InvalidTarget,
                    $"slotLimit({toBattlefield.slotLimit.Value}) must be greater than zero when specified.");
            }

            List<string> toList = isPlayerSide ? toBattlefield.playerTroopIds : toBattlefield.enemyTroopIds;
            if (toBattlefield.slotLimit.HasValue && toList.Count >= toBattlefield.slotLimit.Value)
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.SlotLimitExceeded,
                    $"target battlefield({toIndex}) slotLimit exceeded.");
            }

            BattlefieldState fromBattlefield = state.battlefields[fromIndex];
            fromBattlefield.EnsureInitialized();

            List<string> fromList = isPlayerSide ? fromBattlefield.playerTroopIds : fromBattlefield.enemyTroopIds;
            if (!fromList.Remove(command.troopId))
            {
                return BattleEffectResult.Fail(
                    BattleEffectFailureReason.InvalidTarget,
                    $"troopId({command.troopId}) was not found in source battlefield({fromIndex}).");
            }

            toList.Add(command.troopId);
            return BattleEffectResult.Success();
        }

        static bool TryGetBattlefieldIndex(BattleState state, int battlefieldIndex, out int resolvedIndex)
        {
            resolvedIndex = battlefieldIndex;

            if (battlefieldIndex < 0 || battlefieldIndex >= state.battlefields.Count)
            {
                return false;
            }

            return true;
        }

        static bool TryFindSourceBattlefieldIndex(
            BattleState state,
            string troopId,
            int fromBattlefieldIndex,
            bool isPlayerSide,
            out int foundIndex)
        {
            foundIndex = -1;

            if (fromBattlefieldIndex >= 0)
            {
                if (fromBattlefieldIndex >= state.battlefields.Count)
                {
                    return false;
                }

                BattlefieldState explicitField = state.battlefields[fromBattlefieldIndex];
                explicitField.EnsureInitialized();

                List<string> explicitList = isPlayerSide ? explicitField.playerTroopIds : explicitField.enemyTroopIds;
                if (!explicitList.Contains(troopId))
                {
                    return false;
                }

                foundIndex = fromBattlefieldIndex;
                return true;
            }

            for (int i = 0; i < state.battlefields.Count; i++)
            {
                BattlefieldState field = state.battlefields[i];
                field.EnsureInitialized();

                List<string> list = isPlayerSide ? field.playerTroopIds : field.enemyTroopIds;
                if (!list.Contains(troopId))
                {
                    continue;
                }

                foundIndex = i;
                return true;
            }

            return false;
        }

        sealed class ModifyAttackResultEffectHandler : IBattleEffectHandler
        {
            public BattleEffectOpCode opCode => BattleEffectOpCode.ModifyAttackResult;

            public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context)
            {
                if (string.IsNullOrWhiteSpace(command.troopId))
                {
                    return BattleEffectResult.Fail(
                        BattleEffectFailureReason.MissingField,
                        "troopId is required.");
                }

                if (!state.troopsById.TryGetValue(command.troopId, out TroopInstance troop) || troop == null)
                {
                    return BattleEffectResult.Fail(
                        BattleEffectFailureReason.InvalidTarget,
                        $"troopId({command.troopId}) does not exist.");
                }

                troop.EnsureInitialized();
                troop.attackResultModifiers.Add(new NumericModifier
                {
                    operation = command.modifierOperation,
                    value = command.amount,
                    layer = command.modifierLayer,
                    sourceId = command.sourceId
                });

                if (troop.baseRoll > 0)
                {
                    troop.attackResult = BattleSimulator.ComputeAttackResult(
                        troop.baseRoll,
                        troop.attackResultModifiers);
                }

                return BattleEffectResult.Success();
            }
        }

        sealed class AddAttackModifierEffectHandler : IBattleEffectHandler
        {
            public BattleEffectOpCode opCode => BattleEffectOpCode.AddAttackModifier;

            public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context)
            {
                if (string.IsNullOrWhiteSpace(command.troopId))
                {
                    return BattleEffectResult.Fail(
                        BattleEffectFailureReason.MissingField,
                        "troopId is required.");
                }

                if (!state.troopsById.TryGetValue(command.troopId, out TroopInstance troop) || troop == null)
                {
                    return BattleEffectResult.Fail(
                        BattleEffectFailureReason.InvalidTarget,
                        $"troopId({command.troopId}) does not exist.");
                }

                troop.EnsureInitialized();
                List<NumericModifier> targetModifiers = command.modifierTarget == BattleModifierTarget.AttackResult
                    ? troop.attackResultModifiers
                    : troop.attackModifiers;

                targetModifiers.Add(new NumericModifier
                {
                    operation = command.modifierOperation,
                    value = command.amount,
                    layer = command.modifierLayer,
                    sourceId = command.sourceId
                });

                if (command.modifierTarget == BattleModifierTarget.AttackResult && troop.baseRoll > 0)
                {
                    troop.attackResult = BattleSimulator.ComputeAttackResult(
                        troop.baseRoll,
                        troop.attackResultModifiers);
                }

                return BattleEffectResult.Success();
            }
        }

        sealed class MoveTroopEffectHandler : IBattleEffectHandler
        {
            public BattleEffectOpCode opCode => BattleEffectOpCode.MoveTroop;

            public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context)
            {
                return MoveTroopInternal(state, command, true);
            }
        }

        sealed class MoveEnemyTroopEffectHandler : IBattleEffectHandler
        {
            public BattleEffectOpCode opCode => BattleEffectOpCode.MoveEnemyTroop;

            public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context)
            {
                return MoveTroopInternal(state, command, false);
            }
        }

        sealed class ModifyTotalAttackEffectHandler : IBattleEffectHandler
        {
            public BattleEffectOpCode opCode => BattleEffectOpCode.ModifyTotalAttack;

            public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context)
            {
                if (!TryGetBattlefieldIndex(state, command.battlefieldIndex, out int battlefieldIndex))
                {
                    return BattleEffectResult.Fail(
                        BattleEffectFailureReason.InvalidIndex,
                        $"battlefieldIndex({command.battlefieldIndex}) is out of range.");
                }

                BattlefieldState battlefield = state.battlefields[battlefieldIndex];
                battlefield.EnsureInitialized();

                if (command.isPlayerSide)
                {
                    battlefield.totalAttackBonusPlayer += command.amount;
                }
                else
                {
                    battlefield.totalAttackBonusEnemy += command.amount;
                }

                return BattleEffectResult.Success();
            }
        }

        sealed class TransformOutcomeEffectHandler : IBattleEffectHandler
        {
            public BattleEffectOpCode opCode => BattleEffectOpCode.TransformOutcome;

            public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context)
            {
                if (context == null || !context.hasOutcome)
                {
                    return BattleEffectResult.Fail(
                        BattleEffectFailureReason.MissingOutcomeContext,
                        "Outcome transform requires context.hasOutcome=true.");
                }

                switch (command.transformKind)
                {
                    case BattleOutcomeTransformKind.Risky:
                        if (context.outcome == BattleOutcome.Victory)
                        {
                            context.outcome = BattleOutcome.GreatVictory;
                        }

                        return BattleEffectResult.Success();
                    case BattleOutcomeTransformKind.Safe:
                        if (context.outcome == BattleOutcome.GreatDefeat)
                        {
                            context.outcome = BattleOutcome.Defeat;
                        }

                        return BattleEffectResult.Success();
                    default:
                        return BattleEffectResult.Fail(
                            BattleEffectFailureReason.MissingField,
                            $"transformKind({command.transformKind}) is invalid.");
                }
            }
        }

        sealed class ModifyMoraleEffectHandler : IBattleEffectHandler
        {
            public BattleEffectOpCode opCode => BattleEffectOpCode.ModifyMorale;

            public BattleEffectResult Apply(BattleState state, BattleEffectCommand command, BattleEffectContext context)
            {
                if (command.isPlayerSide)
                {
                    state.playerMorale += command.amount;
                }
                else
                {
                    state.enemyMorale += command.amount;
                }

                if (state.playerMorale <= 0 || state.enemyMorale <= 0)
                {
                    state.isBattleEnded = true;
                    BattleSimulator.ClearModifierLayer(state, ModifierLayer.Battle);
                }

                return BattleEffectResult.Success();
            }
        }
    }
}
