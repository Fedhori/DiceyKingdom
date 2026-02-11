using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class SituationManager : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] GameObject cardPrefab;
    [SerializeField] Color cardColor = new(0.32f, 0.18f, 0.18f, 0.94f);
    [SerializeField] Color lowRequirementCardColor = new(0.45f, 0.16f, 0.16f, 0.98f);
    [SerializeField] Color subtleLabelColor = new(0.88f, 0.78f, 0.76f, 1f);
    [SerializeField] Color requirementHighlightColor = new(1.00f, 0.92f, 0.32f, 1.00f);
    [SerializeField] Color successHighlightColor = new(0.68f, 0.96f, 0.74f, 1.00f);
    [SerializeField] Color failureHighlightColor = new(1.00f, 0.66f, 0.66f, 1.00f);

    readonly List<SituationController> cards = new();
    readonly Dictionary<string, SituationDef> situationDefById = new(StringComparer.Ordinal);
    bool loadedDefs;

    void Awake()
    {
        TryResolveOrchestrator();
        TryResolveContentRoot();
        ValidateEditorLayoutSetup();
        LoadSituationDefsIfNeeded();
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

    void OnStageSpawned(int _, string __)
    {
        RebuildCardsIfNeeded(forceRebuild: false);
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

    void OnTargetingSessionChanged()
    {
        RefreshAllCards();
    }

    void SubscribeEvents()
    {
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.StageSpawned -= OnStageSpawned;
        orchestrator.RunEnded -= OnRunEnded;
        orchestrator.StateChanged -= OnStateChanged;
        SkillTargetingSession.SessionChanged -= OnTargetingSessionChanged;

        orchestrator.RunStarted += OnRunStarted;
        orchestrator.PhaseChanged += OnPhaseChanged;
        orchestrator.StageSpawned += OnStageSpawned;
        orchestrator.RunEnded += OnRunEnded;
        orchestrator.StateChanged += OnStateChanged;
        SkillTargetingSession.SessionChanged += OnTargetingSessionChanged;
    }

    void UnsubscribeEvents()
    {
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.StageSpawned -= OnStageSpawned;
        orchestrator.RunEnded -= OnRunEnded;
        orchestrator.StateChanged -= OnStateChanged;
        SkillTargetingSession.SessionChanged -= OnTargetingSessionChanged;
    }

    bool IsReady()
    {
        if (orchestrator == null)
            return false;
        if (contentRoot == null)
            return false;
        if (orchestrator.RunState == null || orchestrator.RunState.situations == null)
            return false;

        return true;
    }

    void RebuildCardsIfNeeded(bool forceRebuild)
    {
        if (!IsReady())
            return;

        int desiredCount = orchestrator.RunState.situations.Count;
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
        if (cardPrefab == null)
        {
            Debug.LogWarning("[SituationManager] cardPrefab is not assigned.");
            return null;
        }

        var root = Instantiate(cardPrefab, contentRoot, false);
        root.name = $"SituationCard_{slotIndex + 1}";

        var controller = root.GetComponent<SituationController>();
        if (controller == null)
        {
            Debug.LogWarning("[SituationManager] cardPrefab requires SituationController.", root);
            if (Application.isPlaying)
                Destroy(root);
            else
                DestroyImmediate(root);
            return null;
        }

        controller.BindOrchestrator(orchestrator);
        controller.BindSituation(string.Empty);
        controller.Render(
            $"S{slotIndex + 1}",
            "Unknown Situation",
            "REQ -",
            "Success: -",
            "Failure: -",
            "D -",
            "Drop target",
            cardColor,
            requirementHighlightColor,
            successHighlightColor,
            failureHighlightColor,
            subtleLabelColor);

        return controller;
    }

    void RefreshAllCards()
    {
        if (!IsReady())
            return;
        if (cards.Count != orchestrator.RunState.situations.Count)
            return;

        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var situation = orchestrator.RunState.situations[index];
            if (card == null || situation == null)
                continue;

            RefreshCardVisual(card, situation, index);
        }
    }

    void RefreshCardVisual(SituationController card, SituationState situation, int slotIndex)
    {
        card.BindOrchestrator(orchestrator);
        card.BindSituation(situation.instanceId);

        int baseRequirement = ResolveBaseRequirement(situation.situationDefId);
        bool isLowRequirement = baseRequirement > 0 &&
                                situation.currentRequirement <= Mathf.Max(1, baseRequirement / 2);
        Color backgroundColor = isLowRequirement ? lowRequirementCardColor : cardColor;

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
            subtleLabelColor);
    }

    string BuildRequirementLine(SituationState situation)
    {
        int baseRequirement = ResolveBaseRequirement(situation?.situationDefId);
        if (baseRequirement <= 0)
            return $"REQ {Mathf.Max(0, situation?.currentRequirement ?? 0)}";

        return $"REQ {Mathf.Max(0, situation.currentRequirement)} / {baseRequirement}";
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
        if (orchestrator?.RunState == null)
            return "Drop target";
        if (SkillTargetingSession.IsFor(orchestrator))
            return "Click to cast";

        return orchestrator.RunState.turn.phase == TurnPhase.TargetAndAttack
            ? "Drop to attack"
            : "Drop target";
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

    int ResolveBaseRequirement(string situationDefId)
    {
        if (string.IsNullOrWhiteSpace(situationDefId))
            return 0;
        if (!situationDefById.TryGetValue(situationDefId, out var def))
            return 0;

        return Mathf.Max(1, def.baseRequirement);
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

        try
        {
            var defs = GameStaticDataLoader.LoadSituationDefs();
            for (int index = 0; index < defs.Count; index++)
            {
                var def = defs[index];
                if (def == null || string.IsNullOrWhiteSpace(def.situationId))
                    continue;

                situationDefById[def.situationId] = def;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[SituationManager] Failed to load situation defs: {exception.Message}");
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
            "stability_delta" => $"Stability {FormatSignedValue(value)}",
            "gold_delta" => $"Gold {FormatSignedValue(value)}",
            "situation_requirement_delta" => $"Req {FormatSignedValue(value)}",
            "die_face_delta" => $"Die {FormatSignedValue(value)}",
            "reroll_adventurer_dice" => "Reroll Adventurer Dice",
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
