using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class UpgradePanelView : MonoBehaviour
{
    [SerializeField] GameObject root;
    [SerializeField] Transform slotContainer;
    [SerializeField] UpgradePanelSlot slotPrefab;
    [SerializeField] Button closeButton;

    readonly List<UpgradePanelSlot> slots = new();
    bool replaceMode;
    Action<UpgradeInstance> replaceAction;

    public IReadOnlyList<UpgradePanelSlot> Slots => slots;
    public bool IsOpen => (root != null ? root : gameObject).activeSelf;

    void Awake()
    {
        if (root == null)
            root = gameObject;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void OnDestroy()
    {
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open(IReadOnlyList<UpgradeInstance> upgrades)
    {
        SetUpgrades(upgrades);
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    public void SetUpgrades(IReadOnlyList<UpgradeInstance> upgrades)
    {
        int count = upgrades != null ? upgrades.Count : 0;
        EnsureSlotCount(count);

        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            bool active = i < count;
            slot.gameObject.SetActive(active);
            if (!active)
                continue;

            slot.Bind(upgrades[i]);
        }

        ApplyReplaceButtons();
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            slot.Bind(null);
            slot.gameObject.SetActive(false);
        }
    }

    public void SetReplaceMode(bool enabled, Action<UpgradeInstance> onReplace)
    {
        replaceMode = enabled;
        replaceAction = enabled ? onReplace : null;
        ApplyReplaceButtons();
    }

    void SetOpen(bool open)
    {
        var target = root != null ? root : gameObject;
        target.SetActive(open);
    }

    void EnsureSlotCount(int count)
    {
        if (slotContainer == null || slotPrefab == null)
            return;

        for (int i = slots.Count - 1; i >= count; i--)
        {
            var slot = slots[i];
            if (slot != null)
                slot.gameObject.SetActive(false);
        }

        while (slots.Count < count)
        {
            var slot = Instantiate(slotPrefab, slotContainer);
            slots.Add(slot);
        }
    }

    void ApplyReplaceButtons()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            var slot = slots[i];
            if (slot == null)
                continue;

            if (!replaceMode || slot.Upgrade == null)
            {
                slot.SetToggleButton(false, null, default, false, null);
                continue;
            }

            var upgrade = slot.Upgrade;
            slot.SetToggleButton(
                true,
                "tooltip.upgrade.replace.label",
                Colors.Upgrade,
                true,
                () => replaceAction?.Invoke(upgrade));
        }
    }
}
