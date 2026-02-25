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
        const string defaultEnemyId = "enemy.northern.footman";

        [Header("Battle Data")]
        [SerializeField] string enemyId = defaultEnemyId;

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

        void Awake()
        {
            CollectCombatZonesIfNeeded();
            RebuildView();
            view.ApplyStaticVisuals();

            if (!UnityEngine.Application.isPlaying)
            {
                return;
            }

            if (!view.ValidateSceneReferencesForRuntime(out string missingReferences))
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenController] Missing required scene references: {missingReferences}. Configure in editor.");
                enabled = false;
                return;
            }

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
                UnityEngine.Debug.LogWarning("[BattleScreenController] GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            if (!sessionRunner.TryInitialize(database, enemyId, advanceToPlayerSetup: true, out string failureMessage))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Failed to initialize duel: {failureMessage}");
                return;
            }

            selectionState.ClearAll();
        }

        void RefreshView()
        {
            view.Refresh(sessionRunner, selectionState, isFlowRunning, HandlePlayerAbilityCardClicked);
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
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Surrender rejected: {failureMessage}");
                return;
            }

            selectionState.ClearAbility();
            RefreshView();
        }

        void HandlePlayerAbilityCardClicked(string abilityId)
        {
            if (isFlowRunning || !sessionRunner.IsInitialized)
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
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Ability move rejected: {failureMessage}");
                return;
            }

            RefreshView();
        }

        IEnumerator RunCombatStartFlow()
        {
            if (!sessionRunner.TryEnsureReadyForCombatStart(out string ensureFailure))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Combat start rejected: {ensureFailure}");
                yield break;
            }

            isFlowRunning = true;
            RefreshView();

            if (!sessionRunner.TryRoll(out DuelRollResult _, out string rollFailure))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Roll failed: {rollFailure}");
                isFlowRunning = false;
                RefreshView();
                yield break;
            }

            yield return view.AnimateRoll(ResolveAnimationConfig());
            RefreshView();

            if (!sessionRunner.TryResolve(out DuelCombatResolveResult resolveResult, out string resolveFailure))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Resolve failed: {resolveFailure}");
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
                    UnityEngine.Debug.LogWarning(
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
    }
}
