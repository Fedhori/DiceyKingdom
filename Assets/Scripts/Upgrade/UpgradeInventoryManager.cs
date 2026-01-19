using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class UpgradeInventoryManager : MonoBehaviour
{
    public static UpgradeInventoryManager Instance { get; private set; }

    readonly List<UpgradeInstance> upgrades = new();

    public IReadOnlyList<UpgradeInstance> Upgrades => upgrades;
    public int Count => upgrades.Count;
    public UpgradeInstance SelectedUpgrade { get; private set; }
    public int SelectedIndex { get; private set; } = -1;
    public bool HasSelection => SelectedUpgrade != null;

    public event Action OnInventoryChanged;
    public event Action OnSelectionChanged;
    public event Action OnNewUpgradeAdded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void OnEnable()
    {
        UiSelectionEvents.OnSelectionCleared += HandleSelectionCleared;
    }

    void OnDisable()
    {
        UiSelectionEvents.OnSelectionCleared -= HandleSelectionCleared;
    }

    public void ClearSelectionIfAny()
    {
        if (SelectedUpgrade == null && SelectedIndex < 0)
            return;

        ClearSelection();
    }

    public UpgradeInstance GetAt(int index)
    {
        return IsValidIndex(index) ? upgrades[index] : null;
    }

    public void Add(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return;

        upgrades.Add(upgrade);
        OnNewUpgradeAdded?.Invoke();
        NotifyInventoryChanged();
    }

    public bool Remove(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return false;

        bool removed = upgrades.Remove(upgrade);
        if (removed)
            NotifyInventoryChanged();

        return removed;
    }

    public bool RemoveAt(int index, out UpgradeInstance removed)
    {
        removed = null;
        if (!IsValidIndex(index))
            return false;

        removed = upgrades[index];
        upgrades.RemoveAt(index);
        NotifyInventoryChanged();
        return true;
    }

    public void Clear()
    {
        if (upgrades.Count == 0)
            return;

        upgrades.Clear();
        NotifyInventoryChanged();
    }

    public void SelectAt(int index)
    {
        if (!IsValidIndex(index))
        {
            ClearSelection();
            return;
        }

        var upgrade = upgrades[index];
        SetSelection(upgrade, index);
    }

    public void Select(UpgradeInstance upgrade)
    {
        if (upgrade == null)
        {
            ClearSelection();
            return;
        }

        int index = IndexOf(upgrade);
        if (index < 0)
        {
            ClearSelection();
            return;
        }

        SetSelection(upgrade, index);
    }

    public void ToggleSelectionAt(int index)
    {
        if (SelectedUpgrade != null && SelectedIndex == index)
        {
            ClearSelection();
            return;
        }

        SelectAt(index);
    }

    public void ClearSelection()
    {
        if (SelectedUpgrade == null && SelectedIndex < 0)
            return;

        SelectedUpgrade = null;
        SelectedIndex = -1;
        OnSelectionChanged?.Invoke();
    }

    public bool TryApplySelectedUpgradeAt(int slotIndex)
    {
        if (SelectedUpgrade == null)
            return false;

        return TryApplyUpgradeAt(slotIndex, SelectedUpgrade, confirmReplace: true);
    }

    void SetSelection(UpgradeInstance upgrade, int index)
    {
        if (ReferenceEquals(SelectedUpgrade, upgrade) && SelectedIndex == index)
            return;

        SelectedUpgrade = upgrade;
        SelectedIndex = index;
        OnSelectionChanged?.Invoke();
    }

    bool TryApplyUpgradeAt(int slotIndex, UpgradeInstance upgrade, bool confirmReplace)
    {
        if (upgrade == null)
            return false;

        if (IndexOf(upgrade) < 0)
            return false;

        var inventory = ItemManager.Instance?.Inventory;
        if (inventory == null)
            return false;

        if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            return false;

        var targetItem = inventory.GetSlot(slotIndex);
        if (targetItem == null)
            return false;

        if (!upgrade.IsApplicable(targetItem))
            return false;

        if (confirmReplace && NeedsUpgradeReplaceConfirm(targetItem, upgrade))
        {
            ShowUpgradeReplaceModal(targetItem, upgrade, slotIndex);
            return false;
        }

        var upgradeManager = UpgradeManager.Instance;
        if (upgradeManager == null)
            return false;

        if (!upgradeManager.ApplyUpgrade(targetItem, upgrade))
            return false;

        Remove(upgrade);
        return true;
    }

    static bool NeedsUpgradeReplaceConfirm(ItemInstance targetItem, UpgradeInstance newUpgrade)
    {
        if (targetItem?.Upgrade == null || newUpgrade == null)
            return false;

        return targetItem.Upgrade.Id != newUpgrade.Id;
    }

    void ShowUpgradeReplaceModal(ItemInstance targetItem, UpgradeInstance newUpgrade, int slotIndex)
    {
        var modal = ModalManager.Instance;
        if (modal == null)
            return;

        string itemName = LocalizationUtil.GetItemName(targetItem.Id);
        if (string.IsNullOrEmpty(itemName))
            itemName = targetItem.Id;

        string currentUpgradeName = LocalizationUtil.GetUpgradeName(targetItem.Upgrade.Id);
        if (string.IsNullOrEmpty(currentUpgradeName))
            currentUpgradeName = targetItem.Upgrade.Id;

        string newUpgradeName = LocalizationUtil.GetUpgradeName(newUpgrade.Id);
        if (string.IsNullOrEmpty(newUpgradeName))
            newUpgradeName = newUpgrade.Id;

        var args = new Dictionary<string, object>
        {
            ["itemName"] = itemName,
            ["currentUpgrade"] = currentUpgradeName,
            ["newUpgrade"] = newUpgradeName
        };

        modal.ShowConfirmation(
            "modal",
            "modal.upgradeReplace.title",
            "modal",
            "modal.upgradeReplace.message",
            () => TryApplyUpgradeAt(slotIndex, newUpgrade, confirmReplace: false),
            () => { },
            args);
    }

    int IndexOf(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return -1;

        for (int i = 0; i < upgrades.Count; i++)
        {
            if (ReferenceEquals(upgrades[i], upgrade))
                return i;
        }

        return -1;
    }

    bool IsValidIndex(int index)
    {
        return index >= 0 && index < upgrades.Count;
    }

    void NotifyInventoryChanged()
    {
        HandleInventoryChanged();
        OnInventoryChanged?.Invoke();
    }

    void HandleInventoryChanged()
    {
        if (SelectedUpgrade == null)
            return;

        int index = IndexOf(SelectedUpgrade);
        if (index < 0)
        {
            ClearSelection();
            return;
        }

        if (SelectedIndex != index)
        {
            SelectedIndex = index;
            OnSelectionChanged?.Invoke();
        }
    }

    void HandleSelectionCleared()
    {
        ClearSelection();
    }
}
