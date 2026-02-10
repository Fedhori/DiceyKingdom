using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed class GameRuntimePlayUi : MonoBehaviour
{
    sealed class SituationCardView
    {
        public int situationInstanceId;
        public GameObject root;
        public Button button;
        public Text titleText;
        public Text statText;
        public Text successText;
        public Text failText;
    }

    GameManager gameManager;
    GameTurnRuntime runtime;
    Font uiFont;

    readonly Dictionary<int, SituationCardView> situationCards = new();
    readonly List<Button> dieButtons = new();
    readonly List<Text> dieTexts = new();

    readonly Dictionary<string, int> advisorCooldownById = new(StringComparer.OrdinalIgnoreCase);
    readonly Dictionary<string, int> advisorUseCountBySituationKey = new(StringComparer.OrdinalIgnoreCase);
    readonly List<string> advisorOrder = new();
    readonly List<string> decreeInventory = new();

    bool loadoutInitialized;
    bool uiBuilt;
    int observedTurnNumber;
    int selectedDieIndex = -1;
    int? selectedSituationInstanceId;
    string toastMessage = string.Empty;
    float toastExpireTime;

    string lastAdvisorUiKey = string.Empty;
    string lastDecreeUiKey = string.Empty;

    Canvas hostCanvas;
    RectTransform rootRect;

    RectTransform boardContent;
    RectTransform advisorContent;
    RectTransform decreeContent;
    RectTransform diceContent;

    Text turnPhaseText;
    Text resourceText;
    Text selectionText;
    Text phaseHelpText;
    Text toastText;

    Button nextPhaseButton;
    Text nextPhaseButtonLabel;
    Button restartButton;
    Button clearSelectionButton;
    Button unassignButton;

    static readonly Color backgroundColor = new(0.06f, 0.07f, 0.1f, 0.96f);
    static readonly Color panelColor = new(0.11f, 0.13f, 0.17f, 0.96f);
    static readonly Color panelSoftColor = new(0.13f, 0.16f, 0.21f, 0.9f);
    static readonly Color cardColor = new(0.17f, 0.2f, 0.25f, 1f);
    static readonly Color cardSelectedColor = new(0.25f, 0.36f, 0.48f, 1f);
    static readonly Color buttonColor = new(0.23f, 0.27f, 0.34f, 1f);
    static readonly Color buttonDangerColor = new(0.45f, 0.22f, 0.24f, 1f);
    static readonly Color textPrimary = new(0.95f, 0.96f, 0.99f, 1f);
    static readonly Color textMuted = new(0.74f, 0.77f, 0.84f, 1f);
    static readonly Color textSuccess = new(0.58f, 0.9f, 0.66f, 1f);
    static readonly Color textFail = new(0.97f, 0.62f, 0.62f, 1f);

    void Awake()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        BuildUiIfNeeded();
    }

    void OnDestroy()
    {
        rootRect = null;
        hostCanvas = null;
        uiBuilt = false;
    }

    bool TryResolveHostCanvas()
    {
        if (hostCanvas != null)
            return true;

        var canvasObject = GameObject.Find("MainUiCanvas");
        if (canvasObject == null)
            return false;

        hostCanvas = canvasObject.GetComponent<Canvas>();
        return hostCanvas != null;
    }

    void ResetUiReferences()
    {
        uiBuilt = false;
        situationCards.Clear();
        dieButtons.Clear();
        dieTexts.Clear();
        boardContent = null;
        advisorContent = null;
        decreeContent = null;
        diceContent = null;
        turnPhaseText = null;
        resourceText = null;
        selectionText = null;
        phaseHelpText = null;
        toastText = null;
        nextPhaseButton = null;
        nextPhaseButtonLabel = null;
        restartButton = null;
        clearSelectionButton = null;
        unassignButton = null;
    }

    bool TryBindStaticUi()
    {
        if (!TryResolveHostCanvas())
            return false;

        var root = hostCanvas.transform.Find("GameRuntimeRoot");
        if (root == null)
            return false;

        rootRect = root as RectTransform;
        turnPhaseText = FindText(root, "TopPanel/TurnPhaseText");
        resourceText = FindText(root, "TopPanel/ResourceText");
        selectionText = FindText(root, "ActionPanel/SelectionText");
        phaseHelpText = FindText(root, "ActionPanel/PhaseHelpText");
        toastText = FindText(root, "ActionPanel/ToastText");

        boardContent = FindRect(root, "BoardPanel/BoardList/Content");
        advisorContent = FindRect(root, "ActionPanel/AdvisorList/Content");
        decreeContent = FindRect(root, "ActionPanel/DecreeList/Content");
        diceContent = FindRect(root, "BottomPanel/DiceList/Content");

        nextPhaseButton = FindButton(root, "ActionPanel/ControlsPanel/NextPhaseButton");
        restartButton = FindButton(root, "ActionPanel/ControlsPanel/RestartButton");
        unassignButton = FindButton(root, "ActionPanel/ControlsPanel/UnassignButton");
        clearSelectionButton = FindButton(root, "ActionPanel/ControlsPanel/ClearSelectionButton");
        nextPhaseButtonLabel = nextPhaseButton != null ? nextPhaseButton.GetComponentInChildren<Text>() : null;

        return rootRect != null &&
               turnPhaseText != null &&
               resourceText != null &&
               selectionText != null &&
               phaseHelpText != null &&
               toastText != null &&
               boardContent != null &&
               advisorContent != null &&
               decreeContent != null &&
               diceContent != null &&
               nextPhaseButton != null &&
               restartButton != null &&
               unassignButton != null &&
               clearSelectionButton != null &&
               nextPhaseButtonLabel != null;
    }

    static RectTransform FindRect(Transform root, string relativePath)
    {
        var target = root.Find(relativePath);
        return target as RectTransform;
    }

    static Text FindText(Transform root, string relativePath)
    {
        var target = root.Find(relativePath);
        return target != null ? target.GetComponent<Text>() : null;
    }

    static Button FindButton(Transform root, string relativePath)
    {
        var target = root.Find(relativePath);
        return target != null ? target.GetComponent<Button>() : null;
    }

    void Update()
    {
        BuildUiIfNeeded();
        if (!uiBuilt || rootRect == null)
            return;

        EnsureRuntimeReferences();

        if (runtime == null || gameManager == null || gameManager.staticDataCatalog == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        SanitizeSelections();
        SyncTurnTick();
        HandleHotkeys();
        RefreshUi();
    }

    void BuildUiIfNeeded()
    {
        if (uiBuilt &&
            rootRect != null &&
            rootRect.transform != null &&
            turnPhaseText != null &&
            boardContent != null &&
            diceContent != null)
        {
            return;
        }

        ResetUiReferences();
        if (!TryBindStaticUi())
            return;

        SetVisible(false);
        uiBuilt = true;
    }

    void BuildTopPanel(RectTransform parent)
    {
        var title = CreateText("TurnPhaseText", parent, "Turn 1 | Assignment", 28, TextAnchor.MiddleLeft, FontStyle.Bold, textPrimary);
        SetStretch(title.rectTransform, new Vector2(18f, -56f), new Vector2(-18f, -10f));
        turnPhaseText = title;

        var resource = CreateText("ResourceText", parent, "DEF 5  STB 5  Gold 0  Situations 0", 20, TextAnchor.MiddleLeft, FontStyle.Normal, textMuted);
        SetStretch(resource.rectTransform, new Vector2(18f, -98f), new Vector2(-18f, -54f));
        resourceText = resource;
    }

    void BuildBoardPanel(RectTransform parent)
    {
        var title = CreateText("BoardTitle", parent, "Situations", 24, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(title.rectTransform, new Vector2(14f, -14f), new Vector2(280f, 34f), new Vector2(0f, 1f));

        boardContent = CreateVerticalListPanel(parent, "BoardList", new Vector2(12f, 12f), new Vector2(-12f, -56f), 10f);
    }

    void BuildActionPanel(RectTransform parent)
    {
        var title = CreateText("ActionTitle", parent, "Adjustment & Controls", 24, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(title.rectTransform, new Vector2(14f, -14f), new Vector2(420f, 34f), new Vector2(0f, 1f));

        selectionText = CreateText("SelectionText", parent, "Selected: Die - / Situation -", 16, TextAnchor.UpperLeft, FontStyle.Normal, textMuted);
        SetRect(selectionText.rectTransform, new Vector2(14f, -50f), new Vector2(520f, 30f), new Vector2(0f, 1f));

        phaseHelpText = CreateText("PhaseHelpText", parent, "", 15, TextAnchor.UpperLeft, FontStyle.Italic, textMuted);
        phaseHelpText.horizontalOverflow = HorizontalWrapMode.Wrap;
        phaseHelpText.verticalOverflow = VerticalWrapMode.Truncate;
        SetRect(phaseHelpText.rectTransform, new Vector2(14f, -82f), new Vector2(520f, 42f), new Vector2(0f, 1f));

        var controlsPanel = CreatePanel("ControlsPanel", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(12f, -202f), new Vector2(-12f, -132f), panelSoftColor);
        nextPhaseButton = CreateButton(controlsPanel, "NextPhaseButton", "Next", OnNextPhaseClicked, buttonColor);
        SetRect(nextPhaseButton.GetComponent<RectTransform>(), new Vector2(10f, -10f), new Vector2(214f, 48f), new Vector2(0f, 1f));
        nextPhaseButtonLabel = nextPhaseButton.GetComponentInChildren<Text>();

        restartButton = CreateButton(controlsPanel, "RestartButton", "Restart [R]", OnRestartClicked, buttonDangerColor);
        SetRect(restartButton.GetComponent<RectTransform>(), new Vector2(232f, -10f), new Vector2(214f, 48f), new Vector2(0f, 1f));

        unassignButton = CreateButton(controlsPanel, "UnassignButton", "Unassign Die", OnUnassignClicked, buttonColor);
        SetRect(unassignButton.GetComponent<RectTransform>(), new Vector2(10f, -64f), new Vector2(214f, 44f), new Vector2(0f, 1f));

        clearSelectionButton = CreateButton(controlsPanel, "ClearSelectionButton", "Clear Selection", OnClearSelectionClicked, buttonColor);
        SetRect(clearSelectionButton.GetComponent<RectTransform>(), new Vector2(232f, -64f), new Vector2(214f, 44f), new Vector2(0f, 1f));

        var advisorTitle = CreateText("AdvisorTitle", parent, "Advisors", 20, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(advisorTitle.rectTransform, new Vector2(14f, -212f), new Vector2(220f, 28f), new Vector2(0f, 1f));
        advisorContent = CreateVerticalListPanel(parent, "AdvisorList", new Vector2(12f, 338f), new Vector2(-12f, -246f), 6f);

        var decreeTitle = CreateText("DecreeTitle", parent, "Decrees", 20, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(decreeTitle.rectTransform, new Vector2(14f, 298f), new Vector2(220f, 28f), new Vector2(0f, 0f));
        decreeContent = CreateVerticalListPanel(parent, "DecreeList", new Vector2(12f, 78f), new Vector2(-12f, 294f), 6f);

        toastText = CreateText("ToastText", parent, "", 15, TextAnchor.LowerCenter, FontStyle.Bold, textSuccess);
        toastText.horizontalOverflow = HorizontalWrapMode.Wrap;
        toastText.verticalOverflow = VerticalWrapMode.Truncate;
        SetRect(toastText.rectTransform, new Vector2(0f, 12f), new Vector2(520f, 26f), new Vector2(0.5f, 0f));
    }

    void BuildDicePanel(RectTransform parent)
    {
        var title = CreateText("DiceTitle", parent, "Dice Tray", 24, TextAnchor.UpperLeft, FontStyle.Bold, textPrimary);
        SetRect(title.rectTransform, new Vector2(14f, -14f), new Vector2(260f, 34f), new Vector2(0f, 1f));

        var hint = CreateText(
            "DiceHint",
            parent,
            "Select die: 1-0  |  Assignment: die 선택 후 Situation 클릭  |  Advance: SPACE",
            15,
            TextAnchor.UpperLeft,
            FontStyle.Normal,
            textMuted);
        SetRect(hint.rectTransform, new Vector2(280f, -18f), new Vector2(1200f, 28f), new Vector2(0f, 1f));

        diceContent = CreateHorizontalListPanel(parent, "DiceList", new Vector2(12f, 12f), new Vector2(-12f, -52f), 8f);
        var layout = diceContent.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 8f;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlHeight = true;
        layout.childControlWidth = false;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;
    }

    RectTransform CreateVerticalListPanel(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, float spacing)
    {
        var panel = CreatePanel(name, parent, new Vector2(0f, 0f), new Vector2(1f, 1f), offsetMin, offsetMax, panelSoftColor);
        panel.gameObject.AddComponent<RectMask2D>();

        var scrollRect = panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = panel;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(panel, false);
        var contentRect = content.GetComponent<RectTransform>();
        SetStretch(contentRect, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var layout = content.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;

        return contentRect;
    }

    RectTransform CreateHorizontalListPanel(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax, float spacing)
    {
        var panel = CreatePanel(name, parent, new Vector2(0f, 0f), new Vector2(1f, 1f), offsetMin, offsetMax, panelSoftColor);
        panel.gameObject.AddComponent<RectMask2D>();

        var scrollRect = panel.gameObject.AddComponent<ScrollRect>();
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.inertia = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.viewport = panel;

        var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(panel, false);
        var contentRect = content.GetComponent<RectTransform>();
        SetStretch(contentRect, new Vector2(8f, 8f), new Vector2(-8f, -8f));

        var layout = content.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = false;

        var fitter = content.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        scrollRect.content = contentRect;

        return contentRect;
    }

    void EnsureRuntimeReferences()
    {
        if (gameManager == null)
            gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        if (runtime != gameManager.turnRuntime)
        {
            runtime = gameManager.turnRuntime;
            loadoutInitialized = false;
            observedTurnNumber = runtime != null ? runtime.state.turnNumber : 0;
            selectedDieIndex = -1;
            selectedSituationInstanceId = null;
        }

        if (!loadoutInitialized && runtime != null && gameManager.startingLoadout != null)
            InitializeFromLoadout(gameManager.startingLoadout);
    }

    void InitializeFromLoadout(GameStartingLoadout loadout)
    {
        advisorCooldownById.Clear();
        advisorUseCountBySituationKey.Clear();
        advisorOrder.Clear();
        decreeInventory.Clear();

        for (int i = 0; i < loadout.advisorIds.Count; i++)
        {
            var advisorId = loadout.advisorIds[i] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(advisorId))
                continue;
            advisorOrder.Add(advisorId);
            if (!advisorCooldownById.ContainsKey(advisorId))
                advisorCooldownById.Add(advisorId, 0);
        }

        for (int i = 0; i < loadout.decreeIds.Count; i++)
        {
            var decreeId = loadout.decreeIds[i] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(decreeId))
                continue;
            decreeInventory.Add(decreeId);
        }

        loadoutInitialized = true;
        observedTurnNumber = runtime.state.turnNumber;
        InvalidateActionUiKeys();
    }

    void SetVisible(bool visible)
    {
        if (rootRect != null && rootRect.gameObject.activeSelf != visible)
            rootRect.gameObject.SetActive(visible);
    }

    void SyncTurnTick()
    {
        if (runtime == null)
            return;

        var turnNumber = runtime.state.turnNumber;
        if (turnNumber == observedTurnNumber)
            return;

        if (turnNumber < observedTurnNumber)
        {
            if (gameManager != null && gameManager.startingLoadout != null)
                InitializeFromLoadout(gameManager.startingLoadout);
            else
                observedTurnNumber = turnNumber;

            selectedDieIndex = -1;
            selectedSituationInstanceId = null;
            PushToast("Run restarted.");
            return;
        }

        var delta = turnNumber - observedTurnNumber;
        observedTurnNumber = turnNumber;
        for (int i = 0; i < delta; i++)
            TickAdvisorCooldowns();
    }

    void TickAdvisorCooldowns()
    {
        var keys = advisorCooldownById.Keys.ToList();
        for (int i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            advisorCooldownById[key] = Mathf.Max(0, advisorCooldownById[key] - 1);
        }

        InvalidateActionUiKeys();
    }

    void HandleHotkeys()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null || runtime == null)
            return;

        if (keyboard.spaceKey.wasPressedThisFrame)
            OnNextPhaseClicked();
        if (keyboard.rKey.wasPressedThisFrame)
            OnRestartClicked();

        for (int i = 0; i < 10; i++)
        {
            var pressed = i switch
            {
                0 => keyboard.digit1Key.wasPressedThisFrame,
                1 => keyboard.digit2Key.wasPressedThisFrame,
                2 => keyboard.digit3Key.wasPressedThisFrame,
                3 => keyboard.digit4Key.wasPressedThisFrame,
                4 => keyboard.digit5Key.wasPressedThisFrame,
                5 => keyboard.digit6Key.wasPressedThisFrame,
                6 => keyboard.digit7Key.wasPressedThisFrame,
                7 => keyboard.digit8Key.wasPressedThisFrame,
                8 => keyboard.digit9Key.wasPressedThisFrame,
                9 => keyboard.digit0Key.wasPressedThisFrame,
                _ => false
            };

            if (!pressed)
                continue;
            if (i >= runtime.state.dice.Count)
                continue;

            selectedDieIndex = i;
            PushToast($"Selected D{i + 1}");
            break;
        }

        if (keyboard.qKey.wasPressedThisFrame)
            TryUseAdvisorBySlot(0);
        if (keyboard.wKey.wasPressedThisFrame)
            TryUseAdvisorBySlot(1);
        if (keyboard.eKey.wasPressedThisFrame)
            TryUseAdvisorBySlot(2);
    }

    void TryUseAdvisorBySlot(int slot)
    {
        if (slot < 0 || slot >= advisorOrder.Count)
            return;
        OnAdvisorUseClicked(advisorOrder[slot]);
    }

    void SanitizeSelections()
    {
        if (runtime == null)
            return;

        if (selectedDieIndex >= runtime.state.dice.Count)
            selectedDieIndex = runtime.state.dice.Count - 1;

        if (selectedSituationInstanceId.HasValue)
        {
            var exists = runtime.state.activeSituations.Any(item => item.situationInstanceId == selectedSituationInstanceId.Value);
            if (!exists)
                selectedSituationInstanceId = null;
        }
    }

    void RefreshUi()
    {
        var state = runtime.state;
        UpdateTopTexts(state);
        UpdateSituationCards(state);
        UpdateDiceButtons(state);
        UpdateSelectionAndHelpTexts(state);
        UpdateActionLists(state);
        UpdateControlButtons(state);
        UpdateToastView();
    }

    void UpdateTopTexts(GameRunRuntimeState state)
    {
        turnPhaseText.text = $"Turn {state.turnNumber} | {GetPhaseLabel(state.phase)}";
        resourceText.text = $"Defense {state.defense}   Stability {state.stability}   Gold {state.gold}   Situations {state.activeSituations.Count}";
        if (state.isGameOver)
            resourceText.text += $"   |   GAME OVER ({state.gameOverReason})";
    }

    void UpdateSituationCards(GameRunRuntimeState state)
    {
        var activeIds = new HashSet<int>(state.activeSituations.Select(item => item.situationInstanceId));
        var stale = situationCards.Keys.Where(id => !activeIds.Contains(id)).ToList();
        for (int i = 0; i < stale.Count; i++)
        {
            var staleId = stale[i];
            if (situationCards.TryGetValue(staleId, out var view) && view.root != null)
                Destroy(view.root);
            situationCards.Remove(staleId);
        }

        var ordered = state.activeSituations.OrderBy(item => item.boardOrder).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var situation = ordered[i];
            if (!situationCards.TryGetValue(situation.situationInstanceId, out var view))
            {
                view = CreateSituationCard(boardContent, situation.situationInstanceId);
                situationCards.Add(situation.situationInstanceId, view);
            }

            view.root.transform.SetSiblingIndex(i);
            UpdateSituationCard(view, situation, state);
        }
    }

    SituationCardView CreateSituationCard(Transform parent, int situationInstanceId)
    {
        var card = new GameObject($"Situation_{situationInstanceId}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(VerticalLayoutGroup), typeof(LayoutElement));
        card.transform.SetParent(parent, false);

        var image = card.GetComponent<Image>();
        image.color = cardColor;

        var button = card.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnSituationClicked(situationInstanceId));

        var layout = card.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 10, 10);
        layout.spacing = 6f;
        layout.childControlHeight = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childForceExpandWidth = true;

        var element = card.GetComponent<LayoutElement>();
        element.preferredHeight = 144f;

        var title = CreateText("Title", card.transform, "-", 18, TextAnchor.MiddleLeft, FontStyle.Bold, textPrimary);
        title.horizontalOverflow = HorizontalWrapMode.Wrap;
        title.verticalOverflow = VerticalWrapMode.Truncate;
        title.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;

        var stat = CreateText("Stats", card.transform, "-", 15, TextAnchor.MiddleLeft, FontStyle.Normal, textMuted);
        stat.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

        var success = CreateText("Success", card.transform, "-", 14, TextAnchor.UpperLeft, FontStyle.Normal, textSuccess);
        success.horizontalOverflow = HorizontalWrapMode.Wrap;
        success.verticalOverflow = VerticalWrapMode.Truncate;
        success.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        var fail = CreateText("Fail", card.transform, "-", 14, TextAnchor.UpperLeft, FontStyle.Normal, textFail);
        fail.horizontalOverflow = HorizontalWrapMode.Wrap;
        fail.verticalOverflow = VerticalWrapMode.Truncate;
        fail.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        return new SituationCardView
        {
            situationInstanceId = situationInstanceId,
            root = card,
            button = button,
            titleText = title,
            statText = stat,
            successText = success,
            failText = fail
        };
    }

    void UpdateSituationCard(SituationCardView view, GameSituationRuntimeState situation, GameRunRuntimeState state)
    {
        var assigned = GetAssignedDiceForSituation(state, situation.situationInstanceId);
        var assignedCount = assigned.Count;
        var assignedPower = 0;
        for (int i = 0; i < assigned.Count; i++)
            assignedPower += Mathf.Max(0, assigned[i].currentFace);

        view.titleText.text = $"#{situation.boardOrder}  {situation.situationId}";
        view.statText.text = $"Demand {situation.demand}   Deadline {situation.deadline}   Assigned {assignedCount} ({assignedPower})";

        if (gameManager.staticDataCatalog.TryGetSituation(situation.situationId, out var definition))
        {
            view.successText.text = "Success: " + FormatEffects(definition.onSuccess);
            view.failText.text = "Fail: " + FormatEffects(definition.onFail);
        }
        else
        {
            view.successText.text = "Success: -";
            view.failText.text = "Fail: -";
        }

        var isSelected = selectedSituationInstanceId.HasValue && selectedSituationInstanceId.Value == situation.situationInstanceId;
        view.root.GetComponent<Image>().color = isSelected ? cardSelectedColor : cardColor;
    }

    void UpdateDiceButtons(GameRunRuntimeState state)
    {
        while (dieButtons.Count < state.dice.Count)
        {
            var dieIndex = dieButtons.Count;
            var button = CreateButton(diceContent, $"Die_{dieIndex}", "-", () => OnDieClicked(dieIndex), buttonColor);
            var layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = 148f;
            layout.preferredHeight = 92f;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
            var text = button.GetComponentInChildren<Text>();
            text.fontSize = 14;
            text.alignment = TextAnchor.UpperLeft;
            text.fontStyle = FontStyle.Normal;
            SetStretch(text.rectTransform, new Vector2(10f, 8f), new Vector2(-8f, -8f));

            dieButtons.Add(button);
            dieTexts.Add(text);
        }

        for (int i = 0; i < dieButtons.Count; i++)
        {
            var visible = i < state.dice.Count;
            var button = dieButtons[i];
            if (button == null)
                continue;
            button.gameObject.SetActive(visible);
            if (!visible)
                continue;

            var die = state.dice[i];
            var assignedText = die.assignedSituationInstanceId.HasValue ? $"S#{die.assignedSituationInstanceId.Value}" : "-";
            var faceText = die.hasRolled ? die.currentFace.ToString() : "?";
            var rolledText = die.hasRolled ? die.rolledFace.ToString() : "-";
            var upg = string.IsNullOrWhiteSpace(die.upgradeId) ? "none" : die.upgradeId;

            dieTexts[i].text = $"D{i + 1}  Face {faceText}\nRoll {rolledText}  Assign {assignedText}\nUpg {upg}";
            button.GetComponent<Image>().color = selectedDieIndex == i ? cardSelectedColor : buttonColor;
        }
    }

    void UpdateSelectionAndHelpTexts(GameRunRuntimeState state)
    {
        var dieLabel = selectedDieIndex >= 0 && selectedDieIndex < state.dice.Count ? $"D{selectedDieIndex + 1}" : "-";
        var situationLabel = selectedSituationInstanceId.HasValue ? $"S#{selectedSituationInstanceId.Value}" : "-";
        selectionText.text = $"Selected: Die {dieLabel} / Situation {situationLabel}";
        phaseHelpText.text = GetPhaseHelp(state.phase);
    }

    void UpdateActionLists(GameRunRuntimeState state)
    {
        var advisorKey = BuildAdvisorUiKey();
        if (!advisorKey.Equals(lastAdvisorUiKey, StringComparison.Ordinal))
        {
            RebuildAdvisorButtons(state);
            lastAdvisorUiKey = advisorKey;
        }

        var decreeKey = BuildDecreeUiKey();
        if (!decreeKey.Equals(lastDecreeUiKey, StringComparison.Ordinal))
        {
            RebuildDecreeButtons(state);
            lastDecreeUiKey = decreeKey;
        }
    }

    void UpdateControlButtons(GameRunRuntimeState state)
    {
        var canAssign = state.phase == GameTurnPhase.Assignment &&
                        selectedDieIndex >= 0 &&
                        selectedDieIndex < state.dice.Count;
        unassignButton.interactable = canAssign;
        clearSelectionButton.interactable = true;
        restartButton.interactable = true;

        nextPhaseButton.interactable = !state.isGameOver;
        nextPhaseButtonLabel.text = GetAdvanceButtonLabel(state.phase);
    }

    void UpdateToastView()
    {
        if (string.IsNullOrWhiteSpace(toastMessage) || Time.unscaledTime > toastExpireTime)
        {
            toastText.text = string.Empty;
            return;
        }

        toastText.text = toastMessage;
    }

    void PushToast(string message, Color? color = null, float duration = 1.8f)
    {
        toastMessage = message ?? string.Empty;
        toastExpireTime = Time.unscaledTime + Mathf.Max(0.2f, duration);
        toastText.color = color ?? textSuccess;
    }

    void OnDieClicked(int dieIndex)
    {
        selectedDieIndex = dieIndex;
    }

    void OnSituationClicked(int situationInstanceId)
    {
        selectedSituationInstanceId = situationInstanceId;

        if (runtime == null)
            return;
        if (runtime.state.phase != GameTurnPhase.Assignment)
            return;
        if (selectedDieIndex < 0 || selectedDieIndex >= runtime.state.dice.Count)
            return;

        if (runtime.TryAssignDieToSituation(selectedDieIndex, situationInstanceId))
            PushToast($"D{selectedDieIndex + 1} -> S#{situationInstanceId}");
    }

    void OnUnassignClicked()
    {
        if (runtime == null)
            return;
        if (runtime.state.phase != GameTurnPhase.Assignment)
        {
            PushToast("Unassign is only available in Assignment phase.", textFail);
            return;
        }
        if (selectedDieIndex < 0 || selectedDieIndex >= runtime.state.dice.Count)
            return;

        if (runtime.TryAssignDieToSituation(selectedDieIndex, null))
            PushToast($"D{selectedDieIndex + 1} unassigned.");
    }

    void OnClearSelectionClicked()
    {
        selectedDieIndex = -1;
        selectedSituationInstanceId = null;
    }

    void OnNextPhaseClicked()
    {
        if (runtime == null)
            return;

        if (!runtime.TryAdvancePhase())
            PushToast("Cannot advance phase now.", textFail);
    }

    void OnRestartClicked()
    {
        if (gameManager == null)
            return;

        if (!gameManager.TryRestartRun())
        {
            PushToast("Restart failed.", textFail);
            return;
        }

        selectedDieIndex = -1;
        selectedSituationInstanceId = null;
    }

    void OnAdvisorUseClicked(string advisorId)
    {
        if (runtime == null || gameManager == null)
            return;
        if (!advisorCooldownById.TryGetValue(advisorId, out var cooldown))
            cooldown = 0;
        if (cooldown > 0)
        {
            PushToast($"{advisorId} is on cooldown.", textFail);
            return;
        }

        if (!TryUseAdvisor(advisorId, out var consumed))
            return;
        if (!consumed)
            return;

        if (gameManager.staticDataCatalog.TryGetAdvisor(advisorId, out var definition))
            advisorCooldownById[advisorId] = Mathf.Max(0, definition.cooldown);

        InvalidateActionUiKeys();
        PushToast($"Advisor used: {advisorId}");
    }

    void OnDecreeUseClicked(string decreeId)
    {
        if (runtime == null)
            return;

        if (!TryUseDecree(decreeId, out var consumed))
            return;
        if (!consumed)
            return;

        var index = decreeInventory.FindIndex(item => item.Equals(decreeId, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
            decreeInventory.RemoveAt(index);

        InvalidateActionUiKeys();
        PushToast($"Decree used: {decreeId}");
    }

    bool TryUseAdvisor(string advisorId, out bool consumed)
    {
        consumed = false;
        if (!gameManager.staticDataCatalog.TryGetAdvisor(advisorId, out var definition))
            return false;

        if (!ResolveTargets(definition.targetType, out var selectedSituationId, out var selectedDieId))
            return false;

        if (definition.maxUsesPerSituation.HasValue && selectedSituationId.HasValue)
        {
            var key = BuildSituationUseKey(advisorId, selectedSituationId.Value);
            if (advisorUseCountBySituationKey.TryGetValue(key, out var used) &&
                used >= definition.maxUsesPerSituation.Value)
            {
                PushToast($"{advisorId} already used on this situation.", textFail);
                return false;
            }
        }

        if (!ValidateConditions(definition.conditions, selectedDieId))
            return false;

        var applied = runtime.TryApplyDirectEffects(definition.effects, null, selectedSituationId, selectedDieId);
        if (!applied)
        {
            PushToast("No valid target/effect.", textFail);
            return false;
        }

        if (definition.maxUsesPerSituation.HasValue && selectedSituationId.HasValue)
        {
            var key = BuildSituationUseKey(advisorId, selectedSituationId.Value);
            advisorUseCountBySituationKey.TryGetValue(key, out var used);
            advisorUseCountBySituationKey[key] = used + 1;
        }

        consumed = true;
        return true;
    }

    bool TryUseDecree(string decreeId, out bool consumed)
    {
        consumed = false;
        if (!gameManager.staticDataCatalog.TryGetDecree(decreeId, out var definition))
            return false;

        if (!ResolveTargets(definition.targetType, out var selectedSituationId, out var selectedDieId))
            return false;
        if (!ValidateConditions(definition.conditions, selectedDieId))
            return false;

        var applied = runtime.TryApplyDirectEffects(definition.effects, null, selectedSituationId, selectedDieId);
        if (!applied)
        {
            PushToast("No valid target/effect.", textFail);
            return false;
        }

        consumed = true;
        return true;
    }

    bool ResolveTargets(string targetType, out int? selectedSituationId, out int? selectedDieId)
    {
        selectedSituationId = null;
        selectedDieId = null;

        if (string.IsNullOrWhiteSpace(targetType) || targetType.Equals("self", StringComparison.OrdinalIgnoreCase))
            return true;

        if (targetType.Equals("selected_situation", StringComparison.OrdinalIgnoreCase))
        {
            if (!selectedSituationInstanceId.HasValue)
            {
                PushToast("Select a situation first.", textFail);
                return false;
            }

            var exists = runtime.state.activeSituations.Any(item => item.situationInstanceId == selectedSituationInstanceId.Value);
            if (!exists)
            {
                PushToast("Selected situation is invalid.", textFail);
                return false;
            }

            selectedSituationId = selectedSituationInstanceId.Value;
            return true;
        }

        if (targetType.Equals("selected_die", StringComparison.OrdinalIgnoreCase))
        {
            if (selectedDieIndex < 0 || selectedDieIndex >= runtime.state.dice.Count)
            {
                PushToast("Select a die first.", textFail);
                return false;
            }
            if (!runtime.state.dice[selectedDieIndex].hasRolled)
            {
                PushToast("Selected die is not rolled yet.", textFail);
                return false;
            }

            selectedDieId = selectedDieIndex;
            return true;
        }

        PushToast($"Unknown target_type: {targetType}", textFail);
        return false;
    }

    bool ValidateConditions(IReadOnlyList<GameConditionDefinition> conditions, int? selectedDieId)
    {
        if (conditions == null || conditions.Count == 0)
            return true;

        for (int i = 0; i < conditions.Count; i++)
        {
            var condition = conditions[i];
            if (condition == null || string.IsNullOrWhiteSpace(condition.conditionType))
                continue;

            switch (condition.conditionType)
            {
                case "selected_die_face_eq":
                {
                    if (!selectedDieId.HasValue)
                    {
                        PushToast("Die condition requires selected die.", textFail);
                        return false;
                    }

                    var expected = Mathf.RoundToInt(condition.value ?? 0f);
                    var current = runtime.state.dice[selectedDieId.Value].currentFace;
                    if (current != expected)
                    {
                        PushToast($"Condition failed: die face must be {expected}.", textFail);
                        return false;
                    }

                    break;
                }
            }
        }

        return true;
    }

    string BuildSituationUseKey(string advisorId, int situationInstanceId)
    {
        return advisorId + "#" + situationInstanceId;
    }

    List<GameDieRuntimeState> GetAssignedDiceForSituation(GameRunRuntimeState state, int situationInstanceId)
    {
        var list = new List<GameDieRuntimeState>();
        for (int i = 0; i < state.dice.Count; i++)
        {
            var die = state.dice[i];
            if (!die.assignedSituationInstanceId.HasValue)
                continue;
            if (die.assignedSituationInstanceId.Value != situationInstanceId)
                continue;
            list.Add(die);
        }

        return list;
    }

    string BuildAdvisorUiKey()
    {
        if (advisorOrder.Count == 0)
            return "none";

        var parts = new List<string>(advisorOrder.Count);
        for (int i = 0; i < advisorOrder.Count; i++)
        {
            var id = advisorOrder[i];
            advisorCooldownById.TryGetValue(id, out var cooldown);
            parts.Add(id + ":" + cooldown);
        }

        return string.Join("|", parts);
    }

    string BuildDecreeUiKey()
    {
        if (decreeInventory.Count == 0)
            return "none";
        return string.Join("|", decreeInventory);
    }

    void InvalidateActionUiKeys()
    {
        lastAdvisorUiKey = string.Empty;
        lastDecreeUiKey = string.Empty;
    }

    void RebuildAdvisorButtons(GameRunRuntimeState state)
    {
        ClearChildren(advisorContent);

        if (advisorOrder.Count == 0)
        {
            CreateText("AdvisorNone", advisorContent, "No advisors", 14, TextAnchor.MiddleLeft, FontStyle.Italic, textMuted);
            return;
        }

        for (int i = 0; i < advisorOrder.Count; i++)
        {
            var advisorId = advisorOrder[i];
            advisorCooldownById.TryGetValue(advisorId, out var cooldown);

            var row = new GameObject($"AdvisorRow_{advisorId}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(advisorContent, false);
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 40f;

            var label = CreateText("Label", row.transform, $"{advisorId}  [CD {cooldown}]  ({GetAdvisorHotkeyText(i)})", 14, TextAnchor.MiddleLeft, FontStyle.Normal, textPrimary);
            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 280f;
            labelElement.flexibleWidth = 1f;

            var useButton = CreateButton(row.transform, "UseButton", "Use", () => OnAdvisorUseClicked(advisorId), buttonColor);
            var useElement = useButton.gameObject.AddComponent<LayoutElement>();
            useElement.preferredWidth = 96f;
            useButton.interactable = !state.isGameOver && cooldown <= 0;
        }
    }

    void RebuildDecreeButtons(GameRunRuntimeState state)
    {
        ClearChildren(decreeContent);

        if (decreeInventory.Count == 0)
        {
            CreateText("DecreeNone", decreeContent, "No decrees", 14, TextAnchor.MiddleLeft, FontStyle.Italic, textMuted);
            return;
        }

        for (int i = 0; i < decreeInventory.Count; i++)
        {
            var decreeId = decreeInventory[i];
            var row = new GameObject($"DecreeRow_{i}", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(decreeContent, false);
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8f;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = false;

            var rowElement = row.GetComponent<LayoutElement>();
            rowElement.preferredHeight = 40f;

            var label = CreateText("Label", row.transform, decreeId, 14, TextAnchor.MiddleLeft, FontStyle.Normal, textPrimary);
            var labelElement = label.gameObject.AddComponent<LayoutElement>();
            labelElement.preferredWidth = 280f;
            labelElement.flexibleWidth = 1f;

            var useButton = CreateButton(row.transform, "UseButton", "Use", () => OnDecreeUseClicked(decreeId), buttonColor);
            var useElement = useButton.gameObject.AddComponent<LayoutElement>();
            useElement.preferredWidth = 96f;
            useButton.interactable = !state.isGameOver;
        }
    }

    string GetAdvisorHotkeyText(int index)
    {
        return index switch
        {
            0 => "Q",
            1 => "W",
            2 => "E",
            3 => "R",
            _ => "-"
        };
    }

    string GetPhaseLabel(GameTurnPhase phase)
    {
        return phase switch
        {
            GameTurnPhase.TurnStart => "Turn Start",
            GameTurnPhase.Assignment => "Assignment",
            GameTurnPhase.Roll => "Roll",
            GameTurnPhase.Adjustment => "Adjustment",
            GameTurnPhase.Resolution => "Resolution",
            _ => "None"
        };
    }

    string GetAdvanceButtonLabel(GameTurnPhase phase)
    {
        return phase switch
        {
            GameTurnPhase.TurnStart => "Start Assignment [SPACE]",
            GameTurnPhase.Assignment => "Roll Dice [SPACE]",
            GameTurnPhase.Roll => "Go To Adjustment [SPACE]",
            GameTurnPhase.Adjustment => "Resolve Turn [SPACE]",
            GameTurnPhase.Resolution => "Next Turn [SPACE]",
            _ => "Advance [SPACE]"
        };
    }

    string GetPhaseHelp(GameTurnPhase phase)
    {
        return phase switch
        {
            GameTurnPhase.TurnStart => "턴 시작 상태입니다. SPACE를 눌러 분배 단계로 이동하세요.",
            GameTurnPhase.Assignment => "주사위를 선택(1~0)한 뒤 Situation 카드를 클릭해 배치하세요. 조정 단계 전까지는 배치만 가능합니다.",
            GameTurnPhase.Roll => "배치된 주사위를 굴리는 단계입니다. SPACE로 조정 단계로 이동하세요.",
            GameTurnPhase.Adjustment => "조언자/칙령을 사용해 수치를 조정하세요. 대상이 필요한 효과는 Die/Situation 선택이 필요합니다.",
            GameTurnPhase.Resolution => "정산 단계입니다. demand 감소, 성공/실패, deadline 감소가 처리됩니다. SPACE로 다음 턴으로 진행합니다.",
            _ => string.Empty
        };
    }

    string FormatEffects(IReadOnlyList<GameEffectDefinition> effects)
    {
        if (effects == null || effects.Count == 0)
            return "none";

        var parts = new List<string>(effects.Count);
        for (int i = 0; i < effects.Count; i++)
            parts.Add(FormatEffect(effects[i]));

        return string.Join(" / ", parts);
    }

    string FormatEffect(GameEffectDefinition effect)
    {
        if (effect == null)
            return "(null)";

        var icon = effect.targetResource switch
        {
            "defense" => "[DEF] ",
            "stability" => "[STB] ",
            _ => string.Empty
        };

        return effect.effectType switch
        {
            "resource_delta" => $"{icon}{FormatSigned(effect.value)}",
            "gold_delta" => $"Gold {FormatSigned(effect.value)}",
            "demand_delta" => $"Demand {FormatSigned(effect.value)} ({effect.targetMode})",
            "deadline_delta" => $"Deadline {FormatSigned(effect.value)} ({effect.targetMode})",
            "resource_guard" => $"{icon}Guard {effect.duration} turn",
            "die_face_delta" => $"Die {FormatSigned(effect.value)} ({effect.targetMode})",
            "die_face_set" => $"Die set {Mathf.RoundToInt(effect.value ?? 0f)} ({effect.targetMode})",
            "die_face_min" => $"Die min {Mathf.RoundToInt(effect.value ?? 0f)} ({effect.targetMode})",
            "die_face_mult" => $"Die x{effect.value:0.##} ({effect.targetMode})",
            "reroll_assigned_dice" => $"Reroll ({effect.targetMode})",
            _ => effect.effectType
        };
    }

    string FormatSigned(float? value)
    {
        if (!value.HasValue)
            return "0";
        var intValue = Mathf.RoundToInt(value.Value);
        return intValue >= 0 ? $"+{intValue}" : intValue.ToString();
    }

    RectTransform CreatePanel(
        string name,
        Transform parent,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 offsetMin,
        Vector2 offsetMax,
        Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        go.GetComponent<Image>().color = color;
        return rect;
    }

    Text CreateText(string name, Transform parent, string text, int fontSize, TextAnchor alignment, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);

        var label = go.GetComponent<Text>();
        label.font = uiFont;
        label.fontSize = fontSize;
        label.alignment = alignment;
        label.fontStyle = style;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow = VerticalWrapMode.Truncate;
        label.color = color;
        label.text = text;

        return label;
    }

    Button CreateButton(Transform parent, string name, string label, Action onClick, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;

        var button = go.GetComponent<Button>();
        button.targetGraphic = go.GetComponent<Image>();
        if (onClick != null)
            button.onClick.AddListener(() => onClick());

        var text = CreateText("Label", go.transform, label, 15, TextAnchor.MiddleCenter, FontStyle.Bold, textPrimary);
        SetStretch(text.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -6f));

        return button;
    }

    void SetRect(RectTransform rect, Vector2 anchoredPos, Vector2 size, Vector2 anchor)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = size;
    }

    void SetStretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }
}
