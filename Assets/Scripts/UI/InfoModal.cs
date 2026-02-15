using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;

// InfoModal.cs - This is a View component.
// The ModalService will be responsible for controlling its content.
/// <summary>
/// Unity component that manages info modal runtime behavior.
/// </summary>
public class InfoModal : MonoBehaviour
{
    // Expose these so the Instance (ModalService) can access them
    [SerializeField] public LocalizeStringEvent titleTextEvent;
    [SerializeField] public LocalizeStringEvent messageTextEvent;
    [SerializeField] private Button confirmButton;

    private Action onConfirmAction;
    readonly DisposableBag subscriptions = new();

    public void Initialize()
    {
    }

    void OnEnable()
    {
        subscriptions.Clear();
        subscriptions.Add(EventSubscription.Subscribe(confirmButton, OnConfirmClicked));
    }

    void OnDisable()
    {
        subscriptions.Clear();
    }
    
    public void Show(Action onConfirm)
    {
        onConfirmAction = onConfirm;
    }

    public void SetModalActive(bool active)
    {
        gameObject.SetActive(active);
    }

    private void OnConfirmClicked()
    {
        SetModalActive(false);
        onConfirmAction?.Invoke();
    }
}


