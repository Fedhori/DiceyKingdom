using System;
using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public sealed class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }
    public UpgradeReplaceRequest PendingReplace { get; private set; }

    public event Action<UpgradeReplaceRequest> OnReplaceRequested;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool ApplyUpgrade(ItemInstance target, UpgradeInstance upgrade)
    {
        return TryAddUpgrade(target, upgrade, GameConfig.MaxUpgradesPerItem);
    }

    public void RemoveUpgrade(ItemInstance target)
    {
        if (target == null)
            return;

        ClearUpgradeModifiers(target);
        target.SetUpgrades(null);
    }

    public bool TryAddUpgrade(ItemInstance target, UpgradeInstance upgrade, int maxSlots = -1)
    {
        if (target == null)
            return false;

        if (upgrade == null)
            return true;

        var current = target.Upgrades;
        int count = current != null ? current.Count : 0;
        if (maxSlots >= 0 && count >= maxSlots)
            return false;

        var list = new List<UpgradeInstance>(count + 1);
        if (current != null && current.Count > 0)
            list.AddRange(current);

        list.Add(upgrade);
        return ApplyUpgrades(target, list);
    }

    public bool TryReplaceUpgrade(ItemInstance target, UpgradeInstance existingUpgrade, UpgradeInstance newUpgrade)
    {
        if (target == null || existingUpgrade == null || newUpgrade == null)
            return false;

        var current = target.Upgrades;
        if (current == null || current.Count == 0)
            return false;

        int index = FindUpgradeIndex(current, existingUpgrade);
        if (index < 0)
            return false;

        var list = new List<UpgradeInstance>(current);
        list[index] = newUpgrade;
        return ApplyUpgrades(target, list);
    }

    public bool TryApplyUpgradeAtSlot(
        ItemInventory inventory,
        int slotIndex,
        UpgradeInstance upgrade,
        int maxSlots,
        Func<ItemInstance, bool> applyHandler,
        Func<ItemInstance, UpgradeInstance, bool> replaceHandler)
    {
        if (inventory == null || upgrade == null || applyHandler == null)
            return false;

        if (slotIndex < 0 || slotIndex >= inventory.SlotCount)
            return false;

        var targetItem = inventory.GetSlot(slotIndex);
        if (targetItem == null)
            return false;

        if (!upgrade.IsApplicable(targetItem))
            return false;

        int effectiveMax = maxSlots < 0 ? int.MaxValue : Mathf.Max(0, maxSlots);
        if (targetItem.Upgrades.Count >= effectiveMax)
        {
            if (replaceHandler == null)
                return false;

            BeginReplace(targetItem, upgrade, slotIndex, existingUpgrade => replaceHandler(targetItem, existingUpgrade));
            return false;
        }

        return applyHandler(targetItem);
    }

    public bool ApplyUpgrades(ItemInstance target, IReadOnlyList<UpgradeInstance> upgrades)
    {
        if (target == null)
            return false;

        List<UpgradeInstance> upgradesToApply = null;
        if (upgrades != null && upgrades.Count > 0)
            upgradesToApply = new List<UpgradeInstance>(upgrades);

        if (upgradesToApply == null || upgradesToApply.Count == 0)
        {
            ClearUpgradeModifiers(target);
            target.SetUpgrades(null);
            return true;
        }

        var effectManager = ItemEffectManager.Instance;
        if (effectManager == null)
            return false;

        ClearUpgradeModifiers(target);

        for (int i = 0; i < upgradesToApply.Count; i++)
            ApplyUpgradeEffects(target, upgradesToApply[i], effectManager);

        target.SetUpgrades(upgradesToApply);
        return true;
    }

    public bool BeginReplace(
        ItemInstance targetItem,
        UpgradeInstance pendingUpgrade,
        int targetSlotIndex,
        Func<UpgradeInstance, bool> confirmHandler,
        Action cancelHandler = null)
    {
        if (targetItem == null || pendingUpgrade == null || confirmHandler == null)
            return false;

        PendingReplace = new UpgradeReplaceRequest(
            targetItem,
            pendingUpgrade,
            targetSlotIndex,
            confirmHandler,
            cancelHandler);
        OnReplaceRequested?.Invoke(PendingReplace);
        return true;
    }

    public bool TryConfirmReplace(UpgradeInstance existingUpgrade)
    {
        if (PendingReplace == null)
            return false;

        bool success = PendingReplace.Confirm(existingUpgrade);
        if (success)
            PendingReplace = null;
        return success;
    }

    public void CancelReplace()
    {
        if (PendingReplace == null)
            return;

        PendingReplace.Cancel();
        PendingReplace = null;
    }

    void ApplyUpgradeEffects(ItemInstance target, UpgradeInstance upgrade, ItemEffectManager effectManager)
    {
        if (upgrade == null)
            return;

        var effects = upgrade.Effects;
        if (effects == null || effects.Count == 0)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            switch (effect.effectType)
            {
                case ItemEffectType.ModifyItemStat:
                case ItemEffectType.SetItemStatus:
                case ItemEffectType.ModifyTriggerRepeat:
                    effectManager.ApplyEffect(effect, target);
                    break;
                default:
                    Debug.LogWarning($"[UpgradeManager] Unsupported effect type: {effect.effectType}");
                    break;
            }
        }
    }

    void ClearUpgradeModifiers(ItemInstance target)
    {
        target.Stats.RemoveModifiers(layer: StatLayer.Upgrade, source: target);
        target.RemoveTriggerRepeatModifiers(layer: StatLayer.Upgrade, source: target);
    }

    static int FindUpgradeIndex(IReadOnlyList<UpgradeInstance> upgrades, UpgradeInstance target)
    {
        if (upgrades == null || target == null)
            return -1;

        for (int i = 0; i < upgrades.Count; i++)
        {
            if (ReferenceEquals(upgrades[i], target))
                return i;
        }

        return -1;
    }
}
