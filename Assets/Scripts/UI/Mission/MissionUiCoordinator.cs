using UnityEngine;

public sealed class MissionUiCoordinator : MonoBehaviour
{
    [SerializeField] MissionWorldListPresenter worldPresenter;
    [SerializeField] MissionOverlayPresenter overlayPresenter;
    [SerializeField] MissionAdventurerListPresenter adventurerListPresenter;
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
        adventurerListPresenter.BindOverlayPresenter(overlayPresenter);
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

        if (adventurerListPresenter == null)
        {
            Debug.LogError("[AdventurerList] adventurerListPresenter is not assigned.", this);
            valid = false;
        }

        return valid;
    }
}
