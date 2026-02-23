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

            int maxAttack = NumericModifierCalculator.Apply(
                ability.attack,
                ability.attackModifiers,
                1,
                "DuelSimulator.RollAbility.Attack");

            if (maxAttack < 1)
            {
                Debug.LogWarning("[DuelSimulator] ability.attack was lower than 1. Roll range was clamped to 1.");
                maxAttack = 1;
            }

            IRollSource source = rollSource ?? defaultRollSource;
            ability.baseRoll = source.Next(1, maxAttack);
            ApplyRollFinalization(ability);
        }

        public static void ApplyRollFinalization(AbilityInstance ability)
        {
            if (ability == null)
            {
                throw new ArgumentNullException(nameof(ability));
            }

            ability.EnsureInitialized();
            ability.attackResult = ComputeAttackResult(ability.baseRoll, ability.attackResultModifiers);
        }

        public static int ComputeAttackResult(int baseRoll, IReadOnlyList<NumericModifier> modifiers)
        {
            return NumericModifierCalculator.Apply(
                baseRoll,
                modifiers,
                1,
                "DuelSimulator.ComputeAttackResult");
        }

        public static int ComputeTotalAttack(
            ClashState clashState,
            IReadOnlyDictionary<string, AbilityInstance> abilitiesById,
            bool isPlayerSide)
        {
            if (clashState == null)
            {
                throw new ArgumentNullException(nameof(clashState));
            }

            if (abilitiesById == null)
            {
                throw new ArgumentNullException(nameof(abilitiesById));
            }

            clashState.EnsureInitialized();

            int total = isPlayerSide
                ? clashState.totalAttackBonusPlayer
                : clashState.totalAttackBonusOpponent;

            List<string> abilityIds = isPlayerSide
                ? clashState.playerAbilityIds
                : clashState.opponentAbilityIds;

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

                total += ability.attackResult;
            }

            return total;
        }

        public static DuelOutcome ComputeOutcome(int playerTotalAttack, int opponentTotalAttack)
        {
            if (playerTotalAttack == opponentTotalAttack)
            {
                return DuelOutcome.Draw;
            }

            if (playerTotalAttack > opponentTotalAttack)
            {
                return DuelOutcome.Victory;
            }

            return DuelOutcome.Defeat;
        }

        public static bool ClashResolveClash(
            DuelState duelState,
            int clashIndex,
            out DuelOutcome outcome,
            out int playerTotalAttack,
            out int opponentTotalAttack)
        {
            if (duelState == null)
            {
                throw new ArgumentNullException(nameof(duelState));
            }

            duelState.EnsureInitialized();

            outcome = DuelOutcome.Draw;
            playerTotalAttack = 0;
            opponentTotalAttack = 0;

            if (duelState.isDuelEnded)
            {
                Debug.LogWarning("[DuelSimulator] ClashResolveClash rejected: duel already ended.");
                return false;
            }

            if (clashIndex < 0 || clashIndex >= duelState.clashes.Count)
            {
                Debug.LogWarning($"[DuelSimulator] clashIndex({clashIndex}) was out of range.");
                return false;
            }

            ClashState clashState = duelState.clashes[clashIndex];
            if (clashState == null)
            {
                duelState.clashes[clashIndex] = new ClashState();
                clashState = duelState.clashes[clashIndex];
                Debug.LogWarning($"[DuelSimulator] clashes[{clashIndex}] was null and has been replaced.");
            }

            playerTotalAttack = ComputeTotalAttack(
                clashState,
                duelState.abilitiesById,
                true);

            opponentTotalAttack = ComputeTotalAttack(
                clashState,
                duelState.abilitiesById,
                false);

            outcome = ComputeOutcome(playerTotalAttack, opponentTotalAttack);
            return true;
        }

        public static int ClashResolveClashesInOrder(DuelState duelState)
        {
            if (duelState == null)
            {
                throw new ArgumentNullException(nameof(duelState));
            }

            duelState.EnsureInitialized();

            if (duelState.isDuelEnded)
            {
                Debug.LogWarning("[DuelSimulator] ClashResolveClashesInOrder rejected: duel already ended.");
                return 0;
            }

            int resolvedCount = 0;

            for (int i = 0; i < duelState.clashes.Count; i++)
            {
                bool resolved = ClashResolveClash(
                    duelState,
                    i,
                    out _,
                    out _,
                    out _);

                if (!resolved)
                {
                    break;
                }

                resolvedCount += 1;
            }

            return resolvedCount;
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
                removedCount += NumericModifierCalculator.ClearByLayer(ability.attackModifiers, layer);
                removedCount += NumericModifierCalculator.ClearByLayer(ability.attackResultModifiers, layer);
            }

            return removedCount;
        }
    }
}

