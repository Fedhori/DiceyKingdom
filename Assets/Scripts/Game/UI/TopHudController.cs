using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TopHudController : MonoBehaviour
{
    [SerializeField] RectTransform contentRoot;
    [SerializeField] Color barColor = new(0.12f, 0.14f, 0.18f, 0.90f);
    [SerializeField] Color panelColor = new(0.20f, 0.24f, 0.30f, 0.96f);
    [SerializeField] Color panelWarningColor = new(0.46f, 0.18f, 0.18f, 0.96f);
    [SerializeField] Color titleColor = new(0.72f, 0.80f, 0.90f, 1.00f);
    [SerializeField] Color valueColor = new(0.94f, 0.97f, 1.00f, 1.00f);
    [SerializeField] Color gameOverValueColor = new(1.00f, 0.84f, 0.84f, 1.00f);
    [SerializeField] Color stabilityWarningValueColor = new(1.00f, 0.70f, 0.70f, 1.00f);
    [SerializeField] Color stabilitySafeValueColor = new(0.78f, 1.00f, 0.82f, 1.00f);
    [SerializeField] Color goldValueColor = new(1.00f, 0.90f, 0.38f, 1.00f);

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
        TryResolveContentRoot();
        BindVisualTree();
    }

    void Start()
    {
        SubscribeEvents();
        RefreshHud();
    }

    void OnDestroy()
    {
        UnsubscribeEvents();
    }

    void OnRunStarted(GameRunState _)
    {
        isRunOver = false;
        RefreshHud();
    }

    void OnRunEnded(GameRunState _)
    {
        isRunOver = true;
        RefreshHud();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RefreshHud();
    }

    void OnTurnNumberChanged(int _)
    {
        RefreshHud();
    }

    void OnStageSpawned(int _, string __)
    {
        RefreshHud();
    }

    void OnStabilityChanged(int _)
    {
        RefreshHud();
    }

    void OnMaxStabilityChanged(int _)
    {
        RefreshHud();
    }

    void OnGoldChanged(int _)
    {
        RefreshHud();
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
            PhaseManager.Instance.TurnNumberChanged -= OnTurnNumberChanged;
            PhaseManager.Instance.PhaseChanged += OnPhaseChanged;
            PhaseManager.Instance.TurnNumberChanged += OnTurnNumberChanged;
        }

        if (SituationManager.Instance != null)
        {
            SituationManager.Instance.StageSpawned -= OnStageSpawned;
            SituationManager.Instance.StageSpawned += OnStageSpawned;
        }

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.StabilityChanged -= OnStabilityChanged;
            PlayerManager.Instance.MaxStabilityChanged -= OnMaxStabilityChanged;
            PlayerManager.Instance.GoldChanged -= OnGoldChanged;
            PlayerManager.Instance.StabilityChanged += OnStabilityChanged;
            PlayerManager.Instance.MaxStabilityChanged += OnMaxStabilityChanged;
            PlayerManager.Instance.GoldChanged += OnGoldChanged;
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
        {
            PhaseManager.Instance.PhaseChanged -= OnPhaseChanged;
            PhaseManager.Instance.TurnNumberChanged -= OnTurnNumberChanged;
        }

        if (SituationManager.Instance != null)
            SituationManager.Instance.StageSpawned -= OnStageSpawned;

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.StabilityChanged -= OnStabilityChanged;
            PlayerManager.Instance.MaxStabilityChanged -= OnMaxStabilityChanged;
            PlayerManager.Instance.GoldChanged -= OnGoldChanged;
        }
    }

    void RefreshHud()
    {
        if (contentRoot == null)
            return;

        var runState = GameManager.Instance != null ? GameManager.Instance.CurrentRunState : null;
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
        SetItem(phaseItem, "Phase", ToDisplayTitle(PhaseManager.Instance.CurrentPhase.ToString()));
        SetItem(stabilityItem, "Stability", $"{PlayerManager.Instance.Stability}/{PlayerManager.Instance.MaxStability}");
        SetItem(goldItem, "Gold", PlayerManager.Instance.Gold.ToString());
        SetItem(runItem, "Run", isRunOver ? "Game Over" : "Running");

        if (runItem?.background != null)
            runItem.background.color = isRunOver ? panelWarningColor : panelColor;
        if (runItem?.valueText != null)
            runItem.valueText.color = isRunOver ? gameOverValueColor : valueColor;

        if (stabilityItem?.valueText != null)
        {
            float ratio = PlayerManager.Instance.MaxStability > 0
                ? (float)PlayerManager.Instance.Stability / PlayerManager.Instance.MaxStability
                : 0f;
            stabilityItem.valueText.color = ratio <= 0.35f
                ? stabilityWarningValueColor
                : stabilitySafeValueColor;
        }

        if (goldItem?.valueText != null)
            goldItem.valueText.color = goldValueColor;
    }

    string BuildStageValue(GameRunState runState)
    {
        int number = Mathf.Max(0, runState.stage.stageNumber);
        if (string.IsNullOrWhiteSpace(runState.stage.activePresetId))
            return number.ToString();

        return $"{number} ({ToDisplayTitle(runState.stage.activePresetId)})";
    }

    void BindVisualTree()
    {
        if (contentRoot == null)
            return;

        var background = contentRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = barColor;
            background.raycastTarget = false;
        }

        rowRoot = FindRectChild(contentRoot, "TopHudRow");
        turnItem = FindHudItem("TurnItem");
        stageItem = FindHudItem("StageItem");
        phaseItem = FindHudItem("PhaseItem");
        stabilityItem = FindHudItem("StabilityItem");
        goldItem = FindHudItem("GoldItem");
        runItem = FindHudItem("RunItem");
    }

    HudItem FindHudItem(string objectName)
    {
        if (rowRoot == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        var panelRoot = FindRectChild(rowRoot, objectName);
        if (panelRoot == null)
            return null;

        return new HudItem
        {
            background = panelRoot.GetComponent<Image>(),
            titleText = FindTextChild(panelRoot, "TitleText"),
            valueText = FindTextChild(panelRoot, "ValueText")
        };
    }

    static RectTransform FindRectChild(RectTransform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        return parent.Find(childName) as RectTransform;
    }

    static TextMeshProUGUI FindTextChild(RectTransform parent, string childName)
    {
        var child = FindRectChild(parent, childName);
        if (child == null)
            return null;

        return child.GetComponent<TextMeshProUGUI>();
    }

    void SetItem(HudItem item, string title, string value)
    {
        if (item == null)
            return;

        if (item.background != null)
            item.background.color = panelColor;
        if (item.titleText != null)
        {
            item.titleText.text = title ?? string.Empty;
            item.titleText.color = titleColor;
        }
        if (item.valueText != null)
        {
            item.valueText.text = value ?? string.Empty;
            item.valueText.color = valueColor;
        }
    }

    void TryResolveContentRoot()
    {
        if (contentRoot != null)
            return;

        contentRoot = transform as RectTransform;
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
