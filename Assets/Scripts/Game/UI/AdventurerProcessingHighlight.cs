using UnityEngine;
using UnityEngine.UI;

public sealed class AdventurerProcessingHighlight : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string adventurerInstanceId = string.Empty;
    [SerializeField] Graphic outlineGraphic;
    [SerializeField] Color inactiveOutlineColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] Color activeOutlineColor = new Color(1f, 0.82f, 0.3f, 1f);
    [SerializeField] GameObject headerBadgeObject;

    bool isAppliedActive;

    public void SetAdventurerInstanceId(string instanceId)
    {
        adventurerInstanceId = instanceId ?? string.Empty;
    }

    public void SetOrchestrator(GameTurnOrchestrator value)
    {
        if (ReferenceEquals(orchestrator, value))
            return;

        UnsubscribeEvents();
        orchestrator = value;
        SubscribeEvents();
        RefreshHighlight();
    }

    public void ConfigureVisuals(Graphic outline, GameObject badgeObject)
    {
        outlineGraphic = outline;
        headerBadgeObject = badgeObject;
    }

    void Awake()
    {
        TryResolveOrchestrator();
        ApplyHighlight(false, force: true);
    }

    void OnEnable()
    {
        SubscribeEvents();
        RefreshHighlight();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void OnRunStarted(GameRunState _)
    {
        RefreshHighlight();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RefreshHighlight();
    }

    void OnRunEnded(GameRunState _)
    {
        RefreshHighlight();
    }

    void OnStateChanged()
    {
        RefreshHighlight();
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

    void RefreshHighlight()
    {
        bool isActive = orchestrator != null &&
                        orchestrator.IsCurrentProcessingAdventurer(adventurerInstanceId);
        ApplyHighlight(isActive, force: false);
    }

    void ApplyHighlight(bool isActive, bool force)
    {
        if (!force && isAppliedActive == isActive)
            return;

        isAppliedActive = isActive;

        if (outlineGraphic != null)
            outlineGraphic.color = isActive ? activeOutlineColor : inactiveOutlineColor;

        if (headerBadgeObject != null && headerBadgeObject.activeSelf != isActive)
            headerBadgeObject.SetActive(isActive);
    }

    void TryResolveOrchestrator()
    {
        if (orchestrator != null)
            return;

        orchestrator = FindFirstObjectByType<GameTurnOrchestrator>();
    }
}
