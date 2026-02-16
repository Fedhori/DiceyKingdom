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
        {
            enabled = false;
            return;
        }

        worldPresenter.BindOverlayPresenter(overlayPresenter);
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
}
