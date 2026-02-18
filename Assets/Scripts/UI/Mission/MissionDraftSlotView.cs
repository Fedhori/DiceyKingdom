using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class MissionDraftSlotView : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Button slotButton;
    [SerializeField] TMP_Text slotLabelText;
    [SerializeField] TMP_Text assignedNameText;
    [SerializeField] Image hoverHighlight;
    [SerializeField] Image occupiedHighlight;
    int slotIndex;
    bool canInteract;
    bool hasAssigned;
    Action<int> onClear;
    Action<int, string> onDropAdventurer;
    bool setupValid;

    void Awake()
    {
        setupValid = ValidateReferences();
        if (!setupValid)
        {
            enabled = false;
            return;
        }

        slotButton.onClick.AddListener(HandleClick);
    }

    public void SetData(
        MissionDraftSlotData data,
        Action<int> clearHandler,
        Action<int, string> dropHandler)
    {
        if (!setupValid || data == null)
            return;

        slotIndex = data.slotIndex;
        canInteract = data.canInteract;
        hasAssigned = data.hasAssigned;
        onClear = clearHandler;
        onDropAdventurer = dropHandler;

        slotLabelText.text = (slotIndex + 1).ToString();
        assignedNameText.text = data.hasAssigned
            ? data.assignedDisplayName
            : "비어 있음";

        slotButton.interactable = canInteract && hasAssigned;
        if (occupiedHighlight != null)
            occupiedHighlight.enabled = data.hasAssigned;

        if (hoverHighlight != null)
            hoverHighlight.enabled = false;
    }

    bool ValidateReferences()
    {
        bool valid = true;
        if (slotButton == null)
        {
            Debug.LogError("[MissionOverlay] slotButton is not assigned.", this);
            valid = false;
        }

        if (slotLabelText == null)
        {
            Debug.LogError("[MissionOverlay] slotLabelText is not assigned.", this);
            valid = false;
        }

        if (assignedNameText == null)
        {
            Debug.LogError("[MissionOverlay] assignedNameText is not assigned.", this);
            valid = false;
        }

        return valid;
    }

    void HandleClick()
    {
        if (!canInteract || !hasAssigned)
            return;

        onClear?.Invoke(slotIndex);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (!canInteract || eventData == null)
            return;

        GameObject pointerDrag = eventData.pointerDrag;
        if (pointerDrag == null)
            return;

        var row = pointerDrag.GetComponent<MissionAdventurerRowView>();
        if (row == null || !row.IsAssignable || string.IsNullOrWhiteSpace(row.AdventurerUid))
            return;

        onDropAdventurer?.Invoke(slotIndex, row.AdventurerUid);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverHighlight != null)
            hoverHighlight.enabled = false;
    }
}
