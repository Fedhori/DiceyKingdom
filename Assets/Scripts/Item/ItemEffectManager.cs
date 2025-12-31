using Data;
using GameStats;
using UnityEngine;

public sealed class ItemEffectManager : MonoBehaviour
{
    public static ItemEffectManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ApplyEffect(ItemEffectDto dto, ItemInstance item)
    {
        if (dto == null || item == null)
            return;

        switch (dto.effectType)
        {
            case ItemEffectType.ModifyStat:
                ApplyPlayerStat(dto, item);
                break;
            case ItemEffectType.AddCurrency:
                ApplyCurrency(dto);
                break;
            default:
                Debug.LogWarning($"[ItemEffectManager] Unsupported effect type: {dto.effectType}");
                break;
        }
    }

    void ApplyPlayerStat(ItemEffectDto dto, ItemInstance item)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[ItemEffectManager] ModifyStat with empty statId.");
            return;
        }

        var layer = dto.temporary ? StatLayer.Temporary : StatLayer.Permanent;

        player.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: dto.effectMode,
            value: dto.value,
            layer: layer,
            source: item
        ));
    }

    void ApplyCurrency(ItemEffectDto dto)
    {
        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
            return;

        currencyMgr.AddCurrency(Mathf.RoundToInt(dto.value));
    }
}
