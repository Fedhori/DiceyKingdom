using System;
using System.Collections.Generic;
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

            int maxFaceValue = troop.power;
            if (maxFaceValue < 1)
            {
                Debug.LogWarning("[BattleSimulator] troop.power was lower than 1. Roll range was clamped to 1.");
                maxFaceValue = 1;
            }

            IRollSource source = rollSource ?? defaultRollSource;
            troop.baseRoll = source.Next(1, maxFaceValue);
            ApplyRollFinalization(troop);
        }

        public static void ApplyRollFinalization(TroopInstance troop)
        {
            if (troop == null)
            {
                throw new ArgumentNullException(nameof(troop));
            }

            troop.EnsureInitialized();
            troop.faceValueFinal = ComputeFinalFaceValue(troop.baseRoll, troop.modifiers);
        }

        public static int ComputeFinalFaceValue(int baseRoll, IReadOnlyList<TroopModifierEntry> modifiers)
        {
            int addTotal = 0;
            int percentBonusTotal = 0;

            if (modifiers != null)
            {
                for (int i = 0; i < modifiers.Count; i++)
                {
                    TroopModifierEntry modifier = modifiers[i];
                    if (modifier == null)
                    {
                        Debug.LogWarning($"[BattleSimulator] modifiers[{i}] was null and has been ignored.");
                        continue;
                    }

                    switch (modifier.modifierType)
                    {
                        case TroopModifierType.Add:
                            addTotal += modifier.delta;
                            break;
                        case TroopModifierType.PercentBonus:
                            percentBonusTotal += modifier.delta;
                            break;
                        default:
                            Debug.LogWarning(
                                $"[BattleSimulator] Unknown TroopModifierType({modifier.modifierType}) was ignored. sourceId={modifier.sourceId}");
                            break;
                    }
                }
            }

            float rawFinalValue = (baseRoll + addTotal) * (1f + (percentBonusTotal / 100f));
            int finalValue = Mathf.FloorToInt(rawFinalValue);

            if (finalValue < 1)
            {
                finalValue = 1;
            }

            return finalValue;
        }

        public static int ComputeCombatStrength(
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
                ? battlefieldState.combatStrengthBonusPlayer
                : battlefieldState.combatStrengthBonusEnemy;

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

                total += troop.faceValueFinal;
            }

            return total;
        }

        public static BattleOutcome ComputeOutcome(int playerCombatStrength, int enemyCombatStrength)
        {
            if (playerCombatStrength == enemyCombatStrength)
            {
                return BattleOutcome.Draw;
            }

            if (playerCombatStrength > enemyCombatStrength)
            {
                long doubledLoser = (long)enemyCombatStrength * 2L;
                if (enemyCombatStrength == 0 || playerCombatStrength >= doubledLoser)
                {
                    return BattleOutcome.GreatVictory;
                }

                return BattleOutcome.Victory;
            }

            long doubledPlayer = (long)playerCombatStrength * 2L;
            if (playerCombatStrength == 0 || enemyCombatStrength >= doubledPlayer)
            {
                return BattleOutcome.GreatDefeat;
            }

            return BattleOutcome.Defeat;
        }

        public static bool ResolveBattlefield(
            BattleState battleState,
            int battlefieldIndex,
            out BattleOutcome outcome,
            out int playerCombatStrength,
            out int enemyCombatStrength)
        {
            if (battleState == null)
            {
                throw new ArgumentNullException(nameof(battleState));
            }

            battleState.EnsureInitialized();

            outcome = BattleOutcome.Draw;
            playerCombatStrength = 0;
            enemyCombatStrength = 0;

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

            playerCombatStrength = ComputeCombatStrength(
                battlefieldState,
                battleState.troopsById,
                true);

            enemyCombatStrength = ComputeCombatStrength(
                battlefieldState,
                battleState.troopsById,
                false);

            outcome = ComputeOutcome(playerCombatStrength, enemyCombatStrength);
            ApplyMoraleDelta(battleState, outcome);

            if (battleState.playerMorale <= 0 || battleState.enemyMorale <= 0)
            {
                battleState.isBattleEnded = true;
            }

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

                if (battleState.isBattleEnded)
                {
                    break;
                }
            }

            return resolvedCount;
        }

        static void ApplyMoraleDelta(BattleState battleState, BattleOutcome outcome)
        {
            switch (outcome)
            {
                case BattleOutcome.GreatVictory:
                    battleState.enemyMorale -= 2;
                    break;
                case BattleOutcome.Victory:
                    battleState.enemyMorale -= 1;
                    break;
                case BattleOutcome.Draw:
                    break;
                case BattleOutcome.Defeat:
                    battleState.playerMorale -= 1;
                    break;
                case BattleOutcome.GreatDefeat:
                    battleState.playerMorale -= 2;
                    break;
                default:
                    Debug.LogWarning($"[BattleSimulator] Unknown BattleOutcome({outcome}) was ignored.");
                    break;
            }
        }
    }
}
