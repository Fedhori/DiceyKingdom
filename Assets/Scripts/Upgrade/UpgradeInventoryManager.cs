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

    public UpgradeInstance GetAt(int index)
    {
        return IsValidIndex(index) ? upgrades[index] : null;
    }

    public void Add(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return;

        upgrades.Add(upgrade);
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

    void SetSelection(UpgradeInstance upgrade, int index)
    {
        if (ReferenceEquals(SelectedUpgrade, upgrade) && SelectedIndex == index)
            return;

        SelectedUpgrade = upgrade;
        SelectedIndex = index;
        OnSelectionChanged?.Invoke();
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
