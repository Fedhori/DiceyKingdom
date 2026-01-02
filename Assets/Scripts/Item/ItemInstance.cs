using System;
using System.Collections.Generic;
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
    public int MaxPierces { get; private set; }
    public float ProjectileLifeTime { get; private set; }
    public int PelletCount { get; private set; }
    public float SpreadAngle { get; private set; }
    public bool IsObject { get; private set; }

    private readonly List<ItemRuleDto> rules = new();
    public IReadOnlyList<ItemRuleDto> Rules => rules;

    public event Action<ItemEffectDto, ItemInstance> OnEffectTriggered;

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
            MaxPierces = 0;
            ProjectileLifeTime = 0f;
            PelletCount = 1;
            SpreadAngle = 0f;
            IsObject = false;
            return;
        }

        Id = dto.id;
        DamageMultiplier = Mathf.Max(0.1f, dto.damageMultiplier);
        AttackSpeed = Mathf.Max(0.1f, dto.attackSpeed);
        IsObject = dto.isObject;

        if (dto.rules != null)
            rules.AddRange(dto.rules);

        var projectile = dto.projectile;
        if (projectile != null)
        {
            ProjectileKey = projectile.key ?? string.Empty;
            ProjectileSize = Mathf.Max(0.1f, projectile.size);
            ProjectileSpeed = Mathf.Max(0.1f, projectile.speed);
            ProjectileHitBehavior = projectile.hitBehavior;
            MaxBounces = Mathf.Max(0, projectile.maxBounces);
            MaxPierces = projectile.maxPierces;
            ProjectileLifeTime = Mathf.Max(0f, projectile.lifeTime);
            PelletCount = Mathf.Max(1, projectile.pelletCount);
            SpreadAngle = Mathf.Max(0f, projectile.spreadAngle);
        }
        else
        {
            ProjectileKey = string.Empty;
            ProjectileSize = 1f;
            ProjectileSpeed = 1f;
            ProjectileHitBehavior = ProjectileHitBehavior.Destroy;
            MaxBounces = 0;
            MaxPierces = 0;
            ProjectileLifeTime = 0f;
            PelletCount = 1;
            SpreadAngle = 0f;
        }
    }

    public void HandleTrigger(ItemTriggerType trigger)
    {
        if (rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.triggerType != trigger)
                continue;

            if (!IsConditionMet(rule.condition, trigger))
                continue;

            ApplyEffects(rule.effects);
        }
    }

    bool IsConditionMet(ItemConditionDto condition, ItemTriggerType trigger)
    {
        if (condition == null)
            return false;

        switch (condition.conditionKind)
        {
            case ItemConditionKind.Always:
                return true;
            case ItemConditionKind.PlayerIdle:
                var controller = PlayerController.Instance;
                return controller != null && !controller.IsMoveInputActive;
            default:
                Debug.LogWarning($"[ItemInstance] Unsupported condition {condition.conditionKind} for trigger {trigger}");
                return false;
        }
    }

    void ApplyEffects(List<ItemEffectDto> effects)
    {
        if (effects == null || effects.Count == 0)
            return;

        if (OnEffectTriggered == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            OnEffectTriggered?.Invoke(effect, this);
        }
    }
}
