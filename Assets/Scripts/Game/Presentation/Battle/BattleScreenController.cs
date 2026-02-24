using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        const int expectedRuntimeLayoutVersion = 27;
        const string defaultEnemyId = "enemy.northern.footman";

        static readonly Color defaultBackgroundColor = Colors.Primitive.Bone300;
        static readonly Color defaultTopBarColor = Colors.Primitive.Slate500;
        static readonly Color defaultCombatStartButtonColor = Colors.Semantic.StateInfo;
        static readonly Color defaultSurrenderButtonColor = Colors.Semantic.StateDanger;
        static readonly Color defaultButtonDisabledColor = Colors.Semantic.ActionSecondaryBgDisabled;
        static readonly Color defaultTooltipBackgroundColor = Colors.Semantic.SurfaceSecondary;
        static readonly Color defaultTooltipTextColor = Colors.Semantic.TextPrimary;

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
        [SerializeField, HideInInspector] int runtimeLayoutVersion;

        DuelState duelState;
        DuelPhaseRunner phaseRunner;
        DuelSessionBuilder sessionBuilder;
        DuelTurnProcessor turnProcessor;
        GameDatabase database;

        string selectedAbilityId = string.Empty;
        bool isFlowRunning;
        int maxPlayerHealth = 1;
        int maxOpponentHealth = 1;
        BattleAnimationConfig runtimeAnimationConfig;

        readonly List<BattleAbilityCardView> spawnedCards = new();

        enum AbilityLocationType
        {
            None = 0,
            Loadout = 1,
            Combat = 2
        }

        void Awake()
        {
            if (!UnityEngine.Application.isPlaying)
            {
                EnsureRuntimeLayoutIfNeeded();
                ApplyStaticVisuals();
                return;
            }

            EnsureRuntimeLayoutIfNeeded();
            ApplyStaticVisuals();
            EnsureLoadoutLayout(enemyLoadoutRow);
            EnsureLoadoutLayout(playerLoadoutRow);
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

            EnsureRuntimeLayoutIfNeeded();
            ApplyStaticVisuals();
        }

        void EnsureRuntimeLayoutIfNeeded()
        {
            bool hasValidZones = combatZones != null &&
                combatZones.Length == 3 &&
                combatZones[0] != null &&
                combatZones[1] != null &&
                combatZones[2] != null;
            bool hasCoreReferences = backgroundImage != null &&
                topBarImage != null &&
                turnText != null &&
                enemyHealthText != null &&
                playerHealthText != null &&
                combatStartButton != null &&
                surrenderButton != null &&
                enemyLoadoutRow != null &&
                playerLoadoutRow != null &&
                hasValidZones &&
                abilityCardPrefab != null &&
                tooltipText != null &&
                tooltipBackgroundImage != null;
            bool hasExpectedLayoutVersion = runtimeLayoutVersion == expectedRuntimeLayoutVersion;

            if (hasCoreReferences && hasExpectedLayoutVersion && !HasLegacyDebugLayoutMarkers())
            {
                return;
            }

            BuildRuntimeLayout();
        }

        bool HasLegacyDebugLayoutMarkers()
        {
            return transform.Find("TopHeader") != null ||
                transform.Find("StatusBar") != null ||
                transform.Find("BodyRow") != null ||
                transform.Find("ActionHolderColumn") != null ||
                transform.Find("BattlefieldsColumn") != null ||
                transform.Find("ActionsColumn") != null ||
                transform.Find("ActionsLogColumn") != null;
        }

        void BuildRuntimeLayout()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == null)
                {
                    continue;
                }

                if (UnityEngine.Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            if (transform is RectTransform rootRect)
            {
                rootRect.anchorMin = Vector2.zero;
                rootRect.anchorMax = Vector2.one;
                rootRect.pivot = new Vector2(0.5f, 0.5f);
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = Vector2.zero;
            }

            backgroundImage = gameObject.GetComponent<Image>();
            if (backgroundImage == null)
            {
                backgroundImage = gameObject.AddComponent<Image>();
            }

            RectTransform topBarRect = CreateRectTransform(
                "TopBar",
                transform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                Vector2.zero,
                new Vector2(0f, 44f));
            topBarImage = EnsureImage(topBarRect.gameObject, defaultTopBarColor);
            turnText = CreateText(
                "TurnText",
                topBarRect,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 0.5f),
                new Vector2(18f, 0f),
                new Vector2(-36f, 0f),
                "Turn: 0",
                defaultTooltipTextColor,
                30f,
                FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft);

            surrenderButton = CreateButton(
                "SurrenderButton",
                transform,
                "SURRENDER",
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(16f, -56f),
                new Vector2(156f, 40f),
                defaultSurrenderButtonColor,
                Colors.Semantic.TextOnAccentLight);

            enemyLoadoutRow = CreateLoadoutRow(
                "EnemyLoadoutRow",
                transform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -130f),
                new Vector2(560f, 92f));

            enemyHealthText = CreateText(
                "EnemyHealthText",
                transform as RectTransform,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -208f),
                new Vector2(340f, 34f),
                string.Empty,
                defaultTooltipTextColor,
                36f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

            combatZones = CreateCombatZoneRow(transform);

            playerHealthText = CreateText(
                "PlayerHealthText",
                transform as RectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 204f),
                new Vector2(340f, 34f),
                string.Empty,
                defaultTooltipTextColor,
                36f,
                FontStyles.Normal,
                TextAlignmentOptions.Center);

            playerLoadoutRow = CreateLoadoutRow(
                "PlayerLoadoutRow",
                transform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 124f),
                new Vector2(640f, 92f));

            combatStartButton = CreateButton(
                "CombatStartButton",
                transform,
                "COMBAT START",
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-16f, 16f),
                new Vector2(228f, 72f),
                defaultCombatStartButtonColor,
                Colors.Semantic.TextOnAccentLight);

            RectTransform tooltipPanelRect = CreateRectTransform(
                "TooltipPanel",
                transform,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(16f, 16f),
                new Vector2(480f, 84f));
            tooltipBackgroundImage = EnsureImage(tooltipPanelRect.gameObject, defaultTooltipBackgroundColor);
            tooltipText = CreateText(
                "TooltipText",
                tooltipPanelRect,
                new Vector2(0f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(-16f, -8f),
                string.Empty,
                defaultTooltipTextColor,
                24f,
                FontStyles.Normal,
                TextAlignmentOptions.TopLeft);
            tooltipPanelRect.gameObject.SetActive(false);

            abilityCardPrefab = CreateAbilityCardTemplate(transform);
            runtimeLayoutVersion = expectedRuntimeLayoutVersion;
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

            for (int i = 0; i < combatZones.Length; i++)
            {
                if (combatZones[i] == null)
                {
                    continue;
                }

                combatZones[i].SetClickHandler(null);
            }
        }

        void OnValidate()
        {
            if (backgroundImage == null)
            {
                backgroundImage = ResolveImage("Background");
            }

            if (topBarImage == null)
            {
                topBarImage = ResolveImage("TopBar");
            }

            if (turnText == null)
            {
                turnText = ResolveText("TurnText");
            }

            if (enemyHealthText == null)
            {
                enemyHealthText = ResolveText("EnemyHealthText");
            }

            if (playerHealthText == null)
            {
                playerHealthText = ResolveText("PlayerHealthText");
            }

            if (combatStartButton == null)
            {
                combatStartButton = ResolveButton("CombatStartButton");
            }

            if (surrenderButton == null)
            {
                surrenderButton = ResolveButton("SurrenderButton");
            }

            if (enemyLoadoutRow == null)
            {
                enemyLoadoutRow = ResolveRect("EnemyLoadoutRow");
            }

            if (playerLoadoutRow == null)
            {
                playerLoadoutRow = ResolveRect("PlayerLoadoutRow");
            }

            if (tooltipText == null)
            {
                tooltipText = ResolveText("TooltipText");
            }

            if (tooltipBackgroundImage == null)
            {
                tooltipBackgroundImage = ResolveImage("TooltipPanel");
            }

            if (combatZones == null || combatZones.Length <= 0)
            {
                combatZones = GetComponentsInChildren<BattleCombatZoneView>(true);
            }
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

            for (int i = 0; i < combatZones.Length; i++)
            {
                BattleCombatZoneView zone = combatZones[i];
                if (zone == null)
                {
                    continue;
                }

                zone.SetCombatIndex(i);
                zone.SetClickHandler(HandleCombatZoneClicked);
                zone.EnsureRowsAndSlots();
            }
        }

        void InitializeDuelOrWarn()
        {
            database = GameDataRuntime.CurrentDatabase;
            if (database == null)
            {
                UnityEngine.Debug.LogWarning("[BattleScreenController] GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            sessionBuilder = new DuelSessionBuilder(database);
            turnProcessor = new DuelTurnProcessor(database);

            if (!sessionBuilder.TryCreateInitialState(enemyId, out DuelState state, out string failureMessage))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Failed to create duel state: {failureMessage}");
                return;
            }

            duelState = state;
            phaseRunner = new DuelPhaseRunner(duelState);
            if (!phaseRunner.StartDuel())
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenController] Failed to start duel: {phaseRunner.LastFailureReason}");
                return;
            }

            maxPlayerHealth = Mathf.Max(1, duelState.playerHealth);
            maxOpponentHealth = Mathf.Max(1, duelState.opponentHealth);

            if (!TryAdvanceToPlayerSetupForCurrentTurn(out string advanceFailure))
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenController] Failed to reach PlayerSetup after start: {advanceFailure}");
            }
        }

        void RefreshView()
        {
            ClearSpawnedCards();
            HideTooltip();

            UpdateTopBar();
            RenderHealth();
            RenderLoadoutRows();
            RenderCombatZones();
            UpdateButtonState();
        }

        void UpdateTopBar()
        {
            if (turnText == null)
            {
                return;
            }

            int turnIndex = duelState == null ? 0 : duelState.turnIndex;
            turnText.text = $"Turn: {turnIndex}";
            turnText.color = Colors.Semantic.TextPrimary;
        }

        void RenderHealth()
        {
            if (enemyHealthText != null)
            {
                enemyHealthText.text = BuildHeartText(
                    duelState == null ? 0 : duelState.opponentHealth,
                    maxOpponentHealth);
            }

            if (playerHealthText != null)
            {
                playerHealthText.text = BuildHeartText(
                    duelState == null ? 0 : duelState.playerHealth,
                    maxPlayerHealth);
            }
        }

        void RenderLoadoutRows()
        {
            if (duelState == null || database == null)
            {
                return;
            }

            List<BattleAbilityCardView.BindData> enemyCards = ExpandOpponentLoadoutCards();
            for (int i = 0; i < enemyCards.Count; i++)
            {
                CreateCardInLoadoutRow(
                    enemyLoadoutRow,
                    enemyCards[i],
                    isSelected: false,
                    isInteractable: false,
                    onClick: null);
            }

            List<string> playerLoadoutAbilityIds = duelState.loadoutAbilityIds == null
                ? new List<string>()
                : duelState.loadoutAbilityIds
                    .OrderBy(id => ResolveSortPriority(id))
                    .ThenBy(id => id, StringComparer.Ordinal)
                    .ToList();

            for (int i = 0; i < playerLoadoutAbilityIds.Count; i++)
            {
                string abilityId = playerLoadoutAbilityIds[i];
                if (!TryResolveAbilityAndDef(abilityId, out AbilityInstance ability, out AbilityDef def))
                {
                    continue;
                }

                bool isInteractable = !isFlowRunning &&
                    !duelState.isDuelEnded &&
                    phaseRunner != null &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    ability.abilityType == AbilityType.Attack;

                bool isSelected = string.Equals(selectedAbilityId, abilityId, StringComparison.Ordinal);
                BattleAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def);
                CreateCardInLoadoutRow(
                    playerLoadoutRow,
                    bindData,
                    isSelected,
                    isInteractable,
                    HandlePlayerAbilityCardClicked);
            }
        }

        void RenderCombatZones()
        {
            if (combatZones == null || combatZones.Length <= 0)
            {
                return;
            }

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                BattleCombatZoneView zone = combatZones[zoneIndex];
                if (zone == null)
                {
                    continue;
                }

                zone.EnsureRowsAndSlots();
                zone.SetCombatIndex(zoneIndex);

                int enemyTotal = 0;
                int playerTotal = 0;
                if (duelState != null &&
                    duelState.combats != null &&
                    zoneIndex >= 0 &&
                    zoneIndex < duelState.combats.Count &&
                    duelState.combats[zoneIndex] != null)
                {
                    CombatState combat = duelState.combats[zoneIndex];
                    combat.EnsureInitialized();
                    enemyTotal = DuelSimulator.ComputeTotalPower(combat, duelState.abilitiesById, false);
                    playerTotal = DuelSimulator.ComputeTotalPower(combat, duelState.abilitiesById, true);

                    ClearSlotCards(zone.EnemySlots);
                    ClearSlotCards(zone.PlayerSlots);

                    RenderCombatSideCards(zone.EnemySlots, combat.opponentAbilityIds, isPlayerSide: false);
                    RenderCombatSideCards(zone.PlayerSlots, combat.playerAbilityIds, isPlayerSide: true);
                }
                else
                {
                    ClearSlotCards(zone.EnemySlots);
                    ClearSlotCards(zone.PlayerSlots);
                }

                bool canDeployToZone = !isFlowRunning &&
                    duelState != null &&
                    !duelState.isDuelEnded &&
                    phaseRunner != null &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    !string.IsNullOrWhiteSpace(selectedAbilityId);

                zone.SetInteractable(canDeployToZone);
                zone.SetTotals(enemyTotal, playerTotal);
            }
        }

        void RenderCombatSideCards(
            IReadOnlyList<RectTransform> slots,
            List<string> abilityIds,
            bool isPlayerSide)
        {
            if (slots == null || abilityIds == null)
            {
                return;
            }

            int visibleCount = Mathf.Min(slots.Count, abilityIds.Count);
            if (abilityIds.Count > slots.Count)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenController] Slot overflow: abilityCount={abilityIds.Count}, slotCount={slots.Count}");
            }

            for (int i = 0; i < visibleCount; i++)
            {
                string abilityId = abilityIds[i];
                if (!TryResolveAbilityAndDef(abilityId, out AbilityInstance ability, out AbilityDef def))
                {
                    continue;
                }

                bool isSelected = string.Equals(selectedAbilityId, abilityId, StringComparison.Ordinal);
                bool isInteractable = isPlayerSide &&
                    !isFlowRunning &&
                    !duelState.isDuelEnded &&
                    phaseRunner != null &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    ability.abilityType == AbilityType.Attack;

                BattleAbilityCardView.BindData bindData = CreateBindData(abilityId, ability, def);
                CreateCardInSlot(
                    slots[i],
                    bindData,
                    isSelected,
                    isInteractable,
                    isPlayerSide ? HandlePlayerAbilityCardClicked : null);
            }
        }

        int ResolveSortPriority(string abilityId)
        {
            if (!TryResolveAbilityAndDef(abilityId, out AbilityInstance ability, out _))
            {
                return 999;
            }

            return ability.abilityType == AbilityType.Attack ? 0 : 1;
        }

        void UpdateButtonState()
        {
            bool isBusy = isFlowRunning || duelState == null || phaseRunner == null;

            if (combatStartButton != null)
            {
                bool canStart = !isBusy && !duelState.isDuelEnded;
                combatStartButton.interactable = canStart;
                ApplyButtonVisual(
                    combatStartButton,
                    canStart
                        ? defaultCombatStartButtonColor
                        : defaultButtonDisabledColor);
            }

            if (surrenderButton != null)
            {
                bool canSurrender = !isBusy &&
                    !duelState.isDuelEnded &&
                    phaseRunner.currentPhase == DuelPhase.PlayerSetup &&
                    duelState.honor > 0;
                surrenderButton.interactable = canSurrender;
                ApplyButtonVisual(
                    surrenderButton,
                    canSurrender
                        ? defaultSurrenderButtonColor
                        : defaultButtonDisabledColor);
            }
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
            if (isFlowRunning || phaseRunner == null || duelState == null)
            {
                return;
            }

            if (!phaseRunner.TrySurrender())
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenController] Surrender rejected: {phaseRunner.LastFailureReason}");
                return;
            }

            selectedAbilityId = string.Empty;
            RefreshView();
        }

        void HandlePlayerAbilityCardClicked(string abilityId)
        {
            if (isFlowRunning ||
                duelState == null ||
                phaseRunner == null ||
                phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                return;
            }

            if (!TryResolveAbilityAndDef(abilityId, out AbilityInstance ability, out _))
            {
                return;
            }

            if (ability.abilityType != AbilityType.Attack)
            {
                return;
            }

            selectedAbilityId = string.Equals(selectedAbilityId, abilityId, StringComparison.Ordinal)
                ? string.Empty
                : abilityId;
            RefreshView();
        }

        void HandleCombatZoneClicked(int combatIndex)
        {
            if (isFlowRunning ||
                duelState == null ||
                phaseRunner == null ||
                phaseRunner.currentPhase != DuelPhase.PlayerSetup ||
                string.IsNullOrWhiteSpace(selectedAbilityId))
            {
                return;
            }

            if (!TryMovePlayerAbilityToCombat(selectedAbilityId, combatIndex, out string failureMessage))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Ability move rejected: {failureMessage}");
                return;
            }

            RefreshView();
        }

        IEnumerator RunCombatStartFlow()
        {
            if (!TryEnsureReadyForCombatStart(out string ensureFailure))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Combat start rejected: {ensureFailure}");
                yield break;
            }

            isFlowRunning = true;
            RefreshView();

            if (!turnProcessor.TryRollAllDeployedAbilities(
                    duelState,
                    phaseRunner,
                    out DuelRollResult rollResult,
                    out string rollFailure))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Roll failed: {rollFailure}");
                isFlowRunning = false;
                RefreshView();
                yield break;
            }

            yield return AnimateRoll();
            RefreshView();

            if (!turnProcessor.TryResolveAllCombats(
                    duelState,
                    phaseRunner,
                    out DuelCombatResolveResult resolveResult,
                    out string resolveFailure))
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Resolve failed: {resolveFailure}");
                isFlowRunning = false;
                RefreshView();
                yield break;
            }

            yield return AnimateResolve(resolveResult);
            RefreshView();

            if (!duelState.isDuelEnded)
            {
                yield return AnimateTurnTransition();

                if (!TryAdvanceToPlayerSetupForCurrentTurn(out string advanceFailure))
                {
                    UnityEngine.Debug.LogWarning(
                        $"[BattleScreenController] Failed to advance to PlayerSetup after resolve: {advanceFailure}");
                }
            }

            selectedAbilityId = string.Empty;
            isFlowRunning = false;
            RefreshView();
        }

        IEnumerator AnimateRoll()
        {
            float duration = ResolveAnimationConfig().rollDuration;
            if (duration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = Mathf.PingPong(elapsed * 5f, 1f);

                for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
                {
                    BattleCombatZoneView zone = combatZones[zoneIndex];
                    zone?.SetRollPulse(normalized);
                }

                for (int i = 0; i < spawnedCards.Count; i++)
                {
                    if (spawnedCards[i] == null)
                    {
                        continue;
                    }

                    spawnedCards[i].SetRollPulse(normalized);
                }

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            for (int zoneIndex = 0; zoneIndex < combatZones.Length; zoneIndex++)
            {
                BattleCombatZoneView zone = combatZones[zoneIndex];
                zone?.RestoreBaseVisual();
            }

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] == null)
                {
                    continue;
                }

                spawnedCards[i].RestoreVisual();
            }
        }

        IEnumerator AnimateResolve(DuelCombatResolveResult resolveResult)
        {
            BattleAnimationConfig config = ResolveAnimationConfig();
            float duration = config.resolvePerCombatDuration;
            float gap = config.resolveCombatGap;

            for (int stepIndex = 0; stepIndex < resolveResult.steps.Count; stepIndex++)
            {
                DuelCombatResolveStepResult step = resolveResult.steps[stepIndex];
                if (step.combatIndex < 0 || step.combatIndex >= combatZones.Length)
                {
                    continue;
                }

                BattleCombatZoneView zone = combatZones[step.combatIndex];
                if (zone == null)
                {
                    continue;
                }

                float elapsed = 0f;
                while (elapsed < duration)
                {
                    float normalized = Mathf.PingPong(elapsed * 4f, 1f);
                    zone.SetResolveHighlight(step.outcome, normalized);

                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                zone.RestoreBaseVisual();

                if (gap > 0f && stepIndex < resolveResult.steps.Count - 1)
                {
                    yield return new WaitForSecondsRealtime(gap);
                }
            }
        }

        IEnumerator AnimateTurnTransition()
        {
            float duration = ResolveAnimationConfig().turnTransitionDuration;
            if (topBarImage == null || duration <= 0f)
            {
                if (duration > 0f)
                {
                    yield return new WaitForSecondsRealtime(duration);
                }

                yield break;
            }

            Color baseColor = defaultTopBarColor;
            Color pulseColor = Colors.Semantic.ActionSecondaryBgHover;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                float normalized = Mathf.PingPong(elapsed * 5f, 1f);
                topBarImage.color = Color.Lerp(baseColor, pulseColor, normalized);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            topBarImage.color = baseColor;
        }

        bool TryEnsureReadyForCombatStart(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null || phaseRunner == null || sessionBuilder == null || turnProcessor == null)
            {
                InitializeDuelOrWarn();
                if (duelState == null || phaseRunner == null || sessionBuilder == null || turnProcessor == null)
                {
                    failureMessage = "duel systems are not initialized.";
                    return false;
                }
            }

            if (duelState.isDuelEnded)
            {
                failureMessage = "duel already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == DuelPhase.Reset || phaseRunner.currentPhase == DuelPhase.OpponentSetup)
            {
                if (!TryAdvanceToPlayerSetupForCurrentTurn(out failureMessage))
                {
                    return false;
                }
            }

            if (phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.PlayerSetup}.";
                return false;
            }

            return true;
        }

        bool TryAdvanceToPlayerSetupForCurrentTurn(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (phaseRunner == null || duelState == null || sessionBuilder == null)
            {
                failureMessage = "duel systems are not initialized.";
                return false;
            }

            if (phaseRunner.currentPhase == DuelPhase.Reset)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter OpponentSetup ({phaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (phaseRunner.currentPhase == DuelPhase.OpponentSetup)
            {
                OpponentSetupBuildResult deployResult = sessionBuilder.AutoDeployOpponentCombat(duelState);
                if (deployResult.skippedCount > 0)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[BattleScreenController] Opponent deploy skipped abilities: {deployResult.skippedCount}");
                }

                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter PlayerSetup ({phaseRunner.LastFailureReason}).";
                    return false;
                }
            }

            if (phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"phase is {phaseRunner.currentPhase}, expected {DuelPhase.PlayerSetup}.";
                return false;
            }

            return true;
        }

        bool TryMovePlayerAbilityToCombat(string abilityId, int targetCombatIndex, out string failureMessage)
        {
            failureMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                failureMessage = "ability id is empty.";
                return false;
            }

            if (targetCombatIndex < 0 || duelState.combats == null || targetCombatIndex >= duelState.combats.Count)
            {
                failureMessage = $"target combat index({targetCombatIndex}) is out of range.";
                return false;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
            {
                failureMessage = $"ability({abilityId}) does not exist.";
                return false;
            }

            if (ability.abilityType != AbilityType.Attack)
            {
                failureMessage = $"ability({abilityId}) type is {ability.abilityType}, only Attack can be deployed.";
                return false;
            }

            if (!TryFindPlayerAbilityLocation(abilityId, out AbilityLocationType sourceLocation, out int sourceCombatIndex))
            {
                failureMessage = $"ability({abilityId}) is not in player controllable locations.";
                return false;
            }

            CombatState targetCombat = duelState.combats[targetCombatIndex];
            if (targetCombat == null)
            {
                failureMessage = $"combat({targetCombatIndex}) is missing.";
                return false;
            }

            targetCombat.EnsureInitialized();
            if (targetCombat.maxPlayerAssignments.HasValue &&
                targetCombat.maxPlayerAssignments.Value > 0 &&
                !targetCombat.playerAbilityIds.Contains(abilityId) &&
                targetCombat.playerAbilityIds.Count >= targetCombat.maxPlayerAssignments.Value)
            {
                failureMessage =
                    $"target combat({targetCombatIndex}) max assignments({targetCombat.maxPlayerAssignments.Value}) reached.";
                return false;
            }

            if (sourceLocation == AbilityLocationType.Loadout)
            {
                duelState.loadoutAbilityIds?.Remove(abilityId);
            }
            else if (sourceLocation == AbilityLocationType.Combat && sourceCombatIndex >= 0)
            {
                CombatState sourceCombat = duelState.combats[sourceCombatIndex];
                sourceCombat?.playerAbilityIds?.Remove(abilityId);
            }

            if (!targetCombat.playerAbilityIds.Contains(abilityId))
            {
                targetCombat.playerAbilityIds.Add(abilityId);
            }

            selectedAbilityId = abilityId;
            return true;
        }

        static Image EnsureImage(GameObject target, Color color)
        {
            if (target == null)
            {
                return null;
            }

            Image image = target.GetComponent<Image>();
            if (image == null)
            {
                image = target.AddComponent<Image>();
            }

            image.color = color;
            image.raycastTarget = true;
            return image;
        }

        RectTransform CreateRectTransform(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            var target = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            target.transform.SetParent(parent, false);

            RectTransform rect = target.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            return rect;
        }

        TMP_Text CreateText(
            string name,
            RectTransform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            string content,
            Color color,
            float fontSize,
            FontStyles style,
            TextAlignmentOptions alignment)
        {
            RectTransform rect = CreateRectTransform(
                name,
                parent,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta);

            TMP_Text text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = content ?? string.Empty;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return text;
        }

        Button CreateButton(
            string name,
            Transform parent,
            string label,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color backgroundColor,
            Color textColor)
        {
            RectTransform rect = CreateRectTransform(
                name,
                parent,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta);
            Image image = EnsureImage(rect.gameObject, backgroundColor);

            Button button = rect.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = image;

            CreateText(
                "Label",
                rect,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                label,
                textColor,
                34f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);
            return button;
        }

        RectTransform CreateLoadoutRow(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            RectTransform rect = CreateRectTransform(
                name,
                parent,
                anchorMin,
                anchorMax,
                pivot,
                anchoredPosition,
                sizeDelta);
            EnsureLoadoutLayout(rect);
            return rect;
        }

        BattleCombatZoneView[] CreateCombatZoneRow(Transform parent)
        {
            RectTransform rowRect = CreateRectTransform(
                "CombatZoneRow",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -6f),
                new Vector2(1828f, 360f));

            HorizontalLayoutGroup layout = rowRect.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            var zones = new BattleCombatZoneView[3];
            for (int i = 0; i < zones.Length; i++)
            {
                zones[i] = CreateCombatZone(rowRect, i);
            }

            return zones;
        }

        BattleCombatZoneView CreateCombatZone(RectTransform parent, int index)
        {
            RectTransform zoneRect = CreateRectTransform(
                $"CombatZone_{index}",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(600f, 360f));

            LayoutElement layoutElement = zoneRect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 600f;
            layoutElement.preferredHeight = 360f;

            Image zoneImage = EnsureImage(zoneRect.gameObject, Colors.Semantic.SurfaceParchment);
            Outline zoneOutline = zoneRect.gameObject.AddComponent<Outline>();
            zoneOutline.effectColor = Colors.Semantic.BorderParchment;
            zoneOutline.effectDistance = new Vector2(1f, -1f);

            Button zoneButton = zoneRect.gameObject.AddComponent<Button>();
            zoneButton.transition = Selectable.Transition.None;
            zoneButton.targetGraphic = zoneImage;

            RectTransform enemyRow = CreateRectTransform(
                "EnemySlotsRow",
                zoneRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 86f),
                new Vector2(400f, 84f));
            RectTransform playerRow = CreateRectTransform(
                "PlayerSlotsRow",
                zoneRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -86f),
                new Vector2(400f, 84f));

            RectTransform enemyTotalPanel = CreateRectTransform(
                "EnemyTotalPanel",
                zoneRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 26f),
                new Vector2(162f, 40f));
            EnsureImage(enemyTotalPanel.gameObject, Colors.Semantic.SurfaceSecondary);

            CreateText(
                "EnemyTotalText",
                enemyTotalPanel,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                "0",
                Colors.Semantic.StateDanger,
                38f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            RectTransform playerTotalPanel = CreateRectTransform(
                "PlayerTotalPanel",
                zoneRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -26f),
                new Vector2(162f, 40f));
            EnsureImage(playerTotalPanel.gameObject, Colors.Semantic.SurfaceSecondary);

            CreateText(
                "PlayerTotalText",
                playerTotalPanel,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                "0",
                Colors.Semantic.StateInfo,
                38f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            RectTransform dividerRect = CreateRectTransform(
                "MiddleDivider",
                zoneRect,
                new Vector2(0.02f, 0.5f),
                new Vector2(0.98f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(0f, 2f));
            EnsureImage(dividerRect.gameObject, Colors.Semantic.DividerParchment);

            BattleCombatZoneView zoneView = zoneRect.gameObject.AddComponent<BattleCombatZoneView>();
            zoneView.SetCombatIndex(index);
            zoneView.EnsureRowsAndSlots();
            return zoneView;
        }

        BattleAbilityCardView CreateAbilityCardTemplate(Transform parent)
        {
            RectTransform templateRoot = CreateRectTransform(
                "TemplateRoot",
                parent,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(-1000f, -1000f),
                Vector2.zero);

            RectTransform cardRect = CreateRectTransform(
                "AbilityCardTemplate",
                templateRoot,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(52f, 76f));
            EnsureImage(cardRect.gameObject, Colors.Semantic.SurfaceParchment);
            cardRect.gameObject.AddComponent<Button>().transition = Selectable.Transition.None;

            Outline outline = cardRect.gameObject.AddComponent<Outline>();
            outline.effectColor = Colors.Semantic.StateDanger;
            outline.effectDistance = new Vector2(2f, -2f);

            CreateRectTransform(
                "IconBackground",
                cardRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 4f),
                new Vector2(34f, 46f));
            EnsureImage(cardRect.Find("IconBackground").gameObject, Colors.Semantic.SurfaceParchmentMuted);

            CreateRectTransform(
                "IconImage",
                cardRect,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 4f),
                new Vector2(28f, 40f));
            EnsureImage(cardRect.Find("IconImage").gameObject, Colors.Semantic.SurfaceParchmentMuted);

            RectTransform badgeRect = CreateRectTransform(
                "PowerBadge",
                cardRect,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(-2f, 2f),
                new Vector2(22f, 22f));
            EnsureImage(badgeRect.gameObject, Colors.Semantic.SurfaceSecondary);

            CreateText(
                "PowerText",
                badgeRect,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero,
                "0",
                Colors.Semantic.TextPrimary,
                18f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            CreateText(
                "TitleText",
                cardRect,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -2f),
                new Vector2(-6f, 14f),
                string.Empty,
                Colors.Semantic.TextOnLightPrimary,
                9f,
                FontStyles.Bold,
                TextAlignmentOptions.Center);

            RectTransform disabledOverlayRect = CreateRectTransform(
                "DisabledOverlay",
                cardRect,
                Vector2.zero,
                Vector2.one,
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);
            EnsureImage(disabledOverlayRect.gameObject, Colors.Semantic.DisabledTint);

            BattleAbilityCardView templateView = cardRect.gameObject.AddComponent<BattleAbilityCardView>();
            cardRect.gameObject.SetActive(false);
            return templateView;
        }

        void ApplyStaticVisuals()
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = defaultBackgroundColor;
            }

            if (topBarImage != null)
            {
                topBarImage.color = defaultTopBarColor;
            }

            if (turnText != null)
            {
                turnText.color = defaultTooltipTextColor;
            }

            ApplyButtonVisual(combatStartButton, defaultCombatStartButtonColor);
            ApplyButtonVisual(surrenderButton, defaultSurrenderButtonColor);
            HideTooltip();
        }

        static void EnsureLoadoutLayout(RectTransform row)
        {
            if (row == null)
            {
                return;
            }

            if (!row.TryGetComponent(out HorizontalLayoutGroup layoutGroup))
            {
                layoutGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.spacing = 10f;
            layoutGroup.childControlWidth = false;
            layoutGroup.childControlHeight = false;
            layoutGroup.childForceExpandWidth = false;
            layoutGroup.childForceExpandHeight = false;

            if (!row.TryGetComponent(out ContentSizeFitter contentSizeFitter))
            {
                contentSizeFitter = row.gameObject.AddComponent<ContentSizeFitter>();
            }

            contentSizeFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        void ClearSpawnedCards()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                BattleAbilityCardView card = spawnedCards[i];
                if (card == null)
                {
                    continue;
                }

                Destroy(card.gameObject);
            }

            spawnedCards.Clear();
        }

        void ShowTooltip(string message)
        {
            if (tooltipText == null || tooltipBackgroundImage == null)
            {
                return;
            }

            tooltipText.text = string.IsNullOrWhiteSpace(message) ? "-" : message;
            tooltipText.color = defaultTooltipTextColor;
            tooltipText.gameObject.SetActive(true);

            tooltipBackgroundImage.color = defaultTooltipBackgroundColor;
            tooltipBackgroundImage.gameObject.SetActive(true);
        }

        void HideTooltip()
        {
            if (tooltipText != null)
            {
                tooltipText.text = string.Empty;
                tooltipText.gameObject.SetActive(false);
            }

            if (tooltipBackgroundImage != null)
            {
                tooltipBackgroundImage.gameObject.SetActive(false);
            }
        }

        string BuildHeartText(int currentHealth, int maxHealth)
        {
            int safeMax = Mathf.Max(1, maxHealth);
            int safeCurrent = Mathf.Clamp(currentHealth, 0, safeMax);

            const string fullHeart = "\u2665";
            const string emptyHeart = "\u2661";

            var builder = new StringBuilder(safeMax * 2);
            for (int i = 0; i < safeCurrent; i++)
            {
                builder.Append(fullHeart);
            }

            for (int i = safeCurrent; i < safeMax; i++)
            {
                builder.Append(emptyHeart);
            }

            return builder.ToString();
        }

        List<BattleAbilityCardView.BindData> ExpandOpponentLoadoutCards()
        {
            var cards = new List<BattleAbilityCardView.BindData>();

            if (duelState?.opponentLoadoutEntries == null || database?.abilitiesById == null)
            {
                return cards;
            }

            for (int i = 0; i < duelState.opponentLoadoutEntries.Count; i++)
            {
                OpponentLoadoutEntry entry = duelState.opponentLoadoutEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.abilityDefId))
                {
                    continue;
                }

                if (!database.abilitiesById.TryGetValue(entry.abilityDefId, out AbilityDef def) || def == null)
                {
                    UnityEngine.Debug.LogWarning($"[BattleScreenController] Missing opponent ability def: {entry.abilityDefId}");
                    continue;
                }

                AbilityType abilityType = AbilityType.Attack;
                if (!def.TryGetAbilityType(out abilityType))
                {
                    abilityType = AbilityType.Attack;
                }

                int count = Mathf.Max(0, entry.count);
                for (int copyIndex = 0; copyIndex < count; copyIndex++)
                {
                    string pseudoInstanceId = $"{entry.abilityDefId}#{copyIndex}";
                    cards.Add(new BattleAbilityCardView.BindData(
                        pseudoInstanceId,
                        def.id,
                        BuildAbilityTooltip(def),
                        abilityType,
                        Mathf.Max(0, def.ResolvePower()),
                        abilityType == AbilityType.Attack));
                }
            }

            return cards
                .OrderBy(bindData => bindData.abilityType == AbilityType.Attack ? 0 : 1)
                .ThenBy(bindData => bindData.title, StringComparer.Ordinal)
                .ToList();
        }

        bool TryResolveAbilityAndDef(string abilityId, out AbilityInstance ability, out AbilityDef def)
        {
            ability = null;
            def = null;

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (duelState?.abilitiesById == null ||
                !duelState.abilitiesById.TryGetValue(abilityId, out ability) ||
                ability == null)
            {
                UnityEngine.Debug.LogWarning($"[BattleScreenController] Missing ability instance: {abilityId}");
                return false;
            }

            if (database?.abilitiesById == null ||
                !database.abilitiesById.TryGetValue(ability.abilityDefId, out def) ||
                def == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleScreenController] Missing ability def for instance({abilityId}) defId({ability.abilityDefId}).");
                return false;
            }

            return true;
        }

        BattleAbilityCardView.BindData CreateBindData(string abilityId, AbilityInstance ability, AbilityDef def)
        {
            int displayPower = Mathf.Max(0, ability.power);
            if (ability.abilityType == AbilityType.Attack &&
                phaseRunner != null &&
                phaseRunner.currentPhase != DuelPhase.PlayerSetup &&
                ability.powerResult > 0)
            {
                displayPower = ability.powerResult;
            }

            return new BattleAbilityCardView.BindData(
                abilityId,
                def.id,
                BuildAbilityTooltip(def),
                ability.abilityType,
                displayPower,
                ability.abilityType == AbilityType.Attack);
        }

        BattleAbilityCardView CreateCardInLoadoutRow(
            RectTransform row,
            BattleAbilityCardView.BindData bindData,
            bool isSelected,
            bool isInteractable,
            Action<string> onClick)
        {
            if (row == null || abilityCardPrefab == null)
            {
                return null;
            }

            BattleAbilityCardView card = Instantiate(abilityCardPrefab, row);
            card.gameObject.SetActive(true);
            EnsureCardLayout(card);
            card.Bind(bindData, isSelected, isInteractable, onClick, ShowTooltip, HideTooltip);
            spawnedCards.Add(card);
            return card;
        }

        static void ClearSlotCards(IReadOnlyList<RectTransform> slots)
        {
            if (slots == null)
            {
                return;
            }

            for (int slotIndex = 0; slotIndex < slots.Count; slotIndex++)
            {
                RectTransform slot = slots[slotIndex];
                if (slot == null)
                {
                    continue;
                }

                int childCount = slot.childCount;
                for (int childIndex = childCount - 1; childIndex >= 0; childIndex--)
                {
                    Transform child = slot.GetChild(childIndex);
                    if (child == null)
                    {
                        continue;
                    }

                    Destroy(child.gameObject);
                }
            }
        }

        BattleAbilityCardView CreateCardInSlot(
            RectTransform slot,
            BattleAbilityCardView.BindData bindData,
            bool isSelected,
            bool isInteractable,
            Action<string> onClick)
        {
            if (slot == null || abilityCardPrefab == null)
            {
                return null;
            }

            BattleAbilityCardView card = Instantiate(abilityCardPrefab, slot);
            card.gameObject.SetActive(true);
            EnsureCardLayout(card);

            RectTransform cardRect = card.transform as RectTransform;
            if (cardRect != null)
            {
                cardRect.anchorMin = new Vector2(0.5f, 0.5f);
                cardRect.anchorMax = new Vector2(0.5f, 0.5f);
                cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;
            }

            card.Bind(bindData, isSelected, isInteractable, onClick, ShowTooltip, HideTooltip);
            spawnedCards.Add(card);
            return card;
        }

        static void EnsureCardLayout(BattleAbilityCardView card)
        {
            if (card == null)
            {
                return;
            }

            if (!card.TryGetComponent(out LayoutElement layoutElement))
            {
                layoutElement = card.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = 52f;
            layoutElement.preferredHeight = 76f;
            layoutElement.minWidth = 52f;
            layoutElement.minHeight = 76f;
            layoutElement.flexibleWidth = 0f;
            layoutElement.flexibleHeight = 0f;
        }

        static string BuildAbilityTooltip(AbilityDef def)
        {
            if (def == null)
            {
                return string.Empty;
            }

            string line1 = def.id;
            string line2 = string.IsNullOrWhiteSpace(def.descLocKey)
                ? string.Empty
                : def.descLocKey;
            return string.IsNullOrWhiteSpace(line2)
                ? line1
                : $"{line1}\n{line2}";
        }

        static void ApplyButtonVisual(Button button, Color backgroundColor)
        {
            if (button == null)
            {
                return;
            }

            if (button.TryGetComponent(out Image image))
            {
                image.color = backgroundColor;
            }
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

        bool TryFindPlayerAbilityLocation(string abilityId, out AbilityLocationType locationType, out int combatIndex)
        {
            locationType = AbilityLocationType.None;
            combatIndex = -1;

            if (duelState == null || string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (duelState.loadoutAbilityIds != null && duelState.loadoutAbilityIds.Contains(abilityId))
            {
                locationType = AbilityLocationType.Loadout;
                return true;
            }

            if (duelState.combats == null)
            {
                return false;
            }

            for (int i = 0; i < duelState.combats.Count; i++)
            {
                CombatState combat = duelState.combats[i];
                if (combat == null)
                {
                    continue;
                }

                combat.EnsureInitialized();
                if (!combat.playerAbilityIds.Contains(abilityId))
                {
                    continue;
                }

                locationType = AbilityLocationType.Combat;
                combatIndex = i;
                return true;
            }

            return false;
        }

        Image ResolveImage(string objectName)
        {
            return ResolveComponentByName<Image>(objectName);
        }

        TMP_Text ResolveText(string objectName)
        {
            return ResolveComponentByName<TMP_Text>(objectName);
        }

        Button ResolveButton(string objectName)
        {
            return ResolveComponentByName<Button>(objectName);
        }

        RectTransform ResolveRect(string objectName)
        {
            return ResolveComponentByName<RectTransform>(objectName);
        }

        T ResolveComponentByName<T>(string objectName)
            where T : Component
        {
            if (string.IsNullOrWhiteSpace(objectName))
            {
                return null;
            }

            T[] components = GetComponentsInChildren<T>(true);
            for (int i = 0; i < components.Length; i++)
            {
                T component = components[i];
                if (component == null || !string.Equals(component.name, objectName, StringComparison.Ordinal))
                {
                    continue;
                }

                return component;
            }

            return null;
        }
    }
}
