using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class TopHudController : MonoBehaviour
{
    [SerializeField] RectTransform contentRoot;

    [Header("Turn")]
    [SerializeField] Image turnBackground;
    [SerializeField] TextMeshProUGUI turnTitleText;
    [SerializeField] TextMeshProUGUI turnValueText;

    [Header("Stage")]
    [SerializeField] Image stageBackground;
    [SerializeField] TextMeshProUGUI stageTitleText;
    [SerializeField] TextMeshProUGUI stageValueText;

    [Header("Phase")]
    [SerializeField] Image phaseBackground;
    [SerializeField] TextMeshProUGUI phaseTitleText;
    [SerializeField] TextMeshProUGUI phaseValueText;

    [Header("Stability")]
    [SerializeField] Image stabilityBackground;
    [SerializeField] TextMeshProUGUI stabilityTitleText;
    [SerializeField] TextMeshProUGUI stabilityValueText;

    [Header("Gold")]
    [SerializeField] Image goldBackground;
    [SerializeField] TextMeshProUGUI goldTitleText;
    [SerializeField] TextMeshProUGUI goldValueText;

    [Header("Run")]
    [SerializeField] Image runBackground;
    [SerializeField] TextMeshProUGUI runTitleText;
    [SerializeField] TextMeshProUGUI runValueText;

    [SerializeField] Color barColor = new(0.12f, 0.14f, 0.18f, 0.90f);
    [SerializeField] Color panelColor = new(0.20f, 0.24f, 0.30f, 0.96f);
    [SerializeField] Color panelWarningColor = new(0.46f, 0.18f, 0.18f, 0.96f);
    [SerializeField] Color titleColor = new(0.72f, 0.80f, 0.90f, 1.00f);
    [SerializeField] Color valueColor = new(0.94f, 0.97f, 1.00f, 1.00f);
    [SerializeField] Color gameOverValueColor = new(1.00f, 0.84f, 0.84f, 1.00f);
    [SerializeField] Color stabilityWarningValueColor = new(1.00f, 0.70f, 0.70f, 1.00f);
    [SerializeField] Color stabilitySafeValueColor = new(0.78f, 1.00f, 0.82f, 1.00f);
    [SerializeField] Color goldValueColor = new(1.00f, 0.90f, 0.38f, 1.00f);

    bool isRunOver;

    void Awake()
    {
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

        var background = contentRoot.GetComponent<Image>();
        if (background != null)
        {
            background.color = barColor;
            background.raycastTarget = false;
        }

        var runState = GameManager.Instance != null ? GameManager.Instance.CurrentRunState : null;
        if (runState == null)
        {
            SetItem(turnBackground, turnTitleText, turnValueText, "Turn", "-");
            SetItem(stageBackground, stageTitleText, stageValueText, "Stage", "-");
            SetItem(phaseBackground, phaseTitleText, phaseValueText, "Phase", "-");
            SetItem(stabilityBackground, stabilityTitleText, stabilityValueText, "Stability", "-");
            SetItem(goldBackground, goldTitleText, goldValueText, "Gold", "-");
            SetItem(runBackground, runTitleText, runValueText, "Run", "Idle");
            return;
        }

        SetItem(turnBackground, turnTitleText, turnValueText, "Turn", runState.turn.turnNumber.ToString());
        SetItem(stageBackground, stageTitleText, stageValueText, "Stage", BuildStageValue(runState));
        SetItem(phaseBackground, phaseTitleText, phaseValueText, "Phase", ToDisplayTitle(PhaseManager.Instance.CurrentPhase.ToString()));
        SetItem(
            stabilityBackground,
            stabilityTitleText,
            stabilityValueText,
            "Stability",
            $"{PlayerManager.Instance.Stability}/{PlayerManager.Instance.MaxStability}");
        SetItem(goldBackground, goldTitleText, goldValueText, "Gold", PlayerManager.Instance.Gold.ToString());
        SetItem(runBackground, runTitleText, runValueText, "Run", isRunOver ? "Game Over" : "Running");

        if (runBackground != null)
            runBackground.color = isRunOver ? panelWarningColor : panelColor;
        if (runValueText != null)
            runValueText.color = isRunOver ? gameOverValueColor : valueColor;

        if (stabilityValueText != null)
        {
            float ratio = PlayerManager.Instance.MaxStability > 0
                ? (float)PlayerManager.Instance.Stability / PlayerManager.Instance.MaxStability
                : 0f;
            stabilityValueText.color = ratio <= 0.35f
                ? stabilityWarningValueColor
                : stabilitySafeValueColor;
        }

        if (goldValueText != null)
            goldValueText.color = goldValueColor;
    }

    string BuildStageValue(GameRunState runState)
    {
        int number = Mathf.Max(0, runState.stage.stageNumber);
        if (string.IsNullOrWhiteSpace(runState.stage.activePresetId))
            return number.ToString();

        return $"{number} ({ToDisplayTitle(runState.stage.activePresetId)})";
    }

    void SetItem(
        Image background,
        TextMeshProUGUI titleText,
        TextMeshProUGUI valueTextComp,
        string title,
        string value)
    {
        if (background != null)
            background.color = panelColor;
        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
            titleText.color = titleColor;
        }
        if (valueTextComp != null)
        {
            valueTextComp.text = value ?? string.Empty;
            valueTextComp.color = valueColor;
        }
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
}
