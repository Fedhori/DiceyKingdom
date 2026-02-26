using System;
using System.Collections.Generic;
using Game.Application.Duel;
using Game.Common;
using Game.Domain.Duel;
using Game.Infrastructure.Data;

namespace Game.Presentation.Duel
{
    public readonly struct DuelTopBarState
    {
        public int turnIndex { get; }

        public DuelTopBarState(int turnIndex)
        {
            this.turnIndex = turnIndex;
        }
    }

    public readonly struct DuelHealthState
    {
        public int playerHealth { get; }
        public int maxPlayerHealth { get; }
        public int opponentHealth { get; }
        public int maxOpponentHealth { get; }

        public DuelHealthState(
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

    public readonly struct DuelButtonState
    {
        public bool canCombatStart { get; }
        public bool canSurrender { get; }

        public DuelButtonState(bool canCombatStart, bool canSurrender)
        {
            this.canCombatStart = canCombatStart;
            this.canSurrender = canSurrender;
        }
    }

    public readonly struct DuelBoardState
    {
        public DuelState duelState { get; }
        public DuelPhaseRunner phaseRunner { get; }
        public GameDatabase database { get; }
        public string selectedAbilityId { get; }
        public bool isFlowRunning { get; }
        public int revision { get; }

        public DuelBoardState(
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

    public readonly struct DuelRollOverlayValue
    {
        public bool isVisible { get; }
        public int value { get; }
        public bool isFinal { get; }

        public DuelRollOverlayValue(bool isVisible, int value, bool isFinal)
        {
            this.isVisible = isVisible;
            this.value = value;
            this.isFinal = isFinal;
        }
    }

    public readonly struct DuelRevealState
    {
        readonly int[] opponentTotals;
        readonly int[] playerTotals;
        readonly IReadOnlyDictionary<string, DuelRollOverlayValue> rollOverlayLookup;

        public bool isRunning { get; }
        public int displayOpponentHealth { get; }
        public int displayPlayerHealth { get; }
        public int revision { get; }

        public static DuelRevealState Empty =>
            new(
                false,
                Array.Empty<int>(),
                Array.Empty<int>(),
                -1,
                -1,
                null,
                0);

        public DuelRevealState(
            bool isRunning,
            int[] opponentTotals,
            int[] playerTotals,
            int displayOpponentHealth,
            int displayPlayerHealth,
            IReadOnlyDictionary<string, DuelRollOverlayValue> rollOverlayByAbilityId,
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
                ? new Dictionary<string, DuelRollOverlayValue>(StringComparer.Ordinal)
                : new Dictionary<string, DuelRollOverlayValue>(rollOverlayByAbilityId, StringComparer.Ordinal);
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

        public bool TryGetOverlay(string abilityId, out DuelRollOverlayValue overlay)
        {
            overlay = default;
            if (string.IsNullOrWhiteSpace(abilityId) || rollOverlayLookup == null)
            {
                return false;
            }

            return rollOverlayLookup.TryGetValue(abilityId, out overlay);
        }
    }

    public class DuelScreenObservableState
    {
        readonly ObservableValue<DuelTopBarState> topBarState = new(new DuelTopBarState(0));
        readonly ObservableValue<DuelHealthState> healthState = new(new DuelHealthState(0, 1, 0, 1));
        readonly ObservableValue<DuelButtonState> buttonState = new(new DuelButtonState(false, false));
        readonly ObservableValue<DuelBoardState> boardState = new(
            new DuelBoardState(
                null,
                null,
                null,
                string.Empty,
                false,
                0));
        readonly ObservableValue<DuelRevealState> revealState = new(DuelRevealState.Empty);

        int boardRevision;
        int revealRevision;

        public IReadOnlyObservableValue<DuelTopBarState> TopBarState => topBarState;
        public IReadOnlyObservableValue<DuelHealthState> HealthState => healthState;
        public IReadOnlyObservableValue<DuelButtonState> ButtonState => buttonState;
        public IReadOnlyObservableValue<DuelBoardState> BoardState => boardState;
        public IReadOnlyObservableValue<DuelRevealState> RevealState => revealState;

        public void Publish(
            DuelSessionRunner sessionRunner,
            DuelSelectionState selectionState,
            bool isFlowRunning,
            bool publishBoard = true)
        {
            DuelState duelState = sessionRunner == null ? null : sessionRunner.DuelState;
            DuelPhaseRunner phaseRunner = sessionRunner == null ? null : sessionRunner.PhaseRunner;
            GameDatabase database = sessionRunner == null ? null : sessionRunner.Database;

            int turnIndex = duelState == null ? 0 : duelState.turnIndex;
            topBarState.Value = new DuelTopBarState(turnIndex);

            int maxPlayerHealth = sessionRunner == null ? 1 : sessionRunner.MaxPlayerHealth;
            int maxOpponentHealth = sessionRunner == null ? 1 : sessionRunner.MaxOpponentHealth;
            int playerHealth = duelState == null ? 0 : duelState.playerHealth;
            int opponentHealth = duelState == null ? 0 : duelState.opponentHealth;
            healthState.Value = new DuelHealthState(
                playerHealth,
                maxPlayerHealth,
                opponentHealth,
                maxOpponentHealth);

            bool canCombatStart = !isFlowRunning &&
                duelState != null &&
                phaseRunner != null &&
                !duelState.isDuelEnded &&
                phaseRunner.currentPhase == DuelPhase.PlayerSetup;
            bool canSurrender = !isFlowRunning &&
                duelState != null &&
                phaseRunner != null &&
                !duelState.isDuelEnded &&
                phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                duelState.honor > 0;
            buttonState.Value = new DuelButtonState(canCombatStart, canSurrender);

            if (!publishBoard)
            {
                return;
            }

            boardRevision += 1;
            boardState.Value = new DuelBoardState(
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
            IReadOnlyDictionary<string, DuelRollOverlayValue> rollOverlayByAbilityId)
        {
            revealRevision += 1;
            revealState.Value = new DuelRevealState(
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
            revealState.Value = new DuelRevealState(
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


