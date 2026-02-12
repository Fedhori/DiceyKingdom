using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class AgentManager : MonoBehaviour
{
    const string AgentLocalizationTable = "Agent";

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

    readonly List<AgentController> cards = new();
    readonly Dictionary<string, AgentDef> agentDefById = new(StringComparer.Ordinal);
    bool loadedDefs;

    void Awake()
    {
        TryResolveOrchestrator();
        TryResolveContentRoot();
        ValidateEditorLayoutSetup();
        LoadAgentDefsIfNeeded();
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
        if (orchestrator.RunState == null || orchestrator.RunState.agents == null)
            return false;

        return true;
    }

    void RebuildCardsIfNeeded(bool forceRebuild)
    {
        if (!IsReady())
            return;

        int desiredCount = orchestrator.RunState.agents.Count;
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

    AgentController CreateCard(int slotIndex)
    {
        if (cardPrefab == null)
        {
            Debug.LogWarning("[AgentManager] cardPrefab is not assigned.");
            return null;
        }

        var root = Instantiate(cardPrefab, contentRoot, false);
        root.name = $"AgentCard_{slotIndex + 1}";

        var controller = root.GetComponent<AgentController>();
        if (controller == null)
        {
            Debug.LogWarning("[AgentManager] cardPrefab requires AgentController.", root);
            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);
            return null;
        }

        controller.SetDicePrefab(dicePrefab);
        controller.BindOrchestrator(orchestrator);
        controller.BindAgent(string.Empty);
        controller.Render(
            $"A{slotIndex + 1}",
            "Unknown Agent",
            "Rules: -",
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
        if (cards.Count != orchestrator.RunState.agents.Count)
            return;

        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var agent = orchestrator.RunState.agents[index];
            if (card == null || agent == null)
                continue;

            RefreshCardVisual(card, agent, index);
        }
    }

    void RefreshCardVisual(AgentController card, AgentState agent, int slotIndex)
    {
        card.SetDicePrefab(dicePrefab);
        card.BindOrchestrator(orchestrator);
        card.BindAgent(agent.instanceId);

        int diceCount = ResolveDiceCount(agent.agentDefId);
        Color statusColor = ResolveStatusColor(agent);
        Color backgroundColor;
        bool isProcessing = orchestrator.IsCurrentProcessingAgent(agent.instanceId);
        if (agent.actionConsumed)
            backgroundColor = consumedCardColor;
        else if (isProcessing)
            backgroundColor = rolledCardColor;
        else if (agent.rolledDiceValues != null && agent.rolledDiceValues.Count > 0)
            backgroundColor = rolledCardColor;
        else
            backgroundColor = pendingCardColor;

        card.Render(
            $"A{slotIndex + 1}",
            ResolveAgentName(agent.agentDefId),
            BuildInfoLine(agent.agentDefId),
            BuildExpectedDamageLine(agent),
            BuildStatusLine(agent),
            $"Roll [{slotIndex + 1}]",
            statusColor,
            backgroundColor,
            agent.rolledDiceValues,
            diceCount);
    }

    string BuildInfoLine(string agentDefId)
    {
        string ruleSummary = ResolveRuleSummary(agentDefId);
        return $"Rules: {ruleSummary}";
    }

    string BuildExpectedDamageLine(AgentState agent)
    {
        if (agent?.rolledDiceValues == null || agent.rolledDiceValues.Count == 0)
            return "ATK  -";

        if (orchestrator != null &&
            orchestrator.TryGetAgentAttackBreakdown(
                agent.instanceId,
                out int baseAttack,
                out int ruleBonus,
                out int totalAttack))
        {
            return $"ATK  {totalAttack} ({baseAttack} + {ruleBonus})";
        }

        int sum = 0;
        for (int index = 0; index < agent.rolledDiceValues.Count; index++)
            sum += agent.rolledDiceValues[index];

        return $"ATK  {sum} ({sum} + 0)";
    }

    string BuildStatusLine(AgentState agent)
    {
        if (agent == null)
            return string.Empty;
        if (agent.actionConsumed)
            return "Status: Resolved";

        var phase = orchestrator.RunState.turn.phase;
        bool isProcessing = orchestrator.IsCurrentProcessingAgent(agent.instanceId);
        if (isProcessing && phase == TurnPhase.Adjustment)
            return "Status: Adjusting";
        if (isProcessing && phase == TurnPhase.TargetAndAttack)
            return "Status: Choose target";
        if (agent.rolledDiceValues != null && agent.rolledDiceValues.Count > 0)
            return "Status: Ready to attack";
        if (orchestrator.CanRollAgent(agent.instanceId))
            return "Status: Ready to roll";
        return "Status: Waiting";
    }

    Color ResolveStatusColor(AgentState agent)
    {
        if (agent == null)
            return subtleLabelColor;
        if (agent.actionConsumed)
            return subtleLabelColor;

        var phase = orchestrator.RunState.turn.phase;
        bool isProcessing = orchestrator.IsCurrentProcessingAgent(agent.instanceId);
        if (isProcessing && (phase == TurnPhase.Adjustment || phase == TurnPhase.TargetAndAttack))
            return damageHighlightColor;
        if (agent.rolledDiceValues != null && agent.rolledDiceValues.Count > 0)
            return infoEmphasisColor;

        return subtleLabelColor;
    }

    string ResolveAgentName(string agentDefId)
    {
        if (!string.IsNullOrWhiteSpace(agentDefId) &&
            agentDefById.TryGetValue(agentDefId, out var def))
        {
            if (!string.IsNullOrWhiteSpace(def.agentId))
            {
                string nameKey = $"{def.agentId}.name";
                string localized = LocalizationUtil.Get(AgentLocalizationTable, nameKey);
                if (!string.IsNullOrWhiteSpace(localized))
                    return localized;

                return ToDisplayTitle(def.agentId);
            }
        }

        if (string.IsNullOrWhiteSpace(agentDefId))
            return "Unknown Agent";

        string fallbackLocalized = LocalizationUtil.Get(AgentLocalizationTable, $"{agentDefId}.name");
        if (!string.IsNullOrWhiteSpace(fallbackLocalized))
            return fallbackLocalized;
        return ToDisplayTitle(agentDefId);
    }

    int ResolveDiceCount(string agentDefId)
    {
        if (string.IsNullOrWhiteSpace(agentDefId))
            return 1;
        if (!agentDefById.TryGetValue(agentDefId, out var def))
            return 1;

        return Mathf.Max(1, def.diceCount);
    }

    string ResolveRuleSummary(string agentDefId)
    {
        if (string.IsNullOrWhiteSpace(agentDefId))
            return "None";

        if (!agentDefById.TryGetValue(agentDefId, out var def))
            return "None";
        if (def.rules == null || def.rules.Count == 0)
            return "None";

        var tokens = new List<string>(def.rules.Count);
        for (int index = 0; index < def.rules.Count; index++)
        {
            var rule = def.rules[index];
            string key = $"{agentDefId}.rule.{index}";
            var args = AgentRuleTextArgsBuilder.Build(rule);
            string token = LocalizationUtil.Get(AgentLocalizationTable, key, args);
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
                "[AgentManager] contentRoot requires a LayoutGroup configured in the editor.");
        }

        if (contentRoot.GetComponent<RectMask2D>() == null)
        {
            Debug.LogWarning(
                "[AgentManager] contentRoot requires RectMask2D configured in the editor.");
        }
    }

    void LoadAgentDefsIfNeeded()
    {
        if (loadedDefs)
            return;
        loadedDefs = true;

        agentDefById.Clear();

        try
        {
            var defs = GameStaticDataLoader.LoadAgentDefs();
            for (int index = 0; index < defs.Count; index++)
            {
                var def = defs[index];
                if (def == null || string.IsNullOrWhiteSpace(def.agentId))
                    continue;

                agentDefById[def.agentId] = def;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[AgentManager] Failed to load agent defs: {exception.Message}");
        }
    }

    static string ToDisplayTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string normalized = raw.Trim();
        if (normalized.StartsWith("agent_", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("agent_".Length);
        else if (normalized.StartsWith("agent.", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("agent.".Length);

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

