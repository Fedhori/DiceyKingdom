using UnityEngine;
using System;
using System.Collections.Generic;

public class ModalManager : MonoBehaviour
{
    public static ModalManager Instance { get; private set; }

    [SerializeField] private ConfirmationModal confirmationModalPrefab;
    [SerializeField] private InfoModal infoModalPrefab;
    [SerializeField] private Canvas modalCanvas;

    private ConfirmationModal confirmationModalInstance;
    private InfoModal infoModalInstance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (confirmationModalPrefab != null)
        {
            confirmationModalInstance = Instantiate(confirmationModalPrefab, modalCanvas.transform);
            confirmationModalInstance.Initialize();
            confirmationModalInstance.SetModalActive(false);
        }
        else
            Debug.LogError("Confirmation Modal Prefab is not assigned in ModalManager.");

        if (infoModalPrefab != null)
        {
            infoModalInstance = Instantiate(infoModalPrefab, modalCanvas.transform);
            infoModalInstance.Initialize();
            infoModalInstance.SetModalActive(false);
        }
        else
            Debug.LogError("Info Modal Prefab is not assigned in ModalManager.");
    }

    public void ShowInfo(string titleTable, string titleKey, string messageTable, string messageKey, Action onConfirm, Dictionary<string, object> messageArgs = null)
    {
        if (infoModalInstance == null)
        {
            Debug.LogError("Info Modal Instance is null. Cannot show modal.");
            return;
        }

        infoModalInstance.titleTextEvent.StringReference.SetReference(titleTable, titleKey);
        infoModalInstance.messageTextEvent.StringReference.SetReference(messageTable, messageKey);
        infoModalInstance.messageTextEvent.StringReference.Arguments = new object[] { messageArgs };
        infoModalInstance.messageTextEvent.StringReference.RefreshString();

        infoModalInstance.Show(onConfirm);
        infoModalInstance.SetModalActive(true);
    }

    public void ShowConfirmation(
        string titleTable, 
        string titleKey, 
        string messageTable, 
        string messageKey, 
        Action onConfirm, 
        Action onCancel, 
        Dictionary<string, object> messageArgs = null)
    {
        if (confirmationModalInstance == null)
        {
            Debug.LogError("Confirmation Modal Instance is null. Cannot show modal.");
            return;
        }

        if (confirmationModalInstance.titleTextEvent != null)
        {
            confirmationModalInstance.titleTextEvent.StringReference.SetReference(titleTable, titleKey);
            confirmationModalInstance.titleTextEvent.StringReference.RefreshString();
        }

        if (confirmationModalInstance.messageTextEvent != null)
        {
            confirmationModalInstance.messageTextEvent.StringReference.SetReference(messageTable, messageKey);
            confirmationModalInstance.messageTextEvent.StringReference.Arguments = new object[] { messageArgs };
            confirmationModalInstance.messageTextEvent.StringReference.RefreshString();
        }

        confirmationModalInstance.Show(onConfirm, onCancel);
        confirmationModalInstance.SetModalActive(true);
    }

    public void HideConfirmation()
    {
        if (confirmationModalInstance == null)
        {
            Debug.LogError("Confirmation Modal Instance is null. Cannot hide modal.");
            return;
        }
        confirmationModalInstance.SetModalActive(false);
    }
}
