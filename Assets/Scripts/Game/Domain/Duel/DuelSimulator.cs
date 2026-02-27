using System;
using System.Collections.Generic;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Domain.Duel
{
    public static class DuelSimulator
    {
        static readonly IRollSource defaultRollSource = new SystemRandomRollSource();

        public static void RollAbility(AbilityInstance ability, IRollSource rollSource = null)
        {
            if (ability == null)
            {
                throw new ArgumentNullException(nameof(ability));
            }

            ability.EnsureInitialized();

            int maxPower = NumericModifierCalculator.Apply(
                ability.power,
                ability.powerModifiers,
                1,
                "DuelSimulator.RollAbility.Power");

            if (maxPower < 1)
            {
                Debug.LogWarning("[DuelSimulator] ability.power was lower than 1. Roll range was clamped to 1.");
                maxPower = 1;
            }

            int minPower = 1;
            if (ability.rollMinPercent > 0)
            {
                minPower = Mathf.Max(
                    1,
                    Mathf.FloorToInt(maxPower * (ability.rollMinPercent / 100f)));
                minPower = Mathf.Min(minPower, maxPower);
            }

            IRollSource source = rollSource ?? defaultRollSource;
            ability.baseRoll = source.Next(minPower, maxPower);
            ApplyRollFinalization(ability);
        }

        public static void ApplyRollFinalization(AbilityInstance ability)
        {
            if (ability == null)
            {
                throw new ArgumentNullException(nameof(ability));
            }

            ability.EnsureInitialized();
            ability.powerResult = ComputePowerResult(ability.baseRoll, ability.powerResultModifiers);
        }

        public static int ComputePowerResult(int baseRoll, IReadOnlyList<NumericModifier> modifiers)
        {
            return NumericModifierCalculator.Apply(
                baseRoll,
                modifiers,
                1,
                "DuelSimulator.ComputePowerResult");
        }

        public static int ComputeTotalPower(
            CombatState combatState,
            IReadOnlyDictionary<string, AbilityInstance> abilitiesById,
            bool isPlayerSide)
        {
            if (combatState == null)
            {
                throw new ArgumentNullException(nameof(combatState));
            }

            if (abilitiesById == null)
            {
                throw new ArgumentNullException(nameof(abilitiesById));
            }

            combatState.EnsureInitialized();

            int total = isPlayerSide
                ? combatState.totalPowerBonusPlayer
                : combatState.totalPowerBonusOpponent;

            List<string> abilityIds = isPlayerSide
                ? combatState.playerAbilityIds
                : combatState.opponentAbilityIds;

            for (int i = 0; i < abilityIds.Count; i++)
            {
                string abilityId = abilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    Debug.LogWarning($"[DuelSimulator] Empty abilityId at index {i} was ignored.");
                    continue;
                }

                if (!abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
                {
                    Debug.LogWarning($"[DuelSimulator] abilityId({abilityId}) was missing and has been ignored.");
                    continue;
                }

                if (ability.abilityType != AbilityType.Attack)
                {
                    continue;
                }

                total += ability.powerResult;
            }

            return total;
        }

        public static DuelOutcome ComputeOutcome(int playerTotalPower, int opponentTotalPower)
        {
            if (playerTotalPower == opponentTotalPower)
            {
                return DuelOutcome.Draw;
            }

            if (playerTotalPower > opponentTotalPower)
            {
                return DuelOutcome.Victory;
            }

            return DuelOutcome.Defeat;
        }

        public static int ClearModifierLayer(DuelState duelState, ModifierLayer layer)
        {
            if (duelState == null)
            {
                throw new ArgumentNullException(nameof(duelState));
            }

            duelState.EnsureInitialized();

            int removedCount = 0;

            foreach (KeyValuePair<string, AbilityInstance> pair in duelState.abilitiesById)
            {
                AbilityInstance ability = pair.Value;
                if (ability == null)
                {
                    Debug.LogWarning($"[DuelSimulator] abilitiesById[{pair.Key}] was null and has been ignored.");
                    continue;
                }

                ability.EnsureInitialized();
                removedCount += NumericModifierCalculator.ClearByLayer(ability.powerModifiers, layer);
                removedCount += NumericModifierCalculator.ClearByLayer(ability.powerResultModifiers, layer);
            }

            return removedCount;
        }
    }
}
