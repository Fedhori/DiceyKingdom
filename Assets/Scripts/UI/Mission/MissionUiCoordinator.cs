using UnityEngine;

public sealed class MissionUiCoordinator : MonoBehaviour
{
    [SerializeField] MissionWorldListPresenter worldPresenter;
    [SerializeField] MissionOverlayPresenter overlayPresenter;
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
            enabled = false;
    }

    void OnEnable()
    {
        if (!setupValid)
            return;

        worldPresenter.MissionCardSelected += HandleMissionCardSelected;
    }

    void OnDisable()
    {
        if (!setupValid)
            return;

        worldPresenter.MissionCardSelected -= HandleMissionCardSelected;
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (worldPresenter == null)
        {
            Debug.LogError("[MissionOverlay] worldPresenter is not assigned.", this);
            valid = false;
        }

        if (overlayPresenter == null)
        {
            Debug.LogError("[MissionOverlay] overlayPresenter is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    void HandleMissionCardSelected(string missionUid)
    {
        overlayPresenter.OpenOrFocus(missionUid);
    }
}
