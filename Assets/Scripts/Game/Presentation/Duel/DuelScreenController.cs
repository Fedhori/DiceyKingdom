using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Domain.Modifiers;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    [ExecuteAlways]
    public class DuelScreenController : MonoBehaviour
    {
        const string DefaultEnemyId = "enemy.northern_footman";
        const float dragGhostAlpha = 0.85f;
        const float defaultCardRollDuration = 0.5f;
        const float defaultOpponentDeployCardDuration = 0.25f;
        const float defaultOpponentDeployCardGap = 0.06f;
        const float opponentDeployGhostAlpha = 0.95f;

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
        readonly List<IDisposable> uiSubscriptions = new();
        readonly System.Random revealRandom = new();

        DuelScreenView view;
        bool isFlowRunning;
        DuelAnimationConfig runtimeAnimationConfig;
        bool isCardDragActive;
        string dragAbilityId = string.Empty;
        RectTransform dragGhostRect;
        DuelCombatZoneView hoveredDropZone;

        void Awake()
        {
            if (!ValidateCombatZonesSerialized("Awake"))
            {
                enabled = false;
                return;
            }

            RebuildView();
            view.ApplyStaticVisuals();
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
            view.ApplyStaticVisuals();
        }

        void OnValidate()
        {
            if (!ValidateCombatZonesSerialized("OnValidate"))
            {
                return;
            }

            RebuildView();
            view.ApplyStaticVisuals();
        }

        void OnDestroy()
        {
            if (combatStartButton != null)
            {
                combatStartButton.onClick.RemoveListener(HandleCombatStartClicked);
            }

            if (surrenderButton != null)
            {
                surrenderButton.onClick.RemoveListener(HandleSurrenderClicked);
            }

            ClearDragState();
            UnwireObservableBindings();
            view?.UnwireZoneCallbacks();
            abilityIconCache.Dispose();
        }

        void RebuildView()
        {
            view = new DuelScreenView(
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
                abilityIconCache.ResolveOrDefault);
        }

        bool ValidateCombatZonesSerialized(string stage)
        {
            bool hasValidZones = combatZones != null &&
                combatZones.Length == 3 &&
                combatZones.All(zone => zone != null);
            bool hasCardPrefab = abilityCardPrefab != null;
            if (hasValidZones && hasCardPrefab)
            {
                return true;
            }

            int length = combatZones == null ? 0 : combatZones.Length;
            Debug.LogError(
                $"[DuelScreenController] Invalid serialized references at {stage}. " +
                $"combatZonesLength={length}, combatZonesAllAssigned={hasValidZones}, abilityCardPrefabAssigned={hasCardPrefab}. " +
                "Auto-assignment is disabled.",
                this);
            return false;
        }

        void WireCallbacks()
        {
            if (combatStartButton != null)
            {
                combatStartButton.onClick.RemoveListener(HandleCombatStartClicked);
                combatStartButton.onClick.AddListener(HandleCombatStartClicked);
            }

            if (surrenderButton != null)
            {
                surrenderButton.onClick.RemoveListener(HandleSurrenderClicked);
                surrenderButton.onClick.AddListener(HandleSurrenderClicked);
            }

            view.WireZoneCallbacks(HandleCombatZoneClicked);
        }

        void InitializeDuelOrWarn()
        {
            GameDatabase database = GameDataRuntime.CurrentDatabase;
            if (database == null)
            {
                Debug.LogWarning("[DuelScreenController] GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            abilityIconCache.Rebuild(database);

            if (!sessionRunner.TryInitialize(database, enemyId, advanceToPlayerSetup: false, out string failureMessage))
            {
                Debug.LogWarning($"[DuelScreenController] Failed to initialize duel: {failureMessage}");
                return;
            }

            selectionState.ClearAll();
        }

        void WireObservableBindings()
        {
            UnwireObservableBindings();

            if (view == null)
            {
                return;
            }

            uiSubscriptions.Add(observableState.TopBarState.Subscribe(view.RenderTopBar));
            uiSubscriptions.Add(observableState.HealthState.Subscribe(view.RenderHealth));
            uiSubscriptions.Add(observableState.ButtonState.Subscribe(view.RenderButtons));
            uiSubscriptions.Add(observableState.BoardState.Subscribe(HandleBoardStateChanged));
            uiSubscriptions.Add(observableState.RevealState.Subscribe(HandleRevealStateChanged));
        }

        void UnwireObservableBindings()
        {
            for (int i = 0; i < uiSubscriptions.Count; i++)
            {
                uiSubscriptions[i]?.Dispose();
            }

            uiSubscriptions.Clear();
        }

        void HandleBoardStateChanged(DuelBoardState boardState)
        {
            view.RenderBoard(
                boardState,
                HandlePlayerAbilityCardClicked,
                HandleCardDragStarted,
                HandleCardDragMoved,
                HandleCardDragEnded,
                HandleCardRightClicked);
        }

        void HandleRevealStateChanged(DuelRevealState revealState)
        {
            view.RenderReveal(revealState);
        }

        void PublishObservableState(bool publishBoard = true)
        {
            observableState.Publish(sessionRunner, selectionState, isFlowRunning, publishBoard);
        }

        void TryStartOpponentSetupFlow()
        {
            if (!UnityEngine.Application.isPlaying || !sessionRunner.IsInitialized || isFlowRunning)
            {
                return;
            }

            StartCoroutine(RunOpponentSetupFlowIfNeeded());
        }

        void HandleCombatStartClicked()
        {
            if (isFlowRunning)
            {
                return;
            }

            StartCoroutine(RunCombatStartFlow());
        }

        void HandleSurrenderClicked()
        {
            if (isFlowRunning || !sessionRunner.IsInitialized)
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

        void HandlePlayerAbilityCardClicked(string abilityId)
        {
            if (isFlowRunning || !sessionRunner.IsInitialized || isCardDragActive)
            {
                return;
            }

            if (!selectionState.TryToggleAttackSelection(
                    sessionRunner.DuelState,
                    sessionRunner.PhaseRunner,
                    abilityId,
                    out _))
            {
                return;
            }

            PublishObservableState();
        }

        void HandleCardDragStarted(
            DuelAbilityCardView cardView,
            string abilityId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (!CanUseCardInteractions(abilityId))
            {
                return;
            }

            if (cardView == null)
            {
                return;
            }

            isCardDragActive = true;
            dragAbilityId = abilityId;

            CreateDragGhost(cardView, screenPosition, eventCamera);
            UpdateDropZoneHover(screenPosition, eventCamera);
        }

        void HandleCardDragMoved(
            DuelAbilityCardView cardView,
            string abilityId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (!isCardDragActive || !string.Equals(dragAbilityId, abilityId, StringComparison.Ordinal))
            {
                return;
            }

            UpdateDragGhostPosition(screenPosition, eventCamera);
            UpdateDropZoneHover(screenPosition, eventCamera);
        }

        void HandleCardDragEnded(
            DuelAbilityCardView cardView,
            string abilityId,
            DuelAbilityCardView.InteractionContext context,
            Vector2 screenPosition,
            Camera eventCamera)
        {
            if (!isCardDragActive || !string.Equals(dragAbilityId, abilityId, StringComparison.Ordinal))
            {
                return;
            }

            bool shouldPublishState = false;
            bool isDropFailure = false;
            if (CanUseCardInteractions(abilityId) &&
                TryFindDropCombatIndex(screenPosition, eventCamera, out int targetCombatIndex))
            {
                bool isNoOp = context.isCombat && context.combatIndex == targetCombatIndex;
                if (!isNoOp)
                {
                    if (selectionState.TryMovePlayerAbilityToCombat(
                            sessionRunner.DuelState,
                            sessionRunner.PhaseRunner,
                            abilityId,
                            targetCombatIndex,
                            out string failureMessage))
                    {
                        sessionRunner.NotifyPlayerAbilityDeployed(abilityId);
                        shouldPublishState = true;
                    }
                    else
                    {
                        isDropFailure = true;
                        Debug.LogWarning($"[DuelScreenController] Drag move rejected: {failureMessage}");
                    }
                }
            }
            else
            {
                isDropFailure = true;
            }

            if (isDropFailure && cardView != null)
            {
                cardView.PlayInvalidDropFeedback();
            }

            ClearDragState();

            if (shouldPublishState)
            {
                PublishObservableState();
            }
        }

        void HandleCardRightClicked(
            DuelAbilityCardView cardView,
            string abilityId,
            DuelAbilityCardView.InteractionContext context)
        {
            if (!context.isCombat || !CanUseCardInteractions(abilityId))
            {
                return;
            }

            if (!selectionState.TryReturnPlayerAbilityToLoadout(
                    sessionRunner.DuelState,
                    sessionRunner.PhaseRunner,
                    abilityId,
                    out string failureMessage))
            {
                Debug.LogWarning($"[DuelScreenController] Return to loadout rejected: {failureMessage}");
                return;
            }

            PublishObservableState();
        }

        void HandleCombatZoneClicked(int combatIndex)
        {
            if (isFlowRunning ||
                !sessionRunner.IsInitialized ||
                sessionRunner.PhaseRunner.currentPhase != DuelPhase.PlayerSetup ||
                string.IsNullOrWhiteSpace(selectionState.SelectedAbilityId))
            {
                return;
            }

            if (!selectionState.TryMovePlayerAbilityToCombat(
                    sessionRunner.DuelState,
                    sessionRunner.PhaseRunner,
                    selectionState.SelectedAbilityId,
                    combatIndex,
                    out string failureMessage))
            {
                Debug.LogWarning($"[DuelScreenController] Ability move rejected: {failureMessage}");
                return;
            }

            sessionRunner.NotifyPlayerAbilityDeployed(selectionState.SelectedAbilityId);
            PublishObservableState();
        }

        IEnumerator RunCombatStartFlow()
        {
            if (!sessionRunner.TryValidatePlayerSetupForCombatStart(out string ensureFailure))
            {
                Debug.LogWarning($"[DuelScreenController] Combat start rejected: {ensureFailure}");
                yield break;
            }

            isFlowRunning = true;
            observableState.ClearReveal();
            PublishObservableState();

            if (!sessionRunner.TryRoll(out DuelRollResult _, out string rollFailure))
            {
                Debug.LogWarning($"[DuelScreenController] Roll failed: {rollFailure}");
                isFlowRunning = false;
                observableState.ClearReveal();
                PublishObservableState();
                yield break;
            }

            List<CombatRevealSnapshot> revealSnapshots = CaptureCombatRevealSnapshots(sessionRunner.DuelState);
            int displayPlayerHealth = sessionRunner.DuelState.playerHealth;
            int displayOpponentHealth = sessionRunner.DuelState.opponentHealth;

            if (!sessionRunner.TryResolve(out DuelCombatResolveResult resolveResult, out string resolveFailure))
            {
                Debug.LogWarning($"[DuelScreenController] Resolve failed: {resolveFailure}");
                isFlowRunning = false;
                observableState.ClearReveal();
                PublishObservableState();
                yield break;
            }

            yield return PlayRollAndResolveRevealSequence(
                revealSnapshots,
                resolveResult,
                displayPlayerHealth,
                displayOpponentHealth,
                ResolveAnimationConfig());

            if (!sessionRunner.DuelState.isDuelEnded)
            {
                yield return RunOpponentSetupFlowIfNeeded();
            }

            selectionState.ClearAbility();
            isFlowRunning = false;
            observableState.ClearReveal();
            PublishObservableState();
        }

        IEnumerator RunOpponentSetupFlowIfNeeded()
        {
            if (!sessionRunner.IsInitialized ||
                sessionRunner.DuelState == null ||
                sessionRunner.PhaseRunner == null ||
                sessionRunner.DuelState.isDuelEnded ||
                sessionRunner.PhaseRunner.currentPhase == DuelPhase.PlayerSetup)
            {
                yield break;
            }

            bool ownsFlowLock = !isFlowRunning;
            if (ownsFlowLock)
            {
                isFlowRunning = true;
                selectionState.ClearAbility();
            }

            observableState.ClearReveal();
            PublishObservableState();

            if (!sessionRunner.TryPrepareOpponentSetupForCurrentTurn(
                    out OpponentSetupBuildResult deployPlan,
                    out string prepareFailure))
            {
                Debug.LogWarning($"[DuelScreenController] Opponent setup preparation failed: {prepareFailure}");
                if (ownsFlowLock)
                {
                    isFlowRunning = false;
                    PublishObservableState();
                }

                yield break;
            }

            PublishObservableState();

            DuelAnimationConfig config = ResolveAnimationConfig();
            yield return WaitForUiLayoutReady();
            for (int i = 0; i < deployPlan.steps.Count; i++)
            {
                Canvas.ForceUpdateCanvases();
                DuelOpponentDeployStep step = deployPlan.steps[i];
                yield return AnimateAndApplyOpponentDeployStep(step, config);

                if (i < deployPlan.steps.Count - 1)
                {
                    float cardGap = ResolveOpponentDeployCardGap(config);
                    if (cardGap > 0f)
                    {
                        yield return new WaitForSecondsRealtime(cardGap);
                    }
                }
            }

            if (deployPlan.skippedCount > 0)
            {
                Debug.LogWarning($"[DuelScreenController] Opponent setup skipped abilities: {deployPlan.skippedCount}");
            }

            if (!sessionRunner.TryEnterPlayerSetup(out string playerSetupFailure))
            {
                Debug.LogWarning($"[DuelScreenController] Failed to enter PlayerSetup: {playerSetupFailure}");
            }

            selectionState.ClearAbility();
            PublishObservableState();

            if (ownsFlowLock)
            {
                isFlowRunning = false;
                PublishObservableState();
            }
        }

        IEnumerator AnimateAndApplyOpponentDeployStep(
            DuelOpponentDeployStep step,
            DuelAnimationConfig config)
        {
            if (view == null)
            {
                yield break;
            }

            bool hasSourceCard = view.TryGetVisibleCardView(step.abilityId, out DuelAbilityCardView sourceCard);
            bool hasSourceCenter = view.TryGetVisibleCardScreenCenter(step.abilityId, out Vector2 sourceScreenCenter);
            bool hasTargetCenter = view.TryGetCombatSlotScreenCenter(
                step.combatIndex,
                isPlayerSide: false,
                step.slotIndex,
                out Vector2 targetScreenCenter);

            if (!hasSourceCard || !hasSourceCenter || !hasTargetCenter)
            {
                Debug.LogWarning(
                    $"[DuelScreenController] Opponent deploy animation fallback for ability({step.abilityId}) " +
                    $"combat({step.combatIndex}) slot({step.slotIndex}).");
                if (TryApplyOpponentDeployStepWithLogging(step))
                {
                    PublishObservableState();
                }

                yield break;
            }

            view.SetCardVisible(step.abilityId, false);
            RectTransform ghostRect = CreateOpponentDeployGhost(sourceCard);
            if (ghostRect == null ||
                !TrySetRectPositionFromScreenPoint(ghostRect, sourceScreenCenter))
            {
                Debug.LogWarning(
                    $"[DuelScreenController] Failed to create opponent deploy ghost for ability({step.abilityId}).");
                DestroyTransientGhost(ghostRect);
                if (!TryApplyOpponentDeployStepWithLogging(step))
                {
                    view.SetCardVisible(step.abilityId, true);
                    PublishObservableState();
                    yield break;
                }

                PublishObservableState();
                yield break;
            }

            yield return AnimateRectToScreenPoint(
                ghostRect,
                sourceScreenCenter,
                targetScreenCenter,
                ResolveOpponentDeployCardDuration(config));

            DestroyTransientGhost(ghostRect);

            if (!TryApplyOpponentDeployStepWithLogging(step))
            {
                view.SetCardVisible(step.abilityId, true);
                PublishObservableState();
                yield break;
            }

            PublishObservableState();
        }

        bool TryApplyOpponentDeployStepWithLogging(DuelOpponentDeployStep step)
        {
            if (sessionRunner.TryApplyOpponentDeployStep(step, out string applyFailure))
            {
                return true;
            }

            Debug.LogWarning(
                $"[DuelScreenController] Opponent deploy apply failed for ability({step.abilityId}) " +
                $"combat({step.combatIndex}) slot({step.slotIndex}): {applyFailure}");
            return false;
        }

        static IEnumerator WaitForUiLayoutReady()
        {
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();
        }

        IEnumerator PlayRollAndResolveRevealSequence(
            IReadOnlyList<CombatRevealSnapshot> revealSnapshots,
            DuelCombatResolveResult resolveResult,
            int displayPlayerHealth,
            int displayOpponentHealth,
            DuelAnimationConfig config)
        {
            if (resolveResult == null || revealSnapshots == null || revealSnapshots.Count <= 0)
            {
                yield break;
            }

            int combatCount = combatZones == null ? 3 : combatZones.Length;
            int[] opponentTotals = new int[combatCount];
            int[] playerTotals = new int[combatCount];
            var overlayByAbilityId = new Dictionary<string, DuelRollOverlayValue>(StringComparer.Ordinal);

            for (int i = 0; i < revealSnapshots.Count; i++)
            {
                CombatRevealSnapshot snapshot = revealSnapshots[i];
                if (snapshot.combatIndex < 0 || snapshot.combatIndex >= combatCount)
                {
                    continue;
                }

                opponentTotals[snapshot.combatIndex] = snapshot.opponentBaseTotal;
                playerTotals[snapshot.combatIndex] = snapshot.playerBaseTotal;
            }

            void PublishReveal()
            {
                observableState.PublishReveal(
                    true,
                    opponentTotals,
                    playerTotals,
                    displayOpponentHealth,
                    displayPlayerHealth,
                    overlayByAbilityId);
            }

            PublishReveal();

            Dictionary<int, DuelCombatResolveStepResult> stepsByCombatIndex = BuildStepLookup(resolveResult);

            for (int i = 0; i < revealSnapshots.Count; i++)
            {
                CombatRevealSnapshot snapshot = revealSnapshots[i];
                if (!stepsByCombatIndex.TryGetValue(snapshot.combatIndex, out DuelCombatResolveStepResult step))
                {
                    break;
                }

                for (int enemyIndex = 0; enemyIndex < snapshot.opponentAbilityIds.Count; enemyIndex++)
                {
                    string abilityId = snapshot.opponentAbilityIds[enemyIndex];
                    int finalValue = ResolveAbilityFinalPower(abilityId);
                    int rouletteMax = ResolveAbilityRouletteMax(abilityId, finalValue);
                    yield return AnimateAbilityRoulette(
                        abilityId,
                        rouletteMax,
                        finalValue,
                        config,
                        overlayByAbilityId,
                        PublishReveal);
                    opponentTotals[snapshot.combatIndex] += Mathf.Max(0, finalValue);
                    PublishReveal();
                }

                for (int playerIndex = 0; playerIndex < snapshot.playerAbilityIds.Count; playerIndex++)
                {
                    string abilityId = snapshot.playerAbilityIds[playerIndex];
                    int finalValue = ResolveAbilityFinalPower(abilityId);
                    int rouletteMax = ResolveAbilityRouletteMax(abilityId, finalValue);
                    yield return AnimateAbilityRoulette(
                        abilityId,
                        rouletteMax,
                        finalValue,
                        config,
                        overlayByAbilityId,
                        PublishReveal);
                    playerTotals[snapshot.combatIndex] += Mathf.Max(0, finalValue);
                    PublishReveal();
                }

                opponentTotals[snapshot.combatIndex] = Mathf.Max(0, step.opponentTotalPower);
                playerTotals[snapshot.combatIndex] = Mathf.Max(0, step.playerTotalPower);
                PublishReveal();

                yield return view.AnimateResolveSingleCombat(
                    snapshot.combatIndex,
                    step.outcome,
                    config);

                if (step.appliedDamage > 0)
                {
                    if (step.outcome == DuelOutcome.Victory)
                    {
                        displayOpponentHealth = Mathf.Max(0, displayOpponentHealth - step.appliedDamage);
                    }
                    else if (step.outcome == DuelOutcome.Defeat)
                    {
                        displayPlayerHealth = Mathf.Max(0, displayPlayerHealth - step.appliedDamage);
                    }

                    PublishReveal();
                }

                if (config != null && config.resolveCombatGap > 0f && i < revealSnapshots.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(config.resolveCombatGap);
                }
            }
        }

        IEnumerator AnimateAbilityRoulette(
            string abilityId,
            int rouletteMax,
            int finalValue,
            DuelAnimationConfig config,
            IDictionary<string, DuelRollOverlayValue> overlayByAbilityId,
            Action onFrameChanged)
        {
            if (string.IsNullOrWhiteSpace(abilityId) || overlayByAbilityId == null)
            {
                yield break;
            }

            float duration = config == null
                ? defaultCardRollDuration
                : config.cardRollDuration;
            if (duration <= 0f)
            {
                duration = defaultCardRollDuration;
            }

            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    int rouletteValue = revealRandom.Next(1, Mathf.Max(2, rouletteMax + 1));
                    overlayByAbilityId[abilityId] = new DuelRollOverlayValue(true, rouletteValue, false);
                    onFrameChanged?.Invoke();
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            overlayByAbilityId[abilityId] = new DuelRollOverlayValue(true, Mathf.Max(0, finalValue), true);
            onFrameChanged?.Invoke();
        }

        List<CombatRevealSnapshot> CaptureCombatRevealSnapshots(DuelState duelState)
        {
            var snapshots = new List<CombatRevealSnapshot>();
            if (duelState?.combats == null)
            {
                return snapshots;
            }

            for (int combatIndex = 0; combatIndex < duelState.combats.Count; combatIndex++)
            {
                CombatState combat = duelState.combats[combatIndex];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                List<string> opponentAttackIds = FilterAttackAbilityIds(duelState, combat.opponentAbilityIds);
                List<string> playerAttackIds = FilterAttackAbilityIds(duelState, combat.playerAbilityIds);
                snapshots.Add(new CombatRevealSnapshot(
                    combatIndex,
                    Mathf.Max(0, combat.totalPowerBonusOpponent),
                    Mathf.Max(0, combat.totalPowerBonusPlayer),
                    opponentAttackIds,
                    playerAttackIds));
            }

            return snapshots.OrderBy(snapshot => snapshot.combatIndex).ToList();
        }

        static List<string> FilterAttackAbilityIds(DuelState duelState, List<string> abilityIds)
        {
            var result = new List<string>();
            if (duelState?.abilitiesById == null || abilityIds == null)
            {
                return result;
            }

            for (int i = 0; i < abilityIds.Count; i++)
            {
                string abilityId = abilityIds[i];
                if (string.IsNullOrWhiteSpace(abilityId))
                {
                    continue;
                }

                if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
                {
                    continue;
                }

                if (ability.abilityType != AbilityType.Attack)
                {
                    continue;
                }

                result.Add(abilityId);
            }

            return result;
        }

        static Dictionary<int, DuelCombatResolveStepResult> BuildStepLookup(DuelCombatResolveResult resolveResult)
        {
            var lookup = new Dictionary<int, DuelCombatResolveStepResult>();
            if (resolveResult?.steps == null)
            {
                return lookup;
            }

            for (int i = 0; i < resolveResult.steps.Count; i++)
            {
                DuelCombatResolveStepResult step = resolveResult.steps[i];
                if (lookup.ContainsKey(step.combatIndex))
                {
                    continue;
                }

                lookup.Add(step.combatIndex, step);
            }

            return lookup;
        }

        int ResolveAbilityFinalPower(string abilityId)
        {
            if (sessionRunner.DuelState?.abilitiesById == null ||
                string.IsNullOrWhiteSpace(abilityId) ||
                !sessionRunner.DuelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) ||
                ability == null)
            {
                return 0;
            }

            if (ability.abilityType != AbilityType.Attack)
            {
                return 0;
            }

            return ability.powerResult > 0
                ? ability.powerResult
                : ResolveEffectivePower(ability);
        }

        int ResolveAbilityRouletteMax(string abilityId, int finalValue)
        {
            if (sessionRunner.DuelState?.abilitiesById == null ||
                string.IsNullOrWhiteSpace(abilityId) ||
                !sessionRunner.DuelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) ||
                ability == null)
            {
                return Mathf.Max(1, finalValue);
            }

            return Mathf.Max(1, ResolveEffectivePower(ability), finalValue);
        }

        static int ResolveEffectivePower(AbilityInstance ability)
        {
            if (ability == null)
            {
                return 0;
            }

            ability.EnsureInitialized();
            return NumericModifierCalculator.Apply(
                ability.power,
                ability.powerModifiers,
                minValue: 0,
                logContext: "DuelScreenController.ResolveEffectivePower");
        }

        readonly struct CombatRevealSnapshot
        {
            public int combatIndex { get; }
            public int opponentBaseTotal { get; }
            public int playerBaseTotal { get; }
            public List<string> opponentAbilityIds { get; }
            public List<string> playerAbilityIds { get; }

            public CombatRevealSnapshot(
                int combatIndex,
                int opponentBaseTotal,
                int playerBaseTotal,
                List<string> opponentAbilityIds,
                List<string> playerAbilityIds)
            {
                this.combatIndex = combatIndex;
                this.opponentBaseTotal = opponentBaseTotal;
                this.playerBaseTotal = playerBaseTotal;
                this.opponentAbilityIds = opponentAbilityIds ?? new List<string>();
                this.playerAbilityIds = playerAbilityIds ?? new List<string>();
            }
        }

        DuelAnimationConfig ResolveAnimationConfig()
        {
            if (animationConfig != null)
            {
                return animationConfig;
            }

            if (runtimeAnimationConfig == null)
            {
                runtimeAnimationConfig = ScriptableObject.CreateInstance<DuelAnimationConfig>();
                runtimeAnimationConfig.hideFlags = HideFlags.HideAndDontSave;
                runtimeAnimationConfig.rollDuration = 0.35f;
                runtimeAnimationConfig.cardRollDuration = defaultCardRollDuration;
                runtimeAnimationConfig.resolvePerCombatDuration = 0.55f;
                runtimeAnimationConfig.resolveCombatGap = 0.15f;
                runtimeAnimationConfig.opponentDeployCardDuration = defaultOpponentDeployCardDuration;
                runtimeAnimationConfig.opponentDeployCardGap = defaultOpponentDeployCardGap;
            }

            return runtimeAnimationConfig;
        }

        float ResolveOpponentDeployCardDuration(DuelAnimationConfig config)
        {
            if (config == null || config.opponentDeployCardDuration <= 0f)
            {
                return defaultOpponentDeployCardDuration;
            }

            return config.opponentDeployCardDuration;
        }

        float ResolveOpponentDeployCardGap(DuelAnimationConfig config)
        {
            if (config == null || config.opponentDeployCardGap <= 0f)
            {
                return defaultOpponentDeployCardGap;
            }

            return config.opponentDeployCardGap;
        }

        RectTransform CreateOpponentDeployGhost(DuelAbilityCardView sourceCard)
        {
            if (sourceCard == null || !TryResolveCanvasContext(out RectTransform canvasRect, out _))
            {
                return null;
            }

            GameObject ghostObject = Instantiate(sourceCard.gameObject, canvasRect, false);
            ghostObject.name = "OpponentDeployGhost";
            ghostObject.SetActive(true);

            if (ghostObject.TryGetComponent(out DuelAbilityCardView ghostCard))
            {
                ghostCard.enabled = false;
            }

            if (ghostObject.TryGetComponent(out Button ghostButton))
            {
                ghostButton.interactable = false;
                ghostButton.enabled = false;
            }

            Graphic[] graphics = ghostObject.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                Color color = graphic.color;
                color.a *= opponentDeployGhostAlpha;
                graphic.color = color;
                graphic.raycastTarget = false;
            }

            if (!(ghostObject.transform is RectTransform ghostRect))
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(ghostObject);
                }
                else
                {
                    DestroyImmediate(ghostObject);
                }

                return null;
            }

            if (sourceCard.transform is RectTransform sourceRect)
            {
                ghostRect.anchorMin = new Vector2(0.5f, 0.5f);
                ghostRect.anchorMax = new Vector2(0.5f, 0.5f);
                ghostRect.pivot = new Vector2(0.5f, 0.5f);
                ghostRect.sizeDelta = sourceRect.rect.size;
                ghostRect.localScale = Vector3.one;
            }

            ghostRect.SetAsLastSibling();
            return ghostRect;
        }

        IEnumerator AnimateRectToScreenPoint(
            RectTransform rectTransform,
            Vector2 fromScreenPoint,
            Vector2 toScreenPoint,
            float duration)
        {
            if (rectTransform == null)
            {
                yield break;
            }

            if (!TryScreenPointToCanvasLocalPoint(fromScreenPoint, out Vector2 startLocalPoint) ||
                !TryScreenPointToCanvasLocalPoint(toScreenPoint, out Vector2 endLocalPoint))
            {
                yield break;
            }

            if (duration <= 0f)
            {
                rectTransform.anchoredPosition = endLocalPoint;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rectTransform.anchoredPosition = Vector2.LerpUnclamped(startLocalPoint, endLocalPoint, eased);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            rectTransform.anchoredPosition = endLocalPoint;
        }

        bool TrySetRectPositionFromScreenPoint(RectTransform rectTransform, Vector2 screenPoint)
        {
            if (rectTransform == null || !TryScreenPointToCanvasLocalPoint(screenPoint, out Vector2 localPoint))
            {
                return false;
            }

            rectTransform.anchoredPosition = localPoint;
            return true;
        }

        bool TryScreenPointToCanvasLocalPoint(Vector2 screenPoint, out Vector2 localPoint)
        {
            localPoint = Vector2.zero;

            if (!TryResolveCanvasContext(out RectTransform canvasRect, out Camera canvasCamera))
            {
                return false;
            }

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                canvasCamera,
                out localPoint);
        }

        bool TryResolveCanvasContext(out RectTransform canvasRect, out Camera canvasCamera)
        {
            canvasRect = null;
            canvasCamera = null;

            Canvas canvas = ResolveCanvas();
            if (canvas == null || !(canvas.transform is RectTransform canvasRootRect))
            {
                return false;
            }

            canvasRect = canvasRootRect;
            canvasCamera = ResolveCanvasCamera(canvas);
            return true;
        }

        Canvas ResolveCanvas()
        {
            return backgroundImage == null
                ? GetComponentInParent<Canvas>()
                : backgroundImage.canvas;
        }

        static Camera ResolveCanvasCamera(Canvas canvas)
        {
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }

        void DestroyTransientGhost(RectTransform ghostRect)
        {
            if (ghostRect == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(ghostRect.gameObject);
            }
            else
            {
                DestroyImmediate(ghostRect.gameObject);
            }
        }

        bool CanUseCardInteractions(string abilityId)
        {
            if (isFlowRunning ||
                !sessionRunner.IsInitialized ||
                sessionRunner.DuelState == null ||
                sessionRunner.PhaseRunner == null ||
                sessionRunner.DuelState.isDuelEnded ||
                sessionRunner.PhaseRunner.currentPhase != DuelPhase.PlayerSetup ||
                string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (!sessionRunner.DuelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) ||
                ability == null)
            {
                return false;
            }

            return ability.abilityType == AbilityType.Attack && ability.cooldownRemaining <= 0;
        }

        bool TryFindDropCombatIndex(Vector2 screenPosition, Camera eventCamera, out int combatIndex)
        {
            combatIndex = -1;
            if (combatZones == null)
            {
                return false;
            }

            for (int i = 0; i < combatZones.Length; i++)
            {
                DuelCombatZoneView zone = combatZones[i];
                if (zone == null || !zone.ContainsScreenPoint(screenPosition, eventCamera))
                {
                    continue;
                }

                combatIndex = i;
                return true;
            }

            return false;
        }

        void UpdateDropZoneHover(Vector2 screenPosition, Camera eventCamera)
        {
            DuelCombatZoneView nextHovered = null;
            if (combatZones != null)
            {
                for (int i = 0; i < combatZones.Length; i++)
                {
                    DuelCombatZoneView zone = combatZones[i];
                    if (zone != null && zone.ContainsScreenPoint(screenPosition, eventCamera))
                    {
                        nextHovered = zone;
                        break;
                    }
                }
            }

            if (hoveredDropZone == nextHovered)
            {
                return;
            }

            if (hoveredDropZone != null)
            {
                hoveredDropZone.SetDragHover(false);
            }

            hoveredDropZone = nextHovered;
            if (hoveredDropZone != null)
            {
                hoveredDropZone.SetDragHover(true);
            }
        }

        void CreateDragGhost(DuelAbilityCardView sourceCard, Vector2 screenPosition, Camera eventCamera)
        {
            DestroyDragGhost();

            if (sourceCard == null)
            {
                return;
            }

            Canvas canvas = backgroundImage == null ? GetComponentInParent<Canvas>() : backgroundImage.canvas;
            RectTransform canvasRect = canvas == null ? null : canvas.transform as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            GameObject ghostObject = Instantiate(sourceCard.gameObject, canvasRect, false);
            ghostObject.name = "CardDragGhost";

            if (ghostObject.TryGetComponent(out DuelAbilityCardView ghostCard))
            {
                ghostCard.enabled = false;
            }

            if (ghostObject.TryGetComponent(out Button ghostButton))
            {
                ghostButton.interactable = false;
                ghostButton.enabled = false;
            }

            Graphic[] graphics = ghostObject.GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                Graphic graphic = graphics[i];
                if (graphic == null)
                {
                    continue;
                }

                Color color = graphic.color;
                color.a *= dragGhostAlpha;
                graphic.color = color;
                graphic.raycastTarget = false;
            }

            dragGhostRect = ghostObject.transform as RectTransform;
            if (dragGhostRect == null)
            {
                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(ghostObject);
                }
                else
                {
                    DestroyImmediate(ghostObject);
                }

                return;
            }

            RectTransform sourceRect = sourceCard.transform as RectTransform;
            if (sourceRect != null)
            {
                dragGhostRect.anchorMin = new Vector2(0.5f, 0.5f);
                dragGhostRect.anchorMax = new Vector2(0.5f, 0.5f);
                dragGhostRect.pivot = new Vector2(0.5f, 0.5f);
                dragGhostRect.sizeDelta = sourceRect.rect.size;
            }

            dragGhostRect.SetAsLastSibling();
            UpdateDragGhostPosition(screenPosition, eventCamera);
        }

        void UpdateDragGhostPosition(Vector2 screenPosition, Camera eventCamera)
        {
            if (dragGhostRect == null)
            {
                return;
            }

            RectTransform canvasRect = dragGhostRect.parent as RectTransform;
            if (canvasRect == null)
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    eventCamera,
                    out Vector2 localPoint))
            {
                return;
            }

            dragGhostRect.anchoredPosition = localPoint;
        }

        void ClearDragState()
        {
            if (hoveredDropZone != null)
            {
                hoveredDropZone.SetDragHover(false);
                hoveredDropZone = null;
            }

            DestroyDragGhost();

            isCardDragActive = false;
            dragAbilityId = string.Empty;
        }

        void DestroyDragGhost()
        {
            if (dragGhostRect == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                Destroy(dragGhostRect.gameObject);
            }
            else
            {
                DestroyImmediate(dragGhostRect.gameObject);
            }

            dragGhostRect = null;
        }
    }
}




