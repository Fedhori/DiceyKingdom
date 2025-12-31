using UnityEngine;
using Data;

public sealed class ItemInstance
{
    public string Id { get; }

    public float DamageMultiplier { get; private set; }
    public float AttackSpeed { get; private set; }
    public float ProjectileSize { get; private set; }
    public float ProjectileSpeed { get; private set; }
    public string ProjectileKey { get; private set; }
    public ProjectileHitBehavior ProjectileHitBehavior { get; private set; }
    public int MaxBounces { get; private set; }
    public float ProjectileLifeTime { get; private set; }
    public string PrefabKey { get; private set; }

    public float WorldProjectileSize => GameConfig.ItemBaseProjectileSize * Mathf.Max(0.1f, ProjectileSize);
    public float WorldProjectileSpeed => GameConfig.ItemBaseProjectileSpeed * Mathf.Max(0.1f, ProjectileSpeed);

    public ItemInstance(ItemDto dto)
    {
        if (dto == null || string.IsNullOrEmpty(dto.id))
        {
            Debug.LogError("[ItemInstance] Invalid dto");
            Id = string.Empty;
            DamageMultiplier = 1f;
            AttackSpeed = 1f;
            ProjectileSize = 1f;
            ProjectileSpeed = 1f;
            ProjectileKey = string.Empty;
            ProjectileHitBehavior = ProjectileHitBehavior.Destroy;
            MaxBounces = 0;
            ProjectileLifeTime = 0f;
            PrefabKey = string.Empty;
            return;
        }

        Id = dto.id;
        DamageMultiplier = Mathf.Max(0.1f, dto.damageMultiplier);
        AttackSpeed = Mathf.Max(0.1f, dto.attackSpeed);
        PrefabKey = dto.prefabKey ?? string.Empty;

        var projectile = dto.projectile;
        if (projectile != null)
        {
            ProjectileKey = projectile.key ?? string.Empty;
            ProjectileSize = Mathf.Max(0.1f, projectile.size);
            ProjectileSpeed = Mathf.Max(0.1f, projectile.speed);
            ProjectileHitBehavior = projectile.hitBehavior;
            MaxBounces = Mathf.Max(0, projectile.maxBounces);
            ProjectileLifeTime = Mathf.Max(0f, projectile.lifeTime);
        }
        else
        {
            ProjectileKey = string.Empty;
            ProjectileSize = 1f;
            ProjectileSpeed = 1f;
            ProjectileHitBehavior = ProjectileHitBehavior.Destroy;
            MaxBounces = 0;
            ProjectileLifeTime = 0f;
        }
    }
}
