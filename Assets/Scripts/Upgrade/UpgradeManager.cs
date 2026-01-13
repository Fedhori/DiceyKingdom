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
        if (target == null)
            return false;

        RemoveUpgrade(target);

        if (upgrade == null)
            return true;

        var effects = upgrade.Effects;
        if (effects != null && effects.Count > 0)
        {
            var effectManager = ItemEffectManager.Instance;
            if (effectManager == null)
                return false;

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null)
                    continue;

                if (effect.effectType != ItemEffectType.ModifyItemStat)
                {
                    Debug.LogWarning($"[UpgradeManager] Unsupported effect type: {effect.effectType}");
                    continue;
                }

                effectManager.ApplyEffect(effect, target);
            }
        }

        target.Upgrade = upgrade;
        return true;
    }

    public void RemoveUpgrade(ItemInstance target)
    {
        if (target == null)
            return;

        target.Stats.RemoveModifiers(layer: StatLayer.Upgrade, source: target);
        target.Upgrade = null;
    }
}
