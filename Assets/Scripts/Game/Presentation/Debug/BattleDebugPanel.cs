using Game.Application.Battle;
using Game.Application.Battle.Effects;
using Game.Domain.Battle;
using Game.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Game.Presentation.Debug
{
    public sealed class BattleDebugPanel : MonoBehaviour
    {
        const string debugEncounterId = "enc_debug_01";

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
        [SerializeField] ScrollRect logScrollRect;
        [SerializeField] int maxLogEntryCount = 200;
        [SerializeField] int troopBlockHeight = 28;

        BattleState battleState;
        BattlePhaseRunner phaseRunner;
        readonly BattleEffectResolver effectResolver = new BattleEffectResolver();
        TroopTimedEffectRunner troopTimedEffectRunner;
        string selectedTroopId = string.Empty;
        int selectedBattlefieldIndex = -1;
        readonly List<string> logEntries = new List<string>();
        readonly List<GameObject> runtimeTroopBlockObjects = new List<GameObject>();
        readonly UnityAction[] battlefieldCardClickActions = new UnityAction[BattleState.defaultBattlefieldCount];

        RectTransform campTroopBlockRoot;
        readonly RectTransform[] battlefieldPlayerTroopBlockRoots = new RectTransform[BattleState.defaultBattlefieldCount];
        readonly RectTransform[] battlefieldEnemyTroopBlockRoots = new RectTransform[BattleState.defaultBattlefieldCount];

        public BattleState BattleState => battleState;
        public BattlePhaseRunner PhaseRunner => phaseRunner;

        void Awake()
        {
            AutoBindUiReferencesByName();
            EnsureCampTroopTextDisplay();
            EnsureTroopBlockUiSetup();
            EnsureBattlefieldCardClickBindings();
            HideLegacyDeploySelectedButton();
            EnsureLogScrollSetup();
            WireDefaultButtonCallbacks();
            ResetContext();
            RefreshStubTexts();
            RefreshStubButtons();
        }

        public void StartBattle()
        {
            if (!TryCreateBattleContextFromData(
                    out BattleState nextState,
                    out GameDatabase sourceDatabase,
                    out string failureMessage))
            {
                RejectAction("StartBattle", failureMessage);
                return;
            }

            battleState = nextState;
            phaseRunner = new BattlePhaseRunner(battleState);
            troopTimedEffectRunner = CreateTroopTimedEffectRunner(sourceDatabase);

            bool started = phaseRunner.StartBattle();
            if (!started)
            {
                RejectAction("StartBattle", phaseRunner.LastFailureReason.ToString());
                return;
            }

            AutoDeployEnemyIntent(battleState, sourceDatabase);

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"StartBattle success: encounter={debugEncounterId}");
        }

        public void EnemyDeploy()
        {
            EnsureContextInitialized();

            if (!phaseRunner.isStarted)
            {
                RejectAction("EnemyDeploy", "battle is not started.");
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

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("EnemyDeploy phase entered.");
        }

        public void PlayerDeploy()
        {
            EnsureContextInitialized();

            if (!phaseRunner.isStarted)
            {
                RejectAction("PlayerDeploy", "battle is not started.");
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

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("PlayerDeploy phase entered.");
        }

        public void DeploySelected()
        {
            EnsureContextInitialized();

            if (!TryDeploySelectedTroop(out string failureMessage, out string moveLog))
            {
                RejectAction("DeploySelected", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"DeploySelected success: {moveLog}");
        }

        public void Roll()
        {
            EnsureContextInitialized();

            if (!TryRollAllDeployedTroops(out int rolledCount, out string failureMessage))
            {
                RejectAction("Roll", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"Roll success: rolledTroops={rolledCount}");
        }

        public void Resolve()
        {
            EnsureContextInitialized();

            if (!TryResolveAllBattlefields(out int resolvedCount, out string failureMessage))
            {
                RejectAction("Resolve", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"Resolve success: resolvedBattlefields={resolvedCount}");
        }

        public void Retreat()
        {
            EnsureContextInitialized();

            if (!TryRetreatBattle(out string failureMessage))
            {
                RejectAction("Retreat", failureMessage);
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog("Retreat success: battle ended.");
        }

        public void SelectTroop(string troopId)
        {
            EnsureContextInitialized();

            if (string.IsNullOrWhiteSpace(troopId))
            {
                selectedTroopId = string.Empty;
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
            selectedBattlefieldIndex = locationType == TroopLocationType.Battlefield
                ? battlefieldIndex
                : selectedBattlefieldIndex;
            RefreshStubTexts();
            RefreshStubButtons();
            string troopDefId = ResolveTroopDefIdForDisplay(selectedTroopId);
            AppendLog($"Troop selected: {troopDefId}");
        }

        public void SelectBattlefield(int battlefieldIndex)
        {
            EnsureContextInitialized();

            if (battlefieldIndex < 0 || battlefieldIndex >= battleState.battlefields.Count)
            {
                RejectAction(
                    "SelectBattlefield",
                    $"battlefieldIndex({battlefieldIndex}) is out of range.");
                return;
            }

            selectedBattlefieldIndex = battlefieldIndex;

            if (CanAutoDeployOnBattlefieldClick() &&
                !string.IsNullOrWhiteSpace(selectedTroopId))
            {
                if (!TryMovePlayerTroopToLocation(
                        selectedTroopId,
                        TroopLocationType.Battlefield,
                        battlefieldIndex,
                        out string moveLog,
                        out string failureMessage))
                {
                    RejectAction("SelectBattlefieldDeploy", failureMessage);
                    return;
                }

                RefreshStubTexts();
                RefreshStubButtons();
                AppendLog($"Battlefield click deploy success: {moveLog}");
                return;
            }

            RefreshStubTexts();
            RefreshStubButtons();
            AppendLog($"Battlefield selected: {selectedBattlefieldIndex}");
        }

        public void SelectFirstCampTroop()
        {
            EnsureContextInitialized();

            if (!phaseRunner.isStarted)
            {
                RejectAction("SelectFirstCampTroop", "battle is not started.");
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
            GameDatabase currentDatabase = GameDataRuntime.CurrentDatabase;

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

            SetText(battlefield0Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 0, currentDatabase));
            SetText(battlefield1Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 1, currentDatabase));
            SetText(battlefield2Text, BattleDebugPanelFormatter.FormatBattlefield(battleState, 2, currentDatabase));
            RefreshTroopBlocks();
        }

        void RefreshStubButtons()
        {
            EnsureContextInitialized();

            bool isStarted = phaseRunner.isStarted;
            bool isEnded = battleState.isBattleEnded;
            BattlePhase currentPhase = phaseRunner.currentPhase;

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

        void ResetContext()
        {
            battleState = new BattleState();
            phaseRunner = new BattlePhaseRunner(battleState);
            troopTimedEffectRunner = null;
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

            if (troopTimedEffectRunner == null)
            {
                troopTimedEffectRunner = CreateTroopTimedEffectRunner(GameDataRuntime.CurrentDatabase);
            }
        }

        void AutoBindUiReferencesByName()
        {
            phaseText = ResolveBinding(phaseText, "PhaseText");
            turnText = ResolveBinding(turnText, "TurnText");
            manaText = ResolveBinding(manaText, "ManaText");
            stabilityText = ResolveBinding(stabilityText, "StabilityText");
            playerMoraleText = ResolveBinding(playerMoraleText, "PlayerMoraleText");
            enemyMoraleText = ResolveBinding(enemyMoraleText, "EnemyMoraleText");
            selectedTroopText = ResolveBinding(selectedTroopText, "SelectedTroopText");
            selectedBattlefieldText = ResolveBinding(selectedBattlefieldText, "SelectedBattlefieldText");

            battlefield0Text = ResolveBinding(battlefield0Text, "Battlefield0Text");
            battlefield1Text = ResolveBinding(battlefield1Text, "Battlefield1Text");
            battlefield2Text = ResolveBinding(battlefield2Text, "Battlefield2Text");

            startBattleButton = ResolveBinding(startBattleButton, "StartBattleButton");
            enemyDeployButton = ResolveBinding(enemyDeployButton, "EnemyDeployButton");
            playerDeployButton = ResolveBinding(playerDeployButton, "PlayerDeployButton");
            deploySelectedButton = ResolveBinding(deploySelectedButton, "DeploySelectedButton");
            rollButton = ResolveBinding(rollButton, "RollButton");
            resolveButton = ResolveBinding(resolveButton, "ResolveButton");
            retreatButton = ResolveBinding(retreatButton, "RetreatButton");

            logText = ResolveBinding(logText, "LogText");
            if (logScrollRect == null)
            {
                logScrollRect = FindChildComponentByName<ScrollRect>("LogScrollRect");
            }
        }

        void EnsureCampTroopTextDisplay()
        {
            if (selectedTroopText == null)
            {
                return;
            }

            selectedTroopText.textWrappingMode = TextWrappingModes.Normal;
            selectedTroopText.overflowMode = TextOverflowModes.Overflow;
            selectedTroopText.alignment = TextAlignmentOptions.TopLeft;

            LayoutElement layoutElement = selectedTroopText.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = selectedTroopText.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredHeight = 32f;
            layoutElement.flexibleHeight = 0f;
        }

        enum TroopLocationType
        {
            None = 0,
            Camp = 1,
            Battlefield = 2
        }

        void EnsureTroopBlockUiSetup()
        {
            campTroopBlockRoot = EnsureTroopBlockRoot(
                "CampPanel",
                "CampTroopBlockRoot",
                2,
                200f,
                new Color(0.75f, 0.82f, 0.9f, 0.35f));

            for (int i = 0; i < battlefieldPlayerTroopBlockRoots.Length; i++)
            {
                battlefieldPlayerTroopBlockRoots[i] = EnsureTroopBlockRoot(
                    $"Battlefield{i}Card",
                    $"Battlefield{i}PlayerTroopBlockRoot",
                    1,
                    52f,
                    new Color(0.73f, 0.84f, 0.96f, 0.45f));

                battlefieldEnemyTroopBlockRoots[i] = EnsureTroopBlockRoot(
                    $"Battlefield{i}Card",
                    $"Battlefield{i}EnemyTroopBlockRoot",
                    2,
                    52f,
                    new Color(0.94f, 0.78f, 0.78f, 0.45f));
            }
        }

        void EnsureBattlefieldCardClickBindings()
        {
            for (int battlefieldIndex = 0; battlefieldIndex < BattleState.defaultBattlefieldCount; battlefieldIndex++)
            {
                EnsureBattlefieldCardClickBinding(battlefieldIndex);
            }
        }

        void EnsureBattlefieldCardClickBinding(int battlefieldIndex)
        {
            RectTransform cardRect = FindChildComponentByName<RectTransform>($"Battlefield{battlefieldIndex}Card");
            if (cardRect == null)
            {
                WarnAndLog($"Battlefield click setup warning: Battlefield{battlefieldIndex}Card was not found.");
                return;
            }

            TMP_Text[] allCardTexts = cardRect.GetComponentsInChildren<TMP_Text>(true);
            for (int textIndex = 0; textIndex < allCardTexts.Length; textIndex++)
            {
                TMP_Text text = allCardTexts[textIndex];
                if (text == null)
                {
                    continue;
                }

                text.raycastTarget = false;
            }

            string clickZoneName = $"Battlefield{battlefieldIndex}ClickZone";
            RectTransform legacyClickZoneRect = FindDirectChildRect(cardRect, clickZoneName);
            if (legacyClickZoneRect != null)
            {
                legacyClickZoneRect.gameObject.SetActive(false);
            }

            Image cardImage = cardRect.GetComponent<Image>();
            if (cardImage == null)
            {
                cardImage = cardRect.gameObject.AddComponent<Image>();
                cardImage.color = new Color(0f, 0f, 0f, 0.001f);
            }

            cardImage.raycastTarget = true;

            Button cardButton = cardRect.GetComponent<Button>();
            if (cardButton == null)
            {
                cardButton = cardRect.gameObject.AddComponent<Button>();
            }

            cardButton.interactable = true;
            cardButton.enabled = true;
            cardButton.targetGraphic = cardImage;
            cardButton.transition = Selectable.Transition.None;

            if (battlefieldCardClickActions[battlefieldIndex] == null)
            {
                int capturedIndex = battlefieldIndex;
                battlefieldCardClickActions[battlefieldIndex] = () => SelectBattlefield(capturedIndex);
            }

            cardButton.onClick.RemoveListener(battlefieldCardClickActions[battlefieldIndex]);
            cardButton.onClick.AddListener(battlefieldCardClickActions[battlefieldIndex]);
        }

        void HideLegacyDeploySelectedButton()
        {
            if (deploySelectedButton == null)
            {
                return;
            }

            deploySelectedButton.gameObject.SetActive(false);
        }

        RectTransform EnsureTroopBlockRoot(
            string panelName,
            string blockRootName,
            int siblingIndex,
            float preferredHeight,
            Color backgroundColor)
        {
            RectTransform panelRect = FindChildComponentByName<RectTransform>(panelName);
            if (panelRect == null)
            {
                WarnAndLog($"Troop block setup warning: panel '{panelName}' was not found.");
                return null;
            }

            RectTransform blockRoot = FindDirectChildRect(panelRect, blockRootName);
            if (blockRoot == null)
            {
                var blockRootObject = new GameObject(
                    blockRootName,
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Image),
                    typeof(VerticalLayoutGroup));
                blockRoot = blockRootObject.GetComponent<RectTransform>();
                blockRoot.SetParent(panelRect, false);
            }

            int safeSiblingIndex = Mathf.Clamp(siblingIndex, 0, Mathf.Max(0, panelRect.childCount - 1));
            blockRoot.SetSiblingIndex(safeSiblingIndex);

            LayoutElement layoutElement = blockRoot.GetComponent<LayoutElement>();
            layoutElement.minHeight = 24f;
            layoutElement.preferredHeight = preferredHeight;
            layoutElement.flexibleHeight = 1f;

            Image blockImage = blockRoot.GetComponent<Image>();
            blockImage.color = backgroundColor;
            blockImage.raycastTarget = false;

            VerticalLayoutGroup layoutGroup = blockRoot.GetComponent<VerticalLayoutGroup>();
            layoutGroup.padding.left = 4;
            layoutGroup.padding.right = 4;
            layoutGroup.padding.top = 4;
            layoutGroup.padding.bottom = 4;
            layoutGroup.spacing = 4;
            layoutGroup.childAlignment = TextAnchor.UpperLeft;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            return blockRoot;
        }

        static RectTransform FindDirectChildRect(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (!string.Equals(child.name, childName, StringComparison.Ordinal))
                {
                    continue;
                }

                return child as RectTransform;
            }

            return null;
        }

        void RefreshTroopBlocks()
        {
            EnsureTroopBlockUiSetup();
            ClearRuntimeTroopBlocks();

            if (battleState == null || battleState.troopsById == null)
            {
                return;
            }

            if (campTroopBlockRoot != null && battleState.campTroopIds != null)
            {
                for (int i = 0; i < battleState.campTroopIds.Count; i++)
                {
                    CreateTroopBlock(campTroopBlockRoot, battleState.campTroopIds[i], true);
                }
            }

            int battlefieldCount = Mathf.Min(
                battleState.battlefields == null ? 0 : battleState.battlefields.Count,
                Mathf.Min(battlefieldPlayerTroopBlockRoots.Length, battlefieldEnemyTroopBlockRoots.Length));

            for (int battlefieldIndex = 0; battlefieldIndex < battlefieldCount; battlefieldIndex++)
            {
                BattlefieldState battlefield = battleState.battlefields[battlefieldIndex];
                if (battlefield == null)
                {
                    continue;
                }

                battlefield.EnsureInitialized();

                RectTransform playerBlockRoot = battlefieldPlayerTroopBlockRoots[battlefieldIndex];
                if (playerBlockRoot != null)
                {
                    for (int i = 0; i < battlefield.playerTroopIds.Count; i++)
                    {
                        CreateTroopBlock(playerBlockRoot, battlefield.playerTroopIds[i], true);
                    }
                }

                RectTransform enemyBlockRoot = battlefieldEnemyTroopBlockRoots[battlefieldIndex];
                if (enemyBlockRoot != null)
                {
                    for (int i = 0; i < battlefield.enemyTroopIds.Count; i++)
                    {
                        CreateTroopBlock(enemyBlockRoot, battlefield.enemyTroopIds[i], false);
                    }
                }
            }
        }

        void ClearRuntimeTroopBlocks()
        {
            for (int i = 0; i < runtimeTroopBlockObjects.Count; i++)
            {
                GameObject troopBlockObject = runtimeTroopBlockObjects[i];
                if (troopBlockObject == null)
                {
                    continue;
                }

                troopBlockObject.SetActive(false);
                Destroy(troopBlockObject);
            }

            runtimeTroopBlockObjects.Clear();
        }

        void CreateTroopBlock(RectTransform parent, string troopId, bool canSelect)
        {
            if (parent == null || string.IsNullOrWhiteSpace(troopId))
            {
                return;
            }

            if (battleState == null ||
                battleState.troopsById == null ||
                !battleState.troopsById.TryGetValue(troopId, out TroopInstance troop) ||
                troop == null)
            {
                return;
            }

            string capturedTroopId = troopId;
            var troopObject = new GameObject(
                $"TroopBlock_{capturedTroopId}",
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(Image));
            RectTransform troopRect = troopObject.GetComponent<RectTransform>();
            troopRect.SetParent(parent, false);
            troopRect.anchorMin = new Vector2(0f, 0f);
            troopRect.anchorMax = new Vector2(1f, 1f);
            troopRect.pivot = new Vector2(0.5f, 0.5f);
            troopRect.anchoredPosition = Vector2.zero;
            troopRect.sizeDelta = Vector2.zero;

            LayoutElement layoutElement = troopObject.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = Mathf.Max(24, troopBlockHeight);
            layoutElement.flexibleHeight = 0f;

            Image backgroundImage = troopObject.GetComponent<Image>();
            bool isSelected = string.Equals(capturedTroopId, selectedTroopId, StringComparison.Ordinal);
            backgroundImage.color = isSelected
                ? new Color(0.2f, 0.5f, 0.8f, 1f)
                : canSelect
                    ? new Color(0.52f, 0.58f, 0.66f, 0.95f)
                    : new Color(0.72f, 0.48f, 0.48f, 0.95f);
            backgroundImage.raycastTarget = canSelect;

            if (canSelect)
            {
                Button button = troopObject.AddComponent<Button>();
                button.targetGraphic = backgroundImage;
                button.transition = Selectable.Transition.ColorTint;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectTroop(capturedTroopId));
            }

            TMP_Text infoLabel = CreateTroopInfoLabel(troopRect);
            infoLabel.text = BuildTroopInfoLabel(capturedTroopId, troop);

            TMP_Text attackLabel = CreateTroopAttackLabel(troopRect);
            attackLabel.text = BuildTroopAttackText(troop);

            runtimeTroopBlockObjects.Add(troopObject);
        }

        TMP_Text CreateTroopInfoLabel(RectTransform parent)
        {
            var labelObject = new GameObject(
                "InfoLabel",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.SetParent(parent, false);
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.pivot = new Vector2(0f, 0.5f);
            labelRect.anchoredPosition = Vector2.zero;
            labelRect.offsetMin = new Vector2(8f, 1f);
            labelRect.offsetMax = new Vector2(-66f, -1f);

            var label = labelObject.GetComponent<TextMeshProUGUI>();
            label.fontSize = 12f;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.raycastTarget = false;
            return label;
        }

        TMP_Text CreateTroopAttackLabel(RectTransform parent)
        {
            var attackObject = new GameObject(
                "AttackLabel",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            RectTransform attackRect = attackObject.GetComponent<RectTransform>();
            attackRect.SetParent(parent, false);
            attackRect.anchorMin = new Vector2(1f, 0f);
            attackRect.anchorMax = new Vector2(1f, 1f);
            attackRect.pivot = new Vector2(1f, 0.5f);
            attackRect.anchoredPosition = new Vector2(-8f, 0f);
            attackRect.sizeDelta = new Vector2(56f, 0f);

            var attackLabel = attackObject.GetComponent<TextMeshProUGUI>();
            attackLabel.fontSize = 18f;
            attackLabel.fontStyle = FontStyles.Bold;
            attackLabel.color = Color.white;
            attackLabel.alignment = TextAlignmentOptions.MidlineRight;
            attackLabel.textWrappingMode = TextWrappingModes.NoWrap;
            attackLabel.overflowMode = TextOverflowModes.Overflow;
            attackLabel.raycastTarget = false;
            return attackLabel;
        }

        string BuildTroopInfoLabel(string troopId, TroopInstance troop)
        {
            string troopDefId = ResolveTroopDefIdForDisplay(troopId);
            string effectsLabel = BattleDebugPanelFormatter.FormatTroopEffects(GameDataRuntime.CurrentDatabase, troop.troopDefId);
            return $"{troopDefId} | R:{troop.attackResult} | Effects:{effectsLabel}";
        }

        static string BuildTroopAttackText(TroopInstance troop)
        {
            if (troop == null)
            {
                return "A:0";
            }

            return $"A:{troop.attack}";
        }

        bool TryMovePlayerTroopToLocation(
            string troopId,
            TroopLocationType targetType,
            int targetBattlefieldIndex,
            out string moveLog,
            out string failureMessage)
        {
            moveLog = string.Empty;
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (battleState.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            if (phaseRunner.currentPhase != BattlePhase.PlayerDeploy)
            {
                failureMessage =
                    $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.PlayerDeploy}.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(troopId))
            {
                failureMessage = "troopId is empty.";
                return false;
            }

            if (battleState.troopsById == null ||
                !battleState.troopsById.TryGetValue(troopId, out TroopInstance troopInstance) ||
                troopInstance == null)
            {
                failureMessage = $"troop({troopId}) does not exist.";
                return false;
            }

            if (!TryFindPlayerTroopLocation(troopId, out TroopLocationType sourceType, out int sourceBattlefieldIndex))
            {
                failureMessage = $"troop({troopId}) is not in player controllable zones.";
                return false;
            }

            if (targetType == TroopLocationType.Battlefield &&
                (targetBattlefieldIndex < 0 || targetBattlefieldIndex >= battleState.battlefields.Count))
            {
                failureMessage = $"target battlefield({targetBattlefieldIndex}) is out of range.";
                return false;
            }

            bool isSameLocation =
                sourceType == targetType &&
                (targetType != TroopLocationType.Battlefield || sourceBattlefieldIndex == targetBattlefieldIndex);
            if (isSameLocation)
            {
                string troopDefId = ResolveTroopDefIdForDisplay(troopId);
                string locationLabel = FormatTroopLocationLabel(targetType, targetBattlefieldIndex);
                moveLog = $"Troop move skipped: {troopDefId} already in {locationLabel}.";
                return true;
            }

            if (!RemoveTroopFromLocation(troopId, sourceType, sourceBattlefieldIndex))
            {
                failureMessage = "failed to remove troop from current location.";
                return false;
            }

            if (!TryAddTroopToLocation(troopId, targetType, targetBattlefieldIndex, out string addFailureMessage))
            {
                TryAddTroopToLocation(troopId, sourceType, sourceBattlefieldIndex, out _);
                failureMessage = addFailureMessage;
                return false;
            }

            selectedTroopId = troopId;
            selectedBattlefieldIndex = targetType == TroopLocationType.Battlefield ? targetBattlefieldIndex : -1;

            string troopDefIdAfterMove = ResolveTroopDefIdForDisplay(troopId);
            string fromLabel = FormatTroopLocationLabel(sourceType, sourceBattlefieldIndex);
            string toLabel = FormatTroopLocationLabel(targetType, targetBattlefieldIndex);
            moveLog = $"Troop moved: {troopDefIdAfterMove} {fromLabel} -> {toLabel}.";
            return true;
        }

        bool CanAutoDeployOnBattlefieldClick()
        {
            if (phaseRunner == null || battleState == null)
            {
                return false;
            }

            if (!phaseRunner.isStarted || battleState.isBattleEnded)
            {
                return false;
            }

            return phaseRunner.currentPhase == BattlePhase.PlayerDeploy;
        }

        bool TryFindPlayerTroopLocation(string troopId, out TroopLocationType locationType, out int battlefieldIndex)
        {
            locationType = TroopLocationType.None;
            battlefieldIndex = -1;

            if (battleState == null || string.IsNullOrWhiteSpace(troopId))
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

        bool RemoveTroopFromLocation(string troopId, TroopLocationType sourceType, int sourceBattlefieldIndex)
        {
            if (sourceType == TroopLocationType.Camp)
            {
                return battleState.campTroopIds != null && battleState.campTroopIds.Remove(troopId);
            }

            if (sourceType != TroopLocationType.Battlefield ||
                sourceBattlefieldIndex < 0 ||
                sourceBattlefieldIndex >= battleState.battlefields.Count)
            {
                return false;
            }

            BattlefieldState sourceBattlefield = battleState.battlefields[sourceBattlefieldIndex];
            if (sourceBattlefield == null)
            {
                return false;
            }

            sourceBattlefield.EnsureInitialized();
            return sourceBattlefield.playerTroopIds.Remove(troopId);
        }

        bool TryAddTroopToLocation(
            string troopId,
            TroopLocationType targetType,
            int targetBattlefieldIndex,
            out string failureMessage)
        {
            failureMessage = string.Empty;

            if (targetType == TroopLocationType.Camp)
            {
                if (battleState.campTroopIds == null)
                {
                    failureMessage = "campTroopIds is null.";
                    return false;
                }

                if (!battleState.campTroopIds.Contains(troopId))
                {
                    battleState.campTroopIds.Add(troopId);
                }

                return true;
            }

            if (targetType != TroopLocationType.Battlefield ||
                targetBattlefieldIndex < 0 ||
                targetBattlefieldIndex >= battleState.battlefields.Count)
            {
                failureMessage = $"target battlefield({targetBattlefieldIndex}) is out of range.";
                return false;
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

            if (!targetBattlefield.playerTroopIds.Contains(troopId))
            {
                targetBattlefield.playerTroopIds.Add(troopId);
            }

            return true;
        }

        static string FormatTroopLocationLabel(TroopLocationType locationType, int battlefieldIndex)
        {
            if (locationType == TroopLocationType.Camp)
            {
                return "camp";
            }

            if (locationType == TroopLocationType.Battlefield)
            {
                return $"battlefield({battlefieldIndex})";
            }

            return "unknown";
        }

        void WireDefaultButtonCallbacks()
        {
            WireButton(startBattleButton, StartBattle);
            WireButton(enemyDeployButton, EnemyDeploy);
            WireButton(playerDeployButton, PlayerDeploy);
            WireButton(rollButton, Roll);
            WireButton(resolveButton, Resolve);
            WireButton(retreatButton, Retreat);

            WireOptionalButton("SelectFirstCampTroopButton", SelectFirstCampTroop);
            WireOptionalButton("SelectBattlefield0Button", SelectBattlefield0);
            WireOptionalButton("SelectBattlefield1Button", SelectBattlefield1);
            WireOptionalButton("SelectBattlefield2Button", SelectBattlefield2);
        }

        bool TryCreateBattleContextFromData(
            out BattleState nextState,
            out GameDatabase sourceDatabase,
            out string failureMessage)
        {
            nextState = null;
            sourceDatabase = null;
            failureMessage = string.Empty;

            GameDatabase database = GameDataRuntime.CurrentDatabase;
            if (database == null)
            {
                failureMessage = "GameDataRuntime.CurrentDatabase is null.";
                return false;
            }

            if (database.battleConfig == null)
            {
                failureMessage = "battle_config is missing.";
                return false;
            }

            if (database.runConfig == null)
            {
                failureMessage = "run_config is missing.";
                return false;
            }

            if (database.playerStart == null)
            {
                failureMessage = "player_start is missing.";
                return false;
            }

            if (!database.encountersById.TryGetValue(debugEncounterId, out EncounterDef encounterDef) ||
                encounterDef == null)
            {
                failureMessage = $"encounter('{debugEncounterId}') is missing.";
                return false;
            }

            sourceDatabase = database;
            nextState = CreateInitialBattleState(database, encounterDef);
            return true;
        }

        BattleState CreateInitialBattleState(GameDatabase database, EncounterDef encounterDef)
        {
            var nextState = new BattleState
            {
                turnIndex = 0,
                isBattleEnded = false,
                mana = database.playerStart.startingMana,
                stability = database.playerStart.startingStability,
                playerMorale = Mathf.Max(1, database.playerStart.startingPlayerMorale),
                enemyMorale = Mathf.Max(1, encounterDef.enemyMorale)
            };

            nextState.cooldowns.Clear();
            nextState.campTroopIds.Clear();
            nextState.troopsById.Clear();
            nextState.enemyIntent.Clear();

            InitializeBattlefieldSlots(nextState, database);
            PopulateEnemyIntent(nextState, encounterDef);
            PopulateCampFromPlayerStart(nextState, database);

            return nextState;
        }

        void InitializeBattlefieldSlots(BattleState nextState, GameDatabase database)
        {
            nextState.battlefields.Clear();

            List<BattlefieldDef> orderedDefs = database.battlefieldsById
                .Values
                .OrderBy(def => def.id, StringComparer.Ordinal)
                .ToList();

            int targetCount = Mathf.Max(1, database.battleConfig.battlefieldCount);
            for (int i = 0; i < targetCount; i++)
            {
                BattlefieldDef sourceDef = i < orderedDefs.Count ? orderedDefs[i] : null;
                int? resolvedSlotLimit = sourceDef != null
                    ? sourceDef.slotLimit
                    : database.battleConfig.p0Rules.defaultSlotLimit;

                var field = new BattlefieldState
                {
                    battlefieldId = sourceDef?.id ?? $"missing_battlefield_{i}",
                    slotLimit = resolvedSlotLimit,
                    totalAttackBonusPlayer = 0,
                    totalAttackBonusEnemy = 0
                };

                field.EnsureInitialized();
                nextState.battlefields.Add(field);
            }

            nextState.EnsureInitialized();
        }

        void PopulateEnemyIntent(BattleState nextState, EncounterDef encounterDef)
        {
            if (encounterDef.plans == null)
            {
                return;
            }

            for (int planIndex = 0; planIndex < encounterDef.plans.Count; planIndex++)
            {
                EncounterPlanDef plan = encounterDef.plans[planIndex];
                if (plan == null || plan.troops == null)
                {
                    continue;
                }

                for (int troopIndex = 0; troopIndex < plan.troops.Count; troopIndex++)
                {
                    SummonTroopRefDef troop = plan.troops[troopIndex];
                    if (troop == null || troop.count <= 0 || string.IsNullOrWhiteSpace(troop.troopId))
                    {
                        continue;
                    }

                    nextState.enemyIntent.Add(new EnemyIntentEntry
                    {
                        battlefieldIndex = plan.battlefieldIndex,
                        troopDefId = troop.troopId,
                        count = troop.count
                    });
                }
            }
        }

        void PopulateCampFromPlayerStart(BattleState nextState, GameDatabase database)
        {
            if (database.playerStart == null)
            {
                WarnAndLog("StartBattle warning: player_start is missing.");
                return;
            }

            int addedTroopCount = 0;
            for (int cardIndex = 0; cardIndex < database.playerStart.startingSquadCardIds.Count; cardIndex++)
            {
                string cardId = database.playerStart.startingSquadCardIds[cardIndex];
                if (string.IsNullOrWhiteSpace(cardId))
                {
                    WarnAndLog($"StartBattle warning: startingSquadCardIds[{cardIndex}] is empty.");
                    continue;
                }

                if (!database.cardsById.TryGetValue(cardId, out CardDef squadCard) || squadCard == null)
                {
                    WarnAndLog($"StartBattle warning: card('{cardId}') is missing.");
                    continue;
                }

                if (!string.Equals(squadCard.type, "Squad", StringComparison.Ordinal))
                {
                    WarnAndLog($"StartBattle warning: card('{cardId}') is not Squad.");
                    continue;
                }

                if (squadCard.battleStart == null || squadCard.battleStart.summonTroops == null)
                {
                    continue;
                }

                for (int summonIndex = 0; summonIndex < squadCard.battleStart.summonTroops.Count; summonIndex++)
                {
                    SummonTroopRefDef summon = squadCard.battleStart.summonTroops[summonIndex];
                    if (summon == null || summon.count <= 0 || string.IsNullOrWhiteSpace(summon.troopId))
                    {
                        continue;
                    }

                    if (!database.troopsById.TryGetValue(summon.troopId, out TroopDef troopDef) || troopDef == null)
                    {
                        WarnAndLog($"StartBattle warning: troopDef('{summon.troopId}') is missing.");
                        continue;
                    }

                    for (int i = 0; i < summon.count; i++)
                    {
                        TroopInstance troopInstance = CreateTroopInstance(troopDef);
                        nextState.troopsById[troopInstance.instanceId] = troopInstance;
                        nextState.campTroopIds.Add(troopInstance.instanceId);
                        addedTroopCount += 1;
                    }
                }
            }

            if (addedTroopCount <= 0)
            {
                WarnAndLog("StartBattle warning: no troops were added from player_start.startingSquadCardIds.");
            }
        }

        static TroopInstance CreateTroopInstance(TroopDef troopDef)
        {
            var troopInstance = new TroopInstance
            {
                troopDefId = troopDef.id,
                attack = Mathf.Max(1, troopDef.attack),
                baseRoll = 0,
                attackResult = 0
            };

            if (troopDef.tags != null && troopDef.tags.Count > 0)
            {
                troopInstance.tags.AddRange(troopDef.tags);
            }

            troopInstance.EnsureInitialized();
            return troopInstance;
        }

        void AutoDeployEnemyIntent(BattleState nextState, GameDatabase sourceDatabase)
        {
            if (nextState == null)
            {
                WarnAndLog("Enemy auto deploy skipped: battleState is null.");
                return;
            }

            if (sourceDatabase == null)
            {
                WarnAndLog("Enemy auto deploy skipped: sourceDatabase is null.");
                return;
            }

            int deployedCount = 0;
            int skippedCount = 0;

            for (int i = 0; i < nextState.enemyIntent.Count; i++)
            {
                EnemyIntentEntry intent = nextState.enemyIntent[i];
                if (intent == null)
                {
                    skippedCount += 1;
                    WarnAndLog($"Enemy auto deploy warning: enemyIntent[{i}] is null.");
                    continue;
                }

                if (!sourceDatabase.troopsById.TryGetValue(intent.troopDefId, out TroopDef troopDef) || troopDef == null)
                {
                    skippedCount += Mathf.Max(1, intent.count);
                    WarnAndLog($"Enemy auto deploy warning: troopDef('{intent.troopDefId}') is missing.");
                    continue;
                }

                if (intent.battlefieldIndex < 0 || intent.battlefieldIndex >= nextState.battlefields.Count)
                {
                    WarnAndLog(
                        $"Enemy auto deploy warning: battlefieldIndex({intent.battlefieldIndex}) is out of range. Trying fallback battlefield.");
                }

                int requestedCount = Mathf.Max(0, intent.count);
                for (int copyIndex = 0; copyIndex < requestedCount; copyIndex++)
                {
                    if (!TryFindEnemyBattlefieldForDeploy(
                            nextState,
                            intent.battlefieldIndex,
                            out int deployBattlefieldIndex,
                            out bool usedFallback))
                    {
                        skippedCount += 1;
                        WarnAndLog(
                            $"Enemy auto deploy warning: no available battlefield slot for troopDef('{intent.troopDefId}').");
                        continue;
                    }

                    BattlefieldState deployBattlefield = nextState.battlefields[deployBattlefieldIndex];
                    deployBattlefield.EnsureInitialized();

                    TroopInstance troopInstance = CreateTroopInstance(troopDef);
                    nextState.troopsById[troopInstance.instanceId] = troopInstance;
                    deployBattlefield.enemyTroopIds.Add(troopInstance.instanceId);
                    deployedCount += 1;

                    if (usedFallback)
                    {
                        WarnAndLog(
                            $"Enemy auto deploy warning: redirected troopDef('{intent.troopDefId}') battlefield({intent.battlefieldIndex}) -> battlefield({deployBattlefieldIndex}).");
                    }
                }
            }

            AppendLog(
                $"Enemy auto deploy complete: deployed={deployedCount}, skipped={skippedCount}");
        }

        bool TryFindEnemyBattlefieldForDeploy(
            BattleState state,
            int preferredBattlefieldIndex,
            out int resolvedBattlefieldIndex,
            out bool usedFallback)
        {
            resolvedBattlefieldIndex = -1;
            usedFallback = false;

            if (state == null || state.battlefields == null || state.battlefields.Count <= 0)
            {
                return false;
            }

            if (CanDeployEnemyToBattlefield(state, preferredBattlefieldIndex))
            {
                resolvedBattlefieldIndex = preferredBattlefieldIndex;
                return true;
            }

            for (int battlefieldIndex = 0; battlefieldIndex < state.battlefields.Count; battlefieldIndex++)
            {
                if (battlefieldIndex == preferredBattlefieldIndex)
                {
                    continue;
                }

                if (!CanDeployEnemyToBattlefield(state, battlefieldIndex))
                {
                    continue;
                }

                resolvedBattlefieldIndex = battlefieldIndex;
                usedFallback = true;
                return true;
            }

            return false;
        }

        bool CanDeployEnemyToBattlefield(BattleState state, int battlefieldIndex)
        {
            if (state == null || state.battlefields == null)
            {
                return false;
            }

            if (battlefieldIndex < 0 || battlefieldIndex >= state.battlefields.Count)
            {
                return false;
            }

            BattlefieldState battlefield = state.battlefields[battlefieldIndex];
            if (battlefield == null)
            {
                return false;
            }

            battlefield.EnsureInitialized();
            if (!battlefield.slotLimit.HasValue)
            {
                return true;
            }

            return battlefield.enemyTroopIds.Count < battlefield.slotLimit.Value;
        }

        bool TryDeploySelectedTroop(out string failureMessage, out string moveLog)
        {
            failureMessage = string.Empty;
            moveLog = string.Empty;
            if (selectedBattlefieldIndex < 0 || selectedBattlefieldIndex >= battleState.battlefields.Count)
            {
                failureMessage = $"selectedBattlefieldIndex({selectedBattlefieldIndex}) is out of range.";
                return false;
            }

            return TryMovePlayerTroopToLocation(
                selectedTroopId,
                TroopLocationType.Battlefield,
                selectedBattlefieldIndex,
                out moveLog,
                out failureMessage);
        }

        bool TryRollAllDeployedTroops(out int rolledCount, out string failureMessage)
        {
            rolledCount = 0;
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (battleState.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == BattlePhase.PlayerDeploy)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Roll phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }

                AppendLog("Roll info: advanced from PlayerDeploy to Roll.");
            }

            if (phaseRunner.currentPhase != BattlePhase.Roll)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Roll}.";
                return false;
            }

            var deployedTroopIds = new HashSet<string>(StringComparer.Ordinal);
            for (int battlefieldIndex = 0; battlefieldIndex < battleState.battlefields.Count; battlefieldIndex++)
            {
                BattlefieldState battlefield = battleState.battlefields[battlefieldIndex];
                if (battlefield == null)
                {
                    WarnAndLog($"Roll warning: battlefields[{battlefieldIndex}] is null.");
                    continue;
                }

                battlefield.EnsureInitialized();
                CollectTroopIds(deployedTroopIds, battlefield.playerTroopIds, $"playerTroopIds[{battlefieldIndex}]");
                CollectTroopIds(deployedTroopIds, battlefield.enemyTroopIds, $"enemyTroopIds[{battlefieldIndex}]");
            }

            if (deployedTroopIds.Count == 0)
            {
                failureMessage = "no deployed troops to roll.";
                return false;
            }

            foreach (string troopId in deployedTroopIds)
            {
                if (!battleState.troopsById.TryGetValue(troopId, out TroopInstance troop) || troop == null)
                {
                    WarnAndLog($"Roll warning: troopId({troopId}) does not exist.");
                    continue;
                }

                BattleSimulator.RollTroop(troop);
                rolledCount += 1;
            }

            if (rolledCount <= 0)
            {
                failureMessage = "all deployed troops were invalid.";
                return false;
            }

            ApplyTimedEffectsForTiming(BattleEffectTiming.Roll);

            if (!phaseRunner.AdvanceToNextPhase())
            {
                WarnAndLog($"Roll warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            return true;
        }

        bool TryResolveAllBattlefields(out int resolvedCount, out string failureMessage)
        {
            resolvedCount = 0;
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (battleState.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            if (phaseRunner.currentPhase == BattlePhase.Tactics)
            {
                if (!phaseRunner.AdvanceToNextPhase())
                {
                    failureMessage = $"failed to enter Resolve phase ({phaseRunner.LastFailureReason}).";
                    return false;
                }

                AppendLog("Resolve info: Tactics phase skipped (no tactics UI in T6).");
            }

            if (phaseRunner.currentPhase != BattlePhase.Resolve)
            {
                failureMessage = $"current phase is {phaseRunner.currentPhase}, required phase is {BattlePhase.Resolve}.";
                return false;
            }

            for (int battlefieldIndex = 0; battlefieldIndex < battleState.battlefields.Count; battlefieldIndex++)
            {
                bool resolved = BattleSimulator.ResolveBattlefield(
                    battleState,
                    battlefieldIndex,
                    out BattleOutcome outcome,
                    out int playerTotalAttack,
                    out int enemyTotalAttack);

                if (!resolved)
                {
                    if (resolvedCount == 0)
                    {
                        failureMessage = $"resolve failed at battlefieldIndex({battlefieldIndex}).";
                        return false;
                    }

                    WarnAndLog($"Resolve warning: stopped at battlefieldIndex({battlefieldIndex}).");
                    break;
                }

                resolvedCount += 1;
                AppendLog(
                    $"Resolve[{battlefieldIndex}] outcome={outcome} totalAttack(P:{playerTotalAttack},E:{enemyTotalAttack}) morale(P:{battleState.playerMorale},E:{battleState.enemyMorale})");

                if (battleState.isBattleEnded)
                {
                    AppendLog($"Resolve stopped early: battle ended at battlefieldIndex({battlefieldIndex}).");
                    break;
                }
            }

            if (resolvedCount <= 0)
            {
                failureMessage = "no battlefields were resolved.";
                return false;
            }

            if (!battleState.isBattleEnded)
            {
                ApplyTimedEffectsForTiming(BattleEffectTiming.TurnEnd);
            }

            if (!battleState.isBattleEnded && !phaseRunner.AdvanceToNextPhase())
            {
                WarnAndLog($"Resolve warning: failed to move to next phase ({phaseRunner.LastFailureReason}).");
            }

            return true;
        }

        bool TryRetreatBattle(out string failureMessage)
        {
            failureMessage = string.Empty;

            if (!phaseRunner.isStarted)
            {
                failureMessage = "battle is not started.";
                return false;
            }

            if (battleState.isBattleEnded)
            {
                failureMessage = "battle already ended.";
                return false;
            }

            bool retreated = phaseRunner.TryRetreat();
            if (!retreated)
            {
                failureMessage = phaseRunner.LastFailureReason.ToString();
                return false;
            }

            selectedTroopId = string.Empty;
            selectedBattlefieldIndex = -1;
            return true;
        }

        void CollectTroopIds(HashSet<string> buffer, List<string> troopIds, string sourceLabel)
        {
            if (troopIds == null)
            {
                WarnAndLog($"Roll warning: {sourceLabel} is null.");
                return;
            }

            for (int i = 0; i < troopIds.Count; i++)
            {
                string troopId = troopIds[i];
                if (string.IsNullOrWhiteSpace(troopId))
                {
                    WarnAndLog($"Roll warning: empty troopId at {sourceLabel}[{i}].");
                    continue;
                }

                buffer.Add(troopId);
            }
        }

        void RejectAction(string actionName, string reason)
        {
            WarnAndLog($"{actionName} rejected: {reason}");
            RefreshStubTexts();
            RefreshStubButtons();
        }

        void WarnAndLog(string message)
        {
            UnityEngine.Debug.LogWarning($"[BattleDebugPanel] {message}");
            AppendLog(message);
        }

        TroopTimedEffectRunner CreateTroopTimedEffectRunner(GameDatabase database)
        {
            if (database == null)
            {
                WarnAndLog("Effect setup warning: GameDatabase is null.");
                return null;
            }

            return new TroopTimedEffectRunner(database, effectResolver);
        }

        void ApplyTimedEffectsForTiming(BattleEffectTiming timing)
        {
            if (troopTimedEffectRunner == null)
            {
                WarnAndLog($"Effect warning: timed effect runner is missing for timing({timing}).");
                return;
            }

            TroopTimedEffectRunResult result = troopTimedEffectRunner.ApplyForTiming(battleState, timing);

            if (result.failedCount > 0)
            {
                WarnAndLog($"Effect warning: timing({timing}) failed={result.failedCount}, applied={result.appliedCount}.");
                return;
            }

            if (result.appliedCount > 0)
            {
                AppendLog($"Effect applied: timing={timing}, applied={result.appliedCount}, skipped={result.skippedCount}.");
            }
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

        void AppendLog(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            logEntries.Insert(0, message);
            TrimLogEntries();
            ApplyLogEntriesToText();
            SnapLogScrollToTop();

            UnityEngine.Debug.Log($"[BattleDebugPanel] {message}");
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

            if (logEntries.Count <= 0)
            {
                logText.text = "-";
                return;
            }

            logText.text = string.Join("\n", logEntries);
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

        void EnsureLogScrollSetup()
        {
            if (logText == null)
            {
                return;
            }

            if (logScrollRect == null)
            {
                logScrollRect = logText.GetComponentInParent<ScrollRect>(true);
            }

            if (logScrollRect == null)
            {
                logScrollRect = CreateRuntimeLogScrollRect();
            }

            if (logScrollRect == null)
            {
                return;
            }

            ConfigureLogTextForScroll();
            SnapshotExistingLogText();
            ApplyLogEntriesToText();
            SnapLogScrollToTop();
        }

        void SnapshotExistingLogText()
        {
            if (logText == null)
            {
                return;
            }

            if (logEntries.Count > 0)
            {
                return;
            }

            string rawText = logText.text;
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return;
            }

            string[] lines = rawText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrEmpty(line) || line == "-")
                {
                    continue;
                }

                logEntries.Add(line);
            }

            if (logEntries.Count > 1)
            {
                logEntries.Reverse();
            }
        }

        ScrollRect CreateRuntimeLogScrollRect()
        {
            RectTransform logTextRect = logText.rectTransform;
            RectTransform parentRect = logTextRect.parent as RectTransform;
            if (parentRect == null)
            {
                WarnAndLog("Log setup warning: LogText parent is missing.");
                return null;
            }

            int siblingIndex = logTextRect.GetSiblingIndex();
            RectTransform viewportRect = CreateLogScrollViewport(parentRect, siblingIndex, logTextRect);
            if (viewportRect == null)
            {
                WarnAndLog("Log setup warning: failed to create LogScrollRect.");
                return null;
            }

            RectTransform contentRect = CreateLogScrollContent(viewportRect);
            if (contentRect == null)
            {
                WarnAndLog("Log setup warning: failed to create LogContent.");
                return null;
            }

            LayoutElement sourceLayout = logText.GetComponent<LayoutElement>();
            if (sourceLayout != null)
            {
                LayoutElement viewportLayout = viewportRect.gameObject.GetComponent<LayoutElement>();
                if (viewportLayout == null)
                {
                    viewportLayout = viewportRect.gameObject.AddComponent<LayoutElement>();
                }

                CopyLayoutElement(sourceLayout, viewportLayout);
                Destroy(sourceLayout);
            }
            else
            {
                LayoutElement viewportLayout = viewportRect.gameObject.AddComponent<LayoutElement>();
                viewportLayout.preferredHeight = 170f;
                viewportLayout.flexibleHeight = 1f;
            }

            logTextRect.SetParent(contentRect, false);
            logTextRect.anchorMin = new Vector2(0f, 1f);
            logTextRect.anchorMax = new Vector2(1f, 1f);
            logTextRect.pivot = new Vector2(0.5f, 1f);
            logTextRect.anchoredPosition = Vector2.zero;
            logTextRect.sizeDelta = Vector2.zero;

            ScrollRect scrollRect = viewportRect.gameObject.GetComponent<ScrollRect>();
            if (scrollRect == null)
            {
                WarnAndLog("Log setup warning: ScrollRect component is missing.");
                return null;
            }

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.inertia = true;
            scrollRect.scrollSensitivity = 20f;

            return scrollRect;
        }

        RectTransform CreateLogScrollViewport(
            RectTransform parentRect,
            int siblingIndex,
            RectTransform sourceRect)
        {
            var viewportObject = new GameObject(
                "LogScrollRect",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask),
                typeof(ScrollRect));
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.SetParent(parentRect, false);
            viewportRect.SetSiblingIndex(siblingIndex);
            viewportRect.anchorMin = sourceRect.anchorMin;
            viewportRect.anchorMax = sourceRect.anchorMax;
            viewportRect.pivot = sourceRect.pivot;
            viewportRect.anchoredPosition = sourceRect.anchoredPosition;
            viewportRect.sizeDelta = sourceRect.sizeDelta;

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color(0f, 0f, 0f, 0.02f);
            viewportImage.raycastTarget = true;

            Mask viewportMask = viewportObject.GetComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            return viewportRect;
        }

        RectTransform CreateLogScrollContent(RectTransform viewportRect)
        {
            var contentObject = new GameObject(
                "LogContent",
                typeof(RectTransform),
                typeof(ContentSizeFitter));
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            contentRect.SetParent(viewportRect, false);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = Vector2.zero;

            ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return contentRect;
        }

        void ConfigureLogTextForScroll()
        {
            if (logText == null)
            {
                return;
            }

            logText.raycastTarget = false;
            logText.textWrappingMode = TextWrappingModes.Normal;
            logText.overflowMode = TextOverflowModes.Overflow;
            logText.alignment = TextAlignmentOptions.TopLeft;

            ContentSizeFitter fitter = logText.GetComponent<ContentSizeFitter>();
            if (fitter == null)
            {
                fitter = logText.gameObject.AddComponent<ContentSizeFitter>();
            }

            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        static void CopyLayoutElement(LayoutElement source, LayoutElement destination)
        {
            destination.ignoreLayout = source.ignoreLayout;
            destination.minWidth = source.minWidth;
            destination.minHeight = source.minHeight;
            destination.preferredWidth = source.preferredWidth;
            destination.preferredHeight = source.preferredHeight;
            destination.flexibleWidth = source.flexibleWidth;
            destination.flexibleHeight = source.flexibleHeight;
            destination.layoutPriority = source.layoutPriority;
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

        TComponent ResolveBinding<TComponent>(TComponent currentValue, string childName)
            where TComponent : Component
        {
            if (currentValue != null)
            {
                return currentValue;
            }

            TComponent found = FindChildComponentByName<TComponent>(childName);
            if (found == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[BattleDebugPanel] Auto bind failed: {typeof(TComponent).Name} '{childName}' was not found.");
            }

            return found;
        }

        TComponent FindChildComponentByName<TComponent>(string childName)
            where TComponent : Component
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (!string.Equals(child.name, childName, StringComparison.Ordinal))
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

        void WireOptionalButton(string buttonName, UnityAction callback)
        {
            Button button = FindChildComponentByName<Button>(buttonName);
            WireButton(button, callback);
        }

        static void WireButton(Button button, UnityAction callback)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveListener(callback);
            button.onClick.AddListener(callback);
        }
    }
}
