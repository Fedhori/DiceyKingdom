using System;
using System.Collections.Generic;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Game.Presentation.Debug
{
    public sealed class DuelDebugPanel : MonoBehaviour
    {
        const string debugEnemyId = "enemy.northern.footman";

        [Header("Status")]
        [SerializeField] TMP_Text phaseText;
        [SerializeField] TMP_Text turnText;
        [SerializeField] TMP_Text resourceText;
        [SerializeField] TMP_Text honorText;
        [SerializeField] TMP_Text playerHealthText;
        [SerializeField] TMP_Text opponentHealthText;
        [SerializeField] TMP_Text selectedAbilityText;
        [FormerlySerializedAs("selectedClashText")]
        [SerializeField] TMP_Text selectedCombatText;

        [Header("Combats")]
        [FormerlySerializedAs("clash0Text")]
        [SerializeField] TMP_Text combat0Text;
        [FormerlySerializedAs("clash1Text")]
        [SerializeField] TMP_Text combat1Text;
        [FormerlySerializedAs("clash2Text")]
        [SerializeField] TMP_Text combat2Text;

        [Header("Controls")]
        [SerializeField] Button startDuelButton;
        [SerializeField] Button opponentDeployButton;
        [SerializeField] Button playerDeployButton;
        [SerializeField] Button deploySelectedButton;
        [SerializeField] Button rollButton;
        [SerializeField] Button resolveButton;
        [SerializeField] Button surrenderButton;
        [SerializeField] Button selectFirstLoadoutAbilityButton;
        [FormerlySerializedAs("selectClash0Button")]
        [SerializeField] Button selectCombat0Button;
        [FormerlySerializedAs("selectClash1Button")]
        [SerializeField] Button selectCombat1Button;
        [FormerlySerializedAs("selectClash2Button")]
        [SerializeField] Button selectCombat2Button;

        [Header("Debug")]
        [SerializeField] TMP_Text logText;
        [SerializeField] ScrollRect logScrollRect;
        [SerializeField] int maxLogEntryCount = 200;
        [SerializeField] int maxLogLineLength = 220;

        DuelState duelState;
        DuelPhaseRunner phaseRunner;
        DuelSessionBuilder sessionBuilder;
        DuelTurnProcessor turnProcessor;

        string selectedAbilityId = string.Empty;
        int selectedCombatIndex = -1;
        readonly List<string> logEntries = new();

        enum AbilityLocationType
        {
            None = 0,
            Loadout = 1,
            Combat = 2
        }

        public DuelState DuelState => duelState;
        public DuelPhaseRunner PhaseRunner => phaseRunner;

        void Awake()
        {
            WireButtonCallbacks();
            RefreshView();
        }

        public void StartDuel()
        {
            GameDatabase database = GameDataRuntime.CurrentDatabase;
            if (database == null)
            {
                RejectCommand("StartDuel", "GameDataRuntime.CurrentDatabase is null.");
                return;
            }

            sessionBuilder = new DuelSessionBuilder(database);
            turnProcessor = new DuelTurnProcessor(database);

            if (!sessionBuilder.TryCreateInitialState(debugEnemyId, out DuelState state, out string failureMessage))
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

            OpponentSetupBuildResult deployResult = sessionBuilder.AutoDeployOpponentCombat(duelState);
            AppendLog($"Opponent auto deploy complete: deployed={deployResult.deployedCount}, skipped={deployResult.skippedCount}");
            AppendLog($"StartDuel success: enemy={debugEnemyId}");
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
                RejectCommand("OpponentSetup", $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.Reset}.");
                return;
            }

            if (!phaseRunner.AdvanceToNextPhase())
            {
                RejectCommand("OpponentSetup", phaseRunner.LastFailureReason.ToString());
                return;
            }

            OpponentSetupBuildResult deployResult = sessionBuilder.AutoDeployOpponentCombat(duelState);
            AppendLog($"OpponentSetup success: deployed={deployResult.deployedCount}, skipped={deployResult.skippedCount}");
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
                RejectCommand("PlayerSetup", $"current phase is {phaseRunner.currentPhase}, required phase is {DuelPhase.OpponentSetup}.");
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

            if (!turnProcessor.TryResolveAllCombats(
                    duelState,
                    phaseRunner,
                    out DuelCombatResolveResult resolveResult,
                    out string resolveFailureMessage))
            {
                RejectCommand("Resolve", resolveFailureMessage);
                return;
            }

            for (int i = 0; i < resolveResult.steps.Count; i++)
            {
                DuelCombatResolveStepResult step = resolveResult.steps[i];
                AppendLog(
                    $"Resolve[{step.combatIndex}] {step.outcome} damage={step.appliedDamage} P:{step.playerTotalPower} E:{step.opponentTotalPower}");
            }

            AppendLog($"Resolve success: resolvedCombats={resolveResult.steps.Count}");
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
            selectedCombatIndex = -1;
            AppendLog("Surrender success: duel ended.");
            RefreshView();
        }

        public void SelectFirstLoadoutAbility()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("SelectFirstLoadoutAbility", failureMessage);
                return;
            }

            if (duelState.loadoutAbilityIds == null || duelState.loadoutAbilityIds.Count <= 0)
            {
                RejectCommand("SelectFirstLoadoutAbility", "no ability exists in loadout.");
                return;
            }

            SelectAbility(duelState.loadoutAbilityIds[0]);
        }

        public void SelectCombat0()
        {
            SelectCombat(0);
        }

        public void SelectCombat1()
        {
            SelectCombat(1);
        }

        public void SelectCombat2()
        {
            SelectCombat(2);
        }

        public void DeploySelected()
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("DeploySelected", failureMessage);
                return;
            }

            if (!TryMovePlayerAbilityToCombat(selectedAbilityId, selectedCombatIndex, out string moveLog, out string moveError))
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

            if (!TryFindPlayerAbilityLocation(abilityId, out AbilityLocationType locationType, out int combatIndex))
            {
                RejectCommand("SelectAbility", $"abilityId({abilityId}) is not in player controllable zones.");
                return;
            }

            selectedAbilityId = abilityId;
            if (locationType == AbilityLocationType.Combat)
            {
                selectedCombatIndex = combatIndex;
            }

            AppendLog($"Ability selected: {ResolveAbilityDefIdForDisplay(abilityId)}");
            RefreshView();
        }

        public void SelectCombat(int combatIndex)
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("SelectCombat", failureMessage);
                return;
            }

            if (combatIndex < 0 || combatIndex >= duelState.combats.Count)
            {
                RejectCommand("SelectCombat", $"combatIndex({combatIndex}) is out of range.");
                return;
            }

            selectedCombatIndex = combatIndex;

            if (!string.IsNullOrWhiteSpace(selectedAbilityId) && phaseRunner.currentPhase == DuelPhase.PlayerSetup)
            {
                if (!TryMovePlayerAbilityToCombat(selectedAbilityId, selectedCombatIndex, out string moveLog, out string moveError))
                {
                    RejectCommand("SelectCombatDeploy", moveError);
                    return;
                }

                AppendLog($"Combat click deploy success: {moveLog}");
                RefreshView();
                return;
            }

            AppendLog($"Combat selected: {combatIndex}");
            RefreshView();
        }

        bool TryMovePlayerAbilityToCombat(string abilityId, int targetCombatIndex, out string moveLog, out string failureMessage)
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
                failureMessage = $"only Attack type ability can be deployed to combat (current: {ability.abilityType}).";
                return false;
            }

            if (targetCombatIndex < 0 || targetCombatIndex >= duelState.combats.Count)
            {
                failureMessage = $"target combat({targetCombatIndex}) is out of range.";
                return false;
            }

            if (!TryFindPlayerAbilityLocation(abilityId, out AbilityLocationType sourceType, out int sourceCombatIndex))
            {
                failureMessage = $"ability({abilityId}) is not in player controllable zones.";
                return false;
            }

            CombatState targetCombat = duelState.combats[targetCombatIndex];
            if (targetCombat == null)
            {
                failureMessage = $"combat({targetCombatIndex}) is null.";
                return false;
            }

            targetCombat.EnsureInitialized();
            if (!targetCombat.playerAbilityIds.Contains(abilityId) &&
                targetCombat.maxPlayerAssignments.HasValue &&
                targetCombat.maxPlayerAssignments.Value > 0 &&
                targetCombat.playerAbilityIds.Count >= targetCombat.maxPlayerAssignments.Value)
            {
                failureMessage = $"target combat({targetCombatIndex}) maxPlayerAssignments exceeded.";
                return false;
            }

            if (sourceType == AbilityLocationType.Loadout)
            {
                duelState.loadoutAbilityIds.Remove(abilityId);
            }
            else
            {
                CombatState sourceCombat = duelState.combats[sourceCombatIndex];
                sourceCombat?.playerAbilityIds.Remove(abilityId);
            }

            if (!targetCombat.playerAbilityIds.Contains(abilityId))
            {
                targetCombat.playerAbilityIds.Add(abilityId);
            }

            selectedAbilityId = abilityId;
            selectedCombatIndex = targetCombatIndex;
            moveLog = $"Ability moved: {ResolveAbilityDefIdForDisplay(abilityId)} -> combat({targetCombatIndex}).";
            return true;
        }

        bool TryFindPlayerAbilityLocation(string abilityId, out AbilityLocationType locationType, out int combatIndex)
        {
            locationType = AbilityLocationType.None;
            combatIndex = -1;

            if (string.IsNullOrWhiteSpace(abilityId))
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

        bool TryValidateDuelStarted(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (duelState == null)
            {
                failureMessage = "duel state is null. call StartDuel first.";
                return false;
            }

            if (phaseRunner == null || !phaseRunner.isStarted)
            {
                failureMessage = "phase runner is not started.";
                return false;
            }

            return true;
        }

        void RefreshView()
        {
            SetText(phaseText, DuelDebugPanelFormatter.FormatPhase(phaseRunner));
            SetText(turnText, DuelDebugPanelFormatter.FormatTurn(duelState));
            SetText(resourceText, DuelDebugPanelFormatter.FormatResourceStatus());
            SetText(honorText, DuelDebugPanelFormatter.FormatHonor(duelState));
            SetText(playerHealthText, DuelDebugPanelFormatter.FormatPlayerHealth(duelState));
            SetText(opponentHealthText, DuelDebugPanelFormatter.FormatOpponentHealth(duelState));
            SetText(selectedAbilityText, DuelDebugPanelFormatter.FormatSelectedAbility(duelState, selectedAbilityId));
            SetText(selectedCombatText, DuelDebugPanelFormatter.FormatSelectedCombat(duelState, selectedCombatIndex));

            SetText(combat0Text, DuelDebugPanelFormatter.FormatCombat(duelState, 0));
            SetText(combat1Text, DuelDebugPanelFormatter.FormatCombat(duelState, 1));
            SetText(combat2Text, DuelDebugPanelFormatter.FormatCombat(duelState, 2));

            RefreshButtonInteractableState();
        }

        void RefreshButtonInteractableState()
        {
            bool started = phaseRunner != null && phaseRunner.isStarted;
            bool ended = duelState != null && duelState.isDuelEnded;
            DuelPhase phase = started ? phaseRunner.currentPhase : DuelPhase.Reset;

            SetButtonInteractable(startDuelButton, !started);
            SetButtonInteractable(opponentDeployButton, started && phase == DuelPhase.Reset && !ended);
            SetButtonInteractable(playerDeployButton, started && phase == DuelPhase.OpponentSetup && !ended);
            SetButtonInteractable(deploySelectedButton, started && phase == DuelPhase.PlayerSetup && !ended);
            SetButtonInteractable(rollButton, started && phase == DuelPhase.Roll && !ended);
            SetButtonInteractable(resolveButton, started && phase == DuelPhase.Resolve && !ended);
            SetButtonInteractable(surrenderButton, started && phase == DuelPhase.PlayerSetup && !ended);
            SetButtonInteractable(selectFirstLoadoutAbilityButton, started && !ended);
            SetButtonInteractable(selectCombat0Button, started && !ended);
            SetButtonInteractable(selectCombat1Button, started && !ended);
            SetButtonInteractable(selectCombat2Button, started && !ended);
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
            BindButton(selectFirstLoadoutAbilityButton, SelectFirstLoadoutAbility);
            BindButton(selectCombat0Button, SelectCombat0);
            BindButton(selectCombat1Button, SelectCombat1);
            BindButton(selectCombat2Button, SelectCombat2);
        }

        static void BindButton(Button button, Action onClick)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(() => onClick.Invoke());
            }
        }

        static void SetButtonInteractable(Button button, bool interactable)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = interactable;
        }

        void RejectCommand(string commandName, string reason)
        {
            WarnAndLog($"{commandName} rejected: {reason}");
        }

        void WarnAndLog(string message)
        {
            UnityEngine.Debug.LogWarning($"[DuelDebugPanel] {message}");
            AppendLog($"Warning: {message}");
        }

        void AppendLog(string message)
        {
            string normalized = NormalizeLog(message);
            logEntries.Insert(0, normalized);
            while (logEntries.Count > Mathf.Max(1, maxLogEntryCount))
            {
                logEntries.RemoveAt(logEntries.Count - 1);
            }

            if (logText != null)
            {
                logText.text = string.Join("\n", logEntries);
            }

            if (logScrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                logScrollRect.verticalNormalizedPosition = 1.0f;
            }
        }

        string NormalizeLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "-";
            }

            if (maxLogLineLength <= 0 || message.Length <= maxLogLineLength)
            {
                return message;
            }

            return message.Substring(0, maxLogLineLength) + "...";
        }

        string ResolveAbilityDefIdForDisplay(string abilityInstanceId)
        {
            if (duelState == null ||
                duelState.abilitiesById == null ||
                string.IsNullOrWhiteSpace(abilityInstanceId) ||
                !duelState.abilitiesById.TryGetValue(abilityInstanceId, out AbilityInstance ability) ||
                ability == null)
            {
                return "(missing)";
            }

            return string.IsNullOrWhiteSpace(ability.abilityDefId)
                ? "(no-def)"
                : ability.abilityDefId;
        }

        static void SetText(TMP_Text target, string text)
        {
            if (target != null)
            {
                target.text = text ?? string.Empty;
            }
        }
    }
}

