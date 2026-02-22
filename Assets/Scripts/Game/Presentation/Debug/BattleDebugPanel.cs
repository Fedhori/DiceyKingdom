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

        string selectedTroopId = string.Empty;
        int selectedBattlefieldIndex = -1;

        void Awake()
        {
            RefreshStubTexts();
            RefreshStubButtons();
        }

        public void StartBattle()
        {
            AppendLog("StartBattle clicked.");
        }

        public void EnemyDeploy()
        {
            AppendLog("EnemyDeploy clicked.");
        }

        public void PlayerDeploy()
        {
            AppendLog("PlayerDeploy clicked.");
        }

        public void DeploySelected()
        {
            AppendLog("DeploySelected clicked.");
        }

        public void Roll()
        {
            AppendLog("Roll clicked.");
        }

        public void Resolve()
        {
            AppendLog("Resolve clicked.");
        }

        public void Retreat()
        {
            AppendLog("Retreat clicked.");
        }

        public void SelectTroop(string troopId)
        {
            selectedTroopId = troopId ?? string.Empty;
            RefreshStubTexts();
        }

        public void SelectBattlefield(int battlefieldIndex)
        {
            selectedBattlefieldIndex = battlefieldIndex;
            RefreshStubTexts();
        }

        void RefreshStubTexts()
        {
            SetText(phaseText, "Phase: (stub)");
            SetText(turnText, "Turn: (stub)");
            SetText(manaText, "Mana: (stub)");
            SetText(stabilityText, "Stability: (stub)");
            SetText(playerMoraleText, "Player Morale: (stub)");
            SetText(enemyMoraleText, "Enemy Morale: (stub)");
            SetText(selectedTroopText, $"Selected Troop: {selectedTroopId}");
            SetText(selectedBattlefieldText, $"Selected Battlefield: {selectedBattlefieldIndex}");

            SetText(battlefield0Text, "Battlefield 0 (stub)");
            SetText(battlefield1Text, "Battlefield 1 (stub)");
            SetText(battlefield2Text, "Battlefield 2 (stub)");
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
