using System;
using System.Collections.Generic;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Domain.Duel
{
    public static class DuelSimulator
    {
        static readonly IRollSource defaultRollSource = new SystemRandomRollSource();

        public static void RollAction(ActionInstance action, IRollSource rollSource = null)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action.EnsureInitialized();

            int maxAttack = NumericModifierCalculator.Apply(
                action.attack,
                action.attackModifiers,
                1,
                "DuelSimulator.RollAction.Attack");

            if (maxAttack < 1)
            {
                Debug.LogWarning("[DuelSimulator] action.attack was lower than 1. Roll range was clamped to 1.");
                maxAttack = 1;
            }

            IRollSource source = rollSource ?? defaultRollSource;
            action.baseRoll = source.Next(1, maxAttack);
            ApplyRollFinalization(action);
        }

        public static void ApplyRollFinalization(ActionInstance action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            action.EnsureInitialized();
            action.attackResult = ComputeAttackResult(action.baseRoll, action.attackResultModifiers);
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
            IReadOnlyDictionary<string, ActionInstance> actionsById,
            bool isPlayerSide)
        {
            if (clashState == null)
            {
                throw new ArgumentNullException(nameof(clashState));
            }

            if (actionsById == null)
            {
                throw new ArgumentNullException(nameof(actionsById));
            }

            clashState.EnsureInitialized();

            int total = isPlayerSide
                ? clashState.totalAttackBonusPlayer
                : clashState.totalAttackBonusOpponent;

            List<string> actionIds = isPlayerSide
                ? clashState.playerActionIds
                : clashState.opponentActionIds;

            for (int i = 0; i < actionIds.Count; i++)
            {
                string actionId = actionIds[i];
                if (string.IsNullOrWhiteSpace(actionId))
                {
                    Debug.LogWarning($"[DuelSimulator] Empty action id at index {i} was ignored.");
                    continue;
                }

                if (!actionsById.TryGetValue(actionId, out ActionInstance action) || action == null)
                {
                    Debug.LogWarning($"[DuelSimulator] actionId({actionId}) was missing and has been ignored.");
                    continue;
                }

                total += action.attackResult;
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
                long doubledLoser = (long)opponentTotalAttack * 2L;
                if (opponentTotalAttack == 0 || playerTotalAttack >= doubledLoser)
                {
                    return DuelOutcome.GreatVictory;
                }

                return DuelOutcome.Victory;
            }

            long doubledPlayer = (long)playerTotalAttack * 2L;
            if (playerTotalAttack == 0 || opponentTotalAttack >= doubledPlayer)
            {
                return DuelOutcome.GreatDefeat;
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
                duelState.actionsById,
                true);

            opponentTotalAttack = ComputeTotalAttack(
                clashState,
                duelState.actionsById,
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

            foreach (KeyValuePair<string, ActionInstance> pair in duelState.actionsById)
            {
                ActionInstance action = pair.Value;
                if (action == null)
                {
                    Debug.LogWarning($"[DuelSimulator] actionsById[{pair.Key}] was null and has been ignored.");
                    continue;
                }

                action.EnsureInitialized();
                removedCount += NumericModifierCalculator.ClearByLayer(action.attackModifiers, layer);
                removedCount += NumericModifierCalculator.ClearByLayer(action.attackResultModifiers, layer);
            }

            return removedCount;
        }
    }
}
