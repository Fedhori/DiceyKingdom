using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class AdventurerManager : MonoBehaviour
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

    readonly List<AdventurerController> cards = new();
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

    void Start()
    {
        RebuildCardsIfNeeded(forceRebuild: true);
        RefreshAllCards();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
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

    void OnStateChanged()
    {
        RebuildCardsIfNeeded(forceRebuild: false);
        RefreshAllCards();
    }

    void SubscribeEvents()
    {
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.RunEnded -= OnRunEnded;
        orchestrator.StateChanged -= OnStateChanged;
        orchestrator.RunStarted += OnRunStarted;
        orchestrator.PhaseChanged += OnPhaseChanged;
        orchestrator.RunEnded += OnRunEnded;
        orchestrator.StateChanged += OnStateChanged;
    }

    void UnsubscribeEvents()
    {
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.RunEnded -= OnRunEnded;
        orchestrator.StateChanged -= OnStateChanged;
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
            if (card == null)
                continue;

            if (Application.isPlaying)
                Destroy(card.gameObject);
            else
                DestroyImmediate(card.gameObject);
        }

        cards.Clear();
    }

    AdventurerController CreateCard(int slotIndex)
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("[AdventurerManager] cardPrefab is not assigned.");
            return null;
        }

        var root = Instantiate(cardPrefab, contentRoot, false);
        root.name = $"AdventurerCard_{slotIndex + 1}";

        var controller = root.GetComponent<AdventurerController>();
        if (controller == null)
        {
            Debug.LogWarning("[AdventurerManager] cardPrefab requires AdventurerController.", root);
            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);
            return null;
        }

        controller.SetDicePrefab(dicePrefab);
        controller.BindOrchestrator(orchestrator);
        controller.BindAdventurer(string.Empty);
        controller.Render(
            $"A{slotIndex + 1}",
            "Unknown Adventurer",
            "Dice 1  |  Rules: -",
            "ATK  -",
            "Status: Waiting",
            $"Roll [{slotIndex + 1}]",
            subtleLabelColor,
            pendingCardColor,
            null,
            1);

        return controller;
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

            RefreshCardVisual(card, adventurer, index);
        }
    }

    void RefreshCardVisual(AdventurerController card, AdventurerState adventurer, int slotIndex)
    {
        card.SetDicePrefab(dicePrefab);
        card.BindOrchestrator(orchestrator);
        card.BindAdventurer(adventurer.instanceId);

        int diceCount = ResolveDiceCount(adventurer.adventurerDefId);
        Color statusColor = ResolveStatusColor(adventurer);
        Color backgroundColor;
        bool isProcessing = orchestrator.IsCurrentProcessingAdventurer(adventurer.instanceId);
        if (adventurer.actionConsumed)
            backgroundColor = consumedCardColor;
        else if (isProcessing)
            backgroundColor = rolledCardColor;
        else if (adventurer.rolledDiceValues != null && adventurer.rolledDiceValues.Count > 0)
            backgroundColor = rolledCardColor;
        else
            backgroundColor = pendingCardColor;

        card.Render(
            $"A{slotIndex + 1}",
            ResolveAdventurerName(adventurer.adventurerDefId),
            BuildInfoLine(adventurer.adventurerDefId),
            BuildExpectedDamageLine(adventurer),
            BuildStatusLine(adventurer),
            $"Roll [{slotIndex + 1}]",
            statusColor,
            backgroundColor,
            adventurer.rolledDiceValues,
            diceCount);
    }

    string BuildInfoLine(string adventurerDefId)
    {
        int diceCount = ResolveDiceCount(adventurerDefId);
        string ruleSummary = ResolveRuleSummary(adventurerDefId);
        return $"Dice {diceCount}  |  Rules: {ruleSummary}";
    }

    string BuildExpectedDamageLine(AdventurerState adventurer)
    {
        if (adventurer?.rolledDiceValues == null || adventurer.rolledDiceValues.Count == 0)
            return "ATK  -";

        if (orchestrator != null &&
            orchestrator.TryGetAdventurerAttackBreakdown(
                adventurer.instanceId,
                out int baseAttack,
                out int ruleBonus,
                out int totalAttack))
        {
            return $"ATK  {totalAttack} ({baseAttack} + {ruleBonus})";
        }

        int sum = 0;
        for (int index = 0; index < adventurer.rolledDiceValues.Count; index++)
            sum += adventurer.rolledDiceValues[index];

        return $"ATK  {sum} ({sum} + 0)";
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

    string ResolveRuleSummary(string adventurerDefId)
    {
        if (string.IsNullOrWhiteSpace(adventurerDefId))
            return "None";

        if (!adventurerDefById.TryGetValue(adventurerDefId, out var def))
            return "None";
        if (def.rules == null || def.rules.Count == 0)
            return "None";

        var tokens = new List<string>(def.rules.Count);
        for (int index = 0; index < def.rules.Count; index++)
        {
            var rule = def.rules[index];
            string key = $"{adventurerDefId}.rule.{index}";
            var args = AdventurerRuleTextArgsBuilder.Build(rule);
            string token = LocalizationUtil.Get("adventurer", key, args);
            if (string.IsNullOrWhiteSpace(token))
                token = key;
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
                "[AdventurerManager] contentRoot requires a LayoutGroup configured in the editor.");
        }

        if (contentRoot.GetComponent<RectMask2D>() == null)
        {
            Debug.LogWarning(
                "[AdventurerManager] contentRoot requires RectMask2D configured in the editor.");
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
            Debug.LogWarning($"[AdventurerManager] Failed to load adventurer defs: {exception.Message}");
        }
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

}
