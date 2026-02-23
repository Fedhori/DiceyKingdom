using System;
using System.Collections.Generic;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Application.Duel
{
    public readonly struct DuelRollResult
    {
        public int rolledActionCount { get; }
        public AbilityTimedEffectRunResult timedEffectResult { get; }

        public DuelRollResult(
            int rolledActionCount,
            AbilityTimedEffectRunResult timedEffectResult)
        {
            this.rolledActionCount = rolledActionCount;
            this.timedEffectResult = timedEffectResult;
        }
    }

    public readonly struct DuelClashResolveStepResult
    {
        public int clashIndex { get; }
        public DuelOutcome outcome { get; }
        public int playerTotalAttack { get; }
        public int opponentTotalAttack { get; }

        public DuelClashResolveStepResult(
            int clashIndex,
            DuelOutcome outcome,
            int playerTotalAttack,
            int opponentTotalAttack)
        {
            this.clashIndex = clashIndex;
            this.outcome = outcome;
            this.playerTotalAttack = playerTotalAttack;
            this.opponentTotalAttack = opponentTotalAttack;
        }
    }

    public sealed class DuelClashResolveResult
    {
        public IReadOnlyList<DuelClashResolveStepResult> steps { get; }
        public AbilityTimedEffectRunResult turnEndTimedEffectResult { get; }
        public int outcomeEffectAppliedCount { get; }
        public int outcomeEffectFailedCount { get; }
        public int cooldownUpdatedCount { get; }

        public DuelClashResolveResult(
            IReadOnlyList<DuelClashResolveStepResult> steps,
            AbilityTimedEffectRunResult turnEndTimedEffectResult,
            int outcomeEffectAppliedCount,
            int outcomeEffectFailedCount,
            int cooldownUpdatedCount)
        {
            this.steps = steps ?? Array.Empty<DuelClashResolveStepResult>();
            this.turnEndTimedEffectResult = turnEndTimedEffectResult;
            this.outcomeEffectAppliedCount = outcomeEffectAppliedCount;
            this.outcomeEffectFailedCount = outcomeEffectFailedCount;
            this.cooldownUpdatedCount = cooldownUpdatedCount;
        }
    }

    public sealed class DuelTurnProcessor
    {
        readonly GameDatabase database;
        readonly DuelEffectClashResolver effectClashResolver;
        readonly AbilityTimedEffectRunner timedEffectRunner;

        public DuelTurnProcessor(GameDatabase database, DuelEffectClashResolver effectClashResolver = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.effectClashResolver = effectClashResolver ?? new DuelEffectClashResolver();
            timedEffectRunner = new AbilityTimedEffectRunner(this.database, this.effectClashResolver);
        }

        public bool TryRollAllDeployedActions(
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

            var deployedActionIds = CollectDeployedActionIds(state);
            if (deployedActionIds.Count <= 0)
            {
                failureMessage = "no deployed abilities to roll.";
                return false;
            }

            int rolledCount = 0;
            foreach (string abilityId in deployedActionIds)
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

                DuelSimulator.RollAction(ability);
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

        public bool TryClashResolveAllClashes(
            DuelState state,
            DuelPhaseRunner phaseRunner,
            out DuelClashResolveResult result,
            out string failureMessage)
        {
            result = new DuelClashResolveResult(
                Array.Empty<DuelClashResolveStepResult>(),
                new AbilityTimedEffectRunResult(0, 0, 0),
                0,
                0,
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

            if (phaseRunner.currentPhase == DuelPhase.Skill)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter ClashResolve phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (phaseRunner.currentPhase != DuelPhase.ClashResolve)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.ClashResolve}.";
                return false;
            }

            var steps = new List<DuelClashResolveStepResult>(state.clashes.Count);
            int outcomeEffectAppliedCount = 0;
            int outcomeEffectFailedCount = 0;

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clash = state.clashes[clashIndex];
                if (clash == null)
                {
                    Debug.LogWarning($"[DuelTurnProcessor] ClashResolve warning: clashes[{clashIndex}] is null.");
                    continue;
                }

                clash.EnsureInitialized();

                int playerTotalAttack = DuelSimulator.ComputeTotalAttack(
                    clash,
                    state.abilitiesById,
                    true);
                int opponentTotalAttack = DuelSimulator.ComputeTotalAttack(
                    clash,
                    state.abilitiesById,
                    false);
                DuelOutcome outcome = DuelSimulator.ComputeOutcome(playerTotalAttack, opponentTotalAttack);

                if (ApplyOutcomeDamageFromClash(state, clashIndex, outcome))
                {
                    outcomeEffectAppliedCount += 1;
                }
                else
                {
                    outcomeEffectFailedCount += 1;
                }

                if (state.playerHealth <= 0 || state.opponentHealth <= 0)
                {
                    state.isDuelEnded = true;
                    DuelSimulator.ClearModifierLayer(state, ModifierLayer.Duel);
                }

                steps.Add(new DuelClashResolveStepResult(
                    clashIndex,
                    outcome,
                    playerTotalAttack,
                    opponentTotalAttack));

                if (state.isDuelEnded)
                {
                    break;
                }
            }

            if (steps.Count <= 0)
            {
                failureMessage = "no clashes were resolved.";
                return false;
            }

            int cooldownUpdatedCount = 0;
            AbilityTimedEffectRunResult turnEndTimedEffects = new AbilityTimedEffectRunResult(0, 0, 0);

            if (!state.isDuelEnded)
            {
                cooldownUpdatedCount = ApplyTurnEndMaintenance(state);
                turnEndTimedEffects = timedEffectRunner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);
            }

            if (!state.isDuelEnded && !phaseRunner.AdvanceToNextPhase())
            {
                Debug.LogWarning(
                    $"[DuelTurnProcessor] ClashResolve warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            result = new DuelClashResolveResult(
                steps,
                turnEndTimedEffects,
                outcomeEffectAppliedCount,
                outcomeEffectFailedCount,
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
                state.abilitiesById = new Dictionary<string, AbilityInstance>();
                Debug.LogWarning("[DuelTurnProcessor] abilitiesById was null and has been auto-initialized.");
                return 0;
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

        bool ApplyOutcomeDamageFromClash(
            DuelState state,
            int clashIndex,
            DuelOutcome outcome)
        {
            ClashDef clashDef = ClashResolveClashDef(state, clashIndex);
            if (clashDef == null)
            {
                Debug.LogWarning($"[DuelTurnProcessor] clashDef for clash[{clashIndex}] is missing.");
                return false;
            }

            int damage = Mathf.Max(0, clashDef.damage);
            if (damage <= 0 || outcome == DuelOutcome.Draw)
            {
                return true;
            }

            switch (outcome)
            {
                case DuelOutcome.Victory:
                    state.opponentHealth -= damage;
                    return true;
                case DuelOutcome.Defeat:
                    state.playerHealth -= damage;
                    return true;
                default:
                    return true;
            }
        }

        ClashDef ClashResolveClashDef(DuelState state, int clashIndex)
        {
            if (state.clashes == null ||
                clashIndex < 0 ||
                clashIndex >= state.clashes.Count)
            {
                return null;
            }

            ClashState clashState = state.clashes[clashIndex];
            if (clashState == null || string.IsNullOrWhiteSpace(clashState.clashId))
            {
                return null;
            }

            if (database.clashesById == null)
            {
                return null;
            }

            if (!database.clashesById.TryGetValue(clashState.clashId, out ClashDef clashDef))
            {
                return null;
            }

            return clashDef;
        }

        static HashSet<string> CollectDeployedActionIds(DuelState state)
        {
            var deployedActionIds = new HashSet<string>(StringComparer.Ordinal);

            if (state.clashes == null)
            {
                return deployedActionIds;
            }

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clash = state.clashes[clashIndex];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();
                CollectActionIds(deployedActionIds, clash.playerActionIds, $"playerActionIds[{clashIndex}]");
                CollectActionIds(deployedActionIds, clash.opponentActionIds, $"opponentActionIds[{clashIndex}]");
            }

            return deployedActionIds;
        }

        static void CollectActionIds(HashSet<string> buffer, List<string> abilityIds, string sourceLabel)
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
