using UnityEngine;
using Data;

public sealed class ItemInstance
{
    public string Id { get; }

    public float DamageMultiplier { get; private set; }
    public float AttackSpeed { get; private set; }
    public float BulletSize { get; private set; }
    public float BulletSpeed { get; private set; }

    public float WorldBulletSize => GameConfig.ItemBaseBulletSize * Mathf.Max(0.1f, BulletSize);
    public float WorldBulletSpeed => GameConfig.ItemBaseBulletSpeed * Mathf.Max(0.1f, BulletSpeed);

    public ItemInstance(ItemDto dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.id))
        {
            Debug.LogError("[ItemInstance] Invalid dto");
            Id = string.Empty;
            DamageMultiplier = 1f;
            AttackSpeed = 1f;
            BulletSize = 1f;
            BulletSpeed = 1f;
            return;
        }

        Id = dto.id;
        DamageMultiplier = Mathf.Max(0.1f, dto.damageMultiplier);
        AttackSpeed = Mathf.Max(0.1f, dto.attackSpeed);
        BulletSize = Mathf.Max(0.1f, dto.bulletSize);
        BulletSpeed = Mathf.Max(0.1f, dto.bulletSpeed);
    }
}
