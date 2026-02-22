using System;
using System.Collections.Generic;
using Game.Application.Battle;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Presentation.Debug
{
    public sealed class BattleDebugPanel : MonoBehaviour
    {
        const string debugEncounterId = "enc_debug_01";
        const string troopBlockPrefabPath = "Assets/Prefabs/Ui/BattleTroopBlock.prefab";

        [Header("Status")]
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text turnText;
        [SerializeField] TMP_Text manaText;
        [SerializeField] TMP_Text stabilityText;
        [SerializeField] TMP_Text playerMoraleText;
        [SerializeField] TMP_Text enemyMoraleText;
        [SerializeField] TMP_Text selectedTroopText;
        [SerializeField] TMP_Text selectedBattlefieldText;

        [Header("Battlefields")]
        [SerializeField] TMP_Text battlefield0Text;
        [SerializeField] TMP_Text battlefield1Text;
        [SerializeField] TMP_Text battlefield2Text;

        [Header("Actions")]
        [SerializeField] Button startBattleButton;
        [SerializeField] Button enemyDeployButton;
        [SerializeField] Button playerDeployButton;
        [SerializeField] Button deploySelectedButton;
        [SerializeField] Button rollButton;
        [SerializeField] Button resolveButton;
        [SerializeField] Button retreatButton;
        [SerializeField] Button selectFirstCampTroopButton;
        [SerializeField] Button selectBattlefield0Button;
        [SerializeField] Button selectBattlefield1Button;
        [SerializeField] Button selectBattlefield2Button;

        [Header("Troop Blocks")]
        [SerializeField] BattleTroopBlockView troopBlockPrefab;
        [SerializeField] RectTransform campTroopBlockRoot;
        [SerializeField] RectTransform[] battlefieldPlayerTroopBlockRoots =
            new RectTransform[BattleState.defaultBattlefieldCount];
        [SerializeField] RectTransform[] battlefieldEnemyTroopBlockRoots =
            new RectTransform[BattleState.defaultBattlefieldCount];

        [Header("Debug")]
        [SerializeField] TMP_Text logText;
        [SerializeField] ScrollRect logScrollRect;
        [SerializeField] int maxLogEntryCount = 200;
        [SerializeField] int maxLogLineLength = 220;

        BattleState battleState;
        BattlePhaseRunner phaseRunner;
        BattleSessionBuilder sessionBuilder;
        BattleTurnProcessor turnProcessor;
        GameDatabase activeDatabase;

        string selectedTroopId = string.Empty;
        int selectedBattlefieldIndex = -1;

        readonly List<string> logEntries = new List<string>();
        readonly List<BattleTroopBlockView> spawnedTroopBlocks = new List<BattleTroopBlockView>();

        enum TroopLocationType
        {
            None = 0,
            Camp = 1,
            Battlefield = 2
        }

        public BattleState BattleState => battleState;
        public BattlePhaseRunner PhaseRunner => phaseRunner;

        void Awake()
        {
            if (!ValidateBindings(out string errorMessage))
            {
                UnityEngine.Debug.LogError($"[BattleDebugPanel] {errorMessage}");
                enabled = false;
                return;
            }

            ConfigureStaticUi();
            WireButtonCallbacks();
            ResetContext();
            RefreshView();
        }

        void OnDestroy()
        {
            ClearTroopBlocks();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            AutoAssignReferencesInEditor();
        }
#endif

        public void StartBattle()
        {
            GameDatabase database = GameDataRuntime.CurrentDatabase;
            if (database == null)
            {
                RejectAction("StartBattle", "GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            activeDatabase = database;
            sessionBuilder = new BattleSessionBuilder(activeDatabase);
            turnProcessor = new BattleTurnProcessor(activeDatabase);

            if (!sessionBuilder.TryCreateInitialState(debugEncounterId, out BattleState state, out string failureMessage))
            {
                RejectAction("StartBattle", failureMessage);
                return;
            }

            battleState = state;
            phaseRunner = new BattlePhaseRunner(battleState);
            if (!phaseRunner.StartBattle())
            {
                RejectAction("StartBattle", phaseRunner.LastFailureReason.ToString());
                return;
            }

            EnemyDeployBuildResult deployResult = sessionBuilder.AutoDeployEnemyIntent(battleState);
            AppendLog($"Enemy auto deploy complete: deployed={deployResult.deployedCount}, skipped={deployResult.skippedCount}");
            AppendLog($"StartBattle success: encounter={debugEncounterId}");
            RefreshView();
        }

        public void EnemyDeploy()
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("EnemyDeploy", failureMessage);
                return;
            }

            if (phaseRunner.currentPhase != BattlePhase.Recall)
            {
                RejectAction(
                    "EnemyDeploy",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Recall}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectAction("EnemyDeploy", phaseRunner.LastFailureReason.ToString());
                return;
            }

            AppendLog("EnemyDeploy phase entered.");
            RefreshView();
        }

        public void PlayerDeploy()
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("PlayerDeploy", failureMessage);
                return;
            }

            if (phaseRunner.currentPhase != BattlePhase.EnemyDeploy)
            {
                RejectAction(
                    "PlayerDeploy",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.EnemyDeploy}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectAction("PlayerDeploy", phaseRunner.LastFailureReason.ToString());
                return;
            }

            AppendLog("PlayerDeploy phase entered.");
            RefreshView();
        }

        public void Roll()
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("Roll", failureMessage);
                return;
            }

            if (turnProcessor == null)
            {
                RejectAction("Roll", "turn processor is not initialized.");
                return;
            }

            if (!turnProcessor.TryRollAllDeployedTroops(
                    battleState,
                    phaseRunner,
                    out BattleRollResult rollResult,
                    out string rollFailureMessage))
            {
                RejectAction("Roll", rollFailureMessage);
                return;
            }

            AppendLog($"Roll success: rolledTroops={rollResult.rolledTroopCount}");
            if (rollResult.timedEffectResult.failedCount > 0)
            {
                WarnAndLog(
                    $"Effect warning: timing(Roll) failed={rollResult.timedEffectResult.failedCount}, applied={rollResult.timedEffectResult.appliedCount}.");
            }
            else if (rollResult.timedEffectResult.appliedCount > 0)
            {
                AppendLog(
                    $"Effect applied: timing=Roll, applied={rollResult.timedEffectResult.appliedCount}, skipped={rollResult.timedEffectResult.skippedCount}.");
            }

            RefreshView();
        }

        public void Resolve()
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("Resolve", failureMessage);
                return;
            }

            if (turnProcessor == null)
            {
                RejectAction("Resolve", "turn processor is not initialized.");
                return;
            }

            if (!turnProcessor.TryResolveAllBattlefields(
                    battleState,
                    phaseRunner,
                    out BattleResolveResult resolveResult,
                    out string resolveFailureMessage))
            {
                RejectAction("Resolve", resolveFailureMessage);
                return;
            }

            for (int i = 0; i < resolveResult.steps.Count; i++)
            {
                BattleResolveStepResult step = resolveResult.steps[i];
                AppendLog(
                    $"Resolve[{step.battlefieldIndex}] outcome={step.outcome} totalAttack(P:{step.playerTotalAttack},E:{step.enemyTotalAttack}) morale(P:{battleState.playerMorale},E:{battleState.enemyMorale})");
            }

            if (resolveResult.outcomeEffectFailedCount > 0)
            {
                WarnAndLog(
                    $"Outcome effect warning: failed={resolveResult.outcomeEffectFailedCount}, applied={resolveResult.outcomeEffectAppliedCount}.");
            }

            if (resolveResult.turnEndTimedEffectResult.failedCount > 0)
            {
                WarnAndLog(
                    $"Effect warning: timing(TurnEnd) failed={resolveResult.turnEndTimedEffectResult.failedCount}, applied={resolveResult.turnEndTimedEffectResult.appliedCount}.");
            }
            else if (resolveResult.turnEndTimedEffectResult.appliedCount > 0)
            {
                AppendLog(
                    $"Effect applied: timing=TurnEnd, applied={resolveResult.turnEndTimedEffectResult.appliedCount}, skipped={resolveResult.turnEndTimedEffectResult.skippedCount}.");
            }

            if (!battleState.isBattleEnded)
            {
                AppendLog(
                    $"TurnEnd applied: mana {resolveResult.manaBeforeTurnEnd} -> {resolveResult.manaAfterTurnEnd}, cooldownUpdated={resolveResult.cooldownUpdatedCount}");
            }

            AppendLog($"Resolve success: resolvedBattlefields={resolveResult.steps.Count}");
            RefreshView();
        }

        public void Retreat()
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("Retreat", failureMessage);
                return;
            }

            if (!phaseRunner.TryRetreat())
            {
                RejectAction("Retreat", phaseRunner.LastFailureReason.ToString());
                return;
            }

            selectedTroopId = string.Empty;
            selectedBattlefieldIndex = -1;
            AppendLog("Retreat success: battle ended.");
            RefreshView();
        }

        public void SelectFirstCampTroop()
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("SelectFirstCampTroop", failureMessage);
                return;
            }

            if (battleState.campTroopIds == null || battleState.campTroopIds.Count <= 0)
            {
                RejectAction("SelectFirstCampTroop", "no troop exists in camp.");
                return;
            }

            SelectTroop(battleState.campTroopIds[0]);
        }

        public void SelectBattlefield0()
        {
            SelectBattlefield(0);
        }

        public void SelectBattlefield1()
        {
            SelectBattlefield(1);
        }

        public void SelectBattlefield2()
        {
            SelectBattlefield(2);
        }

        public void DeploySelected()
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("DeploySelected", failureMessage);
                return;
            }

            if (!TryMovePlayerTroopToBattlefield(selectedTroopId, selectedBattlefieldIndex, out string moveLog, out string moveError))
            {
                RejectAction("DeploySelected", moveError);
                return;
            }

            AppendLog($"DeploySelected success: {moveLog}");
            RefreshView();
        }

        public void SelectTroop(string troopId)
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("SelectTroop", failureMessage);
                return;
            }

            if (string.IsNullOrWhiteSpace(troopId))
            {
                RejectAction("SelectTroop", "troopId is empty.");
                return;
            }

            if (!battleState.troopsById.ContainsKey(troopId))
            {
                RejectAction("SelectTroop", $"troopId({troopId}) does not exist.");
                return;
            }

            if (!TryFindPlayerTroopLocation(troopId, out TroopLocationType locationType, out int battlefieldIndex))
            {
                RejectAction("SelectTroop", $"troopId({troopId}) is not in player controllable zones.");
                return;
            }

            selectedTroopId = troopId;
            if (locationType == TroopLocationType.Battlefield)
            {
                selectedBattlefieldIndex = battlefieldIndex;
            }

            AppendLog($"Troop selected: {ResolveTroopDefIdForDisplay(troopId)}");
            RefreshView();
        }

        public void SelectBattlefield(int battlefieldIndex)
        {
            if (!TryValidateBattleStarted(out string failureMessage))
            {
                RejectAction("SelectBattlefield", failureMessage);
                return;
            }

            if (battlefieldIndex < 0 || battlefieldIndex >= battleState.battlefields.Count)
            {
                RejectAction("SelectBattlefield", $"battlefieldIndex({battlefieldIndex}) is out of range.");
                return;
            }

            selectedBattlefieldIndex = battlefieldIndex;

            if (CanAutoDeployOnBattlefieldClick() && !string.IsNullOrWhiteSpace(selectedTroopId))
            {
                if (!TryMovePlayerTroopToBattlefield(
                        selectedTroopId,
                        selectedBattlefieldIndex,
                        out string moveLog,
                        out string moveError))
                {
                    RejectAction("SelectBattlefieldDeploy", moveError);
                    return;
                }

                AppendLog($"Battlefield click deploy success: {moveLog}");
                RefreshView();
                return;
            }

            AppendLog($"Battlefield selected: {battlefieldIndex}");
            RefreshView();
        }

        bool TryMovePlayerTroopToBattlefield(
            string troopId,
            int targetBattlefieldIndex,
            out string moveLog,
            out string failureMessage)
        {
            moveLog = string.Empty;
            failureMessage = string.Empty;

            if (phaseRunner.currentPhase != BattlePhase.PlayerDeploy)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.PlayerDeploy}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(troopId))
            {
                failureMessage = "troopId is empty.";
                return false;
            }

            if (targetBattlefieldIndex < 0 || targetBattlefieldIndex >= battleState.battlefields.Count)
            {
                failureMessage = $"target battlefield({targetBattlefieldIndex}) is out of range.";
                return false;
            }

            if (!TryFindPlayerTroopLocation(troopId, out TroopLocationType sourceType, out int sourceBattlefieldIndex))
            {
                failureMessage = $"troop({troopId}) is not in player controllable zones.";
                return false;
            }

            if (sourceType == TroopLocationType.Battlefield && sourceBattlefieldIndex == targetBattlefieldIndex)
            {
                moveLog = $"Troop move skipped: {ResolveTroopDefIdForDisplay(troopId)} already in battlefield({targetBattlefieldIndex}).";
                return true;
            }

            BattlefieldState targetBattlefield = battleState.battlefields[targetBattlefieldIndex];
            if (targetBattlefield == null)
            {
                failureMessage = $"battlefield({targetBattlefieldIndex}) is null.";
                return false;
            }

            targetBattlefield.EnsureInitialized();
            if (!targetBattlefield.playerTroopIds.Contains(troopId) &&
                targetBattlefield.slotLimit.HasValue &&
                targetBattlefield.playerTroopIds.Count >= targetBattlefield.slotLimit.Value)
            {
                failureMessage = $"target battlefield({targetBattlefieldIndex}) slotLimit exceeded.";
                return false;
            }

            if (sourceType == TroopLocationType.Camp)
            {
                battleState.campTroopIds.Remove(troopId);
            }
            else
            {
                BattlefieldState sourceBattlefield = battleState.battlefields[sourceBattlefieldIndex];
                sourceBattlefield?.playerTroopIds.Remove(troopId);
            }

            if (!targetBattlefield.playerTroopIds.Contains(troopId))
            {
                targetBattlefield.playerTroopIds.Add(troopId);
            }

            selectedTroopId = troopId;
            selectedBattlefieldIndex = targetBattlefieldIndex;
            moveLog = $"Troop moved: {ResolveTroopDefIdForDisplay(troopId)} -> battlefield({targetBattlefieldIndex}).";
            return true;
        }

        bool TryFindPlayerTroopLocation(string troopId, out TroopLocationType locationType, out int battlefieldIndex)
        {
            locationType = TroopLocationType.None;
            battlefieldIndex = -1;

            if (string.IsNullOrWhiteSpace(troopId))
            {
                return false;
            }

            if (battleState.campTroopIds != null && battleState.campTroopIds.Contains(troopId))
            {
                locationType = TroopLocationType.Camp;
                return true;
            }

            if (battleState.battlefields == null)
            {
                return false;
            }

            for (int i = 0; i < battleState.battlefields.Count; i++)
            {
                BattlefieldState battlefield = battleState.battlefields[i];
                if (battlefield == null)
                {
                    continue;
                }

                battlefield.EnsureInitialized();
                if (!battlefield.playerTroopIds.Contains(troopId))
                {
                    continue;
                }

                locationType = TroopLocationType.Battlefield;
                battlefieldIndex = i;
                return true;
            }

            return false;
        }

        bool CanAutoDeployOnBattlefieldClick()
        {
            return phaseRunner != null &&
                   phaseRunner.isStarted &&
                   !battleState.isBattleEnded &&
                   phaseRunner.currentPhase == BattlePhase.PlayerDeploy;
        }

        bool TryValidateBattleStarted(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (battleState == null)
            {
                failureMessage = "battle state is null.";
                return false;
            }

            if (phaseRunner == null || !phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            battleState.EnsureInitialized();
            return true;
        }

        void ResetContext()
        {
            battleState = new BattleState();
            phaseRunner = null;
            sessionBuilder = null;
            turnProcessor = null;
            activeDatabase = GameDataRuntime.CurrentDatabase;
            selectedTroopId = string.Empty;
            selectedBattlefieldIndex = -1;
            ClearTroopBlocks();
        }

        void RefreshView()
        {
            RefreshTexts();
            RefreshButtons();
            RefreshTroopBlocks();
        }

        void RefreshTexts()
        {
            SetText(phaseText, BattleDebugPanelFormatter.FormatPhase(phaseRunner));
            SetText(turnText, BattleDebugPanelFormatter.FormatTurn(battleState));
            SetText(manaText, BattleDebugPanelFormatter.FormatMana(battleState));
            SetText(stabilityText, BattleDebugPanelFormatter.FormatStability(battleState));
            SetText(playerMoraleText, BattleDebugPanelFormatter.FormatPlayerMorale(battleState));
            SetText(enemyMoraleText, BattleDebugPanelFormatter.FormatEnemyMorale(battleState));
            SetText(selectedTroopText, BattleDebugPanelFormatter.FormatSelectedTroop(battleState, selectedTroopId));
            SetText(
                selectedBattlefieldText,
                BattleDebugPanelFormatter.FormatSelectedBattlefield(battleState, selectedBattlefieldIndex));

            SetText(battlefield0Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 0, activeDatabase));
            SetText(battlefield1Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 1, activeDatabase));
            SetText(battlefield2Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 2, activeDatabase));
        }

        void RefreshButtons()
        {
            bool isStarted = phaseRunner != null && phaseRunner.isStarted;
            bool isEnded = battleState != null && battleState.isBattleEnded;
            BattlePhase currentPhase = phaseRunner == null ? BattlePhase.Recall : phaseRunner.currentPhase;
            bool canProgress = isStarted && !isEnded;

            SetButtonInteractable(startBattleButton, !isStarted || isEnded);
            SetButtonInteractable(enemyDeployButton, canProgress && currentPhase == BattlePhase.Recall);
            SetButtonInteractable(playerDeployButton, canProgress && currentPhase == BattlePhase.EnemyDeploy);
            SetButtonInteractable(deploySelectedButton, false);
            SetButtonInteractable(
                rollButton,
                canProgress &&
                (currentPhase == BattlePhase.PlayerDeploy || currentPhase == BattlePhase.Roll));
            SetButtonInteractable(
                resolveButton,
                canProgress &&
                (currentPhase == BattlePhase.Tactics || currentPhase == BattlePhase.Resolve));
            SetButtonInteractable(
                retreatButton,
                canProgress &&
                currentPhase == BattlePhase.PlayerDeploy &&
                battleState.stability > 0);
        }

        void RefreshTroopBlocks()
        {
            ClearTroopBlocks();

            if (battleState == null || battleState.troopsById == null)
            {
                return;
            }

            if (campTroopBlockRoot != null && battleState.campTroopIds != null)
            {
                for (int i = 0; i < battleState.campTroopIds.Count; i++)
                {
                    CreateTroopBlock(campTroopBlockRoot, battleState.campTroopIds[i], true, true);
                }
            }

            for (int battlefieldIndex = 0; battlefieldIndex < BattleState.defaultBattlefieldCount; battlefieldIndex++)
            {
                if (battlefieldIndex >= battleState.battlefields.Count)
                {
                    continue;
                }

                BattlefieldState battlefield = battleState.battlefields[battlefieldIndex];
                if (battlefield == null)
                {
                    continue;
                }

                battlefield.EnsureInitialized();

                RectTransform playerRoot = battlefieldPlayerTroopBlockRoots[battlefieldIndex];
                if (playerRoot != null)
                {
                    for (int i = 0; i < battlefield.playerTroopIds.Count; i++)
                    {
                        CreateTroopBlock(playerRoot, battlefield.playerTroopIds[i], true, true);
                    }
                }

                RectTransform enemyRoot = battlefieldEnemyTroopBlockRoots[battlefieldIndex];
                if (enemyRoot != null)
                {
                    for (int i = 0; i < battlefield.enemyTroopIds.Count; i++)
                    {
                        CreateTroopBlock(enemyRoot, battlefield.enemyTroopIds[i], false, false);
                    }
                }
            }
        }

        void CreateTroopBlock(
            RectTransform parent,
            string troopId,
            bool isPlayerSide,
            bool canSelect)
        {
            if (parent == null || string.IsNullOrWhiteSpace(troopId))
            {
                return;
            }

            if (!battleState.troopsById.TryGetValue(troopId, out TroopInstance troop) || troop == null)
            {
                WarnAndLog($"Troop block warning: troopId({troopId}) does not exist.");
                return;
            }

            string troopDefId = ResolveTroopDefIdForDisplay(troopId);
            string effectsLabel = BattleDebugPanelFormatter.FormatTroopEffects(activeDatabase, troop.troopDefId);
            bool isSelected = string.Equals(troopId, selectedTroopId, StringComparison.Ordinal);

            BattleTroopBlockView troopBlock = Instantiate(troopBlockPrefab, parent);
            troopBlock.name = $"TroopBlock_{troopId}";
            troopBlock.Bind(
                troopDefId,
                troop.attack,
                troop.attackResult,
                effectsLabel,
                isPlayerSide,
                isSelected,
                canSelect,
                () => SelectTroop(troopId));

            spawnedTroopBlocks.Add(troopBlock);
        }

        void ClearTroopBlocks()
        {
            for (int i = 0; i < spawnedTroopBlocks.Count; i++)
            {
                BattleTroopBlockView troopBlock = spawnedTroopBlocks[i];
                if (troopBlock == null)
                {
                    continue;
                }

                Destroy(troopBlock.gameObject);
            }

            spawnedTroopBlocks.Clear();
        }

        string ResolveTroopDefIdForDisplay(string troopId)
        {
            if (battleState == null ||
                battleState.troopsById == null ||
                string.IsNullOrWhiteSpace(troopId))
            {
                return "(no-def)";
            }

            if (!battleState.troopsById.TryGetValue(troopId, out TroopInstance troop) ||
                troop == null ||
                string.IsNullOrWhiteSpace(troop.troopDefId))
            {
                return "(no-def)";
            }

            return troop.troopDefId;
        }

        void RejectAction(string actionName, string reason)
        {
            WarnAndLog($"{actionName} rejected: {reason}");
            RefreshView();
        }

        void WarnAndLog(string message)
        {
            UnityEngine.Debug.LogWarning($"[BattleDebugPanel] {message}");
            AppendLog(message);
        }

        void AppendLog(string message)
        {
            string normalizedMessage = NormalizeLogMessage(message);
            if (string.IsNullOrWhiteSpace(normalizedMessage))
            {
                return;
            }

            logEntries.Insert(0, normalizedMessage);
            TrimLogEntries();
            ApplyLogEntriesToText();
            SnapLogScrollToTop();

            UnityEngine.Debug.Log($"[BattleDebugPanel] {normalizedMessage}");
        }

        void TrimLogEntries()
        {
            int clampedMaxCount = Mathf.Max(1, maxLogEntryCount);
            if (logEntries.Count <= clampedMaxCount)
            {
                return;
            }

            int removeCount = logEntries.Count - clampedMaxCount;
            logEntries.RemoveRange(logEntries.Count - removeCount, removeCount);
        }

        void ApplyLogEntriesToText()
        {
            if (logText == null)
            {
                return;
            }

            logText.text = logEntries.Count <= 0
                ? "-"
                : string.Join("\n", logEntries);
        }

        void SnapLogScrollToTop()
        {
            if (logScrollRect == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            logScrollRect.verticalNormalizedPosition = 1f;
        }

        void ConfigureStaticUi()
        {
            ConfigureButtonLabelAlignment(startBattleButton);
            ConfigureButtonLabelAlignment(enemyDeployButton);
            ConfigureButtonLabelAlignment(playerDeployButton);
            ConfigureButtonLabelAlignment(deploySelectedButton);
            ConfigureButtonLabelAlignment(rollButton);
            ConfigureButtonLabelAlignment(resolveButton);
            ConfigureButtonLabelAlignment(retreatButton);
            ConfigureButtonLabelAlignment(selectFirstCampTroopButton);

            if (logText != null)
            {
                logText.textWrappingMode = TextWrappingModes.Normal;
                logText.overflowMode = TextOverflowModes.Ellipsis;
                logText.alignment = TextAlignmentOptions.TopLeft;
            }

            if (logScrollRect != null)
            {
                logScrollRect.horizontal = false;
                logScrollRect.vertical = true;
                logScrollRect.movementType = ScrollRect.MovementType.Clamped;
            }

            if (deploySelectedButton != null)
            {
                deploySelectedButton.gameObject.SetActive(false);
            }
        }

        string NormalizeLogMessage(string rawMessage)
        {
            if (string.IsNullOrWhiteSpace(rawMessage))
            {
                return string.Empty;
            }

            string singleLine = rawMessage
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();

            int clampedMaxLength = Mathf.Max(16, maxLogLineLength);
            if (singleLine.Length <= clampedMaxLength)
            {
                return singleLine;
            }

            return $"{singleLine.Substring(0, clampedMaxLength - 3)}...";
        }

        static void ConfigureButtonLabelAlignment(Button button)
        {
            if (button == null)
            {
                return;
            }

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label == null)
            {
                return;
            }

            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
        }

        void WireButtonCallbacks()
        {
            BindButton(startBattleButton, StartBattle);
            BindButton(enemyDeployButton, EnemyDeploy);
            BindButton(playerDeployButton, PlayerDeploy);
            BindButton(deploySelectedButton, DeploySelected);
            BindButton(rollButton, Roll);
            BindButton(resolveButton, Resolve);
            BindButton(retreatButton, Retreat);
            BindButton(selectFirstCampTroopButton, SelectFirstCampTroop);
            BindButton(selectBattlefield0Button, SelectBattlefield0);
            BindButton(selectBattlefield1Button, SelectBattlefield1);
            BindButton(selectBattlefield2Button, SelectBattlefield2);
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        bool ValidateBindings(out string errorMessage)
        {
            var missing = new List<string>();

            ValidateRequired(phaseText, nameof(phaseText), missing);
            ValidateRequired(turnText, nameof(turnText), missing);
            ValidateRequired(manaText, nameof(manaText), missing);
            ValidateRequired(stabilityText, nameof(stabilityText), missing);
            ValidateRequired(playerMoraleText, nameof(playerMoraleText), missing);
            ValidateRequired(enemyMoraleText, nameof(enemyMoraleText), missing);
            ValidateRequired(selectedTroopText, nameof(selectedTroopText), missing);
            ValidateRequired(selectedBattlefieldText, nameof(selectedBattlefieldText), missing);
            ValidateRequired(battlefield0Text, nameof(battlefield0Text), missing);
            ValidateRequired(battlefield1Text, nameof(battlefield1Text), missing);
            ValidateRequired(battlefield2Text, nameof(battlefield2Text), missing);

            ValidateRequired(startBattleButton, nameof(startBattleButton), missing);
            ValidateRequired(enemyDeployButton, nameof(enemyDeployButton), missing);
            ValidateRequired(playerDeployButton, nameof(playerDeployButton), missing);
            ValidateRequired(rollButton, nameof(rollButton), missing);
            ValidateRequired(resolveButton, nameof(resolveButton), missing);
            ValidateRequired(retreatButton, nameof(retreatButton), missing);
            ValidateRequired(selectFirstCampTroopButton, nameof(selectFirstCampTroopButton), missing);
            ValidateRequired(selectBattlefield0Button, nameof(selectBattlefield0Button), missing);
            ValidateRequired(selectBattlefield1Button, nameof(selectBattlefield1Button), missing);
            ValidateRequired(selectBattlefield2Button, nameof(selectBattlefield2Button), missing);

            ValidateRequired(troopBlockPrefab, nameof(troopBlockPrefab), missing);
            ValidateRequired(campTroopBlockRoot, nameof(campTroopBlockRoot), missing);
            ValidateRequired(logText, nameof(logText), missing);
            ValidateRequired(logScrollRect, nameof(logScrollRect), missing);

            if (battlefieldPlayerTroopBlockRoots == null ||
                battlefieldPlayerTroopBlockRoots.Length != BattleState.defaultBattlefieldCount)
            {
                missing.Add(nameof(battlefieldPlayerTroopBlockRoots));
            }
            else
            {
                for (int i = 0; i < battlefieldPlayerTroopBlockRoots.Length; i++)
                {
                    if (battlefieldPlayerTroopBlockRoots[i] == null)
                    {
                        missing.Add($"{nameof(battlefieldPlayerTroopBlockRoots)}[{i}]");
                    }
                }
            }

            if (battlefieldEnemyTroopBlockRoots == null ||
                battlefieldEnemyTroopBlockRoots.Length != BattleState.defaultBattlefieldCount)
            {
                missing.Add(nameof(battlefieldEnemyTroopBlockRoots));
            }
            else
            {
                for (int i = 0; i < battlefieldEnemyTroopBlockRoots.Length; i++)
                {
                    if (battlefieldEnemyTroopBlockRoots[i] == null)
                    {
                        missing.Add($"{nameof(battlefieldEnemyTroopBlockRoots)}[{i}]");
                    }
                }
            }

            if (missing.Count > 0)
            {
                errorMessage = $"Missing serialized references: {string.Join(", ", missing)}";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        static void ValidateRequired(UnityEngine.Object target, string fieldName, List<string> missing)
        {
            if (target == null)
            {
                missing.Add(fieldName);
            }
        }

#if UNITY_EDITOR
        void AutoAssignReferencesInEditor()
        {
            phaseText = AssignByName(phaseText, "PhaseText");
            turnText = AssignByName(turnText, "TurnText");
            manaText = AssignByName(manaText, "ManaText");
            stabilityText = AssignByName(stabilityText, "StabilityText");
            playerMoraleText = AssignByName(playerMoraleText, "PlayerMoraleText");
            enemyMoraleText = AssignByName(enemyMoraleText, "EnemyMoraleText");
            selectedTroopText = AssignByName(selectedTroopText, "SelectedTroopText");
            selectedBattlefieldText = AssignByName(selectedBattlefieldText, "SelectedBattlefieldText");

            battlefield0Text = AssignByName(battlefield0Text, "Battlefield0Text");
            battlefield1Text = AssignByName(battlefield1Text, "Battlefield1Text");
            battlefield2Text = AssignByName(battlefield2Text, "Battlefield2Text");

            startBattleButton = AssignByName(startBattleButton, "StartBattleButton");
            enemyDeployButton = AssignByName(enemyDeployButton, "EnemyDeployButton");
            playerDeployButton = AssignByName(playerDeployButton, "PlayerDeployButton");
            deploySelectedButton = AssignByName(deploySelectedButton, "DeploySelectedButton");
            rollButton = AssignByName(rollButton, "RollButton");
            resolveButton = AssignByName(resolveButton, "ResolveButton");
            retreatButton = AssignByName(retreatButton, "RetreatButton");
            selectFirstCampTroopButton = AssignByName(selectFirstCampTroopButton, "SelectFirstCampTroopButton");
            selectBattlefield0Button = AssignByName(selectBattlefield0Button, "SelectBattlefield0Button");
            selectBattlefield1Button = AssignByName(selectBattlefield1Button, "SelectBattlefield1Button");
            selectBattlefield2Button = AssignByName(selectBattlefield2Button, "SelectBattlefield2Button");

            campTroopBlockRoot = AssignByName(campTroopBlockRoot, "CampTroopBlockRoot");
            EnsureTroopRootArrays();
            battlefieldPlayerTroopBlockRoots[0] = AssignByName(
                battlefieldPlayerTroopBlockRoots[0],
                "Battlefield0PlayerTroopBlockRoot");
            battlefieldPlayerTroopBlockRoots[1] = AssignByName(
                battlefieldPlayerTroopBlockRoots[1],
                "Battlefield1PlayerTroopBlockRoot");
            battlefieldPlayerTroopBlockRoots[2] = AssignByName(
                battlefieldPlayerTroopBlockRoots[2],
                "Battlefield2PlayerTroopBlockRoot");

            battlefieldEnemyTroopBlockRoots[0] = AssignByName(
                battlefieldEnemyTroopBlockRoots[0],
                "Battlefield0EnemyTroopBlockRoot");
            battlefieldEnemyTroopBlockRoots[1] = AssignByName(
                battlefieldEnemyTroopBlockRoots[1],
                "Battlefield1EnemyTroopBlockRoot");
            battlefieldEnemyTroopBlockRoots[2] = AssignByName(
                battlefieldEnemyTroopBlockRoots[2],
                "Battlefield2EnemyTroopBlockRoot");

            logText = AssignByName(logText, "LogText");
            logScrollRect = AssignByName(logScrollRect, "LogScrollRect");

            if (troopBlockPrefab == null)
            {
                troopBlockPrefab = AssetDatabase.LoadAssetAtPath<BattleTroopBlockView>(troopBlockPrefabPath);
            }
        }

        void EnsureTroopRootArrays()
        {
            if (battlefieldPlayerTroopBlockRoots == null ||
                battlefieldPlayerTroopBlockRoots.Length != BattleState.defaultBattlefieldCount)
            {
                battlefieldPlayerTroopBlockRoots = new RectTransform[BattleState.defaultBattlefieldCount];
            }

            if (battlefieldEnemyTroopBlockRoots == null ||
                battlefieldEnemyTroopBlockRoots.Length != BattleState.defaultBattlefieldCount)
            {
                battlefieldEnemyTroopBlockRoots = new RectTransform[BattleState.defaultBattlefieldCount];
            }
        }

        TComponent AssignByName<TComponent>(TComponent currentValue, string objectName)
            where TComponent : Component
        {
            if (currentValue != null)
            {
                return currentValue;
            }

            Transform[] allChildren = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < allChildren.Length; i++)
            {
                Transform child = allChildren[i];
                if (!string.Equals(child.name, objectName, StringComparison.Ordinal))
                {
                    continue;
                }

                if (child.TryGetComponent(out TComponent component))
                {
                    return component;
                }
            }

            return null;
        }
#endif

        static void SetText(TMP_Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            target.text = value;
        }

        static void SetButtonInteractable(Button button, bool isInteractable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = isInteractable;
        }
    }
}
