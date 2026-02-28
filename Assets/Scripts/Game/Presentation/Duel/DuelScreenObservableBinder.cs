using System;
using System.Collections.Generic;

namespace Game.Presentation.Duel
{
    public sealed class DuelScreenObservableBinder : IDisposable
    {
        readonly DuelScreenObservableState observableState;
        readonly List<IDisposable> subscriptions = new();

        public DuelScreenObservableBinder(DuelScreenObservableState observableState)
        {
            this.observableState = observableState ?? throw new ArgumentNullException(nameof(observableState));
        }

        public void Wire(
            DuelScreenView view,
            Action<DuelBoardState> onBoardStateChanged,
            Action<DuelRevealState> onRevealStateChanged)
        {
            Unwire();

            if (view == null)
            {
                return;
            }

            subscriptions.Add(observableState.TopBarState.Subscribe(view.RenderTopBar));
            subscriptions.Add(observableState.HealthState.Subscribe(view.RenderHealth));
            subscriptions.Add(observableState.ButtonState.Subscribe(view.RenderButtons));
            subscriptions.Add(observableState.BoardState.Subscribe(onBoardStateChanged));
            subscriptions.Add(observableState.RevealState.Subscribe(onRevealStateChanged));
        }

        public void Publish(
            DuelSessionRunner sessionRunner,
            DuelSelectionState selectionState,
            bool isFlowRunning,
            bool publishBoard = true)
        {
            observableState.Publish(sessionRunner, selectionState, isFlowRunning, publishBoard);
        }

        public void PublishReveal(
            bool isRunning,
            int[] opponentTotals,
            int[] playerTotals,
            IReadOnlyDictionary<string, DuelRollOverlayValue> rollOverlayByAbilityId,
            IReadOnlyDictionary<string, int> powerBadgeByAbilityId)
        {
            observableState.PublishReveal(
                isRunning,
                opponentTotals,
                playerTotals,
                rollOverlayByAbilityId,
                powerBadgeByAbilityId);
        }

        public void ClearReveal()
        {
            observableState.ClearReveal();
        }

        public void Unwire()
        {
            for (int i = 0; i < subscriptions.Count; i++)
            {
                subscriptions[i]?.Dispose();
            }

            subscriptions.Clear();
        }

        public void Dispose()
        {
            Unwire();
        }
    }
}
