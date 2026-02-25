using System;
using System.Collections.Generic;
using Game.Application.Duel;
using Game.Domain.Duel;
using Game.Infrastructure.Data;
using Game.Presentation.Battle;
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

        readonly BattleSessionRunner sessionRunner = new();
        readonly BattleSelectionState selectionState = new();
        readonly List<string> logEntries = new();

        public DuelState DuelState => sessionRunner.DuelState;
        public DuelPhaseRunner PhaseRunner => sessionRunner.PhaseRunner;

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

            if (!sessionRunner.TryInitialize(database, debugEnemyId, advanceToPlayerSetup: false, out string failureMessage))
            {
                RejectCommand("StartDuel", failureMessage);
                return;
            }

            selectionState.ClearAll();

            if (!sessionRunner.TryAutoDeployOpponent(out OpponentSetupBuildResult deployResult, out string deployFailure))
            {
                RejectCommand("StartDuel", deployFailure);
                return;
            }

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

            if (!sessionRunner.TryEnterOpponentSetup(out OpponentSetupBuildResult deployResult, out string setupFailure))
            {
                RejectCommand("OpponentSetup", setupFailure);
                return;
            }

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

            if (!sessionRunner.TryEnterPlayerSetup(out string setupFailure))
            {
                RejectCommand("PlayerSetup", setupFailure);
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

            if (!sessionRunner.TryRoll(out DuelRollResult rollResult, out string rollFailureMessage))
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

            if (!sessionRunner.TryResolve(out DuelCombatResolveResult resolveResult, out string resolveFailureMessage))
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

            if (!sessionRunner.TrySurrender(out string surrenderFailure))
            {
                RejectCommand("Surrender", surrenderFailure);
                return;
            }

            selectionState.ClearAll();
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

            DuelState duelState = sessionRunner.DuelState;
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

            if (!selectionState.TryMovePlayerAbilityToCombat(
                    sessionRunner.DuelState,
                    sessionRunner.PhaseRunner,
                    selectionState.SelectedAbilityId,
                    selectionState.SelectedCombatIndex,
                    out string moveError))
            {
                RejectCommand("DeploySelected", moveError);
                return;
            }

            AppendLog($"DeploySelected success: {BuildMoveLog()}");
            RefreshView();
        }

        public void SelectAbility(string abilityId)
        {
            if (!TryValidateDuelStarted(out string failureMessage))
            {
                RejectCommand("SelectAbility", failureMessage);
                return;
            }

            if (!selectionState.TrySelectAbility(sessionRunner.DuelState, abilityId, out string selectFailure))
            {
                RejectCommand("SelectAbility", selectFailure);
                return;
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

            if (!selectionState.TrySetSelectedCombat(sessionRunner.DuelState, combatIndex, out string selectFailure))
            {
                RejectCommand("SelectCombat", selectFailure);
                return;
            }

            if (!string.IsNullOrWhiteSpace(selectionState.SelectedAbilityId) &&
                sessionRunner.PhaseRunner.currentPhase == DuelPhase.PlayerSetup)
            {
                if (!selectionState.TryMovePlayerAbilityToCombat(
                        sessionRunner.DuelState,
                        sessionRunner.PhaseRunner,
                        selectionState.SelectedAbilityId,
                        selectionState.SelectedCombatIndex,
                        out string moveError))
                {
                    RejectCommand("SelectCombatDeploy", moveError);
                    return;
                }

                AppendLog($"Combat click deploy success: {BuildMoveLog()}");
                RefreshView();
                return;
            }

            AppendLog($"Combat selected: {combatIndex}");
            RefreshView();
        }

        bool TryValidateDuelStarted(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!sessionRunner.IsInitialized || sessionRunner.DuelState == null)
            {
                failureMessage = "duel state is null. call StartDuel first.";
                return false;
            }

            if (sessionRunner.PhaseRunner == null || !sessionRunner.PhaseRunner.isStarted)
            {
                failureMessage = "phase runner is not started.";
                return false;
            }

            return true;
        }

        void RefreshView()
        {
            DuelState duelState = sessionRunner.DuelState;
            DuelPhaseRunner phaseRunner = sessionRunner.PhaseRunner;

            SetText(phaseText, DuelDebugPanelFormatter.FormatPhase(phaseRunner));
            SetText(turnText, DuelDebugPanelFormatter.FormatTurn(duelState));
            SetText(resourceText, DuelDebugPanelFormatter.FormatResourceStatus());
            SetText(honorText, DuelDebugPanelFormatter.FormatHonor(duelState));
            SetText(playerHealthText, DuelDebugPanelFormatter.FormatPlayerHealth(duelState));
            SetText(opponentHealthText, DuelDebugPanelFormatter.FormatOpponentHealth(duelState));
            SetText(selectedAbilityText, DuelDebugPanelFormatter.FormatSelectedAbility(duelState, selectionState.SelectedAbilityId));
            SetText(selectedCombatText, DuelDebugPanelFormatter.FormatSelectedCombat(duelState, selectionState.SelectedCombatIndex));

            SetText(combat0Text, DuelDebugPanelFormatter.FormatCombat(duelState, 0));
            SetText(combat1Text, DuelDebugPanelFormatter.FormatCombat(duelState, 1));
            SetText(combat2Text, DuelDebugPanelFormatter.FormatCombat(duelState, 2));

            RefreshButtonInteractableState();
        }

        void RefreshButtonInteractableState()
        {
            DuelPhaseRunner phaseRunner = sessionRunner.PhaseRunner;
            DuelState duelState = sessionRunner.DuelState;

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

        string BuildMoveLog()
        {
            return $"Ability moved: {ResolveAbilityDefIdForDisplay(selectionState.SelectedAbilityId)} -> combat({selectionState.SelectedCombatIndex}).";
        }

        string ResolveAbilityDefIdForDisplay(string abilityInstanceId)
        {
            DuelState duelState = sessionRunner.DuelState;
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
