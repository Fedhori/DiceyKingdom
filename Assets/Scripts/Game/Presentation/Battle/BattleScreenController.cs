using System;
using System.Collections;
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

        BattleScreenView view;
        bool isFlowRunning;
        BattleAnimationConfig runtimeAnimationConfig;
        bool isCardDragActive;
        string dragAbilityId = string.Empty;
        BattleCombatZoneView hoveredDropZone;

        void Awake()
        {
            CollectCombatZonesIfNeeded();
            RebuildView();
            view.ApplyStaticVisuals();
            WireCallbacks();
            InitializeDuelOrWarn();
            RefreshView();
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

        void RefreshView()
        {
            view.Refresh(
                sessionRunner,
                selectionState,
                isFlowRunning,
                HandlePlayerAbilityCardClicked,
                HandleCardDragStarted,
                HandleCardDragMoved,
                HandleCardDragEnded,
                HandleCardRightClicked);
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
            RefreshView();
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

            RefreshView();
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

            bool shouldRefresh = false;
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
                        shouldRefresh = true;
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

            if (shouldRefresh)
            {
                RefreshView();
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

            RefreshView();
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

            RefreshView();
        }

        IEnumerator RunCombatStartFlow()
        {
            if (!sessionRunner.TryEnsureReadyForCombatStart(out string ensureFailure))
            {
                Debug.LogWarning($"[BattleScreenController] Combat start rejected: {ensureFailure}");
                yield break;
            }

            isFlowRunning = true;
            RefreshView();

            if (!sessionRunner.TryRoll(out DuelRollResult _, out string rollFailure))
            {
                Debug.LogWarning($"[BattleScreenController] Roll failed: {rollFailure}");
                isFlowRunning = false;
                RefreshView();
                yield break;
            }

            yield return view.AnimateRoll(ResolveAnimationConfig());
            RefreshView();

            if (!sessionRunner.TryResolve(out DuelCombatResolveResult resolveResult, out string resolveFailure))
            {
                Debug.LogWarning($"[BattleScreenController] Resolve failed: {resolveFailure}");
                isFlowRunning = false;
                RefreshView();
                yield break;
            }

            yield return view.AnimateResolve(resolveResult, ResolveAnimationConfig());
            RefreshView();

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
            RefreshView();
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

        void ClearDragState()
        {
            if (hoveredDropZone != null)
            {
                hoveredDropZone.SetDragHover(false);
                hoveredDropZone = null;
            }

            isCardDragActive = false;
            dragAbilityId = string.Empty;
        }
    }
}
