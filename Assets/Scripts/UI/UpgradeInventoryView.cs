using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization;

public sealed class UpgradeInventoryView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GameObject root;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private UpgradeInventorySlotController slotPrefab;
    [SerializeField] private GameObject emptyRoot;
    [SerializeField] private TMP_Text emptyText;

    readonly List<UpgradeInventorySlotController> slots = new();

    const string EmptyKey = "upgrade.inventory.empty";

    void Awake()
    {
        ClearEditorPlacedSlots();

        UpdateEmptyState(0);
    }

    public void Open()
    {
        SetVisible(true);
        UpgradeInventoryNotice.Instance?.Clear();
    }

    public void Close()
    {
        SetVisible(false);
    }

    public void SetSlots(IReadOnlyList<UpgradeInstance> upgrades)
    {
        int count = upgrades != null ? upgrades.Count : 0;
        EnsureSlots(count);

        for (int i = 0; i < count; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            slot.gameObject.SetActive(true);
            slot.SetIndex(i);
            slot.Bind(upgrades[i]);
        }

        for (int i = count; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot != null)
                slot.gameObject.SetActive(false);
        }

        UpdateEmptyState(count);
    }

    void SetVisible(bool visible)
    {
        if (root != null)
        {
            root.SetActive(visible);
            return;
        }

        gameObject.SetActive(visible);
    }

    void EnsureSlots(int count)
    {
        if (contentRoot == null || slotPrefab == null)
            return;

        while (slots.Count < count)
        {
            var slot = Instantiate(slotPrefab, contentRoot);
            slots.Add(slot);
        }
    }

    void UpdateEmptyState(int count)
    {
        var target = emptyRoot != null ? emptyRoot : (emptyText != null ? emptyText.gameObject : null);
        if (target == null)
            return;

        bool show = count <= 0;
        target.SetActive(show);

        if (show && emptyText != null)
        {
            var loc = new LocalizedString("upgrade", EmptyKey);
            emptyText.text = loc.GetLocalizedString();
        }
    }

    void ClearEditorPlacedSlots()
    {
        slots.Clear();

        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            var child = contentRoot.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (StageManager.Instance == null || StageManager.Instance.CurrentPhase != StagePhase.Shop)
            return;

        UiSelectionEvents.RaiseSelectionCleared();
    }
}
