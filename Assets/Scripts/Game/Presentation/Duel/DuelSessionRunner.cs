using System.Collections.Generic;
using Game.Application.Duel;
using Game.Application.Duel.Effects;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using Game.Infrastructure.Data.Effects;
using UnityEngine;

namespace Game.Presentation.Duel
{
    public class DuelSessionRunner
    {
        DuelSessionBuilder sessionBuilder;
        DuelTurnProcessor turnProcessor;
        DuelResolveSession activeResolveSession;
        readonly List<string> singleAbilityBuffer = new(1);

        public DuelState DuelState { get; private set; }
        public DuelPhaseRunner PhaseRunner { get; private set; }
        public GameDatabase Database { get; private set; }
        public int MaxPlayerHealth { get; private set; } = 1;
        public int MaxOpponentHealth { get; private set; } = 1;

        public bool IsInitialized =>
            DuelState != null &&
            PhaseRunner != null &&
            sessionBuilder != null &&
            turnProcessor != null;

        public bool TryInitialize(
            GameDatabase database,
            string enemyId,
            bool advanceToPlayerSetup,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (database == null)
            {
                failureMessage = "database is null.";
                return false;
            }

            Database = database;
            sessionBuilder = new DuelSessionBuilder(database);
            turnProcessor = new DuelTurnProcessor(database);
            activeResolveSession = null;

            if (!sessionBuilder.TryCreateInitialState(enemyId, out DuelState state, out failureMessage))
            {
                return false;
            }

            DuelState = state;
            PhaseRunner = new DuelPhaseRunner(state);

            if (!PhaseRunner.StartDuel())
            {
                failureMessage = $"failed to start duel ({PhaseRunner.LastFailureReason}).";
                return false;
            }

            MaxPlayerHealth = Mathf.Max(1, DuelState.maxPlayerHealth);
            MaxOpponentHealth = Mathf.Max(1, DuelState.maxOpponentHealth);
            turnProcessor.ApplyTimedEffects(DuelState, DuelEffectTiming.DuelStart);

            if (!advanceToPlayerSetup)
            {
                return true;
            }

            return TryAdvanceToPlayerSetupForCurrentTurn(out failureMessage);
        }

        public bool TryAutoDeployOpponent(out OpponentSetupBuildResult deployResult, out string failureMessage)
        {
            deployResult = new OpponentSetupBuildResult(0, 0);
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            deployResult = sessionBuilder.AutoDeployOpponentCombat(DuelState);
            ApplyDeployTimedEffects(deployResult.deployedAbilityIds);
            return true;
        }

        public bool TryEnterOpponentSetup(out OpponentSetupBuildResult deployResult, out string failureMessage)
        {
            deployResult = new OpponentSetupBuildResult(0, 0);
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (PhaseRunner.currentPhase != DuelPhase.Reset)
            {
                failureMessage = $"current phase is {PhaseRunner.currentPhase}, required phase is {DuelPhase.Reset}.";
                return false;
            }

            if (!PhaseRunner.AdvanceToNextPhase())
            {
                failureMessage = $"failed to enter OpponentSetup ({PhaseRunner.LastFailureReason}).";
                return false;
            }

            deployResult = sessionBuilder.AutoDeployOpponentCombat(DuelState);
            ApplyDeployTimedEffects(deployResult.deployedAbilityIds);
            return true;
        }

        public bool TryPrepareOpponentSetupForCurrentTurn(
            out OpponentSetupBuildResult deployPlan,
            out string failureMessage)
        {
            deployPlan = new OpponentSetupBuildResult(0, 0);
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (DuelState.isDuelEnded)
            {
                failureMessage = "duel already ended.";
                return false;
            }

            if (PhaseRunner.currentPhase == DuelPhase.Reset)
            {
                if (!PhaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter OpponentSetup ({PhaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (PhaseRunner.currentPhase != DuelPhase.OpponentSetup)
            {
                failureMessage = $"current phase is {PhaseRunner.currentPhase}, required phase is {DuelPhase.OpponentSetup}.";
                return false;
            }

            deployPlan = sessionBuilder.BuildOpponentDeployPlan(DuelState);
            return true;
        }

        public bool TryApplyOpponentDeployStep(
            DuelOpponentDeployStep step,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (PhaseRunner.currentPhase != DuelPhase.OpponentSetup)
            {
                failureMessage = $"current phase is {PhaseRunner.currentPhase}, required phase is {DuelPhase.OpponentSetup}.";
                return false;
            }

            if (!sessionBuilder.TryApplyOpponentDeployStep(DuelState, step, out failureMessage))
            {
                return false;
            }

            ApplyDeployTimedEffectsForSingleAbility(step.abilityId);
            return true;
        }

        public bool TryEnterPlayerSetup(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (PhaseRunner.currentPhase != DuelPhase.OpponentSetup)
            {
                failureMessage = $"current phase is {PhaseRunner.currentPhase}, required phase is {DuelPhase.OpponentSetup}.";
                return false;
            }

            if (!PhaseRunner.AdvanceToNextPhase())
            {
                failureMessage = $"failed to enter PlayerSetup ({PhaseRunner.LastFailureReason}).";
                return false;
            }

            return true;
        }

        public bool TryValidatePlayerSetupForCombatStart(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (DuelState.isDuelEnded)
            {
                failureMessage = "duel already ended.";
                return false;
            }

            if (PhaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"current phase is {PhaseRunner.currentPhase}, required phase is {DuelPhase.PlayerSetup}.";
                return false;
            }

            return true;
        }

        public bool TryEnsureReadyForCombatStart(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (DuelState.isDuelEnded)
            {
                failureMessage = "duel already ended.";
                return false;
            }

            if (PhaseRunner.currentPhase == DuelPhase.Reset ||
                PhaseRunner.currentPhase == DuelPhase.OpponentSetup)
            {
                return TryAdvanceToPlayerSetupForCurrentTurn(out failureMessage);
            }

            if (PhaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"current phase is {PhaseRunner.currentPhase}, required phase is {DuelPhase.PlayerSetup}.";
                return false;
            }

            return true;
        }

        public bool TryAdvanceToPlayerSetupForCurrentTurn(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (PhaseRunner.currentPhase == DuelPhase.PlayerSetup)
            {
                return true;
            }

            if (!TryPrepareOpponentSetupForCurrentTurn(out OpponentSetupBuildResult deployPlan, out failureMessage))
            {
                return false;
            }

            for (int i = 0; i < deployPlan.steps.Count; i++)
            {
                if (!TryApplyOpponentDeployStep(deployPlan.steps[i], out string applyFailure))
                {
                    failureMessage = $"failed to apply opponent deploy step[{i}] ({applyFailure}).";
                    return false;
                }
            }

            if (deployPlan.skippedCount > 0)
            {
                Debug.LogWarning($"[DuelSessionRunner] Opponent deploy skipped abilities: {deployPlan.skippedCount}");
            }

            if (!TryEnterPlayerSetup(out failureMessage))
            {
                return false;
            }

            return true;
        }

        public bool TryRoll(out DuelRollResult result, out string failureMessage)
        {
            result = new DuelRollResult(0, default);
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            return turnProcessor.TryRollAllDeployedAbilities(
                DuelState,
                PhaseRunner,
                out result,
                out failureMessage);
        }

        public bool TryBeginResolve(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (activeResolveSession != null)
            {
                failureMessage = "resolve session is already active.";
                return false;
            }

            bool success = turnProcessor.TryBeginResolve(
                DuelState,
                PhaseRunner,
                out DuelResolveSession session,
                out failureMessage);
            if (!success)
            {
                return false;
            }

            activeResolveSession = session;
            return true;
        }

        public bool TryResolveNextCombat(
            out DuelCombatResolveStepResult step,
            out bool hasRemainingCombats,
            out string failureMessage)
        {
            step = default;
            hasRemainingCombats = false;
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (activeResolveSession == null)
            {
                failureMessage = "resolve session is not active.";
                return false;
            }

            return turnProcessor.TryResolveNextCombat(
                DuelState,
                activeResolveSession,
                out step,
                out hasRemainingCombats,
                out failureMessage);
        }

        public bool TryFinalizeResolve(out DuelCombatResolveResult result, out string failureMessage)
        {
            result = new DuelCombatResolveResult(System.Array.Empty<DuelCombatResolveStepResult>(), default, 0);
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (activeResolveSession == null)
            {
                failureMessage = "resolve session is not active.";
                return false;
            }

            bool success = turnProcessor.TryFinalizeResolve(
                DuelState,
                PhaseRunner,
                activeResolveSession,
                out result,
                out failureMessage);
            if (!success)
            {
                activeResolveSession = null;
                return false;
            }

            activeResolveSession = null;
            return true;
        }

        public bool TrySurrender(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            if (PhaseRunner.TrySurrender())
            {
                activeResolveSession = null;
                return true;
            }

            failureMessage = $"surrender rejected ({PhaseRunner.LastFailureReason}).";
            return false;
        }

        public void NotifyPlayerAbilityDeployed(string abilityId)
        {
            if (!IsInitialized || string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            singleAbilityBuffer.Clear();
            singleAbilityBuffer.Add(abilityId);
            ApplyDeployTimedEffects(singleAbilityBuffer);
            singleAbilityBuffer.Clear();
        }

        public AbilityTimedEffectRunResult TriggerSkillTiming(IReadOnlyCollection<string> sourceAbilityIds = null)
        {
            if (!IsInitialized || DuelState.isDuelEnded)
            {
                return new AbilityTimedEffectRunResult(0, 0, 0);
            }

            return turnProcessor.ApplyTimedEffects(
                DuelState,
                DuelEffectTiming.Skill,
                sourceAbilityIds);
        }

        bool TryValidateStarted(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!IsInitialized)
            {
                failureMessage = "duel systems are not initialized.";
                return false;
            }

            if (!PhaseRunner.isStarted)
            {
                failureMessage = "phase runner is not started.";
                return false;
            }

            return true;
        }

        void ApplyDeployTimedEffectsForSingleAbility(string abilityId)
        {
            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            singleAbilityBuffer.Clear();
            singleAbilityBuffer.Add(abilityId);
            ApplyDeployTimedEffects(singleAbilityBuffer);
            singleAbilityBuffer.Clear();
        }

        void ApplyDeployTimedEffects(IReadOnlyCollection<string> deployedAbilityIds)
        {
            if (!IsInitialized || deployedAbilityIds == null || deployedAbilityIds.Count <= 0)
            {
                return;
            }

            turnProcessor.ApplyTimedEffects(
                DuelState,
                DuelEffectTiming.Deploy,
                deployedAbilityIds);
        }
    }
}
