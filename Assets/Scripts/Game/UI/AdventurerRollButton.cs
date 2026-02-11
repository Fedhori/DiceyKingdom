using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class AdventurerRollButton : MonoBehaviour
{
    [SerializeField] GameTurnOrchestrator orchestrator;
    [SerializeField] string adventurerInstanceId = string.Empty;
    [SerializeField] Button button;

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
        RefreshInteractable();
    }

    public void SetButton(Button value)
    {
        button = value;
    }

    public void OnRollPressed()
    {
        if (orchestrator == null)
            return;
        if (string.IsNullOrWhiteSpace(adventurerInstanceId))
            return;

        orchestrator.TryRollAdventurer(adventurerInstanceId);
    }

    void Reset()
    {
        if (button == null)
            button = GetComponent<Button>();

        TryResolveOrchestrator();
        RefreshInteractable();
    }

    void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        TryResolveOrchestrator();
    }

    void OnEnable()
    {
        SubscribeEvents();
        RefreshInteractable();
    }

    void OnDisable()
    {
        UnsubscribeEvents();
    }

    void OnRunStarted(GameRunState _)
    {
        RefreshInteractable();
    }

    void OnPhaseChanged(TurnPhase _)
    {
        RefreshInteractable();
    }

    void OnRunEnded(GameRunState _)
    {
        RefreshInteractable();
    }

    void OnStateChanged()
    {
        RefreshInteractable();
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

    void RefreshInteractable()
    {
        if (button == null)
            button = GetComponent<Button>();
        if (button == null)
            return;

        bool canRoll = orchestrator != null && orchestrator.CanRollAdventurer(adventurerInstanceId);
        if (button.interactable == canRoll)
            return;

        button.interactable = canRoll;
    }

    void TryResolveOrchestrator()
    {
        if (orchestrator != null)
            return;

        orchestrator = FindFirstObjectByType<GameTurnOrchestrator>();
    }
}
