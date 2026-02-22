using Game.Application.Battle;
using Game.Domain.Battle;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Presentation.Debug
{
    public sealed class BattleDebugPanel : MonoBehaviour
    {
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

        [Header("Debug")]
        [SerializeField] TMP_Text logText;

        BattleState battleState;
        BattlePhaseRunner phaseRunner;
        string selectedTroopId = string.Empty;
        int selectedBattlefieldIndex = -1;

        public BattleState BattleState => battleState;
        public BattlePhaseRunner PhaseRunner => phaseRunner;

        void Awake()
        {
            ResetContext();
            RefreshStubTexts();
            RefreshStubButtons();
        }

        public void StartBattle()
        {
            EnsureContextInitialized();
            AppendLog("StartBattle clicked.");
        }

        public void EnemyDeploy()
        {
            EnsureContextInitialized();
            AppendLog("EnemyDeploy clicked.");
        }

        public void PlayerDeploy()
        {
            EnsureContextInitialized();
            AppendLog("PlayerDeploy clicked.");
        }

        public void DeploySelected()
        {
            EnsureContextInitialized();
            AppendLog("DeploySelected clicked.");
        }

        public void Roll()
        {
            EnsureContextInitialized();
            AppendLog("Roll clicked.");
        }

        public void Resolve()
        {
            EnsureContextInitialized();
            AppendLog("Resolve clicked.");
        }

        public void Retreat()
        {
            EnsureContextInitialized();
            AppendLog("Retreat clicked.");
        }

        public void SelectTroop(string troopId)
        {
            EnsureContextInitialized();
            selectedTroopId = troopId ?? string.Empty;
            RefreshStubTexts();
        }

        public void SelectBattlefield(int battlefieldIndex)
        {
            EnsureContextInitialized();
            selectedBattlefieldIndex = battlefieldIndex;
            RefreshStubTexts();
        }

        public void ResetBattleContext()
        {
            ResetContext();
            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("Battle context reset.");
        }

        void RefreshStubTexts()
        {
            EnsureContextInitialized();

            string phaseLabel = phaseRunner == null ? "(none)" : phaseRunner.currentPhase.ToString();

            SetText(phaseText, $"Phase: {phaseLabel}");
            SetText(turnText, $"Turn: {battleState.turnIndex}");
            SetText(manaText, $"Mana: {battleState.mana}");
            SetText(stabilityText, $"Stability: {battleState.stability}");
            SetText(playerMoraleText, $"Player Morale: {battleState.playerMorale}");
            SetText(enemyMoraleText, $"Enemy Morale: {battleState.enemyMorale}");
            SetText(selectedTroopText, $"Selected Troop: {selectedTroopId}");
            SetText(selectedBattlefieldText, $"Selected Battlefield: {selectedBattlefieldIndex}");

            SetText(battlefield0Text, BuildBattlefieldStubLine(0));
            SetText(battlefield1Text, BuildBattlefieldStubLine(1));
            SetText(battlefield2Text, BuildBattlefieldStubLine(2));
        }

        void RefreshStubButtons()
        {
            SetButtonInteractable(startBattleButton, true);
            SetButtonInteractable(enemyDeployButton, true);
            SetButtonInteractable(playerDeployButton, true);
            SetButtonInteractable(deploySelectedButton, true);
            SetButtonInteractable(rollButton, true);
            SetButtonInteractable(resolveButton, true);
            SetButtonInteractable(retreatButton, true);
        }

        void ResetContext()
        {
            battleState = new BattleState();
            phaseRunner = new BattlePhaseRunner(battleState);
            selectedTroopId = string.Empty;
            selectedBattlefieldIndex = -1;
        }

        void EnsureContextInitialized()
        {
            if (battleState == null || phaseRunner == null)
            {
                ResetContext();
                AppendLog("Context auto-initialized.");
            }
        }

        string BuildBattlefieldStubLine(int index)
        {
            if (index < 0 || index >= battleState.battlefields.Count)
            {
                return $"Battlefield {index}: (missing)";
            }

            BattlefieldState battlefield = battleState.battlefields[index];
            battlefield.EnsureInitialized();

            return $"Battlefield {index} | P:{battlefield.playerTroopIds.Count} E:{battlefield.enemyTroopIds.Count}";
        }

        void AppendLog(string message)
        {
            if (logText != null)
            {
                if (string.IsNullOrEmpty(logText.text))
                {
                    logText.text = message;
                }
                else
                {
                    logText.text = $"{logText.text}\n{message}";
                }
            }

            UnityEngine.Debug.Log($"[BattleDebugPanel] {message}");
        }

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
