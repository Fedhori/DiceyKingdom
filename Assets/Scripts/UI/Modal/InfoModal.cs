using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Components;

// InfoModal.cs - This is a View component.
// The ModalManager will be responsible for controlling its content.
public class InfoModal : MonoBehaviour
{
    // Expose these so the Instance (ModalManager) can access them
    [SerializeField] public LocalizeStringEvent titleTextEvent;
    [SerializeField] public LocalizeStringEvent messageTextEvent;
    [SerializeField] private Button confirmButton;

    private Action onConfirmAction;

    public void Initialize()
    {
        confirmButton.onClick.RemoveAllListeners();
        confirmButton.onClick.AddListener(OnConfirmClicked);
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