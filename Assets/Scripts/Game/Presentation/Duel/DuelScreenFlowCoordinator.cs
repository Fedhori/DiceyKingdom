using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Application.Duel;
using Game.Domain.Duel;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Duel
{
    public sealed class DuelScreenFlowCoordinator
    {
        const float defaultCardRollDuration = 0.5f;
        const float defaultOpponentDeployCardDuration = 0.25f;
        const float defaultOpponentDeployCardGap = 0.06f;
        const float opponentDeployGhostAlpha = 0.95f;

        readonly Func<DuelScreenView> resolveView;
        readonly Func<Canvas> resolveCanvas;
        readonly DuelCombatZoneView[] combatZones;
        readonly DuelAnimationConfig serializedAnimationConfig;
        readonly DuelSessionRunner sessionRunner;
        readonly DuelSelectionState selectionState;
        readonly DuelUiQueryService uiQueryService;
        readonly DuelScreenObservableBinder observableBinder;
        readonly System.Random revealRandom;

        DuelAnimationConfig runtimeAnimationConfig;

        public bool IsRunning { get; private set; }

        public DuelScreenFlowCoordinator(
            Func<DuelScreenView> resolveView,
            Func<Canvas> resolveCanvas,
            DuelCombatZoneView[] combatZones,
            DuelAnimationConfig serializedAnimationConfig,
            DuelSessionRunner sessionRunner,
            DuelSelectionState selectionState,
            DuelUiQueryService uiQueryService,
            DuelScreenObservableBinder observableBinder,
            System.Random revealRandom)
        {
            this.resolveView = resolveView ?? throw new ArgumentNullException(nameof(resolveView));
            this.resolveCanvas = resolveCanvas ?? throw new ArgumentNullException(nameof(resolveCanvas));
            this.combatZones = combatZones ?? Array.Empty<DuelCombatZoneView>();
            this.serializedAnimationConfig = serializedAnimationConfig;
            this.sessionRunner = sessionRunner ?? throw new ArgumentNullException(nameof(sessionRunner));
            this.selectionState = selectionState ?? throw new ArgumentNullException(nameof(selectionState));
            this.uiQueryService = uiQueryService ?? throw new ArgumentNullException(nameof(uiQueryService));
            this.observableBinder = observableBinder ?? throw new ArgumentNullException(nameof(observableBinder));
            this.revealRandom = revealRandom ?? throw new ArgumentNullException(nameof(revealRandom));
        }

        public void TryStartOpponentSetupFlow(MonoBehaviour host)
        {
            if (host == null ||
                !UnityEngine.Application.isPlaying ||
                !sessionRunner.IsInitialized ||
                IsRunning)
            {
                return;
            }

            host.StartCoroutine(RunOpponentSetupFlowIfNeeded());
        }

        public IEnumerator RunCombatStartFlow()
        {
            if (!sessionRunner.TryValidatePlayerSetupForCombatStart(out string ensureFailure))
            {
                Debug.LogWarning($"[DuelScreenFlowCoordinator] Combat start rejected: {ensureFailure}");
                yield break;
            }

            IsRunning = true;
            observableBinder.ClearReveal();
            PublishObservableState();

            if (!sessionRunner.TryRoll(out DuelRollResult _, out string rollFailure))
            {
                Debug.LogWarning($"[DuelScreenFlowCoordinator] Roll failed: {rollFailure}");
                IsRunning = false;
                observableBinder.ClearReveal();
                PublishObservableState();
                yield break;
            }

            List<CombatRevealSnapshot> revealSnapshots = CaptureCombatRevealSnapshots(sessionRunner.DuelState);
            if (!sessionRunner.TryBeginResolve(out string beginResolveFailure))
            {
                Debug.LogWarning($"[DuelScreenFlowCoordinator] Resolve begin failed: {beginResolveFailure}");
                IsRunning = false;
                observableBinder.ClearReveal();
                PublishObservableState();
                yield break;
            }

            yield return PlayRollAndResolveRevealSequence(
                revealSnapshots,
                ResolveAnimationConfig());

            if (!sessionRunner.TryFinalizeResolve(out DuelCombatResolveResult _, out string finalizeResolveFailure))
            {
                Debug.LogWarning($"[DuelScreenFlowCoordinator] Resolve finalize failed: {finalizeResolveFailure}");
                IsRunning = false;
                observableBinder.ClearReveal();
                PublishObservableState();
                yield break;
            }

            if (!sessionRunner.DuelState.isDuelEnded)
            {
                yield return RunOpponentSetupFlowIfNeeded();
            }

            selectionState.ClearAbility();
            IsRunning = false;
            observableBinder.ClearReveal();
            PublishObservableState();
        }

        public IEnumerator RunOpponentSetupFlowIfNeeded()
        {
            if (!sessionRunner.IsInitialized ||
                sessionRunner.DuelState == null ||
                sessionRunner.PhaseRunner == null ||
                sessionRunner.DuelState.isDuelEnded ||
                sessionRunner.PhaseRunner.currentPhase == DuelPhase.PlayerSetup)
            {
                yield break;
            }

            bool ownsFlowLock = !IsRunning;
            if (ownsFlowLock)
            {
                IsRunning = true;
                selectionState.ClearAbility();
            }

            observableBinder.ClearReveal();
            PublishObservableState();

            if (!sessionRunner.TryPrepareOpponentSetupForCurrentTurn(
                    out OpponentSetupBuildResult deployPlan,
                    out string prepareFailure))
            {
                Debug.LogWarning($"[DuelScreenFlowCoordinator] Opponent setup preparation failed: {prepareFailure}");
                if (ownsFlowLock)
                {
                    IsRunning = false;
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
                Debug.LogWarning($"[DuelScreenFlowCoordinator] Opponent setup skipped abilities: {deployPlan.skippedCount}");
            }

            if (!sessionRunner.TryEnterPlayerSetup(out string playerSetupFailure))
            {
                Debug.LogWarning($"[DuelScreenFlowCoordinator] Failed to enter PlayerSetup: {playerSetupFailure}");
            }

            selectionState.ClearAbility();
            PublishObservableState();

            if (ownsFlowLock)
            {
                IsRunning = false;
                PublishObservableState();
            }
        }

        IEnumerator AnimateAndApplyOpponentDeployStep(
            DuelOpponentDeployStep step,
            DuelAnimationConfig config)
        {
            DuelScreenView view = resolveView.Invoke();
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
                    $"[DuelScreenFlowCoordinator] Opponent deploy animation fallback for ability({step.abilityId}) " +
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
                    $"[DuelScreenFlowCoordinator] Failed to create opponent deploy ghost for ability({step.abilityId}).");
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
                $"[DuelScreenFlowCoordinator] Opponent deploy apply failed for ability({step.abilityId}) " +
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
            DuelAnimationConfig config)
        {
            if (revealSnapshots == null || revealSnapshots.Count <= 0)
            {
                yield break;
            }

            DuelScreenView view = resolveView.Invoke();
            if (view == null)
            {
                yield break;
            }

            int combatCount = combatZones == null ? 3 : combatZones.Length;
            int[] opponentTotals = new int[combatCount];
            int[] playerTotals = new int[combatCount];
            var overlayByAbilityId = new Dictionary<string, DuelRollOverlayValue>(StringComparer.Ordinal);
            IReadOnlyDictionary<string, int> powerBadgeByAbilityId = null;
            var snapshotsByCombatIndex = new Dictionary<int, CombatRevealSnapshot>();

            for (int i = 0; i < revealSnapshots.Count; i++)
            {
                CombatRevealSnapshot snapshot = revealSnapshots[i];
                if (snapshot.combatIndex < 0 || snapshot.combatIndex >= combatCount)
                {
                    continue;
                }

                opponentTotals[snapshot.combatIndex] = snapshot.opponentBaseTotal;
                playerTotals[snapshot.combatIndex] = snapshot.playerBaseTotal;
                snapshotsByCombatIndex[snapshot.combatIndex] = snapshot;
            }

            void PublishReveal()
            {
                observableBinder.PublishReveal(
                    true,
                    opponentTotals,
                    playerTotals,
                    overlayByAbilityId,
                    powerBadgeByAbilityId);
            }

            PublishReveal();

            bool hasRemainingCombats = true;
            while (hasRemainingCombats)
            {
                if (!sessionRunner.TryResolveNextCombat(
                        out DuelCombatResolveStepResult step,
                        out hasRemainingCombats,
                        out string resolveStepFailure))
                {
                    Debug.LogWarning($"[DuelScreenFlowCoordinator] Resolve step failed: {resolveStepFailure}");
                    break;
                }

                PublishObservableState(publishBoard: false);
                CombatRevealSnapshot snapshot = snapshotsByCombatIndex.TryGetValue(step.combatIndex, out CombatRevealSnapshot foundSnapshot)
                    ? foundSnapshot
                    : new CombatRevealSnapshot(
                        step.combatIndex,
                        0,
                        0,
                        new List<string>(),
                        new List<string>());

                for (int enemyIndex = 0; enemyIndex < snapshot.opponentAbilityIds.Count; enemyIndex++)
                {
                    string abilityInstanceId = snapshot.opponentAbilityIds[enemyIndex];
                    int finalValue = ResolveAbilityFinalPower(step, abilityInstanceId);
                    int rouletteMax = ResolveAbilityRouletteMax(abilityInstanceId, finalValue);
                    yield return AnimateAbilityRoulette(
                        abilityInstanceId,
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
                    string abilityInstanceId = snapshot.playerAbilityIds[playerIndex];
                    int finalValue = ResolveAbilityFinalPower(step, abilityInstanceId);
                    int rouletteMax = ResolveAbilityRouletteMax(abilityInstanceId, finalValue);
                    yield return AnimateAbilityRoulette(
                        abilityInstanceId,
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

                powerBadgeByAbilityId = step.abilityPowerAfterStep;
                PublishReveal();

                if (config != null && config.resolveCombatGap > 0f && hasRemainingCombats)
                {
                    yield return new WaitForSecondsRealtime(config.resolveCombatGap);
                }
            }
        }

        IEnumerator AnimateAbilityRoulette(
            string abilityInstanceId,
            int rouletteMax,
            int finalValue,
            DuelAnimationConfig config,
            IDictionary<string, DuelRollOverlayValue> overlayByAbilityId,
            Action onFrameChanged)
        {
            if (string.IsNullOrWhiteSpace(abilityInstanceId) || overlayByAbilityId == null)
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
                    overlayByAbilityId[abilityInstanceId] = new DuelRollOverlayValue(true, rouletteValue, false);
                    onFrameChanged?.Invoke();
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }
            }

            overlayByAbilityId[abilityInstanceId] = new DuelRollOverlayValue(true, Mathf.Max(0, finalValue), true);
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

        List<string> FilterAttackAbilityIds(DuelState duelState, List<string> abilityInstanceIds)
        {
            var result = new List<string>();
            if (duelState?.abilitiesById == null || abilityInstanceIds == null)
            {
                return result;
            }

            for (int i = 0; i < abilityInstanceIds.Count; i++)
            {
                string abilityInstanceId = abilityInstanceIds[i];
                if (string.IsNullOrWhiteSpace(abilityInstanceId))
                {
                    continue;
                }

                if (!duelState.abilitiesById.TryGetValue(abilityInstanceId, out AbilityInstance ability) || ability == null)
                {
                    continue;
                }

                if (!uiQueryService.IsAttackAbility(ability))
                {
                    continue;
                }

                result.Add(abilityInstanceId);
            }

            return result;
        }

        static int ResolveAbilityFinalPower(DuelCombatResolveStepResult step, string abilityInstanceId)
        {
            if (string.IsNullOrWhiteSpace(abilityInstanceId) ||
                step.rolledPowerByAbilityId == null ||
                !step.rolledPowerByAbilityId.TryGetValue(abilityInstanceId, out int rolledPower))
            {
                return 0;
            }

            return Mathf.Max(0, rolledPower);
        }

        int ResolveAbilityRouletteMax(string abilityInstanceId, int finalValue)
        {
            if (sessionRunner.DuelState?.abilitiesById == null ||
                string.IsNullOrWhiteSpace(abilityInstanceId) ||
                !sessionRunner.DuelState.abilitiesById.TryGetValue(abilityInstanceId, out AbilityInstance ability) ||
                ability == null)
            {
                return Mathf.Max(1, finalValue);
            }

            return Mathf.Max(1, uiQueryService.ResolveEffectivePower(ability), finalValue);
        }

        DuelAnimationConfig ResolveAnimationConfig()
        {
            if (serializedAnimationConfig != null)
            {
                return serializedAnimationConfig;
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

            GameObject ghostObject = UnityEngine.Object.Instantiate(sourceCard.gameObject, canvasRect, false);
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
                    UnityEngine.Object.Destroy(ghostObject);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(ghostObject);
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

            Canvas canvas = resolveCanvas.Invoke();
            if (canvas == null || !(canvas.transform is RectTransform canvasRootRect))
            {
                return false;
            }

            canvasRect = canvasRootRect;
            canvasCamera = ResolveCanvasCamera(canvas);
            return true;
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
                UnityEngine.Object.Destroy(ghostRect.gameObject);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(ghostRect.gameObject);
            }
        }

        void PublishObservableState(bool publishBoard = true)
        {
            observableBinder.Publish(sessionRunner, selectionState, IsRunning, publishBoard);
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
    }
}
