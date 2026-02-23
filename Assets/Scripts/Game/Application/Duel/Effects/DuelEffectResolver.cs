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
            Register(new ModifyPowerResultEffectHandler());
            Register(new AddPowerModifierEffectHandler());
            Register(new MoveAbilityEffectHandler());
            Register(new MoveOpponentAbilityEffectHandler());
            Register(new ModifyTotalPowerEffectHandler());
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
                        DuelEffectOpCode.ModifyPowerResult,
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

        static DuelEffectResult MoveAbilityInternal(
            DuelState state,
            DuelEffectCommand command,
            bool isPlayerSide)
        {
            if (string.IsNullOrWhiteSpace(command.abilityId))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.MissingField,
                    "abilityId is required.");
            }

            if (!state.abilitiesById.ContainsKey(command.abilityId))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"abilityId({command.abilityId}) does not exist.");
            }

            if (!TryGetClashIndex(state, command.toClashIndex, out int toIndex))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidIndex,
                    $"toClashIndex({command.toClashIndex}) is out of range.");
            }

            if (!TryFindSourceClashIndex(
                    state,
                    command.abilityId,
                    command.fromClashIndex,
                    isPlayerSide,
                    out int fromIndex))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"abilityId({command.abilityId}) is not deployed on the selected side.");
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

            List<string> toList = isPlayerSide ? toClash.playerAbilityIds : toClash.opponentAbilityIds;
            if (toClash.slotLimit.HasValue && toList.Count >= toClash.slotLimit.Value)
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.SlotLimitExceeded,
                    $"target clash({toIndex}) slotLimit exceeded.");
            }

            ClashState fromClash = state.clashes[fromIndex];
            fromClash.EnsureInitialized();

            List<string> fromList = isPlayerSide ? fromClash.playerAbilityIds : fromClash.opponentAbilityIds;
            if (!fromList.Remove(command.abilityId))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"abilityId({command.abilityId}) was not found in source clash({fromIndex}).");
            }

            toList.Add(command.abilityId);
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
            string abilityId,
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

                List<string> explicitList = isPlayerSide ? explicitField.playerAbilityIds : explicitField.opponentAbilityIds;
                if (!explicitList.Contains(abilityId))
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

                List<string> list = isPlayerSide ? field.playerAbilityIds : field.opponentAbilityIds;
                if (!list.Contains(abilityId))
                {
                    continue;
                }

                foundIndex = i;
                return true;
            }

            return false;
        }

        sealed class ModifyPowerResultEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.ModifyPowerResult;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                if (string.IsNullOrWhiteSpace(command.abilityId))
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.MissingField,
                        "abilityId is required.");
                }

                if (!state.abilitiesById.TryGetValue(command.abilityId, out AbilityInstance ability) || ability == null)
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.InvalidTarget,
                        $"abilityId({command.abilityId}) does not exist.");
                }

                ability.EnsureInitialized();
                ability.powerResultModifiers.Add(new NumericModifier
                {
                    operation = command.modifierOperation,
                    value = command.amount,
                    layer = command.modifierLayer,
                    sourceId = command.sourceId
                });

                if (ability.baseRoll > 0)
                {
                    ability.powerResult = DuelSimulator.ComputePowerResult(
                        ability.baseRoll,
                        ability.powerResultModifiers);
                }

                return DuelEffectResult.Success();
            }
        }

        sealed class AddPowerModifierEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.AddPowerModifier;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                if (string.IsNullOrWhiteSpace(command.abilityId))
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.MissingField,
                        "abilityId is required.");
                }

                if (!state.abilitiesById.TryGetValue(command.abilityId, out AbilityInstance ability) || ability == null)
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.InvalidTarget,
                        $"abilityId({command.abilityId}) does not exist.");
                }

                ability.EnsureInitialized();
                List<NumericModifier> targetModifiers = command.modifierTarget == DuelModifierTarget.PowerResult
                    ? ability.powerResultModifiers
                    : ability.powerModifiers;

                targetModifiers.Add(new NumericModifier
                {
                    operation = command.modifierOperation,
                    value = command.amount,
                    layer = command.modifierLayer,
                    sourceId = command.sourceId
                });

                if (command.modifierTarget == DuelModifierTarget.PowerResult && ability.baseRoll > 0)
                {
                    ability.powerResult = DuelSimulator.ComputePowerResult(
                        ability.baseRoll,
                        ability.powerResultModifiers);
                }

                return DuelEffectResult.Success();
            }
        }

        sealed class MoveAbilityEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.MoveAbility;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                return MoveAbilityInternal(state, command, true);
            }
        }

        sealed class MoveOpponentAbilityEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.MoveOpponentAbility;

            public DuelEffectResult Apply(DuelState state, DuelEffectCommand command, DuelEffectContext context)
            {
                return MoveAbilityInternal(state, command, false);
            }
        }

        sealed class ModifyTotalPowerEffectHandler : IDuelEffectHandler
        {
            public DuelEffectOpCode opCode => DuelEffectOpCode.ModifyTotalPower;

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
                    clash.totalPowerBonusPlayer += command.amount;
                }
                else
                {
                    clash.totalPowerBonusOpponent += command.amount;
                }

                return DuelEffectResult.Success();
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

