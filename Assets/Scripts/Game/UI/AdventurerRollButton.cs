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
    }

    void OnValidate()
    {
        if (button == null)
            button = GetComponent<Button>();
    }

    void Update()
    {
        if (button == null)
            return;

        bool canRoll = orchestrator != null && orchestrator.CanRollAdventurer(adventurerInstanceId);
        if (button.interactable == canRoll)
            return;

        button.interactable = canRoll;
    }
}
