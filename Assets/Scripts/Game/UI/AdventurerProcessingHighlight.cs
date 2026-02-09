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

    void Awake()
    {
        ApplyHighlight(false, force: true);
    }

    void Update()
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
}
