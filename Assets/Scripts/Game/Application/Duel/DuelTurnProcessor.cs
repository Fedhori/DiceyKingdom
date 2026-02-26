using System;
using System.Collections.Generic;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using Game.Infrastructure.Data.Effects;
using UnityEngine;

namespace Game.Application.Duel
{
    public readonly struct DuelRollResult
    {
        public int rolledAbilityCount { get; }
        public AbilityTimedEffectRunResult timedEffectResult { get; }

        public DuelRollResult(
            int rolledAbilityCount,
            AbilityTimedEffectRunResult timedEffectResult)
        {
            this.rolledAbilityCount = rolledAbilityCount;
            this.timedEffectResult = timedEffectResult;
        }
    }

    public readonly struct DuelCombatResolveStepResult
    {
        public int combatIndex { get; }
        public DuelOutcome outcome { get; }
        public int playerTotalPower { get; }
        public int opponentTotalPower { get; }
        public int appliedDamage { get; }

        public DuelCombatResolveStepResult(
            int combatIndex,
            DuelOutcome outcome,
            int playerTotalPower,
            int opponentTotalPower,
            int appliedDamage)
        {
            this.combatIndex = combatIndex;
            this.outcome = outcome;
            this.playerTotalPower = playerTotalPower;
            this.opponentTotalPower = opponentTotalPower;
            this.appliedDamage = appliedDamage;
        }
    }

    public sealed class DuelCombatResolveResult
    {
        public IReadOnlyList<DuelCombatResolveStepResult> steps { get; }
        public AbilityTimedEffectRunResult turnEndTimedEffectResult { get; }
        public int cooldownUpdatedCount { get; }

        public DuelCombatResolveResult(
            IReadOnlyList<DuelCombatResolveStepResult> steps,
            AbilityTimedEffectRunResult turnEndTimedEffectResult,
            int cooldownUpdatedCount)
        {
            this.steps = steps ?? Array.Empty<DuelCombatResolveStepResult>();
            this.turnEndTimedEffectResult = turnEndTimedEffectResult;
            this.cooldownUpdatedCount = cooldownUpdatedCount;
        }
    }

    public sealed class DuelTurnProcessor
    {
        readonly GameDatabase database;
        readonly DuelEffectCombatResolver effectCombatResolver;
        readonly AbilityTimedEffectRunner timedEffectRunner;

        public DuelTurnProcessor(
            GameDatabase database,
            DuelEffectCombatResolver effectCombatResolver = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.effectCombatResolver = effectCombatResolver ?? new DuelEffectCombatResolver();
            timedEffectRunner = new AbilityTimedEffectRunner(this.database, this.effectCombatResolver);
        }

        public bool TryRollAllDeployedAbilities(
            DuelState state,
            DuelPhaseRunner phaseRunner,
            out DuelRollResult result,
            out string failureMessage)
        {
            result = new DuelRollResult(0, new AbilityTimedEffectRunResult(0, 0, 0));
            failureMessage = string.Empty;

            if (state == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            if (phaseRunner == null)
            {
                failureMessage = "phase runner is null.";
                return false;
            }

            state.EnsureInitialized();

            if (!phaseRunner.isStarted)
            {
                failureMessage = "duel is not started.";
                return false;
            }

            if (state.isDuelEnded)
            {
                failureMessage = "duel already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == DuelPhase.PlayerSetup)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Roll phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (phaseRunner.currentPhase != DuelPhase.Roll)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.Roll}.";
                return false;
            }

            HashSet<string> deployedAbilityIds = CollectDeployedAbilityIds(state);
            if (deployedAbilityIds.Count <= 0)
            {
                failureMessage = "no deployed abilities to roll.";
                return false;
            }

            int rolledCount = 0;
            foreach (string abilityId in deployedAbilityIds)
            {
                if (!state.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
                {
                    Debug.LogWarning($"[DuelTurnProcessor] Roll warning: abilityId({abilityId}) does not exist.");
                    continue;
                }

                if (ability.abilityType != AbilityType.Attack)
                {
                    continue;
                }

                DuelSimulator.RollAbility(ability);
                rolledCount += 1;
            }

            if (rolledCount <= 0)
            {
                failureMessage = "all deployed abilities were invalid.";
                return false;
            }

            AbilityTimedEffectRunResult timedResult = timedEffectRunner.ApplyForTiming(state, DuelEffectTiming.Roll);

            if (!phaseRunner.AdvanceToNextPhase())
            {
                Debug.LogWarning(
                    $"[DuelTurnProcessor] Roll warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            result = new DuelRollResult(rolledCount, timedResult);
            return true;
        }

        public bool TryResolveAllCombats(
            DuelState state,
            DuelPhaseRunner phaseRunner,
            out DuelCombatResolveResult result,
            out string failureMessage)
        {
            result = new DuelCombatResolveResult(
                Array.Empty<DuelCombatResolveStepResult>(),
                new AbilityTimedEffectRunResult(0, 0, 0),
                0);
            failureMessage = string.Empty;

            if (state == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            if (phaseRunner == null)
            {
                failureMessage = "phase runner is null.";
                return false;
            }

            state.EnsureInitialized();

            if (!phaseRunner.isStarted)
            {
                failureMessage = "duel is not started.";
                return false;
            }

            if (state.isDuelEnded)
            {
                failureMessage = "duel already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == DuelPhase.Roll)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Resolve phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (phaseRunner.currentPhase != DuelPhase.Resolve)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.Resolve}.";
                return false;
            }

            ClearResolveCombatFlags(state);
            timedEffectRunner.ApplyForTiming(state, DuelEffectTiming.Resolve);

            var steps = new List<DuelCombatResolveStepResult>(state.combats.Count);

            for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
            {
                CombatState combat = state.combats[combatIndex];
                if (combat == null)
                {
                    Debug.LogWarning($"[DuelTurnProcessor] Resolve warning: combats[{combatIndex}] is null.");
                    continue;
                }

                combat.EnsureInitialized();

                int playerTotalPower = DuelSimulator.ComputeTotalPower(
                    combat,
                    state.abilitiesById,
                    true);
                int opponentTotalPower = DuelSimulator.ComputeTotalPower(
                    combat,
                    state.abilitiesById,
                    false);
                DuelOutcome outcome = DuelSimulator.ComputeOutcome(playerTotalPower, opponentTotalPower);
                int appliedDamage = ApplyCombatOutcomeDamage(state, combat, playerTotalPower, opponentTotalPower);

                if (state.playerHealth <= 0 || state.opponentHealth <= 0)
                {
                    state.isDuelEnded = true;
                    DuelSimulator.ClearModifierLayer(state, ModifierLayer.Duel);
                }

                steps.Add(new DuelCombatResolveStepResult(
                    combatIndex,
                    outcome,
                    playerTotalPower,
                    opponentTotalPower,
                    appliedDamage));

                if (state.isDuelEnded)
                {
                    break;
                }
            }

            if (steps.Count <= 0)
            {
                failureMessage = "no combats were resolved.";
                return false;
            }

            int cooldownUpdatedCount = 0;
            AbilityTimedEffectRunResult turnEndTimedEffects = new AbilityTimedEffectRunResult(0, 0, 0);

            if (!state.isDuelEnded)
            {
                cooldownUpdatedCount = ApplyTurnEndMaintenance(state);
                turnEndTimedEffects = timedEffectRunner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);
                ReturnPlayerAbilitiesToLoadout(state);
            }

            if (!state.isDuelEnded && !phaseRunner.AdvanceToNextPhase())
            {
                Debug.LogWarning(
                    $"[DuelTurnProcessor] Resolve warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            result = new DuelCombatResolveResult(
                steps,
                turnEndTimedEffects,
                cooldownUpdatedCount);
            return true;
        }

        int ApplyTurnEndMaintenance(DuelState state)
        {
            if (database.duelConfig == null)
            {
                Debug.LogWarning("[DuelTurnProcessor] TurnEnd maintenance skipped: duel.config is missing.");
                return 0;
            }

            if (state.abilitiesById == null)
            {
                const string message = "[DuelTurnProcessor] Invalid state: abilitiesById is null.";
                Debug.LogError(message);
                throw new InvalidOperationException(message);
            }

            int cooldownUpdatedCount = 0;
            int cooldownTick = Mathf.Abs(database.duelConfig.cooldownTickPerTurn);
            foreach (KeyValuePair<string, AbilityInstance> pair in state.abilitiesById)
            {
                AbilityInstance ability = pair.Value;
                if (ability == null)
                {
                    continue;
                }

                ability.EnsureInitialized();
                if (ability.cooldownRemaining <= 0 || cooldownTick <= 0)
                {
                    continue;
                }

                int updatedValue = Mathf.Max(0, ability.cooldownRemaining - cooldownTick);
                if (updatedValue == ability.cooldownRemaining)
                {
                    continue;
                }

                ability.cooldownRemaining = updatedValue;
                cooldownUpdatedCount += 1;
            }

            return cooldownUpdatedCount;
        }

        static int ApplyCombatOutcomeDamage(
            DuelState state,
            CombatState combat,
            int playerTotalPower,
            int opponentTotalPower)
        {
            int diff = playerTotalPower - opponentTotalPower;
            if (diff == 0)
            {
                return 0;
            }

            bool isPlayerWinner = diff > 0;
            if (IsOutgoingDamagePreventedOnWin(combat, isPlayerWinner))
            {
                return 0;
            }

            const int damage = 1;
            if (isPlayerWinner)
            {
                state.opponentHealth -= damage;
            }
            else
            {
                state.playerHealth -= damage;
            }

            return damage;
        }

        static bool IsOutgoingDamagePreventedOnWin(CombatState combat, bool isPlayerSide)
        {
            if (combat == null)
            {
                return false;
            }

            return isPlayerSide
                ? combat.preventOutgoingDamageOnWinPlayer
                : combat.preventOutgoingDamageOnWinOpponent;
        }

        static void ClearResolveCombatFlags(DuelState state)
        {
            if (state?.combats == null)
            {
                return;
            }

            for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
            {
                CombatState combat = state.combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.preventOutgoingDamageOnWinPlayer = false;
                combat.preventOutgoingDamageOnWinOpponent = false;
            }
        }

        static void ReturnPlayerAbilitiesToLoadout(DuelState state)
        {
            if (state.combats == null || state.loadoutAbilityIds == null)
            {
                return;
            }

            for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
            {
                CombatState combat = state.combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                for (int i = 0; i < combat.playerAbilityIds.Count; i++)
                {
                    string abilityId = combat.playerAbilityIds[i];
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    if (!state.loadoutAbilityIds.Contains(abilityId))
                    {
                        state.loadoutAbilityIds.Add(abilityId);
                    }
                }

                combat.playerAbilityIds.Clear();
            }
        }

        static HashSet<string> CollectDeployedAbilityIds(DuelState state)
        {
            var deployedAbilityIds = new HashSet<string>(StringComparer.Ordinal);

            if (state.combats == null)
            {
                return deployedAbilityIds;
            }

            for (int combatIndex = 0; combatIndex < state.combats.Count; combatIndex++)
            {
                CombatState combat = state.combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                CollectAbilityIds(deployedAbilityIds, combat.playerAbilityIds, $"playerAbilityIds[{combatIndex}]");
                CollectAbilityIds(deployedAbilityIds, combat.opponentAbilityIds, $"opponentAbilityIds[{combatIndex}]");
            }

            return deployedAbilityIds;
        }

        static void CollectAbilityIds(HashSet<string> buffer, List<string> abilityIds, string sourceLabel)
        {
            if (abilityIds == null)
            {
                Debug.LogWarning($"[DuelTurnProcessor] Roll warning: {sourceLabel} is null.");
                return;
            }

            for (int i = 0; i < abilityIds.Count; i++)
            {
                string abilityId = abilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    Debug.LogWarning($"[DuelTurnProcessor] Roll warning: empty abilityId at {sourceLabel}[{i}].");
                    continue;
                }

                buffer.Add(abilityId);
            }
        }
    }
}
