using Game.Application.Duel;
using Game.Common;
using Game.Domain.Duel;
using Game.Infrastructure.Data;

namespace Game.Presentation.Battle
{
    public readonly struct BattleTopBarState
    {
        public int turnIndex { get; }

        public BattleTopBarState(int turnIndex)
        {
            this.turnIndex = turnIndex;
        }
    }

    public readonly struct BattleHealthState
    {
        public int playerHealth { get; }
        public int maxPlayerHealth { get; }
        public int opponentHealth { get; }
        public int maxOpponentHealth { get; }

        public BattleHealthState(
            int playerHealth,
            int maxPlayerHealth,
            int opponentHealth,
            int maxOpponentHealth)
        {
            this.playerHealth = playerHealth;
            this.maxPlayerHealth = maxPlayerHealth;
            this.opponentHealth = opponentHealth;
            this.maxOpponentHealth = maxOpponentHealth;
        }
    }

    public readonly struct BattleButtonState
    {
        public bool canCombatStart { get; }
        public bool canSurrender { get; }

        public BattleButtonState(bool canCombatStart, bool canSurrender)
        {
            this.canCombatStart = canCombatStart;
            this.canSurrender = canSurrender;
        }
    }

    public readonly struct BattleBoardState
    {
        public DuelState duelState { get; }
        public DuelPhaseRunner phaseRunner { get; }
        public GameDatabase database { get; }
        public string selectedAbilityId { get; }
        public bool isFlowRunning { get; }
        public int revision { get; }

        public BattleBoardState(
            DuelState duelState,
            DuelPhaseRunner phaseRunner,
            GameDatabase database,
            string selectedAbilityId,
            bool isFlowRunning,
            int revision)
        {
            this.duelState = duelState;
            this.phaseRunner = phaseRunner;
            this.database = database;
            this.selectedAbilityId = selectedAbilityId;
            this.isFlowRunning = isFlowRunning;
            this.revision = revision;
        }
    }

    public sealed class BattleScreenObservableState
    {
        readonly ObservableValue<BattleTopBarState> topBarState = new(new BattleTopBarState(0));
        readonly ObservableValue<BattleHealthState> healthState = new(new BattleHealthState(0, 1, 0, 1));
        readonly ObservableValue<BattleButtonState> buttonState = new(new BattleButtonState(false, false));
        readonly ObservableValue<BattleBoardState> boardState = new(
            new BattleBoardState(
                null,
                null,
                null,
                string.Empty,
                false,
                0));

        int boardRevision;

        public IReadOnlyObservableValue<BattleTopBarState> TopBarState => topBarState;
        public IReadOnlyObservableValue<BattleHealthState> HealthState => healthState;
        public IReadOnlyObservableValue<BattleButtonState> ButtonState => buttonState;
        public IReadOnlyObservableValue<BattleBoardState> BoardState => boardState;

        public void Publish(BattleSessionRunner sessionRunner, BattleSelectionState selectionState, bool isFlowRunning)
        {
            DuelState duelState = sessionRunner == null ? null : sessionRunner.DuelState;
            DuelPhaseRunner phaseRunner = sessionRunner == null ? null : sessionRunner.PhaseRunner;
            GameDatabase database = sessionRunner == null ? null : sessionRunner.Database;

            int turnIndex = duelState == null ? 0 : duelState.turnIndex;
            topBarState.Value = new BattleTopBarState(turnIndex);

            int maxPlayerHealth = sessionRunner == null ? 1 : sessionRunner.MaxPlayerHealth;
            int maxOpponentHealth = sessionRunner == null ? 1 : sessionRunner.MaxOpponentHealth;
            int playerHealth = duelState == null ? 0 : duelState.playerHealth;
            int opponentHealth = duelState == null ? 0 : duelState.opponentHealth;
            healthState.Value = new BattleHealthState(
                playerHealth,
                maxPlayerHealth,
                opponentHealth,
                maxOpponentHealth);

            bool canCombatStart = !isFlowRunning &&
                duelState != null &&
                phaseRunner != null &&
                !duelState.isDuelEnded;
            bool canSurrender = !isFlowRunning &&
                duelState != null &&
                phaseRunner != null &&
                !duelState.isDuelEnded &&
                phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                duelState.honor > 0;
            buttonState.Value = new BattleButtonState(canCombatStart, canSurrender);

            boardRevision += 1;
            boardState.Value = new BattleBoardState(
                duelState,
                phaseRunner,
                database,
                selectionState == null ? string.Empty : selectionState.SelectedAbilityId,
                isFlowRunning,
                boardRevision);
        }
    }
}
