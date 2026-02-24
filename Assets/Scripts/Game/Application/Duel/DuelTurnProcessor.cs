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

    public readonly struct DuelClashResolveStepResult
    {
        public int clashIndex { get; }
        public DuelOutcome outcome { get; }
        public int playerTotalPower { get; }
        public int opponentTotalPower { get; }
        public int appliedDamage { get; }

        public DuelClashResolveStepResult(
            int clashIndex,
            DuelOutcome outcome,
            int playerTotalPower,
            int opponentTotalPower,
            int appliedDamage)
        {
            this.clashIndex = clashIndex;
            this.outcome = outcome;
            this.playerTotalPower = playerTotalPower;
            this.opponentTotalPower = opponentTotalPower;
            this.appliedDamage = appliedDamage;
        }
    }

    public sealed class DuelClashResolveResult
    {
        public IReadOnlyList<DuelClashResolveStepResult> steps { get; }
        public AbilityTimedEffectRunResult turnEndTimedEffectResult { get; }
        public int cooldownUpdatedCount { get; }
        public bool patternAdvanced { get; }

        public DuelClashResolveResult(
            IReadOnlyList<DuelClashResolveStepResult> steps,
            AbilityTimedEffectRunResult turnEndTimedEffectResult,
            int cooldownUpdatedCount,
            bool patternAdvanced)
        {
            this.steps = steps ?? Array.Empty<DuelClashResolveStepResult>();
            this.turnEndTimedEffectResult = turnEndTimedEffectResult;
            this.cooldownUpdatedCount = cooldownUpdatedCount;
            this.patternAdvanced = patternAdvanced;
        }
    }

    public sealed class DuelTurnProcessor
    {
        const string noOutgoingDamageOnWinTag = "ability.effect.no.outgoing.damage.on.win";

        readonly GameDatabase database;
        readonly DuelEffectClashResolver effectClashResolver;
        readonly AbilityTimedEffectRunner timedEffectRunner;
        readonly System.Random random;

        public DuelTurnProcessor(
            GameDatabase database,
            DuelEffectClashResolver effectClashResolver = null,
            System.Random random = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.effectClashResolver = effectClashResolver ?? new DuelEffectClashResolver();
            timedEffectRunner = new AbilityTimedEffectRunner(this.database, this.effectClashResolver);
            this.random = random ?? new System.Random();
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

            var deployedAbilityIds = CollectDeployedAbilityIds(state);
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

        public bool TryResolveAllClashes(
            DuelState state,
            DuelPhaseRunner phaseRunner,
            out DuelClashResolveResult result,
            out string failureMessage)
        {
            result = new DuelClashResolveResult(
                Array.Empty<DuelClashResolveStepResult>(),
                new AbilityTimedEffectRunResult(0, 0, 0),
                0,
                false);
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

            var steps = new List<DuelClashResolveStepResult>(state.clashes.Count);

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clash = state.clashes[clashIndex];
                if (clash == null)
                {
                    Debug.LogWarning($"[DuelTurnProcessor] Resolve warning: clashes[{clashIndex}] is null.");
                    continue;
                }

                clash.EnsureInitialized();

                int playerTotalPower = DuelSimulator.ComputeTotalPower(
                    clash,
                    state.abilitiesById,
                    true);
                int opponentTotalPower = DuelSimulator.ComputeTotalPower(
                    clash,
                    state.abilitiesById,
                    false);
                DuelOutcome outcome = DuelSimulator.ComputeOutcome(playerTotalPower, opponentTotalPower);
                int appliedDamage = ApplyClashOutcomeDamage(state, clash, playerTotalPower, opponentTotalPower);

                if (state.playerHealth <= 0 || state.opponentHealth <= 0)
                {
                    state.isDuelEnded = true;
                    DuelSimulator.ClearModifierLayer(state, ModifierLayer.Duel);
                }

                steps.Add(new DuelClashResolveStepResult(
                    clashIndex,
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
                failureMessage = "no clashes were resolved.";
                return false;
            }

            int cooldownUpdatedCount = 0;
            AbilityTimedEffectRunResult turnEndTimedEffects = new AbilityTimedEffectRunResult(0, 0, 0);
            bool patternAdvanced = false;

            if (!state.isDuelEnded)
            {
                cooldownUpdatedCount = ApplyTurnEndMaintenance(state);
                turnEndTimedEffects = timedEffectRunner.ApplyForTiming(state, DuelEffectTiming.TurnEnd);
                ReturnPlayerAbilitiesToLoadout(state);
                patternAdvanced = TryAdvancePatternAndRebuildClash(state);
            }

            if (!state.isDuelEnded && !phaseRunner.AdvanceToNextPhase())
            {
                Debug.LogWarning(
                    $"[DuelTurnProcessor] Resolve warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            result = new DuelClashResolveResult(
                steps,
                turnEndTimedEffects,
                cooldownUpdatedCount,
                patternAdvanced);
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

        static int ApplyClashOutcomeDamage(
            DuelState state,
            ClashState clash,
            int playerTotalPower,
            int opponentTotalPower)
        {
            int diff = playerTotalPower - opponentTotalPower;
            if (diff == 0)
            {
                return 0;
            }

            bool isPlayerWinner = diff > 0;
            if (HasNoOutgoingDamageOnWinTag(state, clash, isPlayerWinner))
            {
                return 0;
            }

            int damage = Mathf.Abs(diff);
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

        static bool HasNoOutgoingDamageOnWinTag(DuelState state, ClashState clash, bool isPlayerSide)
        {
            if (state?.abilitiesById == null || clash == null)
            {
                return false;
            }

            List<string> winnerAbilityIds = isPlayerSide
                ? clash.playerAbilityIds
                : clash.opponentAbilityIds;
            if (winnerAbilityIds == null)
            {
                return false;
            }

            for (int i = 0; i < winnerAbilityIds.Count; i++)
            {
                string abilityId = winnerAbilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId) ||
                    !state.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) ||
                    ability == null)
                {
                    continue;
                }

                ability.EnsureInitialized();
                if (ability.tags != null && ability.tags.Contains(noOutgoingDamageOnWinTag))
                {
                    return true;
                }
            }

            return false;
        }

        static void ReturnPlayerAbilitiesToLoadout(DuelState state)
        {
            if (state.clashes == null || state.loadoutAbilityIds == null)
            {
                return;
            }

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clash = state.clashes[clashIndex];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();
                for (int i = 0; i < clash.playerAbilityIds.Count; i++)
                {
                    string abilityId = clash.playerAbilityIds[i];
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    if (!state.loadoutAbilityIds.Contains(abilityId))
                    {
                        state.loadoutAbilityIds.Add(abilityId);
                    }
                }

                clash.playerAbilityIds.Clear();
            }
        }

        bool TryAdvancePatternAndRebuildClash(DuelState state)
        {
            if (string.IsNullOrWhiteSpace(state.encounterId))
            {
                return false;
            }

            if (database.encountersById == null ||
                !database.encountersById.TryGetValue(state.encounterId, out EncounterDef encounter) ||
                encounter?.enemy == null ||
                encounter.enemy.patterns == null ||
                encounter.enemy.patterns.Count <= 0)
            {
                return false;
            }

            EncounterEnemyPatternDef currentPattern = ResolvePattern(encounter.enemy.patterns, state.currentPatternId);
            if (currentPattern == null)
            {
                currentPattern = ResolvePattern(encounter.enemy.patterns, encounter.enemy.startPatternId);
                if (currentPattern == null)
                {
                    return false;
                }
            }

            string nextPatternId = ResolveNextPatternId(currentPattern);
            EncounterEnemyPatternDef nextPattern = ResolvePattern(encounter.enemy.patterns, nextPatternId);
            if (nextPattern == null)
            {
                return false;
            }

            state.currentPatternId = nextPattern.patternId;
            RebuildClashSlotsFromPattern(state, nextPattern);
            return true;
        }

        string ResolveNextPatternId(EncounterEnemyPatternDef currentPattern)
        {
            if (currentPattern.nextPatterns == null || currentPattern.nextPatterns.Count <= 0)
            {
                return currentPattern.patternId;
            }

            double totalProbability = 0.0d;
            for (int i = 0; i < currentPattern.nextPatterns.Count; i++)
            {
                EncounterEnemyPatternTransitionDef transition = currentPattern.nextPatterns[i];
                if (transition == null || transition.probability <= 0.0d)
                {
                    continue;
                }

                totalProbability += transition.probability;
            }

            if (totalProbability <= 0.0d)
            {
                return currentPattern.patternId;
            }

            double roll = random.NextDouble() * totalProbability;
            double cumulative = 0.0d;

            for (int i = 0; i < currentPattern.nextPatterns.Count; i++)
            {
                EncounterEnemyPatternTransitionDef transition = currentPattern.nextPatterns[i];
                if (transition == null || transition.probability <= 0.0d)
                {
                    continue;
                }

                cumulative += transition.probability;
                if (roll <= cumulative)
                {
                    return transition.patternId;
                }
            }

            return currentPattern.patternId;
        }

        static EncounterEnemyPatternDef ResolvePattern(
            IReadOnlyList<EncounterEnemyPatternDef> patterns,
            string patternId)
        {
            if (patterns == null || string.IsNullOrWhiteSpace(patternId))
            {
                return null;
            }

            for (int i = 0; i < patterns.Count; i++)
            {
                EncounterEnemyPatternDef pattern = patterns[i];
                if (pattern == null)
                {
                    continue;
                }

                if (string.Equals(pattern.patternId, patternId, StringComparison.Ordinal))
                {
                    return pattern;
                }
            }

            return null;
        }

        static void RebuildClashSlotsFromPattern(DuelState state, EncounterEnemyPatternDef pattern)
        {
            RemoveOpponentAbilityInstances(state);

            state.clashes.Clear();
            state.opponentClashLoadoutEntries.Clear();

            if (pattern?.clashes == null)
            {
                return;
            }

            for (int clashIndex = 0; clashIndex < pattern.clashes.Count; clashIndex++)
            {
                EncounterEnemyClashDef clashDef = pattern.clashes[clashIndex];
                if (clashDef == null)
                {
                    state.clashes.Add(new ClashState());
                    continue;
                }

                var clashState = new ClashState
                {
                    clashId = clashDef.clashId,
                    maxPlayerAssignments = clashDef.maxPlayerAssignments
                };
                clashState.EnsureInitialized();
                state.clashes.Add(clashState);

                if (clashDef.abilityLoadout == null)
                {
                    continue;
                }

                for (int loadoutIndex = 0; loadoutIndex < clashDef.abilityLoadout.Count; loadoutIndex++)
                {
                    SummonAbilityRefDef abilityRef = clashDef.abilityLoadout[loadoutIndex];
                    if (abilityRef == null || abilityRef.count <= 0 || string.IsNullOrWhiteSpace(abilityRef.abilityId))
                    {
                        continue;
                    }

                    state.opponentClashLoadoutEntries.Add(new OpponentClashLoadoutEntry
                    {
                        clashIndex = clashIndex,
                        abilityDefId = abilityRef.abilityId,
                        count = abilityRef.count
                    });
                }
            }
        }

        static void RemoveOpponentAbilityInstances(DuelState state)
        {
            if (state.clashes == null || state.abilitiesById == null)
            {
                return;
            }

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clashState = state.clashes[clashIndex];
                if (clashState == null)
                {
                    continue;
                }

                clashState.EnsureInitialized();
                for (int i = 0; i < clashState.opponentAbilityIds.Count; i++)
                {
                    string abilityId = clashState.opponentAbilityIds[i];
                    if (string.IsNullOrWhiteSpace(abilityId))
                    {
                        continue;
                    }

                    state.abilitiesById.Remove(abilityId);
                }

                clashState.opponentAbilityIds.Clear();
            }
        }

        static HashSet<string> CollectDeployedAbilityIds(DuelState state)
        {
            var deployedAbilityIds = new HashSet<string>(StringComparer.Ordinal);

            if (state.clashes == null)
            {
                return deployedAbilityIds;
            }

            for (int clashIndex = 0; clashIndex < state.clashes.Count; clashIndex++)
            {
                ClashState clash = state.clashes[clashIndex];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();
                CollectAbilityIds(deployedAbilityIds, clash.playerAbilityIds, $"playerAbilityIds[{clashIndex}]");
                CollectAbilityIds(deployedAbilityIds, clash.opponentAbilityIds, $"opponentAbilityIds[{clashIndex}]");
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
