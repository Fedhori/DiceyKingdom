using System;
using System.Collections.Generic;
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

    public readonly struct BattleRollOverlayValue
    {
        public bool isVisible { get; }
        public int value { get; }
        public bool isFinal { get; }

        public BattleRollOverlayValue(bool isVisible, int value, bool isFinal)
        {
            this.isVisible = isVisible;
            this.value = value;
            this.isFinal = isFinal;
        }
    }

    public readonly struct BattleRevealState
    {
        readonly int[] opponentTotals;
        readonly int[] playerTotals;
        readonly IReadOnlyDictionary<string, BattleRollOverlayValue> rollOverlayLookup;

        public bool isRunning { get; }
        public int displayOpponentHealth { get; }
        public int displayPlayerHealth { get; }
        public int revision { get; }

        public static BattleRevealState Empty =>
            new(
                false,
                Array.Empty<int>(),
                Array.Empty<int>(),
                -1,
                -1,
                null,
                0);

        public BattleRevealState(
            bool isRunning,
            int[] opponentTotals,
            int[] playerTotals,
            int displayOpponentHealth,
            int displayPlayerHealth,
            IReadOnlyDictionary<string, BattleRollOverlayValue> rollOverlayByAbilityId,
            int revision)
        {
            this.isRunning = isRunning;
            this.opponentTotals = opponentTotals == null
                ? Array.Empty<int>()
                : (int[])opponentTotals.Clone();
            this.playerTotals = playerTotals == null
                ? Array.Empty<int>()
                : (int[])playerTotals.Clone();
            this.displayOpponentHealth = displayOpponentHealth;
            this.displayPlayerHealth = displayPlayerHealth;
            rollOverlayLookup = rollOverlayByAbilityId == null
                ? new Dictionary<string, BattleRollOverlayValue>(StringComparer.Ordinal)
                : new Dictionary<string, BattleRollOverlayValue>(rollOverlayByAbilityId, StringComparer.Ordinal);
            this.revision = revision;
        }

        public bool TryGetZoneTotals(int combatIndex, out int opponentTotal, out int playerTotal)
        {
            opponentTotal = 0;
            playerTotal = 0;

            if (combatIndex < 0 ||
                combatIndex >= opponentTotals.Length ||
                combatIndex >= playerTotals.Length)
            {
                return false;
            }

            opponentTotal = opponentTotals[combatIndex];
            playerTotal = playerTotals[combatIndex];
            return true;
        }

        public bool TryGetOverlay(string abilityId, out BattleRollOverlayValue overlay)
        {
            overlay = default;
            if (string.IsNullOrWhiteSpace(abilityId) || rollOverlayLookup == null)
            {
                return false;
            }

            return rollOverlayLookup.TryGetValue(abilityId, out overlay);
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
        readonly ObservableValue<BattleRevealState> revealState = new(BattleRevealState.Empty);

        int boardRevision;
        int revealRevision;

        public IReadOnlyObservableValue<BattleTopBarState> TopBarState => topBarState;
        public IReadOnlyObservableValue<BattleHealthState> HealthState => healthState;
        public IReadOnlyObservableValue<BattleButtonState> ButtonState => buttonState;
        public IReadOnlyObservableValue<BattleBoardState> BoardState => boardState;
        public IReadOnlyObservableValue<BattleRevealState> RevealState => revealState;

        public void Publish(
            BattleSessionRunner sessionRunner,
            BattleSelectionState selectionState,
            bool isFlowRunning,
            bool publishBoard = true)
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

            if (!publishBoard)
            {
                return;
            }

            boardRevision += 1;
            boardState.Value = new BattleBoardState(
                duelState,
                phaseRunner,
                database,
                selectionState == null ? string.Empty : selectionState.SelectedAbilityId,
                isFlowRunning,
                boardRevision);
        }

        public void PublishReveal(
            bool isRunning,
            int[] opponentTotals,
            int[] playerTotals,
            int displayOpponentHealth,
            int displayPlayerHealth,
            IReadOnlyDictionary<string, BattleRollOverlayValue> rollOverlayByAbilityId)
        {
            revealRevision += 1;
            revealState.Value = new BattleRevealState(
                isRunning,
                opponentTotals,
                playerTotals,
                displayOpponentHealth,
                displayPlayerHealth,
                rollOverlayByAbilityId,
                revealRevision);
        }

        public void ClearReveal()
        {
            revealRevision += 1;
            revealState.Value = new BattleRevealState(
                false,
                Array.Empty<int>(),
                Array.Empty<int>(),
                -1,
                -1,
                null,
                revealRevision);
        }
    }
}
