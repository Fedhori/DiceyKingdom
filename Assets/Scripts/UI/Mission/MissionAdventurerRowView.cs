using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MissionAdventurerRowView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] Button rowButton;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text strengthText;
    [SerializeField] TMP_Text agilityText;
    [SerializeField] TMP_Text intelligenceText;
    [SerializeField] Image selectedHighlight;
    [SerializeField] Image disabledOverlay;
    Action<string> onClicked;
    string adventurerUid = string.Empty;
    bool isAssignable;
    bool dragging;
    bool setupValid;

    public string AdventurerUid => adventurerUid;
    public bool IsAssignable => isAssignable;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
        {
            enabled = false;
            return;
        }

        rowButton.onClick.AddListener(HandleClicked);
    }

    public void SetData(MissionAdventurerRowData data, Action<string> clickHandler)
    {
        if (!setupValid || data == null)
            return;

        onClicked = clickHandler;
        adventurerUid = data.adventurerUid ?? string.Empty;
        isAssignable = data.isAssignable;

        nameText.text = string.IsNullOrWhiteSpace(data.displayName) ? "모험가" : data.displayName;
        strengthText.text = data.strength.ToString();
        agilityText.text = data.agility.ToString();
        intelligenceText.text = data.intelligence.ToString();

        rowButton.interactable = data.isAssignable;
        canvasGroup.alpha = data.isAssignable ? 1f : 0.45f;
        canvasGroup.blocksRaycasts = data.isAssignable;

        if (selectedHighlight != null)
            selectedHighlight.enabled = data.isSelected;
        if (disabledOverlay != null)
            disabledOverlay.enabled = !data.isAssignable;
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (rowButton == null)
        {
            Debug.LogError("[MissionOverlay] rowButton is not assigned.", this);
            valid = false;
        }

        if (canvasGroup == null)
        {
            Debug.LogError("[MissionOverlay] canvasGroup is not assigned.", this);
            valid = false;
        }

        if (nameText == null)
        {
            Debug.LogError("[MissionOverlay] nameText is not assigned.", this);
            valid = false;
        }

        if (strengthText == null)
        {
            Debug.LogError("[MissionOverlay] strengthText is not assigned.", this);
            valid = false;
        }

        if (agilityText == null)
        {
            Debug.LogError("[MissionOverlay] agilityText is not assigned.", this);
            valid = false;
        }

        if (intelligenceText == null)
        {
            Debug.LogError("[MissionOverlay] intelligenceText is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    void HandleClicked()
    {
        if (!isAssignable || string.IsNullOrWhiteSpace(adventurerUid))
            return;

        onClicked?.Invoke(adventurerUid);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isAssignable || string.IsNullOrWhiteSpace(adventurerUid))
            return;

        dragging = true;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragging)
            return;

        dragging = false;
        canvasGroup.blocksRaycasts = isAssignable;
        canvasGroup.alpha = isAssignable ? 1f : 0.45f;
    }
}
