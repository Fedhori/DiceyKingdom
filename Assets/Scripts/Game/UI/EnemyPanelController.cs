using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EnemyPanelController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] float cardHeight = 192f;
    [SerializeField] Color cardColor = new(0.32f, 0.18f, 0.18f, 0.94f);
    [SerializeField] Color lowRequirementCardColor = new(0.45f, 0.16f, 0.16f, 0.98f);
    [SerializeField] Color labelColor = new(0.98f, 0.94f, 0.93f, 1f);
    [SerializeField] Color subtleLabelColor = new(0.88f, 0.78f, 0.76f, 1f);
    [SerializeField] Color deadlineBadgeColor = new(0.94f, 0.72f, 0.30f, 0.95f);
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
        EnsureContentLayout();
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
            $"EnemyCard_{slotIndex + 1}",
            typeof(RectTransform),
            typeof(Image),
            typeof(LayoutElement));
        cardObject.layer = LayerMask.NameToLayer("UI");

        var cardRect = cardObject.GetComponent<RectTransform>();
        cardRect.SetParent(contentRoot, false);

        var cardImage = cardObject.GetComponent<Image>();
        cardImage.color = cardColor;
        cardImage.raycastTarget = true;

        var layout = cardObject.GetComponent<LayoutElement>();
        layout.preferredHeight = cardHeight;
        layout.flexibleWidth = 1f;

        var dropTarget = cardObject.AddComponent<EnemyDropTarget>();
        dropTarget.SetOrchestrator(orchestrator);

        var headerRect = CreateUiContainer("Header", cardRect);
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.anchoredPosition = new Vector2(0f, -8f);
        headerRect.sizeDelta = new Vector2(-16f, 30f);

        var slotText = CreateLabel("SlotText", headerRect, 19f, FontStyles.Bold, TextAlignmentOptions.Left, subtleLabelColor);
        slotText.rectTransform.anchorMin = new Vector2(0f, 0f);
        slotText.rectTransform.anchorMax = new Vector2(0f, 1f);
        slotText.rectTransform.pivot = new Vector2(0f, 0.5f);
        slotText.rectTransform.anchoredPosition = Vector2.zero;
        slotText.rectTransform.sizeDelta = new Vector2(44f, 0f);

        var nameText = CreateLabel("NameText", headerRect, 26f, FontStyles.Bold, TextAlignmentOptions.Left, labelColor);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameText.rectTransform.pivot = new Vector2(0f, 0.5f);
        nameText.rectTransform.offsetMin = new Vector2(50f, 0f);
        nameText.rectTransform.offsetMax = new Vector2(-10f, 0f);

        var hpText = CreateLabel("HpText", cardRect, 31f, FontStyles.Bold, TextAlignmentOptions.TopLeft, requirementHighlightColor);
        hpText.rectTransform.anchorMin = new Vector2(0f, 1f);
        hpText.rectTransform.anchorMax = new Vector2(1f, 1f);
        hpText.rectTransform.pivot = new Vector2(0.5f, 1f);
        hpText.rectTransform.anchoredPosition = new Vector2(0f, -46f);
        hpText.rectTransform.sizeDelta = new Vector2(-24f, 36f);

        var actionText = CreateLabel("ActionText", cardRect, 20f, FontStyles.Bold, TextAlignmentOptions.TopLeft, successHighlightColor);
        actionText.rectTransform.anchorMin = new Vector2(0f, 1f);
        actionText.rectTransform.anchorMax = new Vector2(1f, 1f);
        actionText.rectTransform.pivot = new Vector2(0.5f, 1f);
        actionText.rectTransform.anchoredPosition = new Vector2(0f, -86f);
        actionText.rectTransform.sizeDelta = new Vector2(-24f, 30f);

        var actionEffectText = CreateLabel("ActionEffectText", cardRect, 20f, FontStyles.Bold, TextAlignmentOptions.TopLeft, failureHighlightColor);
        actionEffectText.rectTransform.anchorMin = new Vector2(0f, 1f);
        actionEffectText.rectTransform.anchorMax = new Vector2(1f, 1f);
        actionEffectText.rectTransform.pivot = new Vector2(0.5f, 1f);
        actionEffectText.rectTransform.anchoredPosition = new Vector2(0f, -116f);
        actionEffectText.rectTransform.sizeDelta = new Vector2(-24f, 30f);

        var prepBadgeObject = new GameObject("PrepBadge", typeof(RectTransform), typeof(Image));
        prepBadgeObject.layer = LayerMask.NameToLayer("UI");
        var prepBadgeRect = prepBadgeObject.GetComponent<RectTransform>();
        prepBadgeRect.SetParent(cardRect, false);
        prepBadgeRect.anchorMin = new Vector2(1f, 0f);
        prepBadgeRect.anchorMax = new Vector2(1f, 0f);
        prepBadgeRect.pivot = new Vector2(1f, 0f);
        prepBadgeRect.anchoredPosition = new Vector2(-10f, 10f);
        prepBadgeRect.sizeDelta = new Vector2(138f, 36f);

        var prepBadgeImage = prepBadgeObject.GetComponent<Image>();
        prepBadgeImage.color = deadlineBadgeColor;
        prepBadgeImage.raycastTarget = false;

        var prepText = CreateLabel("PrepText", prepBadgeRect, 21f, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        prepText.rectTransform.anchorMin = Vector2.zero;
        prepText.rectTransform.anchorMax = Vector2.one;
        prepText.rectTransform.offsetMin = Vector2.zero;
        prepText.rectTransform.offsetMax = Vector2.zero;

        var targetHintText = CreateLabel("TargetHintText", cardRect, 18f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, subtleLabelColor);
        targetHintText.rectTransform.anchorMin = new Vector2(0f, 0f);
        targetHintText.rectTransform.anchorMax = new Vector2(1f, 0f);
        targetHintText.rectTransform.pivot = new Vector2(0.5f, 0f);
        targetHintText.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        targetHintText.rectTransform.sizeDelta = new Vector2(-24f, 26f);

        slotText.text = $"S{slotIndex + 1}";
        prepText.text = "D -";
        targetHintText.text = "Drop target";

        return new CardWidgets
        {
            root = cardRect,
            background = cardImage,
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
        card.dropTarget.SetSituationInstanceId(situation.instanceId);
        card.dropTarget.SetOrchestrator(orchestrator);
    }

    void RefreshCardVisual(CardWidgets card, SituationState situation, int slotIndex)
    {
        card.slotText.text = $"S{slotIndex + 1}";
        card.nameText.text = ResolveSituationName(situation.situationDefId);
        card.hpText.text = BuildRequirementLine(situation);
        card.actionText.text = BuildSuccessLine(situation);
        card.actionEffectText.text = BuildFailureLine(situation);
        card.prepText.text = $"D {Mathf.Max(0, situation.deadlineTurnsLeft)}";
        card.targetHintText.text = BuildTargetHintLine();
        card.hpText.color = requirementHighlightColor;
        card.actionText.color = successHighlightColor;
        card.actionEffectText.color = failureHighlightColor;

        int baseRequirement = ResolveBaseRequirement(situation.situationDefId);
        bool isLowRequirement = baseRequirement > 0 &&
                                situation.currentRequirement <= Mathf.Max(1, baseRequirement / 2);
        card.background.color = isLowRequirement ? lowRequirementCardColor : cardColor;
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
            ? (int)Mathf.Round((float)effect.value.Value)
            : 0;

        return effect.effectType switch
        {
            "stability_delta" => $"Stability {FormatSignedValue(value)}",
            "gold_delta" => $"Gold {FormatSignedValue(value)}",
            "situation_requirement_delta" => $"Req {FormatSignedValue(value)}",
            "die_face_delta" => $"Die {FormatSignedValue(value)}",
            "reroll_adventurer_dice" => "Reroll Adventurer Dice",
            _ => $"{ToDisplayTitle(effect.effectType)} {FormatSignedValue(value)}"
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
