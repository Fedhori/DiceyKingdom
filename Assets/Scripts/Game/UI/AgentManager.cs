using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class AgentManager : MonoBehaviour
{
    const string AgentLocalizationTable = "Agent";

    [SerializeField] RectTransform contentRoot;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] GameObject dicePrefab;
    [SerializeField] Color pendingCardColor = new(0.18f, 0.22f, 0.30f, 0.95f);
    [SerializeField] Color selectedCardColor = new(0.20f, 0.27f, 0.36f, 0.95f);
    [SerializeField] Color consumedCardColor = new(0.13f, 0.16f, 0.20f, 0.85f);
    [SerializeField] Color subtleLabelColor = new(0.72f, 0.80f, 0.90f, 1.00f);
    [SerializeField] Color infoEmphasisColor = new(0.86f, 0.93f, 1.00f, 1.00f);
    [SerializeField] Color damageHighlightColor = new(1.00f, 0.88f, 0.28f, 1.00f);

    readonly List<AgentController> cards = new();
    readonly Dictionary<string, AgentDef> agentDefById = new(StringComparer.Ordinal);

    GameRunState runState;
    bool loadedDefs;

    public static AgentManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        TryResolveContentRoot();
        ValidateEditorLayoutSetup();
        LoadAgentDefsIfNeeded();
    }

    void OnEnable()
    {
        SubscribeEvents();
        runState = GameManager.Instance != null ? GameManager.Instance.CurrentRunState : null;
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

    public void InitializeForRun(GameRunState state, IReadOnlyDictionary<string, AgentDef> defs)
    {
        runState = state;
        agentDefById.Clear();
        if (defs != null)
        {
            foreach (var pair in defs)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    continue;

                agentDefById[pair.Key] = pair.Value;
            }
        }
        else
        {
            LoadAgentDefsIfNeeded();
        }

        RebuildCardsIfNeeded(forceRebuild: true);
        RefreshAllCards();
    }

    public bool TryRollAgentBySlotIndex(int slotIndex)
    {
        if (runState?.agents == null)
            return false;
        if (slotIndex < 0 || slotIndex >= runState.agents.Count)
            return false;

        var agent = runState.agents[slotIndex];
        if (agent == null)
            return false;

        return TryRollAgent(agent.instanceId);
    }

    public bool TryRollAgent(string agentInstanceId)
    {
        if (!CanAcceptAgentSelectionInput())
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        var agent = FindAgentState(agentInstanceId);
        if (agent == null || agent.actionConsumed)
            return false;
        if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
            return false;

        runState.turn.processingAgentInstanceId = agent.instanceId;
        runState.turn.selectedAgentDieIndex = -1;
        PhaseManager.Instance.SetPhase(TurnPhase.Adjustment);
        return true;
    }

    public bool TrySelectProcessingAgentDie(int dieIndex)
    {
        if (!CanAcceptAgentDieSelectionInput())
            return false;

        var agent = FindCurrentProcessingAgent();
        if (!IsValidAgentDieIndex(agent, dieIndex))
            return false;

        runState.turn.selectedAgentDieIndex = dieIndex;
        PhaseManager.Instance.SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    public bool TryBeginAgentTargeting(string agentInstanceId)
    {
        if (runState == null)
            return false;
        if (!string.Equals(runState.turn.processingAgentInstanceId, agentInstanceId, StringComparison.Ordinal))
            return false;

        var agent = FindCurrentProcessingAgent();
        if (agent == null || agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
            return false;

        runState.turn.selectedAgentDieIndex = 0;
        PhaseManager.Instance.SetPhase(TurnPhase.TargetAndAttack);
        return true;
    }

    public bool TryClearAgentAssignment(string agentInstanceId)
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return false;
        if (DuelManager.Instance.IsDuelResolutionPending)
            return false;
        if (PhaseManager.Instance.CurrentPhase != TurnPhase.TargetAndAttack)
            return false;
        if (!string.Equals(agentInstanceId, runState.turn.processingAgentInstanceId, StringComparison.Ordinal))
            return false;

        runState.turn.selectedAgentDieIndex = -1;
        PhaseManager.Instance.SetPhase(TurnPhase.Adjustment);
        return true;
    }

    public bool CanAssignAgent(string agentInstanceId)
    {
        return false;
    }

    public bool CanRollAgent(string agentInstanceId)
    {
        if (!CanAcceptAgentSelectionInput())
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        var agent = FindAgentState(agentInstanceId);
        if (agent == null || agent.actionConsumed)
            return false;

        return agent.remainingDiceFaces != null && agent.remainingDiceFaces.Count > 0;
    }

    public bool IsCurrentProcessingAgent(string agentInstanceId)
    {
        if (runState == null)
            return false;
        if (string.IsNullOrWhiteSpace(agentInstanceId))
            return false;

        return string.Equals(
            runState.turn.processingAgentInstanceId,
            agentInstanceId,
            StringComparison.Ordinal);
    }

    public AgentState FindCurrentProcessingAgent()
    {
        if (runState == null)
            return null;
        if (string.IsNullOrWhiteSpace(runState.turn.processingAgentInstanceId))
            return null;

        return FindAgentState(runState.turn.processingAgentInstanceId);
    }

    public AgentState FindAgentState(string agentInstanceId)
    {
        if (runState?.agents == null)
            return null;

        for (int i = 0; i < runState.agents.Count; i++)
        {
            var agent = runState.agents[i];
            if (agent == null)
                continue;
            if (!string.Equals(agent.instanceId, agentInstanceId, StringComparison.Ordinal))
                continue;

            return agent;
        }

        return null;
    }

    public bool RemoveAgentDie(string agentInstanceId, int dieIndex)
    {
        var agent = FindAgentState(agentInstanceId);
        if (!IsValidAgentDieIndex(agent, dieIndex))
            return false;

        agent.remainingDiceFaces.RemoveAt(dieIndex);
        return true;
    }

    public void AdvanceAfterAgentDieSpent(string actingAgentInstanceId)
    {
        if (runState == null)
            return;

        var agent = FindAgentState(actingAgentInstanceId);
        runState.turn.selectedAgentDieIndex = -1;

        if (agent != null && agent.remainingDiceFaces != null && agent.remainingDiceFaces.Count > 0)
        {
            PhaseManager.Instance.SetPhase(TurnPhase.Adjustment);
            return;
        }

        if (agent != null)
            agent.actionConsumed = true;

        runState.turn.processingAgentInstanceId = string.Empty;

        if (GetPendingAgentCount() > 0)
        {
            PhaseManager.Instance.SetPhase(TurnPhase.AgentRoll);
            return;
        }

        PhaseManager.Instance.SetPhase(TurnPhase.Settlement);
        PhaseManager.Instance.AdvanceToDecisionPoint();
    }

    public int GetUnassignedAgentCount()
    {
        return GetPendingAgentCount();
    }

    public int GetPendingAgentCount()
    {
        if (runState?.agents == null)
            return 0;

        int count = 0;
        for (int i = 0; i < runState.agents.Count; i++)
        {
            var agent = runState.agents[i];
            if (agent == null)
                continue;
            if (agent.actionConsumed)
                continue;
            if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
                continue;

            count += 1;
        }

        return count;
    }

    public void MarkAllPendingAgentsConsumed()
    {
        if (runState?.agents == null)
            return;

        for (int i = 0; i < runState.agents.Count; i++)
        {
            var agent = runState.agents[i];
            if (agent == null)
                continue;
            if (agent.actionConsumed)
                continue;
            if (agent.remainingDiceFaces == null || agent.remainingDiceFaces.Count == 0)
                continue;

            agent.actionConsumed = true;
        }
    }

    public void ResetAgentsForNewTurn()
    {
        if (runState?.agents == null)
            return;

        for (int i = 0; i < runState.agents.Count; i++)
        {
            var agent = runState.agents[i];
            if (agent == null)
                continue;

            if (!agentDefById.TryGetValue(agent.agentDefId, out var agentDef) ||
                agentDef?.diceFaces == null)
            {
                agent.remainingDiceFaces = new List<int>();
                agent.actionConsumed = true;
                continue;
            }

            if (agent.remainingDiceFaces == null)
                agent.remainingDiceFaces = new List<int>(agentDef.diceFaces.Count);
            else
                agent.remainingDiceFaces.Clear();

            for (int dieIndex = 0; dieIndex < agentDef.diceFaces.Count; dieIndex++)
            {
                int face = Mathf.Max(2, agentDef.diceFaces[dieIndex]);
                agent.remainingDiceFaces.Add(face);
            }

            agent.actionConsumed = agent.remainingDiceFaces.Count == 0;
        }
    }

    public static bool IsValidAgentDieIndex(AgentState agent, int dieIndex)
    {
        if (agent?.remainingDiceFaces == null)
            return false;

        return dieIndex >= 0 && dieIndex < agent.remainingDiceFaces.Count;
    }

    void OnRunStarted(GameRunState state)
    {
        runState = state;
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RunStarted -= OnRunStarted;
            GameManager.Instance.RunEnded -= OnRunEnded;
            GameManager.Instance.RunStarted += OnRunStarted;
            GameManager.Instance.RunEnded += OnRunEnded;
        }

        if (PhaseManager.Instance != null)
        {
            PhaseManager.Instance.PhaseChanged -= OnPhaseChanged;
            PhaseManager.Instance.PhaseChanged += OnPhaseChanged;
        }
    }

    void UnsubscribeEvents()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RunStarted -= OnRunStarted;
            GameManager.Instance.RunEnded -= OnRunEnded;
        }

        if (PhaseManager.Instance != null)
            PhaseManager.Instance.PhaseChanged -= OnPhaseChanged;
    }

    bool CanAcceptAgentSelectionInput()
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return false;
        if (DuelManager.Instance.IsDuelResolutionPending)
            return false;
        if (!string.IsNullOrWhiteSpace(runState.turn.processingAgentInstanceId))
            return false;

        return PhaseManager.Instance.CurrentPhase == TurnPhase.AgentRoll;
    }

    bool CanAcceptAgentDieSelectionInput()
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return false;
        if (DuelManager.Instance.IsDuelResolutionPending)
            return false;
        if (PhaseManager.Instance.CurrentPhase != TurnPhase.Adjustment)
            return false;

        var agent = FindCurrentProcessingAgent();
        if (agent == null || agent.actionConsumed)
            return false;

        return agent.remainingDiceFaces != null && agent.remainingDiceFaces.Count > 0;
    }

    bool IsReady()
    {
        if (contentRoot == null)
            return false;
        if (runState == null || runState.agents == null)
            return false;

        return true;
    }

    void RebuildCardsIfNeeded(bool forceRebuild)
    {
        if (!IsReady())
            return;

        int desiredCount = runState.agents.Count;
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
        var root = Instantiate(cardPrefab, contentRoot, false);
        root.name = $"AgentCard_{slotIndex + 1}";

        var controller = root.GetComponent<AgentController>();
        controller.SetDicePrefab(dicePrefab);
        controller.BindAgent(string.Empty);
        controller.Render(
            $"A{slotIndex + 1}",
            "Unknown Agent",
            "Rules: -",
            "Dice Left: -",
            "Status: Waiting",
            $"Select [{slotIndex + 1}]",
            subtleLabelColor,
            pendingCardColor,
            null,
            0);
        return controller;
    }

    void RefreshAllCards()
    {
        if (!IsReady())
            return;
        if (cards.Count != runState.agents.Count)
            return;

        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var agent = runState.agents[index];
            if (card == null || agent == null)
                continue;

            RefreshCardVisual(card, agent, index);
        }
    }

    void RefreshCardVisual(AgentController card, AgentState agent, int slotIndex)
    {
        card.SetDicePrefab(dicePrefab);
        card.BindAgent(agent.instanceId);

        int diceCount = ResolveDiceCount(agent.agentDefId);
        Color statusColor = ResolveStatusColor(agent);
        Color backgroundColor;
        bool isProcessing = IsCurrentProcessingAgent(agent.instanceId);
        if (agent.actionConsumed)
            backgroundColor = consumedCardColor;
        else if (isProcessing)
            backgroundColor = selectedCardColor;
        else
            backgroundColor = pendingCardColor;

        card.Render(
            $"A{slotIndex + 1}",
            ResolveAgentName(agent.agentDefId),
            BuildInfoLine(agent.agentDefId),
            BuildDiceSummaryLine(agent),
            BuildStatusLine(agent),
            $"Select [{slotIndex + 1}]",
            statusColor,
            backgroundColor,
            agent.remainingDiceFaces,
            diceCount);
    }

    string BuildInfoLine(string agentDefId)
    {
        string ruleSummary = ResolveRuleSummary(agentDefId);
        return $"Rules: {ruleSummary}";
    }

    static string BuildDiceSummaryLine(AgentState agent)
    {
        int remaining = agent?.remainingDiceFaces?.Count ?? 0;
        return $"Dice Left: {remaining}";
    }

    string BuildStatusLine(AgentState agent)
    {
        if (agent == null)
            return string.Empty;
        if (agent.actionConsumed)
            return "Status: Exhausted";

        var phase = PhaseManager.Instance.CurrentPhase;
        bool isProcessing = IsCurrentProcessingAgent(agent.instanceId);
        if (isProcessing && phase == TurnPhase.Adjustment)
            return "Status: Pick your die";
        if (isProcessing && phase == TurnPhase.TargetAndAttack)
            return "Status: Pick situation die";
        if (CanRollAgent(agent.instanceId))
            return "Status: Ready";
        return "Status: Waiting";
    }

    Color ResolveStatusColor(AgentState agent)
    {
        if (agent == null)
            return subtleLabelColor;
        if (agent.actionConsumed)
            return subtleLabelColor;

        var phase = PhaseManager.Instance.CurrentPhase;
        bool isProcessing = IsCurrentProcessingAgent(agent.instanceId);
        if (isProcessing && (phase == TurnPhase.Adjustment || phase == TurnPhase.TargetAndAttack))
            return damageHighlightColor;
        if (agent.remainingDiceFaces != null && agent.remainingDiceFaces.Count > 0)
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

        return Mathf.Max(0, def.diceFaces?.Count ?? 0);
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
        var defs = GameStaticDataLoader.LoadAgentDefs();
        for (int index = 0; index < defs.Count; index++)
        {
            var def = defs[index];
            if (def == null || string.IsNullOrWhiteSpace(def.agentId))
                continue;

            agentDefById[def.agentId] = def;
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
