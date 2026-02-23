using System;
using System.Collections.Generic;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Presentation.Debug
{
    public sealed class DuelDebugPanel : MonoBehaviour
    {
        const string debugEncounterId = "encounter.debug.01";
        const string actionBlockPrefabPath = "Assets/Prefabs/Ui/DuelActionBlock.prefab";

        [Header("Status")]
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text turnText;
        [FormerlySerializedAs("manaText")]
        [SerializeField] TMP_Text focusText;
        [FormerlySerializedAs("stabilityText")]
        [SerializeField] TMP_Text honorText;
        [SerializeField] TMP_Text playerHealthText;
        [SerializeField] TMP_Text opponentHealthText;
        [FormerlySerializedAs("selectedTroopText")]
        [SerializeField] TMP_Text selectedActionText;
        [FormerlySerializedAs("selectedBattlefieldText")]
        [SerializeField] TMP_Text selectedClashText;

        [Header("Clashes")]
        [FormerlySerializedAs("battlefield0Text")]
        [SerializeField] TMP_Text clash0Text;
        [FormerlySerializedAs("battlefield1Text")]
        [SerializeField] TMP_Text clash1Text;
        [FormerlySerializedAs("battlefield2Text")]
        [SerializeField] TMP_Text clash2Text;

        [Header("Actions")]
        [FormerlySerializedAs("startBattleButton")]
        [SerializeField] Button startDuelButton;
        [FormerlySerializedAs("enemyDeployButton")]
        [SerializeField] Button opponentDeployButton;
        [SerializeField] Button playerDeployButton;
        [SerializeField] Button deploySelectedButton;
        [SerializeField] Button rollButton;
        [SerializeField] Button resolveButton;
        [SerializeField] Button retreatButton;
        [FormerlySerializedAs("selectFirstCampTroopButton")]
        [SerializeField] Button selectFirstActionHolderActionButton;
        [FormerlySerializedAs("selectBattlefield0Button")]
        [SerializeField] Button selectClash0Button;
        [FormerlySerializedAs("selectBattlefield1Button")]
        [SerializeField] Button selectClash1Button;
        [FormerlySerializedAs("selectBattlefield2Button")]
        [SerializeField] Button selectClash2Button;

        [Header("Action Blocks")]
        [FormerlySerializedAs("troopBlockPrefab")]
        [SerializeField] DuelActionBlockView actionBlockPrefab;
        [FormerlySerializedAs("campTroopBlockRoot")]
        [SerializeField] RectTransform actionHolderActionBlockRoot;
        [FormerlySerializedAs("battlefieldPlayerTroopBlockRoots")]
        [SerializeField] RectTransform[] clashPlayerActionBlockRoots =
            new RectTransform[DuelState.defaultClashCount];
        [FormerlySerializedAs("battlefieldOpponentActionBlockRoots")]
        [SerializeField] RectTransform[] clashOpponentActionBlockRoots =
            new RectTransform[DuelState.defaultClashCount];

        [Header("Debug")]
        [SerializeField] TMP_Text logText;
        [SerializeField] ScrollRect logScrollRect;
        [SerializeField] int maxLogEntryCount = 200;
        [SerializeField] int maxLogLineLength = 220;

        DuelState duelState;
        DuelPhaseRunner phaseRunner;
        DuelSessionBuilder sessionBuilder;
        DuelTurnProcessor turnProcessor;
        GameDatabase activeDatabase;

        string selectedActionId = string.Empty;
        int selectedClashIndex = -1;

        readonly List<string> logEntries = new List<string>();
        readonly List<DuelActionBlockView> spawnedActionBlocks = new List<DuelActionBlockView>();

        enum ActionLocationType
        {
            None = 0,
            ActionHolder = 1,
            Clash = 2
        }

        public DuelState DuelState => duelState;
        public DuelPhaseRunner PhaseRunner => phaseRunner;

        void Awake()
        {
            if (!ValidateBindings(out string errorMessage))
            {
                UnityEngine.Debug.LogError($"[DuelDebugPanel] {errorMessage}");
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
            ClearActionBlocks();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            AutoAssignReferencesInEditor();
        }
#endif

        public void StartDuel()
        {
            GameDatabase database = GameDataRuntime.CurrentDatabase;
            if (database == null)
            {
                RejectAction("StartDuel", "GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            activeDatabase = database;
            sessionBuilder = new DuelSessionBuilder(activeDatabase);
            turnProcessor = new DuelTurnProcessor(activeDatabase);

            if (!sessionBuilder.TryCreateInitialState(debugEncounterId, out DuelState state, out string failureMessage))
            {
                RejectAction("StartDuel", failureMessage);
                return;
            }

            duelState = state;
            phaseRunner = new DuelPhaseRunner(duelState);
            if (!phaseRunner.StartDuel())
            {
                RejectAction("StartDuel", phaseRunner.LastFailureReason.ToString());
                return;
            }

            OpponentSetupBuildResult deployResult = sessionBuilder.AutoDeployOpponentIntent(duelState);
            AppendLog($"Opponent auto deploy complete: deployed={deployResult.deployedCount}, skipped={deployResult.skippedCount}");
            AppendLog($"StartDuel success: encounter={debugEncounterId}");
            RefreshView();
        }

        public void OpponentSetup()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("OpponentSetup", failureMessage);
                return;
            }

            if (phaseRunner.currentPhase != DuelPhase.Reset)
            {
                RejectAction(
                    "OpponentSetup",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.Reset}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectAction("OpponentSetup", phaseRunner.LastFailureReason.ToString());
                return;
            }

            AppendLog("OpponentSetup phase entered.");
            RefreshView();
        }

        public void PlayerSetup()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("PlayerSetup", failureMessage);
                return;
            }

            if (phaseRunner.currentPhase != DuelPhase.OpponentSetup)
            {
                RejectAction(
                    "PlayerSetup",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.OpponentSetup}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectAction("PlayerSetup", phaseRunner.LastFailureReason.ToString());
                return;
            }

            AppendLog("PlayerSetup phase entered.");
            RefreshView();
        }

        public void Roll()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("Roll", failureMessage);
                return;
            }

            if (turnProcessor == null)
            {
                RejectAction("Roll", "turn processor is not initialized.");
                return;
            }

            if (!turnProcessor.TryRollAllDeployedActions(
                    duelState,
                    phaseRunner,
                    out DuelRollResult rollResult,
                    out string rollFailureMessage))
            {
                RejectAction("Roll", rollFailureMessage);
                return;
            }

            AppendLog($"Roll success: rolledActions={rollResult.rolledActionCount}");
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

        public void ClashResolve()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("ClashResolve", failureMessage);
                return;
            }

            if (turnProcessor == null)
            {
                RejectAction("ClashResolve", "turn processor is not initialized.");
                return;
            }

            if (!turnProcessor.TryClashResolveAllClashes(
                    duelState,
                    phaseRunner,
                    out DuelClashResolveResult resolveResult,
                    out string resolveFailureMessage))
            {
                RejectAction("ClashResolve", resolveFailureMessage);
                return;
            }

            for (int i = 0; i < resolveResult.steps.Count; i++)
            {
                DuelClashResolveStepResult step = resolveResult.steps[i];
                AppendLog(
                    $"ClashResolve[{step.clashIndex}] outcome={step.outcome} totalAttack(P:{step.playerTotalAttack},E:{step.opponentTotalAttack}) health(P:{duelState.playerHealth},E:{duelState.opponentHealth})");
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

            if (!duelState.isDuelEnded)
            {
                AppendLog(
                    $"TurnEnd applied: focus {resolveResult.focusBeforeTurnEnd} -> {resolveResult.focusAfterTurnEnd}, cooldownUpdated={resolveResult.cooldownUpdatedCount}");
            }

            AppendLog($"ClashResolve success: resolvedClashes={resolveResult.steps.Count}");
            RefreshView();
        }

        public void Retreat()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("Retreat", failureMessage);
                return;
            }

            if (!phaseRunner.TryRetreat())
            {
                RejectAction("Retreat", phaseRunner.LastFailureReason.ToString());
                return;
            }

            selectedActionId = string.Empty;
            selectedClashIndex = -1;
            AppendLog("Retreat success: duel ended.");
            RefreshView();
        }

        public void SelectFirstActionHolderAction()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("SelectFirstActionHolderAction", failureMessage);
                return;
            }

            if (duelState.actionHolderActionIds == null || duelState.actionHolderActionIds.Count <= 0)
            {
                RejectAction("SelectFirstActionHolderAction", "no action exists in actionHolder.");
                return;
            }

            SelectAction(duelState.actionHolderActionIds[0]);
        }

        public void SelectClash0()
        {
            SelectClash(0);
        }

        public void SelectClash1()
        {
            SelectClash(1);
        }

        public void SelectClash2()
        {
            SelectClash(2);
        }

        public void DeploySelected()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("DeploySelected", failureMessage);
                return;
            }

            if (!TryMovePlayerActionToClash(selectedActionId, selectedClashIndex, out string moveLog, out string moveError))
            {
                RejectAction("DeploySelected", moveError);
                return;
            }

            AppendLog($"DeploySelected success: {moveLog}");
            RefreshView();
        }

        public void SelectAction(string actionId)
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("SelectAction", failureMessage);
                return;
            }

            if (string.IsNullOrWhiteSpace(actionId))
            {
                RejectAction("SelectAction", "actionId is empty.");
                return;
            }

            if (!duelState.actionsById.ContainsKey(actionId))
            {
                RejectAction("SelectAction", $"actionId({actionId}) does not exist.");
                return;
            }

            if (!TryFindPlayerActionLocation(actionId, out ActionLocationType locationType, out int clashIndex))
            {
                RejectAction("SelectAction", $"actionId({actionId}) is not in player controllable zones.");
                return;
            }

            selectedActionId = actionId;
            if (locationType == ActionLocationType.Clash)
            {
                selectedClashIndex = clashIndex;
            }

            AppendLog($"Action selected: {ClashResolveActionDefIdForDisplay(actionId)}");
            RefreshView();
        }

        public void SelectClash(int clashIndex)
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectAction("SelectClash", failureMessage);
                return;
            }

            if (clashIndex < 0 || clashIndex >= duelState.clashes.Count)
            {
                RejectAction("SelectClash", $"clashIndex({clashIndex}) is out of range.");
                return;
            }

            selectedClashIndex = clashIndex;

            if (CanAutoDeployOnClashClick() && !string.IsNullOrWhiteSpace(selectedActionId))
            {
                if (!TryMovePlayerActionToClash(
                        selectedActionId,
                        selectedClashIndex,
                        out string moveLog,
                        out string moveError))
                {
                    RejectAction("SelectClashDeploy", moveError);
                    return;
                }

                AppendLog($"Clash click deploy success: {moveLog}");
                RefreshView();
                return;
            }

            AppendLog($"Clash selected: {clashIndex}");
            RefreshView();
        }

        bool TryMovePlayerActionToClash(
            string actionId,
            int targetClashIndex,
            out string moveLog,
            out string failureMessage)
        {
            moveLog = string.Empty;
            failureMessage = string.Empty;

            if (phaseRunner.currentPhase != DuelPhase.PlayerSetup)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.PlayerSetup}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(actionId))
            {
                failureMessage = "actionId is empty.";
                return false;
            }

            if (targetClashIndex < 0 || targetClashIndex >= duelState.clashes.Count)
            {
                failureMessage = $"target clash({targetClashIndex}) is out of range.";
                return false;
            }

            if (!TryFindPlayerActionLocation(actionId, out ActionLocationType sourceType, out int sourceClashIndex))
            {
                failureMessage = $"action({actionId}) is not in player controllable zones.";
                return false;
            }

            if (sourceType == ActionLocationType.Clash && sourceClashIndex == targetClashIndex)
            {
                moveLog = $"Action move skipped: {ClashResolveActionDefIdForDisplay(actionId)} already in clash({targetClashIndex}).";
                return true;
            }

            ClashState targetClash = duelState.clashes[targetClashIndex];
            if (targetClash == null)
            {
                failureMessage = $"clash({targetClashIndex}) is null.";
                return false;
            }

            targetClash.EnsureInitialized();
            if (!targetClash.playerActionIds.Contains(actionId) &&
                targetClash.slotLimit.HasValue &&
                targetClash.playerActionIds.Count >= targetClash.slotLimit.Value)
            {
                failureMessage = $"target clash({targetClashIndex}) slotLimit exceeded.";
                return false;
            }

            if (sourceType == ActionLocationType.ActionHolder)
            {
                duelState.actionHolderActionIds.Remove(actionId);
            }
            else
            {
                ClashState sourceClash = duelState.clashes[sourceClashIndex];
                sourceClash?.playerActionIds.Remove(actionId);
            }

            if (!targetClash.playerActionIds.Contains(actionId))
            {
                targetClash.playerActionIds.Add(actionId);
            }

            selectedActionId = actionId;
            selectedClashIndex = targetClashIndex;
            moveLog = $"Action moved: {ClashResolveActionDefIdForDisplay(actionId)} -> clash({targetClashIndex}).";
            return true;
        }

        bool TryFindPlayerActionLocation(string actionId, out ActionLocationType locationType, out int clashIndex)
        {
            locationType = ActionLocationType.None;
            clashIndex = -1;

            if (string.IsNullOrWhiteSpace(actionId))
            {
                return false;
            }

            if (duelState.actionHolderActionIds != null && duelState.actionHolderActionIds.Contains(actionId))
            {
                locationType = ActionLocationType.ActionHolder;
                return true;
            }

            if (duelState.clashes == null)
            {
                return false;
            }

            for (int i = 0; i < duelState.clashes.Count; i++)
            {
                ClashState clash = duelState.clashes[i];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();
                if (!clash.playerActionIds.Contains(actionId))
                {
                    continue;
                }

                locationType = ActionLocationType.Clash;
                clashIndex = i;
                return true;
            }

            return false;
        }

        bool CanAutoDeployOnClashClick()
        {
            return phaseRunner != null &&
                   phaseRunner.isStarted &&
                   !duelState.isDuelEnded &&
                   phaseRunner.currentPhase == DuelPhase.PlayerSetup;
        }

        bool TryValidateDuelStarted(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null)
            {
                failureMessage = "duel state is null.";
                return false;
            }

            if (phaseRunner == null || !phaseRunner.isStarted)
            {
                failureMessage = "duel is not started.";
                return false;
            }

            duelState.EnsureInitialized();
            return true;
        }

        void ResetContext()
        {
            duelState = new DuelState();
            phaseRunner = null;
            sessionBuilder = null;
            turnProcessor = null;
            activeDatabase = GameDataRuntime.CurrentDatabase;
            selectedActionId = string.Empty;
            selectedClashIndex = -1;
            ClearActionBlocks();
        }

        void RefreshView()
        {
            RefreshTexts();
            RefreshButtons();
            RefreshActionBlocks();
        }

        void RefreshTexts()
        {
            SetText(phaseText, DuelDebugPanelFormatter.FormatPhase(phaseRunner));
            SetText(turnText, DuelDebugPanelFormatter.FormatTurn(duelState));
            SetText(focusText, DuelDebugPanelFormatter.FormatFocus(duelState));
            SetText(honorText, DuelDebugPanelFormatter.FormatHonor(duelState));
            SetText(playerHealthText, DuelDebugPanelFormatter.FormatPlayerHealth(duelState));
            SetText(opponentHealthText, DuelDebugPanelFormatter.FormatOpponentHealth(duelState));
            SetText(selectedActionText, DuelDebugPanelFormatter.FormatSelectedAction(duelState, selectedActionId));
            SetText(
                selectedClashText,
                DuelDebugPanelFormatter.FormatSelectedClash(duelState, selectedClashIndex));

            SetText(clash0Text, DuelDebugPanelFormatter.FormatClash(duelState, 0, activeDatabase));
            SetText(clash1Text, DuelDebugPanelFormatter.FormatClash(duelState, 1, activeDatabase));
            SetText(clash2Text, DuelDebugPanelFormatter.FormatClash(duelState, 2, activeDatabase));
        }

        void RefreshButtons()
        {
            bool isStarted = phaseRunner != null && phaseRunner.isStarted;
            bool isEnded = duelState != null && duelState.isDuelEnded;
            DuelPhase currentPhase = phaseRunner == null ? DuelPhase.Reset : phaseRunner.currentPhase;
            bool canProgress = isStarted && !isEnded;

            SetButtonInteractable(startDuelButton, !isStarted || isEnded);
            SetButtonInteractable(opponentDeployButton, canProgress && currentPhase == DuelPhase.Reset);
            SetButtonInteractable(playerDeployButton, canProgress && currentPhase == DuelPhase.OpponentSetup);
            SetButtonInteractable(deploySelectedButton, false);
            SetButtonInteractable(
                rollButton,
                canProgress &&
                (currentPhase == DuelPhase.PlayerSetup || currentPhase == DuelPhase.Roll));
            SetButtonInteractable(
                resolveButton,
                canProgress &&
                (currentPhase == DuelPhase.Skill || currentPhase == DuelPhase.ClashResolve));
            SetButtonInteractable(
                retreatButton,
                canProgress &&
                currentPhase == DuelPhase.PlayerSetup &&
                duelState.honor > 0);
        }

        void RefreshActionBlocks()
        {
            ClearActionBlocks();

            if (duelState == null || duelState.actionsById == null)
            {
                return;
            }

            if (actionHolderActionBlockRoot != null && duelState.actionHolderActionIds != null)
            {
                for (int i = 0; i < duelState.actionHolderActionIds.Count; i++)
                {
                    CreateActionBlock(actionHolderActionBlockRoot, duelState.actionHolderActionIds[i], true, true);
                }
            }

            for (int clashIndex = 0; clashIndex < DuelState.defaultClashCount; clashIndex++)
            {
                if (clashIndex >= duelState.clashes.Count)
                {
                    continue;
                }

                ClashState clash = duelState.clashes[clashIndex];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();

                RectTransform playerRoot = clashPlayerActionBlockRoots[clashIndex];
                if (playerRoot != null)
                {
                    for (int i = 0; i < clash.playerActionIds.Count; i++)
                    {
                        CreateActionBlock(playerRoot, clash.playerActionIds[i], true, true);
                    }
                }

                RectTransform opponentRoot = clashOpponentActionBlockRoots[clashIndex];
                if (opponentRoot != null)
                {
                    for (int i = 0; i < clash.opponentActionIds.Count; i++)
                    {
                        CreateActionBlock(opponentRoot, clash.opponentActionIds[i], false, false);
                    }
                }
            }
        }

        void CreateActionBlock(
            RectTransform parent,
            string actionId,
            bool isPlayerSide,
            bool canSelect)
        {
            if (parent == null || string.IsNullOrWhiteSpace(actionId))
            {
                return;
            }

            if (!duelState.actionsById.TryGetValue(actionId, out ActionInstance action) || action == null)
            {
                WarnAndLog($"Action block warning: actionId({actionId}) does not exist.");
                return;
            }

            string actionDefId = ClashResolveActionDefIdForDisplay(actionId);
            string effectsLabel = DuelDebugPanelFormatter.FormatActionEffects(activeDatabase, action.actionDefId);
            bool isSelected = string.Equals(actionId, selectedActionId, StringComparison.Ordinal);

            DuelActionBlockView actionBlock = Instantiate(actionBlockPrefab, parent);
            actionBlock.name = $"ActionBlock_{actionId}";
            actionBlock.Bind(
                actionDefId,
                action.attack,
                action.attackResult,
                effectsLabel,
                isPlayerSide,
                isSelected,
                canSelect,
                () => SelectAction(actionId));

            spawnedActionBlocks.Add(actionBlock);
        }

        void ClearActionBlocks()
        {
            for (int i = 0; i < spawnedActionBlocks.Count; i++)
            {
                DuelActionBlockView actionBlock = spawnedActionBlocks[i];
                if (actionBlock == null)
                {
                    continue;
                }

                Destroy(actionBlock.gameObject);
            }

            spawnedActionBlocks.Clear();
        }

        string ClashResolveActionDefIdForDisplay(string actionId)
        {
            if (duelState == null ||
                duelState.actionsById == null ||
                string.IsNullOrWhiteSpace(actionId))
            {
                return "(no-def)";
            }

            if (!duelState.actionsById.TryGetValue(actionId, out ActionInstance action) ||
                action == null ||
                string.IsNullOrWhiteSpace(action.actionDefId))
            {
                return "(no-def)";
            }

            return action.actionDefId;
        }

        void RejectAction(string actionName, string reason)
        {
            WarnAndLog($"{actionName} rejected: {reason}");
            RefreshView();
        }

        void WarnAndLog(string message)
        {
            UnityEngine.Debug.LogWarning($"[DuelDebugPanel] {message}");
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

            UnityEngine.Debug.Log($"[DuelDebugPanel] {normalizedMessage}");
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
            ConfigureButtonLabelAlignment(startDuelButton);
            ConfigureButtonLabelAlignment(opponentDeployButton);
            ConfigureButtonLabelAlignment(playerDeployButton);
            ConfigureButtonLabelAlignment(deploySelectedButton);
            ConfigureButtonLabelAlignment(rollButton);
            ConfigureButtonLabelAlignment(resolveButton);
            ConfigureButtonLabelAlignment(retreatButton);
            ConfigureButtonLabelAlignment(selectFirstActionHolderActionButton);

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
            BindButton(startDuelButton, StartDuel);
            BindButton(opponentDeployButton, OpponentSetup);
            BindButton(playerDeployButton, PlayerSetup);
            BindButton(deploySelectedButton, DeploySelected);
            BindButton(rollButton, Roll);
            BindButton(resolveButton, ClashResolve);
            BindButton(retreatButton, Retreat);
            BindButton(selectFirstActionHolderActionButton, SelectFirstActionHolderAction);
            BindButton(selectClash0Button, SelectClash0);
            BindButton(selectClash1Button, SelectClash1);
            BindButton(selectClash2Button, SelectClash2);
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
            ValidateRequired(focusText, nameof(focusText), missing);
            ValidateRequired(honorText, nameof(honorText), missing);
            ValidateRequired(playerHealthText, nameof(playerHealthText), missing);
            ValidateRequired(opponentHealthText, nameof(opponentHealthText), missing);
            ValidateRequired(selectedActionText, nameof(selectedActionText), missing);
            ValidateRequired(selectedClashText, nameof(selectedClashText), missing);
            ValidateRequired(clash0Text, nameof(clash0Text), missing);
            ValidateRequired(clash1Text, nameof(clash1Text), missing);
            ValidateRequired(clash2Text, nameof(clash2Text), missing);

            ValidateRequired(startDuelButton, nameof(startDuelButton), missing);
            ValidateRequired(opponentDeployButton, nameof(opponentDeployButton), missing);
            ValidateRequired(playerDeployButton, nameof(playerDeployButton), missing);
            ValidateRequired(rollButton, nameof(rollButton), missing);
            ValidateRequired(resolveButton, nameof(resolveButton), missing);
            ValidateRequired(retreatButton, nameof(retreatButton), missing);
            ValidateRequired(selectFirstActionHolderActionButton, nameof(selectFirstActionHolderActionButton), missing);
            ValidateRequired(selectClash0Button, nameof(selectClash0Button), missing);
            ValidateRequired(selectClash1Button, nameof(selectClash1Button), missing);
            ValidateRequired(selectClash2Button, nameof(selectClash2Button), missing);

            ValidateRequired(actionBlockPrefab, nameof(actionBlockPrefab), missing);
            ValidateRequired(actionHolderActionBlockRoot, nameof(actionHolderActionBlockRoot), missing);
            ValidateRequired(logText, nameof(logText), missing);
            ValidateRequired(logScrollRect, nameof(logScrollRect), missing);

            if (clashPlayerActionBlockRoots == null ||
                clashPlayerActionBlockRoots.Length != DuelState.defaultClashCount)
            {
                missing.Add(nameof(clashPlayerActionBlockRoots));
            }
            else
            {
                for (int i = 0; i < clashPlayerActionBlockRoots.Length; i++)
                {
                    if (clashPlayerActionBlockRoots[i] == null)
                    {
                        missing.Add($"{nameof(clashPlayerActionBlockRoots)}[{i}]");
                    }
                }
            }

            if (clashOpponentActionBlockRoots == null ||
                clashOpponentActionBlockRoots.Length != DuelState.defaultClashCount)
            {
                missing.Add(nameof(clashOpponentActionBlockRoots));
            }
            else
            {
                for (int i = 0; i < clashOpponentActionBlockRoots.Length; i++)
                {
                    if (clashOpponentActionBlockRoots[i] == null)
                    {
                        missing.Add($"{nameof(clashOpponentActionBlockRoots)}[{i}]");
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
            phaseText = AssignByNames(phaseText, "PhaseText");
            turnText = AssignByNames(turnText, "TurnText");
            focusText = AssignByNames(focusText, "FocusText", "ManaText");
            honorText = AssignByNames(honorText, "HonorText", "StabilityText");
            playerHealthText = AssignByNames(playerHealthText, "PlayerHealthText", "PlayerMoraleText");
            opponentHealthText = AssignByNames(opponentHealthText, "OpponentHealthText", "EnemyMoraleText");
            selectedActionText = AssignByNames(selectedActionText, "SelectedActionText", "SelectedTroopText");
            selectedClashText = AssignByNames(selectedClashText, "SelectedClashText", "SelectedBattlefieldText");

            clash0Text = AssignByNames(clash0Text, "Clash0Text", "Battlefield0Text");
            clash1Text = AssignByNames(clash1Text, "Clash1Text", "Battlefield1Text");
            clash2Text = AssignByNames(clash2Text, "Clash2Text", "Battlefield2Text");

            startDuelButton = AssignByNames(startDuelButton, "StartDuelButton", "StartBattleButton");
            opponentDeployButton = AssignByNames(opponentDeployButton, "OpponentSetupButton", "EnemyDeployButton");
            playerDeployButton = AssignByNames(playerDeployButton, "PlayerSetupButton", "PlayerDeployButton");
            deploySelectedButton = AssignByNames(deploySelectedButton, "DeploySelectedButton");
            rollButton = AssignByNames(rollButton, "RollButton");
            resolveButton = AssignByNames(resolveButton, "ClashResolveButton", "ResolveButton");
            retreatButton = AssignByNames(retreatButton, "RetreatButton");
            selectFirstActionHolderActionButton = AssignByNames(
                selectFirstActionHolderActionButton,
                "SelectFirstActionHolderActionButton",
                "SelectFirstCampTroopButton");
            selectClash0Button = AssignByNames(selectClash0Button, "SelectClash0Button", "SelectBattlefield0Button");
            selectClash1Button = AssignByNames(selectClash1Button, "SelectClash1Button", "SelectBattlefield1Button");
            selectClash2Button = AssignByNames(selectClash2Button, "SelectClash2Button", "SelectBattlefield2Button");

            actionHolderActionBlockRoot = AssignByNames(actionHolderActionBlockRoot, "ActionHolderActionBlockRoot", "CampTroopBlockRoot");
            EnsureActionRootArrays();
            clashPlayerActionBlockRoots[0] = AssignByNames(
                clashPlayerActionBlockRoots[0],
                "Clash0PlayerActionBlockRoot",
                "Battlefield0PlayerTroopBlockRoot");
            clashPlayerActionBlockRoots[1] = AssignByNames(
                clashPlayerActionBlockRoots[1],
                "Clash1PlayerActionBlockRoot",
                "Battlefield1PlayerTroopBlockRoot");
            clashPlayerActionBlockRoots[2] = AssignByNames(
                clashPlayerActionBlockRoots[2],
                "Clash2PlayerActionBlockRoot",
                "Battlefield2PlayerTroopBlockRoot");

            clashOpponentActionBlockRoots[0] = AssignByNames(
                clashOpponentActionBlockRoots[0],
                "Clash0OpponentActionBlockRoot",
                "Battlefield0OpponentActionBlockRoot");
            clashOpponentActionBlockRoots[1] = AssignByNames(
                clashOpponentActionBlockRoots[1],
                "Clash1OpponentActionBlockRoot",
                "Battlefield1OpponentActionBlockRoot");
            clashOpponentActionBlockRoots[2] = AssignByNames(
                clashOpponentActionBlockRoots[2],
                "Clash2OpponentActionBlockRoot",
                "Battlefield2OpponentActionBlockRoot");

            logText = AssignByNames(logText, "LogText");
            logScrollRect = AssignByNames(logScrollRect, "LogScrollRect");

            if (actionBlockPrefab == null)
            {
                actionBlockPrefab = AssetDatabase.LoadAssetAtPath<DuelActionBlockView>(actionBlockPrefabPath);
            }
        }

        void EnsureActionRootArrays()
        {
            if (clashPlayerActionBlockRoots == null ||
                clashPlayerActionBlockRoots.Length != DuelState.defaultClashCount)
            {
                clashPlayerActionBlockRoots = new RectTransform[DuelState.defaultClashCount];
            }

            if (clashOpponentActionBlockRoots == null ||
                clashOpponentActionBlockRoots.Length != DuelState.defaultClashCount)
            {
                clashOpponentActionBlockRoots = new RectTransform[DuelState.defaultClashCount];
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

        TComponent AssignByNames<TComponent>(TComponent currentValue, params string[] objectNames)
            where TComponent : Component
        {
            if (currentValue != null || objectNames == null)
            {
                return currentValue;
            }

            for (int i = 0; i < objectNames.Length; i++)
            {
                string objectName = objectNames[i];
                if (string.IsNullOrWhiteSpace(objectName))
                {
                    continue;
                }

                currentValue = AssignByName(currentValue, objectName);
                if (currentValue != null)
                {
                    return currentValue;
                }
            }

            return currentValue;
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
