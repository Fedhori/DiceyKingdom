using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AdventurerPanelController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] GameObject dicePrefab;
    [SerializeField] Color pendingCardColor = new(0.18f, 0.22f, 0.30f, 0.95f);
    [SerializeField] Color rolledCardColor = new(0.20f, 0.27f, 0.36f, 0.95f);
    [SerializeField] Color consumedCardColor = new(0.13f, 0.16f, 0.20f, 0.85f);
    [SerializeField] Color subtleLabelColor = new(0.72f, 0.80f, 0.90f, 1.00f);
    [SerializeField] Color infoEmphasisColor = new(0.86f, 0.93f, 1.00f, 1.00f);
    [SerializeField] Color damageHighlightColor = new(1.00f, 0.88f, 0.28f, 1.00f);

    readonly List<CardWidgets> cards = new();
    readonly Dictionary<string, AdventurerDef> adventurerDefById = new(StringComparer.Ordinal);
    bool loadedDefs;

    void Awake()
    {
        TryResolveOrchestrator();
        TryResolveContentRoot();
        ValidateEditorLayoutSetup();
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
        {
            var card = CreateCard(index);
            if (card != null)
                cards.Add(card);
        }

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
        if (cardPrefab == null)
        {
            Debug.LogWarning("[AdventurerPanelController] cardPrefab is not assigned.");
            return null;
        }

        var root = Instantiate(cardPrefab);
        root.name = $"AdventurerCard_{slotIndex + 1}";
        root.layer = LayerMask.NameToLayer("UI");

        var cardRect = root.GetComponent<RectTransform>();
        if (cardRect == null)
            cardRect = root.AddComponent<RectTransform>();
        cardRect.SetParent(contentRoot, false);

        RemoveLocalizationComponents(root);

        var background = root.GetComponent<Image>();
        if (background == null)
            background = root.AddComponent<Image>();
        background.raycastTarget = true;
        background.color = pendingCardColor;

        var dragHandle = root.GetComponent<AdventurerDragHandle>();
        if (dragHandle == null)
            dragHandle = root.AddComponent<AdventurerDragHandle>();
        dragHandle.SetOrchestrator(orchestrator);

        var highlight = root.GetComponent<AdventurerProcessingHighlight>();
        if (highlight == null)
            highlight = root.AddComponent<AdventurerProcessingHighlight>();
        highlight.SetOrchestrator(orchestrator);

        var slotText = FindTextByName(cardRect, "SlotText");
        var nameText = FindTextByName(cardRect, "NameText");
        var infoText = FindTextByName(cardRect, "InfoText");
        var expectedDamageText = FindTextByName(cardRect, "ExpectedDamageText");
        var statusText = FindTextByName(cardRect, "StatusText");
        var rollButtonText = FindTextByName(cardRect, "RollButtonText");

        var diceRowRoot = FindRectByName(cardRect, "DiceRow");

        var rollButton = FindButtonByName(cardRect, "RollButton");
        AdventurerRollButton rollComponent = null;
        if (rollButton != null)
        {
            rollComponent = rollButton.GetComponent<AdventurerRollButton>();
            if (rollComponent == null)
                rollComponent = rollButton.gameObject.AddComponent<AdventurerRollButton>();
            rollComponent.SetOrchestrator(orchestrator);
            rollComponent.SetButton(rollButton);
            rollButton.onClick.RemoveListener(rollComponent.OnRollPressed);
            rollButton.onClick.AddListener(rollComponent.OnRollPressed);
        }

        var badge = FindTransformByName(cardRect, "ProcessingBadge")?.gameObject;
        highlight.ConfigureVisuals(background, badge);

        if (slotText != null)
            slotText.text = $"A{slotIndex + 1}";
        if (rollButtonText != null)
            rollButtonText.text = $"Roll [{slotIndex + 1}]";

        return new CardWidgets
        {
            root = cardRect,
            background = background,
            slotText = slotText,
            nameText = nameText,
            infoText = infoText,
            diceRowRoot = diceRowRoot,
            expectedDamageText = expectedDamageText,
            statusText = statusText,
            rollButtonText = rollButtonText,
            rollButton = rollButton,
            dragHandle = dragHandle,
            rollComponent = rollComponent,
            highlight = highlight
        };
    }

    static void RemoveLocalizationComponents(GameObject root)
    {
        if (root == null)
            return;

        var components = root.GetComponentsInChildren<Component>(true);
        for (int index = 0; index < components.Length; index++)
        {
            var component = components[index];
            if (component == null)
                continue;

            if (!string.Equals(
                    component.GetType().FullName,
                    "UnityEngine.Localization.Components.LocalizeStringEvent",
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (Application.isPlaying)
                Destroy(component);
            else
                DestroyImmediate(component);
        }
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

        card.dragHandle?.SetAdventurerInstanceId(adventurer.instanceId);
        card.dragHandle?.SetOrchestrator(orchestrator);

        card.rollComponent?.SetAdventurerInstanceId(adventurer.instanceId);
        card.rollComponent?.SetOrchestrator(orchestrator);

        card.highlight?.SetAdventurerInstanceId(adventurer.instanceId);
        card.highlight?.SetOrchestrator(orchestrator);
    }

    void RefreshCardVisual(CardWidgets card, AdventurerState adventurer, int slotIndex)
    {
        if (card.slotText != null)
            card.slotText.text = $"A{slotIndex + 1}";
        if (card.rollButtonText != null)
            card.rollButtonText.text = $"Roll [{slotIndex + 1}]";
        if (card.nameText != null)
            card.nameText.text = ResolveAdventurerName(adventurer.adventurerDefId);

        int diceCount = ResolveDiceCount(adventurer.adventurerDefId);
        if (card.infoText != null)
            card.infoText.text = BuildInfoLine(adventurer.adventurerDefId);

        RefreshDiceFaces(card, adventurer, diceCount);

        if (card.expectedDamageText != null)
            card.expectedDamageText.text = BuildExpectedDamageLine(adventurer);
        if (card.statusText != null)
        {
            card.statusText.text = BuildStatusLine(adventurer);
            card.statusText.color = ResolveStatusColor(adventurer);
        }

        if (card.background != null)
        {
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
        if (card.diceRowRoot == null)
            return;
        if (diceCount < 1)
            diceCount = 1;

        EnsureDiceFaceCount(card, diceCount);

        for (int index = 0; index < card.diceFaces.Count; index++)
        {
            var face = card.diceFaces[index];
            if (face?.valueText == null)
                continue;

            string display = "-";
            if (adventurer?.rolledDiceValues != null &&
                index < adventurer.rolledDiceValues.Count)
            {
                display = adventurer.rolledDiceValues[index].ToString();
            }

            face.valueText.text = display;
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
        {
            var face = CreateDiceFace(card.diceRowRoot, card.diceFaces.Count);
            if (face == null)
                return;

            card.diceFaces.Add(face);
        }
    }

    DiceFaceWidgets CreateDiceFace(Transform parent, int index)
    {
        if (dicePrefab == null)
        {
            Debug.LogWarning("[AdventurerPanelController] dicePrefab is not assigned.");
            return null;
        }

        var root = Instantiate(dicePrefab, parent, false);
        root.name = $"Dice_{index + 1}";
        root.layer = LayerMask.NameToLayer("UI");

        var rootRect = root.GetComponent<RectTransform>();
        if (rootRect == null)
            rootRect = root.AddComponent<RectTransform>();

        var valueText = FindTextByName(rootRect, "ValueText");
        if (valueText == null)
            valueText = root.GetComponentInChildren<TextMeshProUGUI>(true);

        return new DiceFaceWidgets
        {
            root = rootRect,
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

    void ValidateEditorLayoutSetup()
    {
        if (contentRoot == null)
            return;

        if (contentRoot.GetComponent<GridLayoutGroup>() == null &&
            contentRoot.GetComponent<HorizontalLayoutGroup>() == null)
        {
            Debug.LogWarning(
                "[AdventurerPanelController] contentRoot requires a LayoutGroup configured in the editor.");
        }

        if (contentRoot.GetComponent<RectMask2D>() == null)
        {
            Debug.LogWarning(
                "[AdventurerPanelController] contentRoot requires RectMask2D configured in the editor.");
        }
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

    static Transform FindTransformByName(Transform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (string.Equals(child.name, name, StringComparison.Ordinal))
                return child;

            var nested = FindTransformByName(child, name);
            if (nested != null)
                return nested;
        }

        return null;
    }

    static RectTransform FindRectByName(RectTransform root, string name)
    {
        var target = FindTransformByName(root, name) as RectTransform;
        return target;
    }

    static TextMeshProUGUI FindTextByName(RectTransform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int index = 0; index < texts.Length; index++)
        {
            var text = texts[index];
            if (text == null)
                continue;
            if (!string.Equals(text.gameObject.name, name, StringComparison.Ordinal))
                continue;

            return text;
        }

        return null;
    }

    static Button FindButtonByName(RectTransform root, string name)
    {
        if (root == null || string.IsNullOrWhiteSpace(name))
            return null;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int index = 0; index < buttons.Length; index++)
        {
            var button = buttons[index];
            if (button == null)
                continue;
            if (!string.Equals(button.gameObject.name, name, StringComparison.Ordinal))
                continue;

            return button;
        }

        return null;
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
