using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Battle
{
    [ExecuteAlways]
    public sealed class BattleScreenController : MonoBehaviour
    {
        const string DefaultEnemyId = "enemy.northern.footman";
        const float dragGhostAlpha = 0.85f;

        [Header("Battle Data")]
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
        [SerializeField] BattleCombatZoneView[] combatZones = Array.Empty<BattleCombatZoneView>();
        [SerializeField] BattleAbilityCardView abilityCardPrefab;
        [SerializeField] TMP_Text tooltipText;
        [SerializeField] Image tooltipBackgroundImage;
        [SerializeField] BattleAnimationConfig animationConfig;

        readonly BattleSessionRunner sessionRunner = new();
        readonly BattleSelectionState selectionState = new();
        readonly BattleScreenObservableState observableState = new();
        readonly List<IDisposable> uiSubscriptions = new();

        BattleScreenView view;
        bool isFlowRunning;
        BattleAnimationConfig runtimeAnimationConfig;
        bool isCardDragActive;
        string dragAbilityId = string.Empty;
        RectTransform dragGhostRect;
        BattleCombatZoneView hoveredDropZone;

        void Awake()
        {
            CollectCombatZonesIfNeeded();
            RebuildView();
            view.ApplyStaticVisuals();
            WireCallbacks();
            WireObservableBindings();
            InitializeDuelOrWarn();
            PublishObservableState();
        }

        void OnEnable()
        {
            if (UnityEngine.Application.isPlaying)
            {
                return;
            }

            CollectCombatZonesIfNeeded();
            RebuildView();
            view.ApplyStaticVisuals();
        }

        void OnValidate()
        {
            CollectCombatZonesIfNeeded();
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
        }

        void RebuildView()
        {
            view = new BattleScreenView(
                backgroundImage,
                topBarImage,
                turnText,
                enemyHealthText,
                playerHealthText,
                combatStartButton,
                surrenderButton,
                enemyLoadoutRow,
                playerLoadoutRow,
                combatZones,
                abilityCardPrefab,
                tooltipText,
                tooltipBackgroundImage);
        }

        void CollectCombatZonesIfNeeded()
        {
            bool hasValidZones = combatZones != null &&
                combatZones.Length == 3 &&
                combatZones.All(zone => zone != null);
            if (hasValidZones)
            {
                return;
            }

            BattleCombatZoneView[] found = GetComponentsInChildren<BattleCombatZoneView>(true)
                .OrderBy(zone =>
                {
                    if (zone == null || zone.transform == null)
                    {
                        return int.MaxValue;
                    }

                    Transform parent = zone.transform.parent;
                    return parent == null ? int.MaxValue : parent.GetSiblingIndex();
                })
                .ThenBy(zone => zone == null || zone.transform == null ? int.MaxValue : zone.transform.GetSiblingIndex())
                .Take(3)
                .ToArray();

            combatZones = found;
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
                Debug.LogWarning("[BattleScreenController] GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            if (!sessionRunner.TryInitialize(database, enemyId, advanceToPlayerSetup: true, out string failureMessage))
            {
                Debug.LogWarning($"[BattleScreenController] Failed to initialize duel: {failureMessage}");
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
        }

        void UnwireObservableBindings()
        {
            for (int i = 0; i < uiSubscriptions.Count; i++)
            {
                uiSubscriptions[i]?.Dispose();
            }

            uiSubscriptions.Clear();
        }

        void HandleBoardStateChanged(BattleBoardState boardState)
        {
            view.RenderBoard(
                boardState,
                HandlePlayerAbilityCardClicked,
                HandleCardDragStarted,
                HandleCardDragMoved,
                HandleCardDragEnded,
                HandleCardRightClicked);
        }

        void PublishObservableState()
        {
            observableState.Publish(sessionRunner, selectionState, isFlowRunning);
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
                Debug.LogWarning($"[BattleScreenController] Surrender rejected: {failureMessage}");
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
            BattleAbilityCardView cardView,
            string abilityId,
            BattleAbilityCardView.InteractionContext context,
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
            BattleAbilityCardView cardView,
            string abilityId,
            BattleAbilityCardView.InteractionContext context,
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
            BattleAbilityCardView cardView,
            string abilityId,
            BattleAbilityCardView.InteractionContext context,
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
                        shouldPublishState = true;
                    }
                    else
                    {
                        isDropFailure = true;
                        Debug.LogWarning($"[BattleScreenController] Drag move rejected: {failureMessage}");
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
            BattleAbilityCardView cardView,
            string abilityId,
            BattleAbilityCardView.InteractionContext context)
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
                Debug.LogWarning($"[BattleScreenController] Return to loadout rejected: {failureMessage}");
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
                Debug.LogWarning($"[BattleScreenController] Ability move rejected: {failureMessage}");
                return;
            }

            PublishObservableState();
        }

        IEnumerator RunCombatStartFlow()
        {
            if (!sessionRunner.TryEnsureReadyForCombatStart(out string ensureFailure))
            {
                Debug.LogWarning($"[BattleScreenController] Combat start rejected: {ensureFailure}");
                yield break;
            }

            isFlowRunning = true;
            PublishObservableState();

            if (!sessionRunner.TryRoll(out DuelRollResult _, out string rollFailure))
            {
                Debug.LogWarning($"[BattleScreenController] Roll failed: {rollFailure}");
                isFlowRunning = false;
                PublishObservableState();
                yield break;
            }

            yield return view.AnimateRoll(ResolveAnimationConfig());
            PublishObservableState();

            if (!sessionRunner.TryResolve(out DuelCombatResolveResult resolveResult, out string resolveFailure))
            {
                Debug.LogWarning($"[BattleScreenController] Resolve failed: {resolveFailure}");
                isFlowRunning = false;
                PublishObservableState();
                yield break;
            }

            yield return view.AnimateResolve(resolveResult, ResolveAnimationConfig());
            PublishObservableState();

            if (!sessionRunner.DuelState.isDuelEnded)
            {
                yield return view.AnimateTurnTransition(ResolveAnimationConfig());

                if (!sessionRunner.TryAdvanceToPlayerSetupForCurrentTurn(out string advanceFailure))
                {
                    Debug.LogWarning(
                        $"[BattleScreenController] Failed to advance to PlayerSetup after resolve: {advanceFailure}");
                }
            }

            selectionState.ClearAbility();
            isFlowRunning = false;
            PublishObservableState();
        }

        BattleAnimationConfig ResolveAnimationConfig()
        {
            if (animationConfig != null)
            {
                return animationConfig;
            }

            if (runtimeAnimationConfig == null)
            {
                runtimeAnimationConfig = ScriptableObject.CreateInstance<BattleAnimationConfig>();
                runtimeAnimationConfig.hideFlags = HideFlags.HideAndDontSave;
                runtimeAnimationConfig.rollDuration = 0.35f;
                runtimeAnimationConfig.resolvePerCombatDuration = 0.55f;
                runtimeAnimationConfig.resolveCombatGap = 0.15f;
                runtimeAnimationConfig.turnTransitionDuration = 0.30f;
            }

            return runtimeAnimationConfig;
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

            return ability.abilityType == AbilityType.Attack;
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
                BattleCombatZoneView zone = combatZones[i];
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
            BattleCombatZoneView nextHovered = null;
            if (combatZones != null)
            {
                for (int i = 0; i < combatZones.Length; i++)
                {
                    BattleCombatZoneView zone = combatZones[i];
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

        void CreateDragGhost(BattleAbilityCardView sourceCard, Vector2 screenPosition, Camera eventCamera)
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

            if (ghostObject.TryGetComponent(out BattleAbilityCardView ghostCard))
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
