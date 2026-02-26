using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Presentation.Battle
{
    public sealed class BattleSessionRunner
    {
        DuelSessionBuilder sessionBuilder;
        DuelTurnProcessor turnProcessor;

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

            MaxPlayerHealth = Mathf.Max(1, DuelState.playerHealth);
            MaxOpponentHealth = Mathf.Max(1, DuelState.opponentHealth);

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

            if (PhaseRunner.currentPhase == DuelPhase.Reset)
            {
                if (!PhaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter OpponentSetup ({PhaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (PhaseRunner.currentPhase == DuelPhase.OpponentSetup)
            {
                OpponentSetupBuildResult deployResult = sessionBuilder.AutoDeployOpponentCombat(DuelState);
                if (deployResult.skippedCount > 0)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[DuelSessionRunner] Opponent deploy skipped abilities: {deployResult.skippedCount}");
                }

                if (!PhaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter PlayerSetup ({PhaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (PhaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"phase is {PhaseRunner.currentPhase}, expected {DuelPhase.PlayerSetup}.";
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

        public bool TryResolve(out DuelCombatResolveResult result, out string failureMessage)
        {
            result = new DuelCombatResolveResult(System.Array.Empty<DuelCombatResolveStepResult>(), default, 0);
            failureMessage = string.Empty;

            if (!TryValidateStarted(out failureMessage))
            {
                return false;
            }

            return turnProcessor.TryResolveAllCombats(
                DuelState,
                PhaseRunner,
                out result,
                out failureMessage);
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
                return true;
            }

            failureMessage = $"surrender rejected ({PhaseRunner.LastFailureReason}).";
            return false;
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
    }
}
