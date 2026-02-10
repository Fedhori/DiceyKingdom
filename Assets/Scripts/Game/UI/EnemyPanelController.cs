using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EnemyPanelController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] float cardHeight = 170f;
    [SerializeField] Color cardColor = new(0.32f, 0.18f, 0.18f, 0.94f);
    [SerializeField] Color lowHealthCardColor = new(0.45f, 0.16f, 0.16f, 0.98f);
    [SerializeField] Color labelColor = new(0.98f, 0.94f, 0.93f, 1f);
    [SerializeField] Color subtleLabelColor = new(0.88f, 0.78f, 0.76f, 1f);
    [SerializeField] Color prepBadgeColor = new(0.94f, 0.72f, 0.30f, 0.95f);

    readonly List<CardWidgets> cards = new();
    readonly Dictionary<string, EnemyDef> enemyDefById = new(StringComparer.Ordinal);
    bool loadedDefs;

    void Awake()
    {
        TryResolveOrchestrator();
        TryResolveContentRoot();
        EnsureContentLayout();
        LoadEnemyDefsIfNeeded();
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
        if (orchestrator.RunState == null || orchestrator.RunState.enemies == null)
            return false;

        return true;
    }

    void RebuildCardsIfNeeded(bool forceRebuild)
    {
        if (!IsReady())
            return;

        int desiredCount = orchestrator.RunState.enemies.Count;
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

        var nameText = CreateLabel("NameText", headerRect, 24f, FontStyles.Bold, TextAlignmentOptions.Left, labelColor);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameText.rectTransform.pivot = new Vector2(0f, 0.5f);
        nameText.rectTransform.offsetMin = new Vector2(50f, 0f);
        nameText.rectTransform.offsetMax = new Vector2(-10f, 0f);

        var hpText = CreateLabel("HpText", cardRect, 24f, FontStyles.Bold, TextAlignmentOptions.TopLeft, labelColor);
        hpText.rectTransform.anchorMin = new Vector2(0f, 1f);
        hpText.rectTransform.anchorMax = new Vector2(1f, 1f);
        hpText.rectTransform.pivot = new Vector2(0.5f, 1f);
        hpText.rectTransform.anchoredPosition = new Vector2(0f, -46f);
        hpText.rectTransform.sizeDelta = new Vector2(-24f, 28f);

        var actionText = CreateLabel("ActionText", cardRect, 20f, FontStyles.Normal, TextAlignmentOptions.TopLeft, labelColor);
        actionText.rectTransform.anchorMin = new Vector2(0f, 1f);
        actionText.rectTransform.anchorMax = new Vector2(1f, 1f);
        actionText.rectTransform.pivot = new Vector2(0.5f, 1f);
        actionText.rectTransform.anchoredPosition = new Vector2(0f, -78f);
        actionText.rectTransform.sizeDelta = new Vector2(-24f, 28f);

        var actionEffectText = CreateLabel("ActionEffectText", cardRect, 18f, FontStyles.Normal, TextAlignmentOptions.TopLeft, subtleLabelColor);
        actionEffectText.rectTransform.anchorMin = new Vector2(0f, 1f);
        actionEffectText.rectTransform.anchorMax = new Vector2(1f, 1f);
        actionEffectText.rectTransform.pivot = new Vector2(0.5f, 1f);
        actionEffectText.rectTransform.anchoredPosition = new Vector2(0f, -104f);
        actionEffectText.rectTransform.sizeDelta = new Vector2(-24f, 28f);

        var prepBadgeObject = new GameObject("PrepBadge", typeof(RectTransform), typeof(Image));
        prepBadgeObject.layer = LayerMask.NameToLayer("UI");
        var prepBadgeRect = prepBadgeObject.GetComponent<RectTransform>();
        prepBadgeRect.SetParent(cardRect, false);
        prepBadgeRect.anchorMin = new Vector2(1f, 0f);
        prepBadgeRect.anchorMax = new Vector2(1f, 0f);
        prepBadgeRect.pivot = new Vector2(1f, 0f);
        prepBadgeRect.anchoredPosition = new Vector2(-10f, 10f);
        prepBadgeRect.sizeDelta = new Vector2(126f, 32f);

        var prepBadgeImage = prepBadgeObject.GetComponent<Image>();
        prepBadgeImage.color = prepBadgeColor;
        prepBadgeImage.raycastTarget = false;

        var prepText = CreateLabel("PrepText", prepBadgeRect, 19f, FontStyles.Bold, TextAlignmentOptions.Center, Color.black);
        prepText.rectTransform.anchorMin = Vector2.zero;
        prepText.rectTransform.anchorMax = Vector2.one;
        prepText.rectTransform.offsetMin = Vector2.zero;
        prepText.rectTransform.offsetMax = Vector2.zero;

        var targetHintText = CreateLabel("TargetHintText", cardRect, 20f, FontStyles.Normal, TextAlignmentOptions.BottomLeft, subtleLabelColor);
        targetHintText.rectTransform.anchorMin = new Vector2(0f, 0f);
        targetHintText.rectTransform.anchorMax = new Vector2(1f, 0f);
        targetHintText.rectTransform.pivot = new Vector2(0.5f, 0f);
        targetHintText.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        targetHintText.rectTransform.sizeDelta = new Vector2(-24f, 26f);

        slotText.text = $"E{slotIndex + 1}";
        prepText.text = "Prep: -";
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
        if (cards.Count != orchestrator.RunState.enemies.Count)
            return;

        for (int index = 0; index < cards.Count; index++)
        {
            var card = cards[index];
            var enemy = orchestrator.RunState.enemies[index];
            if (card == null || enemy == null)
                continue;

            BindCardIdentity(card, enemy);
            RefreshCardVisual(card, enemy, index);
        }
    }

    void BindCardIdentity(CardWidgets card, EnemyState enemy)
    {
        if (card.enemyInstanceId == enemy.instanceId)
            return;

        card.enemyInstanceId = enemy.instanceId;
        card.dropTarget.SetEnemyInstanceId(enemy.instanceId);
        card.dropTarget.SetOrchestrator(orchestrator);
    }

    void RefreshCardVisual(CardWidgets card, EnemyState enemy, int slotIndex)
    {
        card.slotText.text = $"E{slotIndex + 1}";
        card.nameText.text = ResolveEnemyName(enemy.enemyDefId);
        card.hpText.text = BuildHealthLine(enemy);
        card.actionText.text = BuildActionLine(enemy);
        card.actionEffectText.text = BuildActionEffectLine(enemy);
        card.prepText.text = $"Prep: {Mathf.Max(0, enemy.actionTurnsLeft)}";
        card.targetHintText.text = BuildTargetHintLine();

        int maxHealth = ResolveBaseHealth(enemy.enemyDefId);
        bool isLowHealth = maxHealth > 0 && enemy.currentHealth <= Mathf.Max(1, maxHealth / 2);
        card.background.color = isLowHealth ? lowHealthCardColor : cardColor;
    }

    string BuildHealthLine(EnemyState enemy)
    {
        int maxHealth = ResolveBaseHealth(enemy?.enemyDefId);
        if (maxHealth <= 0)
            return $"HP: {Mathf.Max(0, enemy?.currentHealth ?? 0)}";

        return $"HP: {Mathf.Max(0, enemy.currentHealth)} / {maxHealth}";
    }

    string BuildActionLine(EnemyState enemy)
    {
        if (enemy == null || string.IsNullOrWhiteSpace(enemy.currentActionId))
            return "Action: -";

        return $"Action: {ToDisplayTitle(enemy.currentActionId)}";
    }

    string BuildActionEffectLine(EnemyState enemy)
    {
        if (enemy == null || string.IsNullOrWhiteSpace(enemy.currentActionId))
            return "Resolve: -";

        var actionDef = FindActionDef(enemy.enemyDefId, enemy.currentActionId);
        if (actionDef == null || actionDef.onResolve?.effects == null || actionDef.onResolve.effects.Count == 0)
            return "Resolve: -";

        var tokens = new List<string>(actionDef.onResolve.effects.Count);
        for (int index = 0; index < actionDef.onResolve.effects.Count; index++)
        {
            string token = ToEffectSummary(actionDef.onResolve.effects[index]);
            if (string.IsNullOrWhiteSpace(token))
                continue;

            tokens.Add(token);
        }

        if (tokens.Count == 0)
            return "Resolve: -";

        return $"Resolve: {string.Join(", ", tokens)}";
    }

    string BuildTargetHintLine()
    {
        if (orchestrator?.RunState == null)
            return "Drop target";

        return orchestrator.RunState.turn.phase == TurnPhase.TargetAndAttack
            ? "Drop to attack"
            : "Drop target";
    }

    string ResolveEnemyName(string enemyDefId)
    {
        if (!string.IsNullOrWhiteSpace(enemyDefId) &&
            enemyDefById.TryGetValue(enemyDefId, out var def))
        {
            if (!string.IsNullOrWhiteSpace(def.enemyId))
                return ToDisplayTitle(def.enemyId);
            if (!string.IsNullOrWhiteSpace(def.nameKey))
                return ToDisplayTitle(def.nameKey);
        }

        if (string.IsNullOrWhiteSpace(enemyDefId))
            return "Unknown Enemy";
        return ToDisplayTitle(enemyDefId);
    }

    int ResolveBaseHealth(string enemyDefId)
    {
        if (string.IsNullOrWhiteSpace(enemyDefId))
            return 0;
        if (!enemyDefById.TryGetValue(enemyDefId, out var def))
            return 0;

        return Mathf.Max(1, def.baseHealth);
    }

    ActionDef FindActionDef(string enemyDefId, string actionId)
    {
        if (string.IsNullOrWhiteSpace(enemyDefId) || string.IsNullOrWhiteSpace(actionId))
            return null;
        if (!enemyDefById.TryGetValue(enemyDefId, out var enemyDef))
            return null;
        if (enemyDef.actionPool == null)
            return null;

        for (int index = 0; index < enemyDef.actionPool.Count; index++)
        {
            var action = enemyDef.actionPool[index];
            if (action == null)
                continue;
            if (!string.Equals(action.actionId, actionId, StringComparison.Ordinal))
                continue;

            return action;
        }

        return null;
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

    void LoadEnemyDefsIfNeeded()
    {
        if (loadedDefs)
            return;
        loadedDefs = true;

        enemyDefById.Clear();

        try
        {
            var defs = GameStaticDataLoader.LoadEnemyDefs();
            for (int index = 0; index < defs.Count; index++)
            {
                var def = defs[index];
                if (def == null || string.IsNullOrWhiteSpace(def.enemyId))
                    continue;

                enemyDefById[def.enemyId] = def;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"[EnemyPanelController] Failed to load enemy defs: {exception.Message}");
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
            "enemy_health_delta" => $"Enemy HP {FormatSignedValue(value)}",
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
        public string enemyInstanceId = string.Empty;
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
