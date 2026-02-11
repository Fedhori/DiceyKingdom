using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AdventurerPanelController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] float cardHeight = 194f;
    [SerializeField] Color pendingCardColor = new(0.18f, 0.22f, 0.30f, 0.95f);
    [SerializeField] Color rolledCardColor = new(0.20f, 0.27f, 0.36f, 0.95f);
    [SerializeField] Color consumedCardColor = new(0.13f, 0.16f, 0.20f, 0.85f);
    [SerializeField] Color labelColor = new(0.93f, 0.96f, 1.00f, 1.00f);
    [SerializeField] Color subtleLabelColor = new(0.72f, 0.80f, 0.90f, 1.00f);
    [SerializeField] Color infoEmphasisColor = new(0.86f, 0.93f, 1.00f, 1.00f);
    [SerializeField] Color damageHighlightColor = new(1.00f, 0.88f, 0.28f, 1.00f);
    [SerializeField] Color diceFaceBackgroundColor = new(0.94f, 0.96f, 1.00f, 0.98f);
    [SerializeField] Color diceFaceTextColor = new(0.10f, 0.14f, 0.20f, 1.00f);
    [SerializeField] Color rollButtonColor = new(0.28f, 0.46f, 0.80f, 1.00f);
    [SerializeField] Color badgeColor = new(0.95f, 0.74f, 0.24f, 0.95f);

    readonly List<CardWidgets> cards = new();
    readonly Dictionary<string, AdventurerDef> adventurerDefById = new(StringComparer.Ordinal);
    bool loadedDefs;

    void Awake()
    {
        TryResolveOrchestrator();
        TryResolveContentRoot();
        EnsureContentLayout();
        LoadAdventurerDefsIfNeeded();
    }

    void OnEnable()
    {
        SubscribeEvents();
        RebuildCardsIfNeeded(forceRebuild: true);
        RefreshAllCards();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void Update()
    {
        if (!IsReady())
            return;

        RebuildCardsIfNeeded(forceRebuild: false);
        RefreshAllCards();
    }

    void OnRunStarted(GameRunState _)
    {
        RebuildCardsIfNeeded(forceRebuild: false);
        RefreshAllCards();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RefreshAllCards();
    }

    void OnRunEnded(GameRunState _)
    {
        RefreshAllCards();
    }

    void SubscribeEvents()
    {
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.RunEnded -= OnRunEnded;
        orchestrator.RunStarted += OnRunStarted;
        orchestrator.PhaseChanged += OnPhaseChanged;
        orchestrator.RunEnded += OnRunEnded;
    }

    void UnsubscribeEvents()
    {
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.RunEnded -= OnRunEnded;
    }

    bool IsReady()
    {
        if (orchestrator == null)
            return false;
        if (contentRoot == null)
            return false;
        if (orchestrator.RunState == null || orchestrator.RunState.adventurers == null)
            return false;

        return true;
    }

    void RebuildCardsIfNeeded(bool forceRebuild)
    {
        if (!IsReady())
            return;

        int desiredCount = orchestrator.RunState.adventurers.Count;
        if (!forceRebuild && cards.Count == desiredCount)
            return;

        ClearCards();
        for (int index = 0; index < desiredCount; index++)
            cards.Add(CreateCard(index));

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);
    }

    void ClearCards()
    {
        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            if (card?.root == null)
                continue;

            if (Application.isPlaying)
                Destroy(card.root.gameObject);
            else
                DestroyImmediate(card.root.gameObject);
        }

        cards.Clear();
    }

    CardWidgets CreateCard(int slotIndex)
    {
        var cardObject = new GameObject(
            $"AdventurerCard_{slotIndex + 1}",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement));
        cardObject.layer = LayerMask.NameToLayer("UI");

        var cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.SetParent(contentRoot, false);

        var cardImage = cardObject.GetComponent<Image>();
        cardImage.color = pendingCardColor;
        cardImage.raycastTarget = true;

        var layout = cardObject.GetComponent<LayoutElement>();
        layout.preferredHeight = cardHeight;
        layout.flexibleWidth = 1f;

        var dragHandle = cardObject.AddComponent<AdventurerDragHandle>();
        dragHandle.SetOrchestrator(orchestrator);

        var highlight = cardObject.AddComponent<AdventurerProcessingHighlight>();
        highlight.SetOrchestrator(orchestrator);

        var headerRect = CreateUiContainer("Header", cardRect);
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -8f);
        headerRect.sizeDelta = new Vector2(-16f, 28f);

        var slotText = CreateLabel("SlotText", headerRect, 19f, FontStyles.Bold, TextAlignmentOptions.Left, subtleLabelColor);
        slotText.rectTransform.anchorMin = new Vector2(0f, 0f);
        slotText.rectTransform.anchorMax = new Vector2(0f, 1f);
        slotText.rectTransform.pivot = new Vector2(0f, 0.5f);
        slotText.rectTransform.anchoredPosition = Vector2.zero;
        slotText.rectTransform.sizeDelta = new Vector2(44f, 0f);

        var nameText = CreateLabel("NameText", headerRect, 24f, FontStyles.Bold, TextAlignmentOptions.Left, labelColor);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameText.rectTransform.pivot = new Vector2(0f, 0.5f);
        nameText.rectTransform.offsetMin = new Vector2(50f, 0f);
        nameText.rectTransform.offsetMax = new Vector2(-120f, 0f);

        var badgeRoot = new GameObject("ProcessingBadge", typeof(RectTransform), typeof(Image));
        badgeRoot.layer = LayerMask.NameToLayer("UI");
        var badgeRect = badgeRoot.GetComponent<RectTransform>();
        badgeRect.SetParent(headerRect, false);
        badgeRect.anchorMin = new Vector2(1f, 0.5f);
        badgeRect.anchorMax = new Vector2(1f, 0.5f);
        badgeRect.pivot = new Vector2(1f, 0.5f);
        badgeRect.anchoredPosition = Vector2.zero;
        badgeRect.sizeDelta = new Vector2(112f, 26f);

        var badgeImage = badgeRoot.GetComponent<Image>();
        badgeImage.color = badgeColor;
        badgeImage.raycastTarget = false;

        var badgeText = CreateLabel("BadgeText", badgeRect, 17f, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        badgeText.rectTransform.anchorMin = Vector2.zero;
        badgeText.rectTransform.anchorMax = Vector2.one;
        badgeText.rectTransform.offsetMin = Vector2.zero;
        badgeText.rectTransform.offsetMax = Vector2.zero;
        badgeText.text = "ACTIVE";
        badgeRoot.SetActive(false);

        var infoText = CreateLabel("InfoText", cardRect, 18f, FontStyles.Bold, TextAlignmentOptions.TopLeft, infoEmphasisColor);
        infoText.rectTransform.anchorMin = new Vector2(0f, 1f);
        infoText.rectTransform.anchorMax = new Vector2(1f, 1f);
        infoText.rectTransform.pivot = new Vector2(0.5f, 1f);
        infoText.rectTransform.anchoredPosition = new Vector2(0f, -42f);
        infoText.rectTransform.sizeDelta = new Vector2(-24f, 24f);
        infoText.enableWordWrapping = false;
        infoText.overflowMode = TextOverflowModes.Ellipsis;

        var diceRowObject = new GameObject(
            "DiceRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        diceRowObject.layer = LayerMask.NameToLayer("UI");
        var diceRowRect = diceRowObject.GetComponent<RectTransform>();
        diceRowRect.SetParent(cardRect, false);
        diceRowRect.anchorMin = new Vector2(0f, 1f);
        diceRowRect.anchorMax = new Vector2(1f, 1f);
        diceRowRect.pivot = new Vector2(0.5f, 1f);
        diceRowRect.anchoredPosition = new Vector2(0f, -70f);
        diceRowRect.sizeDelta = new Vector2(-24f, 44f);

        var diceRowLayout = diceRowObject.GetComponent<HorizontalLayoutGroup>();
        diceRowLayout.childAlignment = TextAnchor.MiddleLeft;
        diceRowLayout.childControlWidth = false;
        diceRowLayout.childControlHeight = true;
        diceRowLayout.childForceExpandWidth = false;
        diceRowLayout.childForceExpandHeight = false;
        diceRowLayout.spacing = 8f;

        var expectedDamageText = CreateLabel(
            "ExpectedDamageText",
            cardRect,
            25f,
            FontStyles.Bold,
            TextAlignmentOptions.TopLeft,
            damageHighlightColor);
        expectedDamageText.rectTransform.anchorMin = new Vector2(0f, 1f);
        expectedDamageText.rectTransform.anchorMax = new Vector2(1f, 1f);
        expectedDamageText.rectTransform.pivot = new Vector2(0.5f, 1f);
        expectedDamageText.rectTransform.anchoredPosition = new Vector2(0f, -118f);
        expectedDamageText.rectTransform.sizeDelta = new Vector2(-24f, 28f);
        expectedDamageText.enableWordWrapping = false;
        expectedDamageText.overflowMode = TextOverflowModes.Ellipsis;

        var statusText = CreateLabel("StatusText", cardRect, 18f, FontStyles.Normal, TextAlignmentOptions.BottomLeft, subtleLabelColor);
        statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
        statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
        statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
        statusText.rectTransform.anchoredPosition = new Vector2(0f, 44f);
        statusText.rectTransform.sizeDelta = new Vector2(-24f, 28f);

        var rollButtonObject = new GameObject(
            "RollButton",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button),
            typeof(AdventurerRollButton));
        rollButtonObject.layer = LayerMask.NameToLayer("UI");
        var rollButtonRect = rollButtonObject.GetComponent<RectTransform>();
        rollButtonRect.SetParent(cardRect, false);
        rollButtonRect.anchorMin = new Vector2(0.5f, 0f);
        rollButtonRect.anchorMax = new Vector2(0.5f, 0f);
        rollButtonRect.pivot = new Vector2(0.5f, 0f);
        rollButtonRect.anchoredPosition = new Vector2(0f, 8f);
        rollButtonRect.sizeDelta = new Vector2(162f, 34f);

        var rollButtonImage = rollButtonObject.GetComponent<Image>();
        rollButtonImage.color = rollButtonColor;
        rollButtonImage.raycastTarget = true;

        var rollButton = rollButtonObject.GetComponent<Button>();
        rollButton.targetGraphic = rollButtonImage;

        var rollButtonText = CreateLabel("RollButtonText", rollButtonRect, 20f, FontStyles.Bold, TextAlignmentOptions.Center, Color.white);
        rollButtonText.rectTransform.anchorMin = Vector2.zero;
        rollButtonText.rectTransform.anchorMax = Vector2.one;
        rollButtonText.rectTransform.offsetMin = Vector2.zero;
        rollButtonText.rectTransform.offsetMax = Vector2.zero;

        var rollComponent = rollButtonObject.GetComponent<AdventurerRollButton>();
        rollComponent.SetOrchestrator(orchestrator);
        rollComponent.SetButton(rollButton);
        rollButton.onClick.AddListener(rollComponent.OnRollPressed);

        highlight.ConfigureVisuals(cardImage, badgeRoot);

        slotText.text = $"A{slotIndex + 1}";
        rollButtonText.text = $"Roll [{slotIndex + 1}]";

        return new CardWidgets
        {
            root = cardRect,
            background = cardImage,
            slotText = slotText,
            nameText = nameText,
            infoText = infoText,
            diceRowRoot = diceRowRect,
            expectedDamageText = expectedDamageText,
            statusText = statusText,
            rollButtonText = rollButtonText,
            rollButton = rollButton,
            dragHandle = dragHandle,
            rollComponent = rollComponent,
            highlight = highlight
        };
    }

    void RefreshAllCards()
    {
        if (!IsReady())
            return;
        if (cards.Count != orchestrator.RunState.adventurers.Count)
            return;

        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var adventurer = orchestrator.RunState.adventurers[index];
            if (card == null || adventurer == null)
                continue;

            BindCardIdentity(card, adventurer);
            RefreshCardVisual(card, adventurer, index);
        }
    }

    void BindCardIdentity(CardWidgets card, AdventurerState adventurer)
    {
        if (card.adventurerInstanceId == adventurer.instanceId)
            return;

        card.adventurerInstanceId = adventurer.instanceId;
        card.dragHandle.SetAdventurerInstanceId(adventurer.instanceId);
        card.dragHandle.SetOrchestrator(orchestrator);
        card.rollComponent.SetAdventurerInstanceId(adventurer.instanceId);
        card.rollComponent.SetOrchestrator(orchestrator);
        card.highlight.SetAdventurerInstanceId(adventurer.instanceId);
        card.highlight.SetOrchestrator(orchestrator);
    }

    void RefreshCardVisual(CardWidgets card, AdventurerState adventurer, int slotIndex)
    {
        card.slotText.text = $"A{slotIndex + 1}";
        card.rollButtonText.text = $"Roll [{slotIndex + 1}]";
        card.nameText.text = ResolveAdventurerName(adventurer.adventurerDefId);
        int diceCount = ResolveDiceCount(adventurer.adventurerDefId);
        card.infoText.text = BuildInfoLine(adventurer.adventurerDefId);
        RefreshDiceFaces(card, adventurer, diceCount);
        card.expectedDamageText.text = BuildExpectedDamageLine(adventurer);
        card.statusText.text = BuildStatusLine(adventurer);
        card.statusText.color = ResolveStatusColor(adventurer);

        bool isProcessing = orchestrator.IsCurrentProcessingAdventurer(adventurer.instanceId);
        if (adventurer.actionConsumed)
            card.background.color = consumedCardColor;
        else if (isProcessing)
            card.background.color = rolledCardColor;
        else if (adventurer.rolledDiceValues != null && adventurer.rolledDiceValues.Count > 0)
            card.background.color = rolledCardColor;
        else
            card.background.color = pendingCardColor;
    }

    string BuildInfoLine(string adventurerDefId)
    {
        int diceCount = ResolveDiceCount(adventurerDefId);
        string innateSummary = ResolveInnateSummary(adventurerDefId);
        return $"Dice {diceCount}  |  Innate: {innateSummary}";
    }

    void RefreshDiceFaces(CardWidgets card, AdventurerState adventurer, int diceCount)
    {
        if (card == null)
            return;
        if (diceCount < 1)
            diceCount = 1;

        EnsureDiceFaceCount(card, diceCount);

        for (int index = 0; index < card.diceFaces.Count; index++)
        {
            string display = "-";
            if (adventurer?.rolledDiceValues != null &&
                index < adventurer.rolledDiceValues.Count)
            {
                display = adventurer.rolledDiceValues[index].ToString();
            }

            card.diceFaces[index].valueText.text = display;
        }
    }

    void EnsureDiceFaceCount(CardWidgets card, int targetCount)
    {
        while (card.diceFaces.Count > targetCount)
        {
            int lastIndex = card.diceFaces.Count - 1;
            var face = card.diceFaces[lastIndex];
            if (face?.root != null)
            {
                if (Application.isPlaying)
                    Destroy(face.root.gameObject);
                else
                    DestroyImmediate(face.root.gameObject);
            }

            card.diceFaces.RemoveAt(lastIndex);
        }

        while (card.diceFaces.Count < targetCount)
            card.diceFaces.Add(CreateDiceFace(card.diceRowRoot, card.diceFaces.Count));
    }

    DiceFaceWidgets CreateDiceFace(Transform parent, int index)
    {
        var faceObject = new GameObject(
            $"Dice_{index + 1}",
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(Image));
        faceObject.layer = LayerMask.NameToLayer("UI");

        var faceRect = faceObject.GetComponent<RectTransform>();
        faceRect.SetParent(parent, false);

        var layout = faceObject.GetComponent<LayoutElement>();
        layout.preferredWidth = 40f;
        layout.preferredHeight = 40f;
        layout.minWidth = 40f;
        layout.minHeight = 40f;

        var faceBackground = faceObject.GetComponent<Image>();
        faceBackground.color = diceFaceBackgroundColor;
        faceBackground.raycastTarget = false;

        var valueText = CreateLabel(
            "ValueText",
            faceRect,
            24f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            diceFaceTextColor);
        valueText.rectTransform.anchorMin = Vector2.zero;
        valueText.rectTransform.anchorMax = Vector2.one;
        valueText.rectTransform.offsetMin = Vector2.zero;
        valueText.rectTransform.offsetMax = Vector2.zero;
        valueText.text = "-";

        return new DiceFaceWidgets
        {
            root = faceRect,
            valueText = valueText
        };
    }

    string BuildExpectedDamageLine(AdventurerState adventurer)
    {
        if (adventurer?.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
            return "ATK  -";

        int sum = 0;
        for (int index = 0; index < adventurer.rolledDiceValues.Count; index++)
            sum += adventurer.rolledDiceValues[index];

        return $"ATK  {sum}";
    }

    string BuildStatusLine(AdventurerState adventurer)
    {
        if (adventurer == null)
            return string.Empty;
        if (adventurer.actionConsumed)
            return "Status: Resolved";

        var phase = orchestrator.RunState.turn.phase;
        bool isProcessing = orchestrator.IsCurrentProcessingAdventurer(adventurer.instanceId);
        if (isProcessing && phase == TurnPhase.Adjustment)
            return "Status: Adjusting";
        if (isProcessing && phase == TurnPhase.TargetAndAttack)
            return "Status: Choose target";
        if (adventurer.rolledDiceValues != null && adventurer.rolledDiceValues.Count > 0)
            return "Status: Ready to attack";
        if (orchestrator.CanRollAdventurer(adventurer.instanceId))
            return "Status: Ready to roll";
        return "Status: Waiting";
    }

    Color ResolveStatusColor(AdventurerState adventurer)
    {
        if (adventurer == null)
            return subtleLabelColor;
        if (adventurer.actionConsumed)
            return subtleLabelColor;

        var phase = orchestrator.RunState.turn.phase;
        bool isProcessing = orchestrator.IsCurrentProcessingAdventurer(adventurer.instanceId);
        if (isProcessing && (phase == TurnPhase.Adjustment || phase == TurnPhase.TargetAndAttack))
            return damageHighlightColor;
        if (adventurer.rolledDiceValues != null && adventurer.rolledDiceValues.Count > 0)
            return infoEmphasisColor;

        return subtleLabelColor;
    }

    string ResolveAdventurerName(string adventurerDefId)
    {
        if (!string.IsNullOrWhiteSpace(adventurerDefId) &&
            adventurerDefById.TryGetValue(adventurerDefId, out var def))
        {
            if (!string.IsNullOrWhiteSpace(def.adventurerId))
                return ToDisplayTitle(def.adventurerId);
            if (!string.IsNullOrWhiteSpace(def.nameKey))
                return ToDisplayTitle(def.nameKey);
        }

        if (string.IsNullOrWhiteSpace(adventurerDefId))
            return "Unknown Adventurer";
        return ToDisplayTitle(adventurerDefId);
    }

    int ResolveDiceCount(string adventurerDefId)
    {
        if (string.IsNullOrWhiteSpace(adventurerDefId))
            return 1;
        if (!adventurerDefById.TryGetValue(adventurerDefId, out var def))
            return 1;

        return Mathf.Max(1, def.diceCount);
    }

    string ResolveInnateSummary(string adventurerDefId)
    {
        if (string.IsNullOrWhiteSpace(adventurerDefId))
            return "None";
        if (!adventurerDefById.TryGetValue(adventurerDefId, out var def))
            return "None";
        if (def.innateEffect?.effects == null || def.innateEffect.effects.Count == 0)
            return "None";

        var tokens = new List<string>(def.innateEffect.effects.Count);
        for (int index = 0; index < def.innateEffect.effects.Count; index++)
        {
            string token = ToEffectSummary(def.innateEffect.effects[index]);
            if (string.IsNullOrWhiteSpace(token))
                continue;

            tokens.Add(token);
        }

        if (tokens.Count == 0)
            return "None";
        return string.Join(", ", tokens);
    }

    void TryResolveOrchestrator()
    {
        if (orchestrator != null)
            return;

        orchestrator = FindFirstObjectByType<GameTurnOrchestrator>();
    }

    void TryResolveContentRoot()
    {
        if (contentRoot != null)
            return;

        contentRoot = transform as RectTransform;
    }

    void EnsureContentLayout()
    {
        if (contentRoot == null)
            return;

        var layout = contentRoot.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
            layout = contentRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 10f;
        layout.padding = new RectOffset(12, 12, 12, 12);
    }

    void LoadAdventurerDefsIfNeeded()
    {
        if (loadedDefs)
            return;
        loadedDefs = true;

        adventurerDefById.Clear();

        try
        {
            var defs = GameStaticDataLoader.LoadAdventurerDefs();
            for (int index = 0; index < defs.Count; index++)
            {
                var def = defs[index];
                if (def == null || string.IsNullOrWhiteSpace(def.adventurerId))
                    continue;

                adventurerDefById[def.adventurerId] = def;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[AdventurerPanelController] Failed to load adventurer defs: {exception.Message}");
        }
    }

    static RectTransform CreateUiContainer(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        return rect;
    }

    static TextMeshProUGUI CreateLabel(
        string name,
        Transform parent,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment,
        Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.layer = LayerMask.NameToLayer("UI");
        var rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);

        var text = go.GetComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        text.text = string.Empty;
        return text;
    }

    static string ToEffectSummary(EffectSpec effect)
    {
        if (effect == null || string.IsNullOrWhiteSpace(effect.effectType))
            return string.Empty;

        int value = effect.value.HasValue
            ? Mathf.RoundToInt((float)effect.value.Value)
            : 0;

        return effect.effectType switch
        {
            "stability_delta" => $"Stability {FormatSignedValue(value)}",
            "gold_delta" => $"Gold {FormatSignedValue(value)}",
            "situation_requirement_delta" => $"Req {FormatSignedValue(value)}",
            "die_face_delta" => $"Die {FormatSignedValue(value)}",
            "reroll_adventurer_dice" => "Reroll Dice",
            _ => value == 0
                ? ToDisplayTitle(effect.effectType)
                : $"{ToDisplayTitle(effect.effectType)} {FormatSignedValue(value)}"
        };
    }

    static string FormatSignedValue(int value)
    {
        if (value > 0)
            return $"+{value}";
        return value.ToString();
    }

    static string ToDisplayTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string normalized = raw.Trim();
        if (normalized.StartsWith("adventurer_", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("adventurer_".Length);

        normalized = normalized.Replace('_', ' ').Replace('.', ' ');
        var parts = normalized.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return normalized;

        for (int index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (part.Length == 0)
                continue;

            if (part.Length == 1)
            {
                parts[index] = part.ToUpperInvariant();
                continue;
            }

            parts[index] = char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant();
        }

        return string.Join(" ", parts);
    }

    sealed class CardWidgets
    {
        public RectTransform root;
        public string adventurerInstanceId = string.Empty;
        public Image background;
        public TextMeshProUGUI slotText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI infoText;
        public RectTransform diceRowRoot;
        public TextMeshProUGUI expectedDamageText;
        public List<DiceFaceWidgets> diceFaces = new();
        public TextMeshProUGUI statusText;
        public TextMeshProUGUI rollButtonText;
        public Button rollButton;
        public AdventurerDragHandle dragHandle;
        public AdventurerRollButton rollComponent;
        public AdventurerProcessingHighlight highlight;
    }

    sealed class DiceFaceWidgets
    {
        public RectTransform root;
        public TextMeshProUGUI valueText;
    }
}
