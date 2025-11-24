using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Localization.Components;

public class ConfirmationModal : MonoBehaviour
{
    [SerializeField] public LocalizeStringEvent titleTextEvent;
    [SerializeField] public LocalizeStringEvent messageTextEvent;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirmAction;
    private Action onCancelAction;

    public void Initialize()
    {
        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(OnYesClicked);

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(OnNoClicked);
    }

    public void Show(Action onConfirm, Action onCancel)
    {
        onConfirmAction = onConfirm;
        onCancelAction = onCancel;
    }

    public void SetModalActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private void OnYesClicked()
    {
        SetModalActive(false);
        onConfirmAction?.Invoke();
    }

    private void OnNoClicked()
    {
        SetModalActive(false);
        onCancelAction?.Invoke();
    }
}
