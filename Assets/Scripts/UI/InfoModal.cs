using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;






public class InfoModal : MonoBehaviour
{
    
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


