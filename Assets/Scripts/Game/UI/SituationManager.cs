using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SituationManager : MonoBehaviour
{
    [SerializeField] RectTransform contentRoot;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] GameObject dicePrefab;
    [SerializeField] Color cardColor = new(0.32f, 0.18f, 0.18f, 0.94f);
    [SerializeField] Color lowDiceCountCardColor = new(0.45f, 0.16f, 0.16f, 0.98f);
    [SerializeField] Color subtleLabelColor = new(0.88f, 0.78f, 0.76f, 1f);
    [SerializeField] Color requirementHighlightColor = new(1.00f, 0.92f, 0.32f, 1.00f);
    [SerializeField] Color successHighlightColor = new(0.68f, 0.96f, 0.74f, 1.00f);
    [SerializeField] Color failureHighlightColor = new(1.00f, 0.66f, 0.66f, 1.00f);

    readonly List<SituationController> cards = new();
    readonly Dictionary<string, SituationDef> situationDefById = new(StringComparer.Ordinal);

    GameStaticDataSet staticData;
    GameRunState runState;
    bool loadedDefs;

    public static SituationManager Instance { get; private set; }

    public event Action<int, string> StageSpawned;

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
        LoadSituationDefsIfNeeded();
    }

    void Start()
    {
        SubscribeEvents();
        staticData = GameManager.Instance != null ? GameManager.Instance.StaticData : staticData;
        runState = GameManager.Instance != null ? GameManager.Instance.CurrentRunState : runState;
        RebuildCardsIfNeeded(forceRebuild: true);
        RefreshAllCards();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();

        if (Instance == this)
            Instance = null;
    }

    public void InitializeForRun(
        GameRunState state,
        GameStaticDataSet dataSet,
        IReadOnlyDictionary<string, SituationDef> defs)
    {
        runState = state;
        staticData = dataSet;
        situationDefById.Clear();
        if (defs != null)
        {
            foreach (var pair in defs)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null)
                    continue;

                situationDefById[pair.Key] = pair.Value;
            }
        }
        else
        {
            LoadSituationDefsIfNeeded();
        }

        RebuildCardsIfNeeded(forceRebuild: true);
        RefreshAllCards();
    }

    public void NotifyStageSpawned(int stageNumber, string presetId)
    {
        StageSpawned?.Invoke(stageNumber, presetId ?? string.Empty);
    }

    public bool TrySpawnPeriodicSituations()
    {
        if (runState == null || staticData == null)
            return false;

        bool spawned = GameRunBootstrap.TrySpawnPeriodicSituations(
            runState,
            staticData,
            GameManager.Instance.Rng);
        if (spawned)
            NotifyStageSpawned(runState.stage.stageNumber, runState.stage.activePresetId);

        return spawned;
    }

    public bool TryTestAgainstSituationDie(string situationInstanceId, int situationDieIndex)
    {
        if (!CanAcceptDuelTargetingInput())
            return false;
        if (string.IsNullOrWhiteSpace(situationInstanceId))
            return false;

        var agent = AgentManager.Instance.FindCurrentProcessingAgent();
        if (agent == null)
            return false;

        int selectedAgentDieIndex = runState.turn.selectedAgentDieIndex;
        if (!AgentManager.IsValidAgentDieIndex(agent, selectedAgentDieIndex))
            return false;

        var situation = FindSituationState(situationInstanceId);
        if (!IsValidSituationDieIndex(situation, situationDieIndex))
            return false;

        int agentDieFace = Mathf.Max(1, agent.remainingDiceFaces[selectedAgentDieIndex]);
        int situationDieFace = Mathf.Max(1, situation.remainingDiceFaces[situationDieIndex]);

        return DuelManager.Instance.BeginDuel(
            agent.instanceId,
            selectedAgentDieIndex,
            agentDieFace,
            situation.instanceId,
            situationDieIndex,
            situationDieFace);
    }

    public bool RemoveSituationDie(string situationInstanceId, int situationDieIndex)
    {
        var situation = FindSituationState(situationInstanceId);
        if (!IsValidSituationDieIndex(situation, situationDieIndex))
            return false;

        situation.remainingDiceFaces.RemoveAt(situationDieIndex);
        return true;
    }

    public bool ReduceSituationDie(string situationInstanceId, int situationDieIndex, int amount)
    {
        var situation = FindSituationState(situationInstanceId);
        if (!IsValidSituationDieIndex(situation, situationDieIndex))
            return false;

        int currentFace = situation.remainingDiceFaces[situationDieIndex];
        int reducedFace = currentFace - Mathf.Max(0, amount);
        if (reducedFace <= 0)
            situation.remainingDiceFaces.RemoveAt(situationDieIndex);
        else
            situation.remainingDiceFaces[situationDieIndex] = reducedFace;

        return true;
    }

    public int GetRemainingDieCount(string situationInstanceId)
    {
        var situation = FindSituationState(situationInstanceId);
        if (situation?.remainingDiceFaces == null)
            return 0;

        return situation.remainingDiceFaces.Count;
    }

    public SituationState FindSituationState(string situationInstanceId)
    {
        if (runState?.situations == null)
            return null;

        for (int i = 0; i < runState.situations.Count; i++)
        {
            var situation = runState.situations[i];
            if (situation == null)
                continue;
            if (!string.Equals(situation.instanceId, situationInstanceId, StringComparison.Ordinal))
                continue;

            return situation;
        }

        return null;
    }

    public void HandleSituationSucceeded(string situationInstanceId)
    {
        var situation = FindSituationState(situationInstanceId);
        if (situation == null)
            return;

        situationDefById.TryGetValue(situation.situationDefId, out var situationDef);
        RemoveSituationState(situation.instanceId);

        if (situationDef != null)
            PlayerManager.Instance.ApplyEffectBundle(situationDef.successReward);
    }

    public void ProcessSettlement()
    {
        if (runState?.situations == null)
            return;

        var situationOrder = new List<string>(runState.situations.Count);
        for (int i = 0; i < runState.situations.Count; i++)
        {
            var situation = runState.situations[i];
            if (situation == null)
                continue;

            situationOrder.Add(situation.instanceId);
        }

        for (int i = 0; i < situationOrder.Count; i++)
        {
            string situationInstanceId = situationOrder[i];
            var situation = FindSituationState(situationInstanceId);
            if (situation == null)
                continue;
            if (situation.remainingDiceFaces == null || situation.remainingDiceFaces.Count == 0)
                continue;

            situation.deadlineTurnsLeft -= 1;
            if (situation.deadlineTurnsLeft > 0)
                continue;

            if (!situationDefById.TryGetValue(situation.situationDefId, out var situationDef))
            {
                RemoveSituationState(situation.instanceId);
                continue;
            }

            PlayerManager.Instance.ApplyEffectBundle(situationDef.failureEffect);

            situation = FindSituationState(situationInstanceId);
            if (situation == null)
                continue;

            HandleSituationFailurePostEffect(situation, situationDef);
        }
    }

    public static bool IsValidSituationDieIndex(SituationState situation, int dieIndex)
    {
        if (situation?.remainingDiceFaces == null)
            return false;

        return dieIndex >= 0 && dieIndex < situation.remainingDiceFaces.Count;
    }

    void HandleSituationFailurePostEffect(SituationState situation, SituationDef situationDef)
    {
        if (situation == null)
            return;

        string mode = NormalizeFailurePersistMode(situationDef?.failurePersistMode);
        if (string.Equals(mode, "resetDeadline", StringComparison.Ordinal))
        {
            int baseDeadline = situationDef != null
                ? Mathf.Max(1, situationDef.baseDeadlineTurns)
                : 1;
            situation.deadlineTurnsLeft = baseDeadline;
            return;
        }

        RemoveSituationState(situation.instanceId);
    }

    void RemoveSituationState(string situationInstanceId)
    {
        if (runState?.situations == null)
            return;

        for (int i = runState.situations.Count - 1; i >= 0; i--)
        {
            var situation = runState.situations[i];
            if (situation == null)
                continue;
            if (!string.Equals(situation.instanceId, situationInstanceId, StringComparison.Ordinal))
                continue;

            runState.situations.RemoveAt(i);
            break;
        }
    }

    bool CanAcceptDuelTargetingInput()
    {
        if (runState == null || GameManager.Instance.IsRunOver)
            return false;
        if (DuelManager.Instance.IsDuelResolutionPending)
            return false;
        if (PhaseManager.Instance.CurrentPhase != TurnPhase.TargetAndAttack)
            return false;

        var agent = AgentManager.Instance.FindCurrentProcessingAgent();
        if (agent == null || agent.actionConsumed)
            return false;

        return AgentManager.IsValidAgentDieIndex(agent, runState.turn.selectedAgentDieIndex);
    }

    static string NormalizeFailurePersistMode(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "remove";

        string mode = raw.Trim();
        if (string.Equals(mode, "remove", StringComparison.OrdinalIgnoreCase))
            return "remove";
        if (string.Equals(mode, "resetDeadline", StringComparison.OrdinalIgnoreCase))
            return "resetDeadline";
        return "remove";
    }

    void OnRunStarted(GameRunState state)
    {
        runState = state;
        staticData = GameManager.Instance.StaticData;
        RebuildCardsIfNeeded(forceRebuild: false);
        RefreshAllCards();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RebuildCardsIfNeeded(forceRebuild: false);
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

    bool IsReady()
    {
        if (contentRoot == null)
            return false;
        if (runState == null || runState.situations == null)
            return false;

        return true;
    }

    void RebuildCardsIfNeeded(bool forceRebuild)
    {
        if (!IsReady())
            return;

        int desiredCount = runState.situations.Count;
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

    SituationController CreateCard(int slotIndex)
    {
        var root = Instantiate(cardPrefab, contentRoot, false);
        root.name = $"SituationCard_{slotIndex + 1}";

        var controller = root.GetComponent<SituationController>();
        controller.BindSituation(string.Empty);
        controller.SetDicePrefab(dicePrefab);
        controller.Render(
            $"S{slotIndex + 1}",
            "Unknown Situation",
            "Dice -",
            "Success: -",
            "Failure: -",
            "D -",
            "Select target",
            cardColor,
            requirementHighlightColor,
            successHighlightColor,
            failureHighlightColor,
            subtleLabelColor,
            null,
            0);

        return controller;
    }

    void RefreshAllCards()
    {
        if (!IsReady())
            return;
        if (cards.Count != runState.situations.Count)
            return;

        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var situation = runState.situations[index];
            if (card == null || situation == null)
                continue;

            RefreshCardVisual(card, situation, index);
        }
    }

    void RefreshCardVisual(SituationController card, SituationState situation, int slotIndex)
    {
        card.BindSituation(situation.instanceId);
        card.SetDicePrefab(dicePrefab);

        int remainingCount = situation?.remainingDiceFaces?.Count ?? 0;
        int totalDiceCount = ResolveSituationDiceCount(situation?.situationDefId);
        if (totalDiceCount <= 0)
            totalDiceCount = remainingCount;
        Color backgroundColor = remainingCount <= 1 ? lowDiceCountCardColor : cardColor;

        card.Render(
            $"S{slotIndex + 1}",
            ResolveSituationName(situation.situationDefId),
            BuildRequirementLine(situation),
            BuildSuccessLine(situation),
            BuildFailureLine(situation),
            $"D {Mathf.Max(0, situation.deadlineTurnsLeft)}",
            BuildTargetHintLine(),
            backgroundColor,
            requirementHighlightColor,
            successHighlightColor,
            failureHighlightColor,
            subtleLabelColor,
            situation.remainingDiceFaces,
            totalDiceCount);
    }

    static string BuildRequirementLine(SituationState situation)
    {
        int remaining = situation?.remainingDiceFaces?.Count ?? 0;
        return $"Dice {Mathf.Max(0, remaining)}";
    }

    string BuildSuccessLine(SituationState situation)
    {
        if (situation == null)
            return "Success: -";
        if (!situationDefById.TryGetValue(situation.situationDefId, out var def))
            return "Success: -";

        return $"Success: {BuildEffectBundleSummary(def.successReward)}";
    }

    string BuildFailureLine(SituationState situation)
    {
        if (situation == null)
            return "Failure: -";
        if (!situationDefById.TryGetValue(situation.situationDefId, out var def))
            return "Failure: -";

        return $"Failure: {BuildEffectBundleSummary(def.failureEffect)}";
    }

    string BuildEffectBundleSummary(EffectBundle bundle)
    {
        if (bundle?.effects == null || bundle.effects.Count == 0)
            return "-";

        var tokens = new List<string>(bundle.effects.Count);
        for (int index = 0; index < bundle.effects.Count; index++)
        {
            string token = ToEffectSummary(bundle.effects[index]);
            if (string.IsNullOrWhiteSpace(token))
                continue;

            tokens.Add(token);
        }

        if (tokens.Count == 0)
            return "-";
        return string.Join(", ", tokens);
    }

    string BuildTargetHintLine()
    {
        if (runState == null)
            return "Select target";

        return PhaseManager.Instance.CurrentPhase == TurnPhase.TargetAndAttack
            ? "Choose situation die"
            : "Waiting";
    }

    string ResolveSituationName(string situationDefId)
    {
        if (!string.IsNullOrWhiteSpace(situationDefId) &&
            situationDefById.TryGetValue(situationDefId, out var def))
        {
            if (!string.IsNullOrWhiteSpace(def.situationId))
                return ToDisplayTitle(def.situationId);
            if (!string.IsNullOrWhiteSpace(def.nameKey))
                return ToDisplayTitle(def.nameKey);
        }

        if (string.IsNullOrWhiteSpace(situationDefId))
            return "Unknown Situation";
        return ToDisplayTitle(situationDefId);
    }

    int ResolveSituationDiceCount(string situationDefId)
    {
        if (string.IsNullOrWhiteSpace(situationDefId))
            return 0;
        if (!situationDefById.TryGetValue(situationDefId, out var def))
            return 0;

        return Mathf.Max(0, def.diceFaces?.Count ?? 0);
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
                "[SituationManager] contentRoot requires a LayoutGroup configured in the editor.");
        }

        if (contentRoot.GetComponent<RectMask2D>() == null)
        {
            Debug.LogWarning(
                "[SituationManager] contentRoot requires RectMask2D configured in the editor.");
        }
    }

    void LoadSituationDefsIfNeeded()
    {
        if (loadedDefs)
            return;
        loadedDefs = true;

        situationDefById.Clear();
        var defs = GameStaticDataLoader.LoadSituationDefs();
        for (int index = 0; index < defs.Count; index++)
        {
            var def = defs[index];
            if (def == null || string.IsNullOrWhiteSpace(def.situationId))
                continue;

            situationDefById[def.situationId] = def;
        }
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
            "stabilityDelta" => $"Stability {FormatSignedValue(value)}",
            "goldDelta" => $"Gold {FormatSignedValue(value)}",
            "situationRequirementDelta" => $"Req {FormatSignedValue(value)}",
            "dieFaceDelta" => $"Die {FormatSignedValue(value)}",
            "rerollAgentDice" => "Reroll Agent Dice",
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
        if (normalized.StartsWith("enemy_", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("enemy_".Length);
        if (normalized.StartsWith("situation_", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("situation_".Length);

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
