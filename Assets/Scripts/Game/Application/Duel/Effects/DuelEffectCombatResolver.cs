using System;
using System.Collections.Generic;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Application.Duel.Effects
{
    public sealed class DuelEffectCombatResolver
    {
        readonly Dictionary<DuelEffectOpCode, IDuelEffectHandler> handlers = new();

        public DuelEffectCombatResolver()
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
            Debug.LogWarning($"[EffectCombatResolver] [{opCode}] {warningMessage}");
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

            if (!TryGetCombatIndex(state, command.toCombatIndex, out int toIndex))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidIndex,
                    $"toCombatIndex({command.toCombatIndex}) is out of range.");
            }

            if (!TryFindSourceCombatIndex(
                    state,
                    command.abilityId,
                    command.fromCombatIndex,
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
                    $"fromCombatIndex({fromIndex}) and toCombatIndex({toIndex}) are the same.");
            }

            CombatState toCombat = state.combats[toIndex];
            toCombat.EnsureInitialized();

            if (isPlayerSide &&
                toCombat.maxPlayerAssignments.HasValue &&
                toCombat.maxPlayerAssignments.Value > 0 &&
                toCombat.playerAbilityIds.Count >= toCombat.maxPlayerAssignments.Value)
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.SlotLimitExceeded,
                    $"target combat({toIndex}) maxPlayerAssignments exceeded.");
            }

            List<string> toList = isPlayerSide ? toCombat.playerAbilityIds : toCombat.opponentAbilityIds;

            CombatState fromCombat = state.combats[fromIndex];
            fromCombat.EnsureInitialized();

            List<string> fromList = isPlayerSide ? fromCombat.playerAbilityIds : fromCombat.opponentAbilityIds;
            if (!fromList.Remove(command.abilityId))
            {
                return DuelEffectResult.Fail(
                    DuelEffectFailureReason.InvalidTarget,
                    $"abilityId({command.abilityId}) was not found in source combat({fromIndex}).");
            }

            toList.Add(command.abilityId);
            return DuelEffectResult.Success();
        }

        static bool TryGetCombatIndex(DuelState state, int combatIndex, out int resolvedIndex)
        {
            resolvedIndex = combatIndex;

            if (combatIndex < 0 || combatIndex >= state.combats.Count)
            {
                return false;
            }

            return true;
        }

        static bool TryFindSourceCombatIndex(
            DuelState state,
            string abilityId,
            int fromCombatIndex,
            bool isPlayerSide,
            out int foundIndex)
        {
            foundIndex = -1;

            if (fromCombatIndex >= 0)
            {
                if (fromCombatIndex >= state.combats.Count)
                {
                    return false;
                }

                CombatState explicitCombat = state.combats[fromCombatIndex];
                explicitCombat.EnsureInitialized();

                List<string> explicitList = isPlayerSide ? explicitCombat.playerAbilityIds : explicitCombat.opponentAbilityIds;
                if (!explicitList.Contains(abilityId))
                {
                    return false;
                }

                foundIndex = fromCombatIndex;
                return true;
            }

            for (int i = 0; i < state.combats.Count; i++)
            {
                CombatState combat = state.combats[i];
                combat.EnsureInitialized();

                List<string> list = isPlayerSide ? combat.playerAbilityIds : combat.opponentAbilityIds;
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
                if (!TryGetCombatIndex(state, command.combatIndex, out int combatIndex))
                {
                    return DuelEffectResult.Fail(
                        DuelEffectFailureReason.InvalidIndex,
                        $"combatIndex({command.combatIndex}) is out of range.");
                }

                CombatState combat = state.combats[combatIndex];
                combat.EnsureInitialized();

                if (command.isPlayerSide)
                {
                    combat.totalPowerBonusPlayer += command.amount;
                }
                else
                {
                    combat.totalPowerBonusOpponent += command.amount;
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
