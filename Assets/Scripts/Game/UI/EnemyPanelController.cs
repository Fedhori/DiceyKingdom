using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EnemyPanelController : MonoBehaviour
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

    readonly List<CardWidgets> cards = new();
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

    void OnStageSpawned(int _, string __)
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
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.StageSpawned -= OnStageSpawned;
        orchestrator.RunEnded -= OnRunEnded;

        orchestrator.RunStarted += OnRunStarted;
        orchestrator.PhaseChanged += OnPhaseChanged;
        orchestrator.StageSpawned += OnStageSpawned;
        orchestrator.RunEnded += OnRunEnded;
    }

    void UnsubscribeEvents()
    {
        if (orchestrator == null)
            return;

        orchestrator.RunStarted -= OnRunStarted;
        orchestrator.PhaseChanged -= OnPhaseChanged;
        orchestrator.StageSpawned -= OnStageSpawned;
        orchestrator.RunEnded -= OnRunEnded;
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
            Debug.LogWarning("[EnemyPanelController] cardPrefab is not assigned.");
            return null;
        }

        var root = Instantiate(cardPrefab);
        root.name = $"SituationCard_{slotIndex + 1}";
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
        background.color = cardColor;

        var dropTarget = root.GetComponent<EnemyDropTarget>();
        if (dropTarget == null)
            dropTarget = root.AddComponent<EnemyDropTarget>();
        dropTarget.SetOrchestrator(orchestrator);

        var slotText = FindTextByName(cardRect, "SlotText");
        var nameText = FindTextByName(cardRect, "NameText");
        var hpText = FindTextByName(cardRect, "HpText");
        var actionText = FindTextByName(cardRect, "ActionText");
        var actionEffectText = FindTextByName(cardRect, "ActionEffectText");
        var prepText = FindTextByName(cardRect, "PrepText");
        var targetHintText = FindTextByName(cardRect, "TargetHintText");

        if (slotText != null)
            slotText.text = $"S{slotIndex + 1}";
        if (prepText != null)
            prepText.text = "D -";
        if (targetHintText != null)
            targetHintText.text = "Drop target";

        return new CardWidgets
        {
            root = cardRect,
            background = background,
            slotText = slotText,
            nameText = nameText,
            hpText = hpText,
            actionText = actionText,
            actionEffectText = actionEffectText,
            prepText = prepText,
            targetHintText = targetHintText,
            dropTarget = dropTarget
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
        if (cards.Count != orchestrator.RunState.situations.Count)
            return;

        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var situation = orchestrator.RunState.situations[index];
            if (card == null || situation == null)
                continue;

            BindCardIdentity(card, situation);
            RefreshCardVisual(card, situation, index);
        }
    }

    void BindCardIdentity(CardWidgets card, SituationState situation)
    {
        if (card.situationInstanceId == situation.instanceId)
            return;

        card.situationInstanceId = situation.instanceId;
        card.dropTarget?.SetSituationInstanceId(situation.instanceId);
        card.dropTarget?.SetOrchestrator(orchestrator);
    }

    void RefreshCardVisual(CardWidgets card, SituationState situation, int slotIndex)
    {
        if (card.slotText != null)
            card.slotText.text = $"S{slotIndex + 1}";
        if (card.nameText != null)
            card.nameText.text = ResolveSituationName(situation.situationDefId);
        if (card.hpText != null)
            card.hpText.text = BuildRequirementLine(situation);
        if (card.actionText != null)
            card.actionText.text = BuildSuccessLine(situation);
        if (card.actionEffectText != null)
            card.actionEffectText.text = BuildFailureLine(situation);
        if (card.prepText != null)
            card.prepText.text = $"D {Mathf.Max(0, situation.deadlineTurnsLeft)}";
        if (card.targetHintText != null)
            card.targetHintText.text = BuildTargetHintLine();

        if (card.hpText != null)
            card.hpText.color = requirementHighlightColor;
        if (card.actionText != null)
            card.actionText.color = successHighlightColor;
        if (card.actionEffectText != null)
            card.actionEffectText.color = failureHighlightColor;
        if (card.targetHintText != null)
            card.targetHintText.color = subtleLabelColor;

        if (card.background != null)
        {
            int baseRequirement = ResolveBaseRequirement(situation.situationDefId);
            bool isLowRequirement = baseRequirement > 0 &&
                                    situation.currentRequirement <= Mathf.Max(1, baseRequirement / 2);
            card.background.color = isLowRequirement ? lowRequirementCardColor : cardColor;
        }
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
                "[EnemyPanelController] contentRoot requires a LayoutGroup configured in the editor.");
        }

        if (contentRoot.GetComponent<RectMask2D>() == null)
        {
            Debug.LogWarning(
                "[EnemyPanelController] contentRoot requires RectMask2D configured in the editor.");
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
            Debug.LogWarning($"[EnemyPanelController] Failed to load situation defs: {exception.Message}");
        }
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

    sealed class CardWidgets
    {
        public RectTransform root;
        public string situationInstanceId = string.Empty;
        public Image background;
        public TextMeshProUGUI slotText;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI hpText;
        public TextMeshProUGUI actionText;
        public TextMeshProUGUI actionEffectText;
        public TextMeshProUGUI prepText;
        public TextMeshProUGUI targetHintText;
        public EnemyDropTarget dropTarget;
    }
}
