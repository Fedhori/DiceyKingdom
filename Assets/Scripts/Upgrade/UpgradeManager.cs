using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public sealed class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

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
}
