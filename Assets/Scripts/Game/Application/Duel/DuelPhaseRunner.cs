using System;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using UnityEngine;

namespace Game.Application.Duel
{
    public sealed class DuelPhaseRunner
    {
        readonly DuelState state;

        public DuelPhase currentPhase { get; private set; } = DuelPhase.Reset;
        public bool isStarted { get; private set; }
        public DuelPhaseFailureReason LastFailureReason { get; private set; } = DuelPhaseFailureReason.None;

        public DuelPhaseRunner(DuelState duelState)
        {
            state = duelState ?? throw new ArgumentNullException(nameof(duelState));
            state.EnsureInitialized();
        }

        public bool StartDuel()
        {
            if (state.isDuelEnded)
            {
                LastFailureReason = DuelPhaseFailureReason.AlreadyEnded;
                Debug.LogWarning("[DuelPhaseRunner] StartDuel rejected: duel already ended.");
                return false;
            }

            isStarted = true;
            currentPhase = DuelPhase.Reset;
            LastFailureReason = DuelPhaseFailureReason.None;
            return true;
        }

        public bool AdvanceToNextPhase()
        {
            if (!isStarted)
            {
                LastFailureReason = DuelPhaseFailureReason.NotStarted;
                Debug.LogWarning("[DuelPhaseRunner] AdvanceToNextPhase rejected: duel is not started.");
                return false;
            }

            if (state.isDuelEnded)
            {
                LastFailureReason = DuelPhaseFailureReason.AlreadyEnded;
                Debug.LogWarning("[DuelPhaseRunner] AdvanceToNextPhase rejected: duel already ended.");
                return false;
            }

            switch (currentPhase)
            {
                case DuelPhase.Reset:
                    currentPhase = DuelPhase.OpponentSetup;
                    break;
                case DuelPhase.OpponentSetup:
                    currentPhase = DuelPhase.PlayerSetup;
                    break;
                case DuelPhase.PlayerSetup:
                    currentPhase = DuelPhase.Roll;
                    break;
                case DuelPhase.Roll:
                    currentPhase = DuelPhase.Resolve;
                    break;
                case DuelPhase.Resolve:
                    state.turnIndex += 1;
                    currentPhase = DuelPhase.Reset;
                    break;
                default:
                    LastFailureReason = DuelPhaseFailureReason.InvalidPhase;
                    Debug.LogWarning("[DuelPhaseRunner] AdvanceToNextPhase rejected: current phase is invalid.");
                    return false;
            }

            LastFailureReason = DuelPhaseFailureReason.None;
            return true;
        }

        public bool TrySurrender()
        {
            if (!isStarted)
            {
                LastFailureReason = DuelPhaseFailureReason.NotStarted;
                Debug.LogWarning("[DuelPhaseRunner] TrySurrender rejected: duel is not started.");
                return false;
            }

            if (state.isDuelEnded)
            {
                LastFailureReason = DuelPhaseFailureReason.AlreadyEnded;
                Debug.LogWarning("[DuelPhaseRunner] TrySurrender rejected: duel already ended.");
                return false;
            }

            if (currentPhase != DuelPhase.PlayerSetup)
            {
                LastFailureReason = DuelPhaseFailureReason.InvalidPhase;
                Debug.LogWarning(
                    $"[DuelPhaseRunner] TrySurrender rejected: current phase is {currentPhase}, required phase is {DuelPhase.PlayerSetup}.");
                return false;
            }

            if (state.honor <= 0)
            {
                LastFailureReason = DuelPhaseFailureReason.HonorInsufficient;
                Debug.LogWarning("[DuelPhaseRunner] TrySurrender rejected: honor is not greater than zero.");
                return false;
            }

            state.honor -= 1;
            if (state.honor < 0)
            {
                state.honor = 0;
            }

            state.isDuelEnded = true;
            DuelSimulator.ClearModifierLayer(state, ModifierLayer.Duel);
            LastFailureReason = DuelPhaseFailureReason.None;
            return true;
        }
    }
}
