using System;
using Game.Domain.Battle;

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
    }
}
