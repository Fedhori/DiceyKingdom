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

        return TryApplyUpgradeAt(slotIndex, SelectedUpgrade);
    }

    public void TrySellUpgrade(UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return;

        if (IndexOf(upgrade) < 0)
            return;

        int price = ShopManager.CalculateSellPrice(upgrade.Price);
        if (price < 0)
            return;

        var modal = ModalManager.Instance;
        if (modal == null)
            return;

        string upgradeName = LocalizationUtil.GetUpgradeName(upgrade.Id);
        if (string.IsNullOrEmpty(upgradeName))
            upgradeName = upgrade.Id;

        var args = new Dictionary<string, object>
        {
            ["upgradeName"] = upgradeName,
            ["value"] = price
        };

        modal.ShowConfirmation(
            "modal",
            "modal.upgradeSell.title",
            "modal",
            "modal.upgradeSell.message",
            () => SellUpgradeInternal(upgrade, price),
            () => { },
            args);
    }

    void SetSelection(UpgradeInstance upgrade, int index)
    {
        if (ReferenceEquals(SelectedUpgrade, upgrade) && SelectedIndex == index)
            return;

        SelectedUpgrade = upgrade;
        SelectedIndex = index;
        OnSelectionChanged?.Invoke();
    }

    bool TryApplyUpgradeAt(int slotIndex, UpgradeInstance upgrade)
    {
        if (upgrade == null)
            return false;

        if (IndexOf(upgrade) < 0)
            return false;

        var upgradeManager = UpgradeManager.Instance;
        if (upgradeManager == null)
            return false;

        var inventory = ItemManager.Instance?.Inventory;
        return upgradeManager.TryApplyUpgradeAtSlot(
            inventory,
            slotIndex,
            upgrade,
            GameConfig.MaxUpgradesPerItem,
            targetItem =>
            {
                if (!upgradeManager.TryAddUpgrade(targetItem, upgrade, GameConfig.MaxUpgradesPerItem))
                    return false;

                Remove(upgrade);
                UiSelectionEvents.RaiseSelectionCleared();
                return true;
            },
            (targetItem, existingUpgrade) => TryReplaceUpgradeInternal(targetItem, existingUpgrade, upgrade)
        );
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

    void SellUpgradeInternal(UpgradeInstance upgrade, int price)
    {
        if (upgrade == null)
            return;

        if (!Remove(upgrade))
            return;

        CurrencyManager.Instance?.AddCurrency(price);
        AudioManager.Instance?.Play("Buy");
        UiSelectionEvents.RaiseSelectionCleared();
    }

    bool TryReplaceUpgradeInternal(ItemInstance targetItem, UpgradeInstance existingUpgrade, UpgradeInstance pendingUpgrade)
    {
        if (targetItem == null || existingUpgrade == null || pendingUpgrade == null)
            return false;

        if (IndexOf(pendingUpgrade) < 0)
            return false;

        var upgradeManager = UpgradeManager.Instance;
        if (upgradeManager == null)
            return false;

        if (!upgradeManager.TryReplaceUpgrade(targetItem, existingUpgrade, pendingUpgrade))
            return false;

        Remove(pendingUpgrade);
        int sellPrice = ShopManager.CalculateSellPrice(existingUpgrade.Price);
        if (sellPrice > 0)
            CurrencyManager.Instance?.AddCurrency(sellPrice);
        UiSelectionEvents.RaiseSelectionCleared();
        return true;
    }
}
