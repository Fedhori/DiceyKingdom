using System;
using System.Collections.Generic;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Domain.Battle
{
    public static class BattleSimulator
    {
        static readonly IRollSource defaultRollSource = new SystemRandomRollSource();

        public static void RollTroop(TroopInstance troop, IRollSource rollSource = null)
        {
            if (troop == null)
            {
                throw new ArgumentNullException(nameof(troop));
            }

            troop.EnsureInitialized();

            int maxAttack = NumericModifierCalculator.Apply(
                troop.attack,
                troop.attackModifiers,
                1,
                "BattleSimulator.RollTroop.Attack");

            if (maxAttack < 1)
            {
                Debug.LogWarning("[BattleSimulator] troop.attack was lower than 1. Roll range was clamped to 1.");
                maxAttack = 1;
            }

            IRollSource source = rollSource ?? defaultRollSource;
            troop.baseRoll = source.Next(1, maxAttack);
            ApplyRollFinalization(troop);
        }

        public static void ApplyRollFinalization(TroopInstance troop)
        {
            if (troop == null)
            {
                throw new ArgumentNullException(nameof(troop));
            }

            troop.EnsureInitialized();
            troop.attackResult = ComputeAttackResult(troop.baseRoll, troop.attackResultModifiers);
        }

        public static int ComputeAttackResult(int baseRoll, IReadOnlyList<NumericModifier> modifiers)
        {
            return NumericModifierCalculator.Apply(
                baseRoll,
                modifiers,
                1,
                "BattleSimulator.ComputeAttackResult");
        }

        public static int ComputeTotalAttack(
            BattlefieldState battlefieldState,
            IReadOnlyDictionary<string, TroopInstance> troopsById,
            bool isPlayerSide)
        {
            if (battlefieldState == null)
            {
                throw new ArgumentNullException(nameof(battlefieldState));
            }

            if (troopsById == null)
            {
                throw new ArgumentNullException(nameof(troopsById));
            }

            battlefieldState.EnsureInitialized();

            int total = isPlayerSide
                ? battlefieldState.totalAttackBonusPlayer
                : battlefieldState.totalAttackBonusEnemy;

            List<string> troopIds = isPlayerSide
                ? battlefieldState.playerTroopIds
                : battlefieldState.enemyTroopIds;

            for (int i = 0; i < troopIds.Count; i++)
            {
                string troopId = troopIds[i];
                if (string.IsNullOrWhiteSpace(troopId))
                {
                    Debug.LogWarning($"[BattleSimulator] Empty troop id at index {i} was ignored.");
                    continue;
                }

                if (!troopsById.TryGetValue(troopId, out TroopInstance troop) || troop == null)
                {
                    Debug.LogWarning($"[BattleSimulator] troopId({troopId}) was missing and has been ignored.");
                    continue;
                }

                total += troop.attackResult;
            }

            return total;
        }

        public static BattleOutcome ComputeOutcome(int playerTotalAttack, int enemyTotalAttack)
        {
            if (playerTotalAttack == enemyTotalAttack)
            {
                return BattleOutcome.Draw;
            }

            if (playerTotalAttack > enemyTotalAttack)
            {
                long doubledLoser = (long)enemyTotalAttack * 2L;
                if (enemyTotalAttack == 0 || playerTotalAttack >= doubledLoser)
                {
                    return BattleOutcome.GreatVictory;
                }

                return BattleOutcome.Victory;
            }

            long doubledPlayer = (long)playerTotalAttack * 2L;
            if (playerTotalAttack == 0 || enemyTotalAttack >= doubledPlayer)
            {
                return BattleOutcome.GreatDefeat;
            }

            return BattleOutcome.Defeat;
        }

        public static bool ResolveBattlefield(
            BattleState battleState,
            int battlefieldIndex,
            out BattleOutcome outcome,
            out int playerTotalAttack,
            out int enemyTotalAttack)
        {
            if (battleState == null)
            {
                throw new ArgumentNullException(nameof(battleState));
            }

            battleState.EnsureInitialized();

            outcome = BattleOutcome.Draw;
            playerTotalAttack = 0;
            enemyTotalAttack = 0;

            if (battleState.isBattleEnded)
            {
                Debug.LogWarning("[BattleSimulator] ResolveBattlefield rejected: battle already ended.");
                return false;
            }

            if (battlefieldIndex < 0 || battlefieldIndex >= battleState.battlefields.Count)
            {
                Debug.LogWarning($"[BattleSimulator] battlefieldIndex({battlefieldIndex}) was out of range.");
                return false;
            }

            BattlefieldState battlefieldState = battleState.battlefields[battlefieldIndex];
            if (battlefieldState == null)
            {
                battleState.battlefields[battlefieldIndex] = new BattlefieldState();
                battlefieldState = battleState.battlefields[battlefieldIndex];
                Debug.LogWarning($"[BattleSimulator] battlefields[{battlefieldIndex}] was null and has been replaced.");
            }

            playerTotalAttack = ComputeTotalAttack(
                battlefieldState,
                battleState.troopsById,
                true);

            enemyTotalAttack = ComputeTotalAttack(
                battlefieldState,
                battleState.troopsById,
                false);

            outcome = ComputeOutcome(playerTotalAttack, enemyTotalAttack);
            return true;
        }

        public static int ResolveBattlefieldsInOrder(BattleState battleState)
        {
            if (battleState == null)
            {
                throw new ArgumentNullException(nameof(battleState));
            }

            battleState.EnsureInitialized();

            if (battleState.isBattleEnded)
            {
                Debug.LogWarning("[BattleSimulator] ResolveBattlefieldsInOrder rejected: battle already ended.");
                return 0;
            }

            int resolvedCount = 0;

            for (int i = 0; i < battleState.battlefields.Count; i++)
            {
                bool resolved = ResolveBattlefield(
                    battleState,
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

        public static int ClearModifierLayer(BattleState battleState, ModifierLayer layer)
        {
            if (battleState == null)
            {
                throw new ArgumentNullException(nameof(battleState));
            }

            battleState.EnsureInitialized();

            int removedCount = 0;

            foreach (KeyValuePair<string, TroopInstance> pair in battleState.troopsById)
            {
                TroopInstance troop = pair.Value;
                if (troop == null)
                {
                    Debug.LogWarning($"[BattleSimulator] troopsById[{pair.Key}] was null and has been ignored.");
                    continue;
                }

                troop.EnsureInitialized();
                removedCount += NumericModifierCalculator.ClearByLayer(troop.attackModifiers, layer);
                removedCount += NumericModifierCalculator.ClearByLayer(troop.attackResultModifiers, layer);
            }

            return removedCount;
        }
    }
}
