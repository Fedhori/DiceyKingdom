using System;
using System.Collections.Generic;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Game.Presentation.Debug
{
    public sealed class DuelDebugPanel : MonoBehaviour
    {
        const string debugEncounterId = "encounter.debug.01";
        const string abilityBlockPrefabPath = "Assets/Prefabs/Ui/DuelAbilityBlock.prefab";
        const int clashUiSlotCount = 3;

        [Header("Status")]
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text turnText;
        [SerializeField] TMP_Text resourceText;
        [SerializeField] TMP_Text honorText;
        [SerializeField] TMP_Text playerHealthText;
        [SerializeField] TMP_Text opponentHealthText;
        [SerializeField] TMP_Text selectedAbilityText;
        [SerializeField] TMP_Text selectedClashText;

        [Header("Clashes")]
        [SerializeField] TMP_Text clash0Text;
        [SerializeField] TMP_Text clash1Text;
        [SerializeField] TMP_Text clash2Text;

        [Header("Controls")]
        [SerializeField] Button startDuelButton;
        [SerializeField] Button opponentDeployButton;
        [SerializeField] Button playerDeployButton;
        [SerializeField] Button deploySelectedButton;
        [SerializeField] Button rollButton;
        [SerializeField] Button resolveButton;
        [SerializeField] Button surrenderButton;
        [SerializeField] Button selectFirstBagAbilityButton;
        [SerializeField] Button selectClash0Button;
        [SerializeField] Button selectClash1Button;
        [SerializeField] Button selectClash2Button;

        [Header("Ability Blocks")]
        [SerializeField] DuelAbilityBlockView abilityBlockPrefab;
        [SerializeField] RectTransform bagAbilityBlockRoot;
        [SerializeField] RectTransform[] clashPlayerAbilityBlockRoots =
            new RectTransform[clashUiSlotCount];
        [SerializeField] RectTransform[] clashOpponentAbilityBlockRoots =
            new RectTransform[clashUiSlotCount];

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

        string selectedAbilityId = string.Empty;
        int selectedClashIndex = -1;

        readonly List<string> logEntries = new List<string>();
        readonly List<DuelAbilityBlockView> spawnedAbilityBlocks = new List<DuelAbilityBlockView>();

        enum AbilityLocationType
        {
            None = 0,
            Bag = 1,
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
            ClearAbilityBlocks();
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
                RejectCommand("StartDuel", "GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            activeDatabase = database;
            sessionBuilder = new DuelSessionBuilder(activeDatabase);
            turnProcessor = new DuelTurnProcessor(activeDatabase);

            if (!sessionBuilder.TryCreateInitialState(debugEncounterId, out DuelState state, out string failureMessage))
            {
                RejectCommand("StartDuel", failureMessage);
                return;
            }

            duelState = state;
            phaseRunner = new DuelPhaseRunner(duelState);
            if (!phaseRunner.StartDuel())
            {
                RejectCommand("StartDuel", phaseRunner.LastFailureReason.ToString());
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
                RejectCommand("OpponentSetup", failureMessage);
                return;
            }

            if (phaseRunner.currentPhase != DuelPhase.Reset)
            {
                RejectCommand(
                    "OpponentSetup",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.Reset}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectCommand("OpponentSetup", phaseRunner.LastFailureReason.ToString());
                return;
            }

            AppendLog("OpponentSetup phase entered.");
            RefreshView();
        }

        public void PlayerSetup()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("PlayerSetup", failureMessage);
                return;
            }

            if (phaseRunner.currentPhase != DuelPhase.OpponentSetup)
            {
                RejectCommand(
                    "PlayerSetup",
                    $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.OpponentSetup}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectCommand("PlayerSetup", phaseRunner.LastFailureReason.ToString());
                return;
            }

            AppendLog("PlayerSetup phase entered.");
            RefreshView();
        }

        public void Roll()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("Roll", failureMessage);
                return;
            }

            if (turnProcessor == null)
            {
                RejectCommand("Roll", "turn processor is not initialized.");
                return;
            }

            if (!turnProcessor.TryRollAllDeployedAbilities(
                    duelState,
                    phaseRunner,
                    out DuelRollResult rollResult,
                    out string rollFailureMessage))
            {
                RejectCommand("Roll", rollFailureMessage);
                return;
            }

            AppendLog($"Roll success: rolledAbilities={rollResult.rolledAbilityCount}");
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
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("Resolve", failureMessage);
                return;
            }

            if (turnProcessor == null)
            {
                RejectCommand("Resolve", "turn processor is not initialized.");
                return;
            }

            if (!turnProcessor.TryClashResolveAllClashes(
                    duelState,
                    phaseRunner,
                    out DuelClashResolveResult resolveResult,
                    out string resolveFailureMessage))
            {
                RejectCommand("Resolve", resolveFailureMessage);
                return;
            }

            for (int i = 0; i < resolveResult.steps.Count; i++)
            {
                DuelClashResolveStepResult step = resolveResult.steps[i];
                AppendLog(
                    $"Resolve[{step.clashIndex}] outcome={step.outcome} TotalPower(P:{step.playerTotalPower},E:{step.opponentTotalPower}) health(P:{duelState.playerHealth},E:{duelState.opponentHealth})");
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
                AppendLog($"TurnEnd applied: cooldownUpdated={resolveResult.cooldownUpdatedCount}");
            }

            AppendLog($"Resolve success: resolvedClashes={resolveResult.steps.Count}");
            RefreshView();
        }

        public void Surrender()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("Surrender", failureMessage);
                return;
            }

            if (!phaseRunner.TrySurrender())
            {
                RejectCommand("Surrender", phaseRunner.LastFailureReason.ToString());
                return;
            }

            selectedAbilityId = string.Empty;
            selectedClashIndex = -1;
            AppendLog("Surrender success: duel ended.");
            RefreshView();
        }

        public void SelectFirstBagAbility()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("SelectFirstBagAbility", failureMessage);
                return;
            }

            if (duelState.bagAbilityIds == null || duelState.bagAbilityIds.Count <= 0)
            {
                RejectCommand("SelectFirstBagAbility", "no ability exists in bag.");
                return;
            }

            SelectAbility(duelState.bagAbilityIds[0]);
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
                RejectCommand("DeploySelected", failureMessage);
                return;
            }

            if (!TryMovePlayerAbilityToClash(selectedAbilityId, selectedClashIndex, out string moveLog, out string moveError))
            {
                RejectCommand("DeploySelected", moveError);
                return;
            }

            AppendLog($"DeploySelected success: {moveLog}");
            RefreshView();
        }

        public void SelectAbility(string abilityId)
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("SelectAbility", failureMessage);
                return;
            }

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                RejectCommand("SelectAbility", "abilityId is empty.");
                return;
            }

            if (!duelState.abilitiesById.ContainsKey(abilityId))
            {
                RejectCommand("SelectAbility", $"abilityId({abilityId}) does not exist.");
                return;
            }

            if (!TryFindPlayerAbilityLocation(abilityId, out AbilityLocationType locationType, out int clashIndex))
            {
                RejectCommand("SelectAbility", $"abilityId({abilityId}) is not in player controllable zones.");
                return;
            }

            selectedAbilityId = abilityId;
            if (locationType == AbilityLocationType.Clash)
            {
                selectedClashIndex = clashIndex;
            }

            AppendLog($"Ability selected: {ResolveAbilityDefIdForDisplay(abilityId)}");
            RefreshView();
        }

        public void SelectClash(int clashIndex)
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("SelectClash", failureMessage);
                return;
            }

            if (clashIndex < 0 || clashIndex >= duelState.clashes.Count)
            {
                RejectCommand("SelectClash", $"clashIndex({clashIndex}) is out of range.");
                return;
            }

            selectedClashIndex = clashIndex;

            if (CanAutoDeployOnClashClick() && !string.IsNullOrWhiteSpace(selectedAbilityId))
            {
                if (!TryMovePlayerAbilityToClash(
                        selectedAbilityId,
                        selectedClashIndex,
                        out string moveLog,
                        out string moveError))
                {
                    RejectCommand("SelectClashDeploy", moveError);
                    return;
                }

                AppendLog($"Clash click deploy success: {moveLog}");
                RefreshView();
                return;
            }

            AppendLog($"Clash selected: {clashIndex}");
            RefreshView();
        }

        bool TryMovePlayerAbilityToClash(
            string abilityId,
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

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                failureMessage = "abilityId is empty.";
                return false;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
            {
                failureMessage = $"ability({abilityId}) does not exist.";
                return false;
            }

            if (ability.abilityType != AbilityType.Attack)
            {
                failureMessage = $"only Attack type ability can be deployed to clash (current: {ability.abilityType}).";
                return false;
            }

            if (targetClashIndex < 0 || targetClashIndex >= duelState.clashes.Count)
            {
                failureMessage = $"target clash({targetClashIndex}) is out of range.";
                return false;
            }

            if (!TryFindPlayerAbilityLocation(abilityId, out AbilityLocationType sourceType, out int sourceClashIndex))
            {
                failureMessage = $"ability({abilityId}) is not in player controllable zones.";
                return false;
            }

            if (sourceType == AbilityLocationType.Clash && sourceClashIndex == targetClashIndex)
            {
                moveLog = $"Ability move skipped: {ResolveAbilityDefIdForDisplay(abilityId)} already in clash({targetClashIndex}).";
                return true;
            }

            ClashState targetClash = duelState.clashes[targetClashIndex];
            if (targetClash == null)
            {
                failureMessage = $"clash({targetClashIndex}) is null.";
                return false;
            }

            targetClash.EnsureInitialized();
            if (!targetClash.playerAbilityIds.Contains(abilityId) &&
                targetClash.slotLimit.HasValue &&
                targetClash.playerAbilityIds.Count >= targetClash.slotLimit.Value)
            {
                failureMessage = $"target clash({targetClashIndex}) slotLimit exceeded.";
                return false;
            }

            if (sourceType == AbilityLocationType.Bag)
            {
                duelState.bagAbilityIds.Remove(abilityId);
            }
            else
            {
                ClashState sourceClash = duelState.clashes[sourceClashIndex];
                sourceClash?.playerAbilityIds.Remove(abilityId);
            }

            if (!targetClash.playerAbilityIds.Contains(abilityId))
            {
                targetClash.playerAbilityIds.Add(abilityId);
            }

            selectedAbilityId = abilityId;
            selectedClashIndex = targetClashIndex;
            moveLog = $"Ability moved: {ResolveAbilityDefIdForDisplay(abilityId)} -> clash({targetClashIndex}).";
            return true;
        }

        bool TryFindPlayerAbilityLocation(string abilityId, out AbilityLocationType locationType, out int clashIndex)
        {
            locationType = AbilityLocationType.None;
            clashIndex = -1;

            if (string.IsNullOrWhiteSpace(abilityId))
            {
                return false;
            }

            if (duelState.bagAbilityIds != null && duelState.bagAbilityIds.Contains(abilityId))
            {
                locationType = AbilityLocationType.Bag;
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
                if (!clash.playerAbilityIds.Contains(abilityId))
                {
                    continue;
                }

                locationType = AbilityLocationType.Clash;
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
            selectedAbilityId = string.Empty;
            selectedClashIndex = -1;
            ClearAbilityBlocks();
        }

        void RefreshView()
        {
            RefreshTexts();
            RefreshButtons();
            RefreshAbilityBlocks();
        }

        void RefreshTexts()
        {
            SetText(phaseText, DuelDebugPanelFormatter.FormatPhase(phaseRunner));
            SetText(turnText, DuelDebugPanelFormatter.FormatTurn(duelState));
            SetText(resourceText, DuelDebugPanelFormatter.FormatResourceStatus());
            SetText(honorText, DuelDebugPanelFormatter.FormatHonor(duelState));
            SetText(playerHealthText, DuelDebugPanelFormatter.FormatPlayerHealth(duelState));
            SetText(opponentHealthText, DuelDebugPanelFormatter.FormatOpponentHealth(duelState));
            SetText(selectedAbilityText, DuelDebugPanelFormatter.FormatSelectedAbility(duelState, selectedAbilityId));
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
                currentPhase == DuelPhase.Resolve);
            SetButtonInteractable(
                surrenderButton,
                canProgress &&
                currentPhase == DuelPhase.PlayerSetup &&
                duelState.honor > 0);
        }

        void RefreshAbilityBlocks()
        {
            ClearAbilityBlocks();

            if (duelState == null || duelState.abilitiesById == null)
            {
                return;
            }

            if (bagAbilityBlockRoot != null && duelState.bagAbilityIds != null)
            {
                for (int i = 0; i < duelState.bagAbilityIds.Count; i++)
                {
                    CreateAbilityBlock(bagAbilityBlockRoot, duelState.bagAbilityIds[i], true, true);
                }
            }

            int clashViewCount = ResolveClashViewCount();
            for (int clashIndex = 0; clashIndex < clashViewCount; clashIndex++)
            {
                ClashState clash = duelState.clashes[clashIndex];
                if (clash == null)
                {
                    continue;
                }

                clash.EnsureInitialized();

                RectTransform playerRoot = clashPlayerAbilityBlockRoots[clashIndex];
                if (playerRoot != null)
                {
                    for (int i = 0; i < clash.playerAbilityIds.Count; i++)
                    {
                        CreateAbilityBlock(playerRoot, clash.playerAbilityIds[i], true, true);
                    }
                }

                RectTransform opponentRoot = clashOpponentAbilityBlockRoots[clashIndex];
                if (opponentRoot != null)
                {
                    for (int i = 0; i < clash.opponentAbilityIds.Count; i++)
                    {
                        CreateAbilityBlock(opponentRoot, clash.opponentAbilityIds[i], false, false);
                    }
                }
            }
        }

        void CreateAbilityBlock(
            RectTransform parent,
            string abilityId,
            bool isPlayerSide,
            bool canSelect)
        {
            if (parent == null || string.IsNullOrWhiteSpace(abilityId))
            {
                return;
            }

            if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) || ability == null)
            {
                WarnAndLog($"Ability block warning: abilityId({abilityId}) does not exist.");
                return;
            }

            string abilityDefId = ResolveAbilityDefIdForDisplay(abilityId);
            string effectSummary = DuelDebugPanelFormatter.FormatAbilityEffects(activeDatabase, ability.abilityDefId);
            string cooldownSummary = ability.cooldownTurns > 0
                ? $"CD {ability.cooldownRemaining}/{ability.cooldownTurns}"
                : "CD -";
            string effectsLabel = $"{ability.abilityType} | {cooldownSummary}";
            if (!string.Equals(effectSummary, "none", StringComparison.OrdinalIgnoreCase))
            {
                effectsLabel = $"{effectsLabel} | {effectSummary}";
            }
            bool isSelected = string.Equals(abilityId, selectedAbilityId, StringComparison.Ordinal);

            DuelAbilityBlockView abilityBlock = Instantiate(abilityBlockPrefab, parent);
            abilityBlock.name = $"AbilityBlock_{abilityId}";
            abilityBlock.Bind(
                abilityDefId,
                ability.power,
                ability.powerResult,
                effectsLabel,
                isPlayerSide,
                isSelected,
                canSelect,
                () => SelectAbility(abilityId));

            spawnedAbilityBlocks.Add(abilityBlock);
        }

        void ClearAbilityBlocks()
        {
            for (int i = 0; i < spawnedAbilityBlocks.Count; i++)
            {
                DuelAbilityBlockView abilityBlock = spawnedAbilityBlocks[i];
                if (abilityBlock == null)
                {
                    continue;
                }

                Destroy(abilityBlock.gameObject);
            }

            spawnedAbilityBlocks.Clear();
        }

        int ResolveClashViewCount()
        {
            if (duelState == null || duelState.clashes == null)
            {
                return 0;
            }

            int playerRootCount = clashPlayerAbilityBlockRoots == null
                ? 0
                : clashPlayerAbilityBlockRoots.Length;
            int opponentRootCount = clashOpponentAbilityBlockRoots == null
                ? 0
                : clashOpponentAbilityBlockRoots.Length;
            int viewCount = Mathf.Min(duelState.clashes.Count, playerRootCount);
            viewCount = Mathf.Min(viewCount, opponentRootCount);
            return Mathf.Max(0, viewCount);
        }

        string ResolveAbilityDefIdForDisplay(string abilityId)
        {
            if (duelState == null ||
                duelState.abilitiesById == null ||
                string.IsNullOrWhiteSpace(abilityId))
            {
                return "(no-def)";
            }

            if (!duelState.abilitiesById.TryGetValue(abilityId, out AbilityInstance ability) ||
                ability == null ||
                string.IsNullOrWhiteSpace(ability.abilityDefId))
            {
                return "(no-def)";
            }

            return ability.abilityDefId;
        }

        void RejectCommand(string commandName, string reason)
        {
            WarnAndLog($"{commandName} rejected: {reason}");
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
            ConfigureButtonLabelAlignment(surrenderButton);
            ConfigureButtonLabelAlignment(selectFirstBagAbilityButton);

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
            BindButton(resolveButton, Resolve);
            BindButton(surrenderButton, Surrender);
            BindButton(selectFirstBagAbilityButton, SelectFirstBagAbility);
            BindButton(selectClash0Button, SelectClash0);
            BindButton(selectClash1Button, SelectClash1);
            BindButton(selectClash2Button, SelectClash2);
        }

        static void BindButton(Button button, UnityEngine.Events.UnityAction callback)
        {
            if (button == null || callback == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback);
        }

        bool ValidateBindings(out string errorMessage)
        {
            var missing = new List<string>();

            ValidateRequired(phaseText, nameof(phaseText), missing);
            ValidateRequired(turnText, nameof(turnText), missing);
            ValidateRequired(resourceText, nameof(resourceText), missing);
            ValidateRequired(honorText, nameof(honorText), missing);
            ValidateRequired(playerHealthText, nameof(playerHealthText), missing);
            ValidateRequired(opponentHealthText, nameof(opponentHealthText), missing);
            ValidateRequired(selectedAbilityText, nameof(selectedAbilityText), missing);
            ValidateRequired(selectedClashText, nameof(selectedClashText), missing);
            ValidateRequired(clash0Text, nameof(clash0Text), missing);
            ValidateRequired(clash1Text, nameof(clash1Text), missing);
            ValidateRequired(clash2Text, nameof(clash2Text), missing);

            ValidateRequired(startDuelButton, nameof(startDuelButton), missing);
            ValidateRequired(opponentDeployButton, nameof(opponentDeployButton), missing);
            ValidateRequired(playerDeployButton, nameof(playerDeployButton), missing);
            ValidateRequired(rollButton, nameof(rollButton), missing);
            ValidateRequired(resolveButton, nameof(resolveButton), missing);
            ValidateRequired(surrenderButton, nameof(surrenderButton), missing);
            ValidateRequired(selectFirstBagAbilityButton, nameof(selectFirstBagAbilityButton), missing);
            ValidateRequired(selectClash0Button, nameof(selectClash0Button), missing);
            ValidateRequired(selectClash1Button, nameof(selectClash1Button), missing);
            ValidateRequired(selectClash2Button, nameof(selectClash2Button), missing);

            ValidateRequired(abilityBlockPrefab, nameof(abilityBlockPrefab), missing);
            ValidateRequired(bagAbilityBlockRoot, nameof(bagAbilityBlockRoot), missing);
            ValidateRequired(logText, nameof(logText), missing);
            ValidateRequired(logScrollRect, nameof(logScrollRect), missing);

            if (clashPlayerAbilityBlockRoots == null ||
                clashPlayerAbilityBlockRoots.Length != clashUiSlotCount)
            {
                missing.Add(nameof(clashPlayerAbilityBlockRoots));
            }
            else
            {
                for (int i = 0; i < clashPlayerAbilityBlockRoots.Length; i++)
                {
                    if (clashPlayerAbilityBlockRoots[i] == null)
                    {
                        missing.Add($"{nameof(clashPlayerAbilityBlockRoots)}[{i}]");
                    }
                }
            }

            if (clashOpponentAbilityBlockRoots == null ||
                clashOpponentAbilityBlockRoots.Length != clashUiSlotCount)
            {
                missing.Add(nameof(clashOpponentAbilityBlockRoots));
            }
            else
            {
                for (int i = 0; i < clashOpponentAbilityBlockRoots.Length; i++)
                {
                    if (clashOpponentAbilityBlockRoots[i] == null)
                    {
                        missing.Add($"{nameof(clashOpponentAbilityBlockRoots)}[{i}]");
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
            resourceText = AssignByNames(resourceText, "ResourceText");
            honorText = AssignByNames(honorText, "HonorText");
            playerHealthText = AssignByNames(playerHealthText, "PlayerHealthText");
            opponentHealthText = AssignByNames(opponentHealthText, "OpponentHealthText");
            selectedAbilityText = AssignByNames(selectedAbilityText, "SelectedAbilityText");
            selectedClashText = AssignByNames(selectedClashText, "SelectedClashText");

            clash0Text = AssignByNames(clash0Text, "Clash0Text");
            clash1Text = AssignByNames(clash1Text, "Clash1Text");
            clash2Text = AssignByNames(clash2Text, "Clash2Text");

            startDuelButton = AssignByNames(startDuelButton, "StartDuelButton");
            opponentDeployButton = AssignByNames(opponentDeployButton, "OpponentSetupButton");
            playerDeployButton = AssignByNames(playerDeployButton, "PlayerSetupButton");
            deploySelectedButton = AssignByNames(deploySelectedButton, "DeploySelectedButton");
            rollButton = AssignByNames(rollButton, "RollButton");
            resolveButton = AssignByNames(resolveButton, "ResolveButton", "ClashResolveButton");
            surrenderButton = AssignByNames(surrenderButton, "SurrenderButton");
            selectFirstBagAbilityButton = AssignByNames(
                selectFirstBagAbilityButton,
                "SelectFirstBagAbilityButton");
            selectClash0Button = AssignByNames(selectClash0Button, "SelectClash0Button");
            selectClash1Button = AssignByNames(selectClash1Button, "SelectClash1Button");
            selectClash2Button = AssignByNames(selectClash2Button, "SelectClash2Button");

            bagAbilityBlockRoot = AssignByNames(
                bagAbilityBlockRoot,
                "BagAbilityBlockRoot");
            EnsureAbilityRootArrays();
            clashPlayerAbilityBlockRoots[0] = AssignByNames(
                clashPlayerAbilityBlockRoots[0],
                "Clash0PlayerAbilityBlockRoot");
            clashPlayerAbilityBlockRoots[1] = AssignByNames(
                clashPlayerAbilityBlockRoots[1],
                "Clash1PlayerAbilityBlockRoot");
            clashPlayerAbilityBlockRoots[2] = AssignByNames(
                clashPlayerAbilityBlockRoots[2],
                "Clash2PlayerAbilityBlockRoot");

            clashOpponentAbilityBlockRoots[0] = AssignByNames(
                clashOpponentAbilityBlockRoots[0],
                "Clash0OpponentAbilityBlockRoot");
            clashOpponentAbilityBlockRoots[1] = AssignByNames(
                clashOpponentAbilityBlockRoots[1],
                "Clash1OpponentAbilityBlockRoot");
            clashOpponentAbilityBlockRoots[2] = AssignByNames(
                clashOpponentAbilityBlockRoots[2],
                "Clash2OpponentAbilityBlockRoot");

            logText = AssignByNames(logText, "LogText");
            logScrollRect = AssignByNames(logScrollRect, "LogScrollRect");

            if (abilityBlockPrefab == null)
            {
                abilityBlockPrefab = AssetDatabase.LoadAssetAtPath<DuelAbilityBlockView>(abilityBlockPrefabPath);
            }
        }

        void EnsureAbilityRootArrays()
        {
            if (clashPlayerAbilityBlockRoots == null ||
                clashPlayerAbilityBlockRoots.Length != clashUiSlotCount)
            {
                clashPlayerAbilityBlockRoots = new RectTransform[clashUiSlotCount];
            }

            if (clashOpponentAbilityBlockRoots == null ||
                clashOpponentAbilityBlockRoots.Length != clashUiSlotCount)
            {
                clashOpponentAbilityBlockRoots = new RectTransform[clashUiSlotCount];
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



