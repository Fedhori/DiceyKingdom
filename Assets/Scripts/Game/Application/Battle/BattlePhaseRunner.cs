using System;
using Game.Domain.Battle;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Application.Battle
{
    public sealed class BattlePhaseRunner
    {
        readonly BattleState state;

        public BattlePhase currentPhase { get; private set; } = BattlePhase.Recall;
        public bool isStarted { get; private set; }
        public BattlePhaseFailureReason LastFailureReason { get; private set; } = BattlePhaseFailureReason.None;

        public BattlePhaseRunner(BattleState battleState)
        {
            state = battleState ?? throw new ArgumentNullException(nameof(battleState));
            state.EnsureInitialized();
        }

        public bool StartBattle()
        {
            if (state.isBattleEnded)
            {
                LastFailureReason = BattlePhaseFailureReason.AlreadyEnded;
                Debug.LogWarning("[BattlePhaseRunner] StartBattle rejected: battle already ended.");
                return false;
            }

            isStarted = true;
            currentPhase = BattlePhase.Recall;
            LastFailureReason = BattlePhaseFailureReason.None;
            return true;
        }

        public bool AdvanceToNextPhase()
        {
            if (!isStarted)
            {
                LastFailureReason = BattlePhaseFailureReason.NotStarted;
                Debug.LogWarning("[BattlePhaseRunner] AdvanceToNextPhase rejected: battle is not started.");
                return false;
            }

            if (state.isBattleEnded)
            {
                LastFailureReason = BattlePhaseFailureReason.AlreadyEnded;
                Debug.LogWarning("[BattlePhaseRunner] AdvanceToNextPhase rejected: battle already ended.");
                return false;
            }

            switch (currentPhase)
            {
                case BattlePhase.Recall:
                    currentPhase = BattlePhase.EnemyDeploy;
                    break;
                case BattlePhase.EnemyDeploy:
                    currentPhase = BattlePhase.PlayerDeploy;
                    break;
                case BattlePhase.PlayerDeploy:
                    currentPhase = BattlePhase.Roll;
                    break;
                case BattlePhase.Roll:
                    currentPhase = BattlePhase.Tactics;
                    break;
                case BattlePhase.Tactics:
                    currentPhase = BattlePhase.Resolve;
                    break;
                case BattlePhase.Resolve:
                    state.turnIndex += 1;
                    currentPhase = BattlePhase.Recall;
                    break;
                default:
                    LastFailureReason = BattlePhaseFailureReason.InvalidPhase;
                    Debug.LogWarning("[BattlePhaseRunner] AdvanceToNextPhase rejected: current phase is invalid.");
                    return false;
            }

            LastFailureReason = BattlePhaseFailureReason.None;
            return true;
        }

        public bool TryRetreat()
        {
            if (!isStarted)
            {
                LastFailureReason = BattlePhaseFailureReason.NotStarted;
                Debug.LogWarning("[BattlePhaseRunner] TryRetreat rejected: battle is not started.");
                return false;
            }

            if (state.isBattleEnded)
            {
                LastFailureReason = BattlePhaseFailureReason.AlreadyEnded;
                Debug.LogWarning("[BattlePhaseRunner] TryRetreat rejected: battle already ended.");
                return false;
            }

            if (currentPhase != BattlePhase.PlayerDeploy)
            {
                LastFailureReason = BattlePhaseFailureReason.InvalidPhase;
                Debug.LogWarning(
                    $"[BattlePhaseRunner] TryRetreat rejected: current phase is {currentPhase}, required phase is {BattlePhase.PlayerDeploy}.");
                return false;
            }

            if (state.stability <= 0)
            {
                LastFailureReason = BattlePhaseFailureReason.StabilityInsufficient;
                Debug.LogWarning("[BattlePhaseRunner] TryRetreat rejected: stability is not greater than zero.");
                return false;
            }

            state.stability -= 1;
            if (state.stability < 0)
            {
                state.stability = 0;
            }

            state.isBattleEnded = true;
            BattleSimulator.ClearModifierLayer(state, ModifierLayer.Battle);
            LastFailureReason = BattlePhaseFailureReason.None;
            return true;
        }
    }
}
