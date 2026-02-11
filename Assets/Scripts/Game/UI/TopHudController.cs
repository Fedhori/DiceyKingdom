using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TopHudController : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] RectTransform contentRoot;
    [SerializeField] Color barColor = new(0.12f, 0.14f, 0.18f, 0.90f);
    [SerializeField] Color panelColor = new(0.20f, 0.24f, 0.30f, 0.96f);
    [SerializeField] Color panelWarningColor = new(0.46f, 0.18f, 0.18f, 0.96f);
    [SerializeField] Color titleColor = new(0.72f, 0.80f, 0.90f, 1.00f);
    [SerializeField] Color valueColor = new(0.94f, 0.97f, 1.00f, 1.00f);
    [SerializeField] Color gameOverValueColor = new(1.00f, 0.84f, 0.84f, 1.00f);

    RectTransform rowRoot;
    HudItem turnItem;
    HudItem stageItem;
    HudItem phaseItem;
    HudItem stabilityItem;
    HudItem goldItem;
    HudItem runItem;
    bool isRunOver;

    void Awake()
    {
        TryResolveOrchestrator();
        TryResolveContentRoot();
        EnsureVisualTree();
    }

    void OnEnable()
    {
        SubscribeEvents();
        RefreshHud();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void OnRunStarted(GameRunState _)
    {
        isRunOver = false;
        RefreshHud();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RefreshHud();
    }

    void OnStageSpawned(int _, string __)
    {
        RefreshHud();
    }

    void OnRunEnded(GameRunState _)
    {
        isRunOver = true;
        RefreshHud();
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

    void RefreshHud()
    {
        if (contentRoot == null)
            return;

        var runState = orchestrator?.RunState;
        if (runState == null)
        {
            SetItem(turnItem, "Turn", "-");
            SetItem(stageItem, "Stage", "-");
            SetItem(phaseItem, "Phase", "-");
            SetItem(stabilityItem, "Stability", "-");
            SetItem(goldItem, "Gold", "-");
            SetItem(runItem, "Run", "Idle");
            return;
        }

        SetItem(turnItem, "Turn", runState.turn.turnNumber.ToString());
        SetItem(stageItem, "Stage", BuildStageValue(runState));
        SetItem(phaseItem, "Phase", ToDisplayTitle(runState.turn.phase.ToString()));
        SetItem(stabilityItem, "Stability", $"{runState.stability}/{runState.maxStability}");
        SetItem(goldItem, "Gold", runState.gold.ToString());
        SetItem(runItem, "Run", isRunOver ? "Game Over" : "Running");

        if (runItem?.background != null)
            runItem.background.color = isRunOver ? panelWarningColor : panelColor;
        if (runItem?.valueText != null)
            runItem.valueText.color = isRunOver ? gameOverValueColor : valueColor;
    }

    string BuildStageValue(GameRunState runState)
    {
        int number = Mathf.Max(0, runState.stage.stageNumber);
        if (string.IsNullOrWhiteSpace(runState.stage.activePresetId))
            return number.ToString();

        return $"{number} ({ToDisplayTitle(runState.stage.activePresetId)})";
    }

    void EnsureVisualTree()
    {
        if (contentRoot == null)
            return;

        EnsureBarBackground();
        EnsureRowRoot();
        EnsureHudItems();
    }

    void EnsureBarBackground()
    {
        var image = contentRoot.GetComponent<Image>();
        if (image == null)
            image = contentRoot.gameObject.AddComponent<Image>();

        image.color = barColor;
        image.raycastTarget = false;
    }

    void EnsureRowRoot()
    {
        if (rowRoot != null)
            return;

        var rowObject = new GameObject(
            "TopHudRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup));
        rowObject.layer = LayerMask.NameToLayer("UI");

        rowRoot = rowObject.GetComponent<RectTransform>();
        rowRoot.SetParent(contentRoot, false);
        rowRoot.anchorMin = new Vector2(0f, 0f);
        rowRoot.anchorMax = new Vector2(1f, 1f);
        rowRoot.offsetMin = new Vector2(12f, 10f);
        rowRoot.offsetMax = new Vector2(-12f, -10f);

        var layout = rowObject.GetComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        layout.spacing = 10f;
    }

    void EnsureHudItems()
    {
        if (turnItem != null)
            return;

        turnItem = CreateHudItem("TurnItem");
        stageItem = CreateHudItem("StageItem");
        phaseItem = CreateHudItem("PhaseItem");
        stabilityItem = CreateHudItem("StabilityItem");
        goldItem = CreateHudItem("GoldItem");
        runItem = CreateHudItem("RunItem");
    }

    HudItem CreateHudItem(string objectName)
    {
        var panelObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(LayoutElement),
            typeof(Image));
        panelObject.layer = LayerMask.NameToLayer("UI");

        var panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.SetParent(rowRoot, false);

        var layout = panelObject.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minWidth = 120f;

        var background = panelObject.GetComponent<Image>();
        background.color = panelColor;
        background.raycastTarget = false;

        var titleText = CreateLabel("TitleText", panelRect, 17f, FontStyles.Bold, TextAlignmentOptions.TopLeft, titleColor);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(10f, -26f);
        titleText.rectTransform.offsetMax = new Vector2(-10f, -4f);

        var valueText = CreateLabel("ValueText", panelRect, 21f, FontStyles.Bold, TextAlignmentOptions.BottomLeft, valueColor);
        valueText.rectTransform.anchorMin = new Vector2(0f, 0f);
        valueText.rectTransform.anchorMax = new Vector2(1f, 1f);
        valueText.rectTransform.offsetMin = new Vector2(10f, 8f);
        valueText.rectTransform.offsetMax = new Vector2(-10f, -20f);
        valueText.enableWordWrapping = false;
        valueText.overflowMode = TextOverflowModes.Ellipsis;

        return new HudItem
        {
            background = background,
            titleText = titleText,
            valueText = valueText
        };
    }

    void SetItem(HudItem item, string title, string value)
    {
        if (item == null)
            return;

        if (item.background != null)
            item.background.color = panelColor;
        if (item.titleText != null)
            item.titleText.text = title ?? string.Empty;
        if (item.valueText != null)
        {
            item.valueText.text = value ?? string.Empty;
            item.valueText.color = valueColor;
        }
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

    static string ToDisplayTitle(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        string normalized = raw.Trim().Replace('_', ' ').Replace('.', ' ');
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

    sealed class HudItem
    {
        public Image background;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI valueText;
    }
}
