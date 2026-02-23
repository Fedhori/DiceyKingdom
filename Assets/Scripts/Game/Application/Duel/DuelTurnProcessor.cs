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
        public ActionTimedEffectRunResult timedEffectResult { get; }

        public DuelRollResult(
            int rolledActionCount,
            ActionTimedEffectRunResult timedEffectResult)
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
        public ActionTimedEffectRunResult turnEndTimedEffectResult { get; }
        public int outcomeEffectAppliedCount { get; }
        public int outcomeEffectFailedCount { get; }
        public int cooldownUpdatedCount { get; }
        public int focusBeforeTurnEnd { get; }
        public int focusAfterTurnEnd { get; }

        public DuelClashResolveResult(
            IReadOnlyList<DuelClashResolveStepResult> steps,
            ActionTimedEffectRunResult turnEndTimedEffectResult,
            int outcomeEffectAppliedCount,
            int outcomeEffectFailedCount,
            int cooldownUpdatedCount,
            int focusBeforeTurnEnd,
            int focusAfterTurnEnd)
        {
            this.steps = steps ?? Array.Empty<DuelClashResolveStepResult>();
            this.turnEndTimedEffectResult = turnEndTimedEffectResult;
            this.outcomeEffectAppliedCount = outcomeEffectAppliedCount;
            this.outcomeEffectFailedCount = outcomeEffectFailedCount;
            this.cooldownUpdatedCount = cooldownUpdatedCount;
            this.focusBeforeTurnEnd = focusBeforeTurnEnd;
            this.focusAfterTurnEnd = focusAfterTurnEnd;
        }
    }

    public sealed class DuelTurnProcessor
    {
        readonly GameDatabase database;
        readonly DuelEffectClashResolver effectClashResolver;
        readonly ActionTimedEffectRunner timedEffectRunner;

        public DuelTurnProcessor(GameDatabase database, DuelEffectClashResolver effectClashResolver = null)
        {
            this.database = database ?? throw new ArgumentNullException(nameof(database));
            this.effectClashResolver = effectClashResolver ?? new DuelEffectClashResolver();
            timedEffectRunner = new ActionTimedEffectRunner(this.database, this.effectClashResolver);
        }

        public bool TryRollAllDeployedActions(
            DuelState state,
            DuelPhaseRunner phaseRunner,
            out DuelRollResult result,
            out string failureMessage)
        {
            result = new DuelRollResult(0, new ActionTimedEffectRunResult(0, 0, 0));
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
                failureMessage = "no deployed actions to roll.";
                return false;
            }

            int rolledCount = 0;
            foreach (string actionId in deployedActionIds)
            {
                if (!state.actionsById.TryGetValue(actionId, out ActionInstance action) || action == null)
                {
                    Debug.LogWarning($"[DuelTurnProcessor] Roll warning: actionId({actionId}) does not exist.");
                    continue;
                }

                DuelSimulator.RollAction(action);
                rolledCount += 1;
            }

            if (rolledCount <= 0)
            {
                failureMessage = "all deployed actions were invalid.";
                return false;
            }

            ActionTimedEffectRunResult timedResult = timedEffectRunner.ApplyForTiming(state, DuelEffectTiming.Roll);

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
                new ActionTimedEffectRunResult(0, 0, 0),
                0,
                0,
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
                    state.actionsById,
                    true);
                int opponentTotalAttack = DuelSimulator.ComputeTotalAttack(
                    clash,
                    state.actionsById,
                    false);
                DuelOutcome outcome = DuelSimulator.ComputeOutcome(playerTotalAttack, opponentTotalAttack);

                ApplyOutcomeEffects(
                    state,
                    clashIndex,
                    outcome,
                    ref outcomeEffectAppliedCount,
                    ref outcomeEffectFailedCount);

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

            int focusBeforeTurnEnd = state.focus;
            int cooldownUpdatedCount = 0;
            ActionTimedEffectRunResult turnEndTimedEffects = new ActionTimedEffectRunResult(0, 0, 0);

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
                cooldownUpdatedCount,
                focusBeforeTurnEnd,
                state.focus);
            return true;
        }

        int ApplyTurnEndMaintenance(DuelState state)
        {
            if (database.duelConfig == null)
            {
                Debug.LogWarning("[DuelTurnProcessor] TurnEnd maintenance skipped: duel.config is missing.");
                return 0;
            }

            int focusMax = Mathf.Max(0, database.duelConfig.focusMax);
            state.focus = Mathf.Clamp(state.focus + database.duelConfig.focusRegenPerTurn, 0, focusMax);

            if (state.cooldowns == null)
            {
                state.cooldowns = new Dictionary<string, int>();
                Debug.LogWarning("[DuelTurnProcessor] cooldowns was null and has been auto-initialized.");
                return 0;
            }

            int cooldownUpdatedCount = 0;
            var cooldownKeys = new List<string>(state.cooldowns.Keys);
            int cooldownTick = database.duelConfig.cooldownTickPerTurn;

            for (int i = 0; i < cooldownKeys.Count; i++)
            {
                string key = cooldownKeys[i];
                int currentValue = state.cooldowns[key];
                int updatedValue = Mathf.Max(0, currentValue + cooldownTick);
                if (updatedValue == currentValue)
                {
                    continue;
                }

                state.cooldowns[key] = updatedValue;
                cooldownUpdatedCount += 1;
            }

            return cooldownUpdatedCount;
        }

        void ApplyOutcomeEffects(
            DuelState state,
            int clashIndex,
            DuelOutcome outcome,
            ref int appliedCount,
            ref int failedCount)
        {
            ClashDef clashDef = ClashResolveClashDef(state, clashIndex);
            if (clashDef == null || clashDef.outcomeEffects == null)
            {
                return;
            }

            string outcomeKey = outcome.ToString();
            if (!clashDef.outcomeEffects.TryGetValue(outcomeKey, out List<EffectBlockDef> blocks) || blocks == null)
            {
                return;
            }

            for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
            {
                EffectBlockDef block = blocks[blockIndex];
                if (block == null || block.ops == null)
                {
                    continue;
                }

                for (int opIndex = 0; opIndex < block.ops.Count; opIndex++)
                {
                    EffectOpDef op = block.ops[opIndex];
                    if (!TryBuildOutcomeCommand(
                            clashDef,
                            clashIndex,
                            outcome,
                            blockIndex,
                            opIndex,
                            op,
                            out DuelEffectCommand command,
                            out string warningMessage))
                    {
                        failedCount += 1;
                        Debug.LogWarning($"[DuelTurnProcessor] Outcome effect warning: {warningMessage}");
                        continue;
                    }

                    DuelEffectResult applyResult = effectClashResolver.Apply(state, command);
                    if (applyResult.isSuccess)
                    {
                        appliedCount += 1;
                    }
                    else
                    {
                        failedCount += 1;
                    }
                }
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

        static bool TryBuildOutcomeCommand(
            ClashDef clashDef,
            int clashIndex,
            DuelOutcome outcome,
            int blockIndex,
            int opIndex,
            EffectOpDef op,
            out DuelEffectCommand command,
            out string warningMessage)
        {
            command = null;
            warningMessage = string.Empty;

            if (op == null)
            {
                warningMessage = "op is null.";
                return false;
            }

            if (!Enum.TryParse(op.op, false, out DuelEffectOpCode opCode))
            {
                warningMessage = $"unsupported op '{op.op}'.";
                return false;
            }

            command = new DuelEffectCommand
            {
                opCode = opCode,
                sourceId = $"Outcome:{clashDef.id}:{outcome}:{blockIndex}:{opIndex}",
                clashIndex = clashIndex
            };

            switch (opCode)
            {
                case DuelEffectOpCode.ModifyHealth:
                case DuelEffectOpCode.ModifyTotalAttack:
                    if (!TryClashResolveSide(op.side, out bool isPlayerSide))
                    {
                        warningMessage = $"invalid side '{op.side}' for op '{op.op}'.";
                        return false;
                    }

                    if (!op.TryGetAmount(out int amount))
                    {
                        warningMessage = $"missing amount for op '{op.op}'.";
                        return false;
                    }

                    command.isPlayerSide = isPlayerSide;
                    command.amount = amount;
                    return true;
                default:
                    warningMessage = $"op '{op.op}' is not allowed in clash outcomeEffects.";
                    return false;
            }
        }

        static bool TryClashResolveSide(string side, out bool isPlayerSide)
        {
            if (string.Equals(side, "Player", StringComparison.Ordinal))
            {
                isPlayerSide = true;
                return true;
            }

            if (string.Equals(side, "Opponent", StringComparison.Ordinal))
            {
                isPlayerSide = false;
                return true;
            }

            isPlayerSide = true;
            return false;
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

        static void CollectActionIds(HashSet<string> buffer, List<string> actionIds, string sourceLabel)
        {
            if (actionIds == null)
            {
                Debug.LogWarning($"[DuelTurnProcessor] Roll warning: {sourceLabel} is null.");
                return;
            }

            for (int i = 0; i < actionIds.Count; i++)
            {
                string actionId = actionIds[i];
                if (string.IsNullOrWhiteSpace(actionId))
                {
                    Debug.LogWarning($"[DuelTurnProcessor] Roll warning: empty actionId at {sourceLabel}[{i}].");
                    continue;
                }

                buffer.Add(actionId);
            }
        }
    }
}
