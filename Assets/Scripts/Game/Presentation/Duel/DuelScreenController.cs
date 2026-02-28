using System;
using Game.Application.Duel;
using Game.Domain.Duel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    [ExecuteAlways]
    public class DuelScreenController : MonoBehaviour
    {
        const string DefaultEnemyId = "enemy.northern_footman";

        [Header("Duel Data")]
        [SerializeField] string enemyId = DefaultEnemyId;

        [Header("Scene References")]
        [SerializeField] Image backgroundImage;
        [SerializeField] Image topBarImage;
        [SerializeField] TMP_Text turnText;
        [SerializeField] TMP_Text enemyHealthText;
        [SerializeField] TMP_Text playerHealthText;
        [SerializeField] Button combatStartButton;
        [SerializeField] Button surrenderButton;
        [SerializeField] RectTransform enemyLoadoutRow;
        [SerializeField] RectTransform playerLoadoutRow;
        [SerializeField] RectTransform enemyPassiveRow;
        [SerializeField] RectTransform playerPassiveRow;
        [SerializeField] DuelCombatZoneView[] combatZones = Array.Empty<DuelCombatZoneView>();
        [SerializeField] DuelAbilityCardView abilityCardPrefab;
        [SerializeField] DuelAnimationConfig animationConfig;

        readonly DuelSessionRunner sessionRunner = new();
        readonly DuelSelectionState selectionState = new();
        readonly DuelScreenObservableState observableState = new();
        readonly DuelAbilityIconCache abilityIconCache = new();
        readonly DuelUiQueryService uiQueryService = new();
        readonly System.Random revealRandom = new();

        DuelScreenView view;
        DuelScreenViewWires viewWires;
        DuelScreenObservableBinder observableBinder;
        DuelScreenDragDropInput dragDropInput;
        DuelScreenFlowCoordinator flowCoordinator;

        bool IsFlowRunning =>
            flowCoordinator != null &&
            flowCoordinator.IsRunning;

        void Awake()
        {
            if (!ValidateCombatZonesSerialized("Awake"))
            {
                enabled = false;
                return;
            }

            RebuildView();
            view?.ApplyStaticVisuals();
            WireCallbacks();
            WireObservableBindings();
            InitializeDuelOrWarn();
            PublishObservableState();
            TryStartOpponentSetupFlow();
        }

        void OnEnable()
        {
            if (UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (!ValidateCombatZonesSerialized("OnEnable"))
            {
                return;
            }

            RebuildView();
            view?.ApplyStaticVisuals();
        }

        void OnValidate()
        {
            if (!ValidateCombatZonesSerialized("OnValidate"))
            {
                return;
            }

            RebuildView();
            view?.ApplyStaticVisuals();
        }

        void OnDestroy()
        {
            UnwireCallbacks();
            dragDropInput?.ClearDragState();
            observableBinder?.Dispose();
            abilityIconCache.Dispose();
        }

        void RebuildView()
        {
            viewWires ??= new DuelScreenViewWires();
            observableBinder ??= new DuelScreenObservableBinder(observableState);

            view = viewWires.CreateView(
                backgroundImage,
                topBarImage,
                turnText,
                enemyHealthText,
                playerHealthText,
                combatStartButton,
                surrenderButton,
                enemyLoadoutRow,
                playerLoadoutRow,
                enemyPassiveRow,
                playerPassiveRow,
                combatZones,
                abilityCardPrefab,
                uiQueryService,
                abilityIconCache.ResolveOrDefault);

            flowCoordinator = new DuelScreenFlowCoordinator(
                () => view,
                ResolveCanvas,
                combatZones,
                animationConfig,
                sessionRunner,
                selectionState,
                uiQueryService,
                observableBinder,
                revealRandom);

            dragDropInput = new DuelScreenDragDropInput(
                ResolveCanvas,
                combatZones,
                sessionRunner,
                selectionState,
                uiQueryService,
                () => IsFlowRunning,
                () => PublishObservableState());
        }

        bool ValidateCombatZonesSerialized(string stage)
        {
            viewWires ??= new DuelScreenViewWires();
            return viewWires.ValidateCombatZonesSerialized(combatZones, abilityCardPrefab, stage, this);
        }

        void WireCallbacks()
        {
            viewWires?.WireCallbacks(
                combatStartButton,
                HandleCombatStartClicked,
                surrenderButton,
                HandleSurrenderClicked,
                view,
                HandleCombatZoneClicked);
        }

        void UnwireCallbacks()
        {
            viewWires?.UnwireCallbacks(
                combatStartButton,
                HandleCombatStartClicked,
                surrenderButton,
                HandleSurrenderClicked,
                view);
        }

        void WireObservableBindings()
        {
            observableBinder ??= new DuelScreenObservableBinder(observableState);
            observableBinder.Wire(view, HandleBoardStateChanged, HandleRevealStateChanged);
        }

        void InitializeDuelOrWarn()
        {
            if (!uiQueryService.TryBindRuntimeData(out string bindFailure))
            {
                Debug.LogWarning($"[DuelScreenController] Failed to bind duel query service: {bindFailure}");
                return;
            }

            abilityIconCache.Rebuild(uiQueryService);

            if (!sessionRunner.TryInitialize(uiQueryService, enemyId, advanceToPlayerSetup: false, out string failureMessage))
            {
                Debug.LogWarning($"[DuelScreenController] Failed to initialize duel: {failureMessage}");
                return;
            }

            selectionState.ClearAll();
        }

        void HandleBoardStateChanged(DuelBoardState boardState)
        {
            view?.RenderBoard(
                boardState,
                HandlePlayerAbilityCardClicked,
                HandleCardDragStarted,
                HandleCardDragMoved,
                HandleCardDragEnded,
                HandleCardRightClicked);
        }

        void HandleRevealStateChanged(DuelRevealState revealState)
        {
            view?.RenderReveal(revealState);
        }

        void PublishObservableState(bool publishBoard = true)
        {
            observableBinder?.Publish(sessionRunner, selectionState, IsFlowRunning, publishBoard);
        }

        void TryStartOpponentSetupFlow()
        {
            flowCoordinator?.TryStartOpponentSetupFlow(this);
        }

        void HandleCombatStartClicked()
        {
            if (IsFlowRunning || flowCoordinator == null)
            {
                return;
            }

            dragDropInput?.ClearDragState();
            StartCoroutine(flowCoordinator.RunCombatStartFlow());
        }

        void HandleSurrenderClicked()
        {
            if (IsFlowRunning || !sessionRunner.IsInitialized)
            {
                return;
            }

            if (!sessionRunner.TrySurrender(out string failureMessage))
            {
                Debug.LogWarning($"[DuelScreenController] Surrender rejected: {failureMessage}");
                return;
            }

            selectionState.ClearAbility();
            PublishObservableState();
        }

        void HandlePlayerAbilityCardClicked(string abilityInstanceId)
        {
            if (IsFlowRunning ||
                !sessionRunner.IsInitialized ||
                (dragDropInput != null && dragDropInput.IsCardDragActive))
            {
                return;
            }

            if (!selectionState.TryToggleAttackSelection(
                    sessionRunner.DuelState,
                    sessionRunner.PhaseRunner,
                    uiQueryService,
                    abilityInstanceId,
                    out _))
            {
                return;
            }

            PublishObservableState();
        }

        void HandleCardDragStarted(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            dragDropInput?.HandleCardDragStarted(
                cardView,
                abilityInstanceId,
                context,
                screenPosition,
                eventCamera);
        }

        void HandleCardDragMoved(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            dragDropInput?.HandleCardDragMoved(
                cardView,
                abilityInstanceId,
                context,
                screenPosition,
                eventCamera);
        }

        void HandleCardDragEnded(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            dragDropInput?.HandleCardDragEnded(
                cardView,
                abilityInstanceId,
                context,
                screenPosition,
                eventCamera);
        }

        void HandleCardRightClicked(
            DuelAbilityCardView cardView,
            string abilityInstanceId,
            DuelAbilityCardView.InteractionContext context)
        {
            dragDropInput?.HandleCardRightClicked(cardView, abilityInstanceId, context);
        }

        void HandleCombatZoneClicked(int combatIndex)
        {
            if (IsFlowRunning ||
                !sessionRunner.IsInitialized ||
                sessionRunner.PhaseRunner.currentPhase != DuelPhase.PlayerSetup ||
                string.IsNullOrWhiteSpace(selectionState.SelectedAbilityInstanceId))
            {
                return;
            }

            if (!selectionState.TryMovePlayerAbilityToCombat(
                    sessionRunner.DuelState,
                    sessionRunner.PhaseRunner,
                    selectionState.SelectedAbilityInstanceId,
                    combatIndex,
                    out string failureMessage))
            {
                Debug.LogWarning($"[DuelScreenController] Ability move rejected: {failureMessage}");
                return;
            }

            sessionRunner.NotifyPlayerAbilityDeployed(selectionState.SelectedAbilityInstanceId);
            PublishObservableState();
        }

        Canvas ResolveCanvas()
        {
            return backgroundImage == null
                ? GetComponentInParent<Canvas>()
                : backgroundImage.canvas;
        }
    }
}
