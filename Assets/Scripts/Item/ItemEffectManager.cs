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
            case ItemEffectType.SpawnProjectile:
                SpawnProjectiles(dto, item);
                break;
            case ItemEffectType.ApplyStatusToRandomBlocks:
                ApplyStatusToRandomBlocks(dto, item);
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

        var layer = dto.duration;

        double value = dto.value;

        player.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: dto.effectMode,
            value: value,
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

    void SpawnProjectiles(ItemEffectDto dto, ItemInstance item)
    {
        if (item == null || dto == null)
            return;

        var factory = ProjectileFactory.Instance;
        if (factory == null)
            return;

        var playArea = BlockManager.Instance;
        if (playArea == null)
            return;

        int count = Mathf.Max(0, Mathf.FloorToInt(dto.value));
        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            var pos = playArea.GetRandomPositionInPlayArea();
            Vector2 dir = Random.insideUnitCircle.normalized;
            if (dir.sqrMagnitude <= 0.001f)
                dir = Vector2.up;

            factory.SpawnProjectile(pos, dir, item);
        }
    }

    void ApplyStatusToRandomBlocks(ItemEffectDto dto, ItemInstance item)
    {
        if (item == null || dto == null)
            return;

        if (item.StatusType == BlockStatusType.Unknown || item.StatusDuration <= 0f)
            return;

        int count = Mathf.Max(0, Mathf.FloorToInt(dto.value));
        if (count <= 0)
            return;

        var manager = BlockManager.Instance;
        if (manager == null)
            return;

        manager.ApplyStatusToRandomBlocks(item.StatusType, item.StatusDuration, count);
    }
}
