using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.Localization.Components;

/// <summary>
/// Unity component that manages confirmation modal runtime behavior.
/// </summary>
public class ConfirmationModal : MonoBehaviour
{
    [SerializeField] public LocalizeStringEvent titleTextEvent;
    [SerializeField] public LocalizeStringEvent messageTextEvent;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    private Action onConfirmAction;
    private Action onCancelAction;
    readonly DisposableBag subscriptions = new();

    public void Initialize()
    {
    }

    void OnEnable()
    {
        subscriptions.Clear();
        subscriptions.Add(EventSubscription.Subscribe(yesButton, OnYesClicked));
        subscriptions.Add(EventSubscription.Subscribe(noButton, OnNoClicked));
    }

    void OnDisable()
    {
        subscriptions.Clear();
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

