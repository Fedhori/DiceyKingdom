using System;
using System.Collections.Generic;
using UnityEngine;
using Data;
using GameStats;

public sealed class ItemInstance
{
    public string Id { get; }

    public float StatusDamageMultiplier { get; private set; }
    public float ProjectileSize { get; private set; }
    public float ProjectileSpeed { get; private set; }
    public string ProjectileKey { get; private set; }
    public ProjectileHitBehavior ProjectileHitBehavior { get; private set; }
    public int Pierce { get; private set; }
    public int PelletCount { get; private set; }
    public float SpreadAngle { get; private set; }
    public float ProjectileRandomAngle { get; private set; }
    public bool IsObject { get; private set; }
    public int PierceBonus { get; private set; }
    public float ProjectileHomingTurnRate { get; private set; }
    public BlockStatusType StatusType { get; private set; }
    public float StatusDuration { get; private set; }
    public int SellValueBonus { get; private set; }
    public ItemRarity Rarity { get; private set; }

    public StatSet Stats { get; }
    public float DamageMultiplier => (float)Stats.GetValue(ItemStatIds.DamageMultiplier);
    public float AttackSpeed => (float)Stats.GetValue(ItemStatIds.AttackSpeed);

    private readonly List<ItemRuleDto> rules = new();
    public IReadOnlyList<ItemRuleDto> Rules => rules;

    public event Action<ItemEffectDto, ItemInstance> OnEffectTriggered;

    public float WorldProjectileSize => GameConfig.ItemBaseProjectileSize * Mathf.Max(0.1f, ProjectileSize);
    public float WorldProjectileSpeed => GameConfig.ItemBaseProjectileSpeed * Mathf.Max(0.1f, ProjectileSpeed);

    readonly Dictionary<ItemTriggerType, int> triggerCounts = new();
    readonly Dictionary<int, float> ruleElapsedSeconds = new();

    public ItemInstance(ItemDto dto)
    {
        Stats = new StatSet();

        if (dto == null || string.IsNullOrEmpty(dto.id))
        {
            Debug.LogError("[ItemInstance] Invalid dto");
            Id = string.Empty;
            Stats.SetBase(ItemStatIds.DamageMultiplier, 0d, 0d);
            Stats.SetBase(ItemStatIds.AttackSpeed, 0d, 0d);
            ProjectileSize = 1f;
            ProjectileSpeed = 1f;
            ProjectileKey = string.Empty;
            ProjectileHitBehavior = ProjectileHitBehavior.Normal;
            Pierce = 0;
            PelletCount = 1;
            SpreadAngle = 0f;
            ProjectileRandomAngle = 0f;
            IsObject = false;
            PierceBonus = 0;
            ProjectileHomingTurnRate = 0f;
            StatusType = BlockStatusType.Unknown;
            StatusDuration = 0f;
            StatusDamageMultiplier = 1f;
            SellValueBonus = 0;
            Rarity = ItemRarity.Common;
            return;
        }

        Id = dto.id;
        StatusDamageMultiplier = Mathf.Max(0f, dto.statusDamageMultiplier);
        Stats.SetBase(ItemStatIds.DamageMultiplier, Mathf.Max(0f, dto.damageMultiplier), 0d);
        Stats.SetBase(ItemStatIds.AttackSpeed, Mathf.Max(0f, dto.attackSpeed), 0d);
        IsObject = dto.isObject;
        PierceBonus = Mathf.Max(0, dto.pierceBonus);
        StatusType = dto.statusType;
        StatusDuration = Mathf.Max(0f, dto.statusDuration);
        SellValueBonus = 0;
        Rarity = dto.rarity;

        if (dto.rules != null)
            rules.AddRange(dto.rules);

        var projectile = dto.projectile;
        if (projectile != null)
        {
            ProjectileKey = projectile.key ?? string.Empty;
            ProjectileSize = Mathf.Max(0.1f, projectile.size);
            ProjectileSpeed = Mathf.Max(0.1f, projectile.speed);
            ProjectileHitBehavior = projectile.hitBehavior;
            Pierce = projectile.pierce;
            PelletCount = Mathf.Max(1, projectile.pelletCount);
            SpreadAngle = Mathf.Max(0f, projectile.spreadAngle);
            ProjectileRandomAngle = Mathf.Max(0f, projectile.randomAngle);
            ProjectileHomingTurnRate = Mathf.Max(0f, projectile.homingTurnRate);
        }
    }

    public void HandleTrigger(ItemTriggerType trigger)
    {
        if (rules.Count == 0)
            return;

        IncrementTriggerCount(trigger);

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
            case ItemConditionKind.EveryNthTrigger:
                if (condition.count <= 0)
                    return false;
                var triggerCount = GetTriggerCount(trigger);
                if (triggerCount <= 0)
                    return false;
                return triggerCount % condition.count == 0;
            case ItemConditionKind.Time:
                return false;
            default:
                Debug.LogWarning($"[ItemInstance] Unsupported condition {condition.conditionKind} for trigger {trigger}");
                return false;
        }
    }

    public void HandleTime(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.triggerType != ItemTriggerType.OnTimeChanged)
                continue;

            var condition = rule.condition;
            if (condition == null || condition.conditionKind != ItemConditionKind.Time)
                continue;

            float interval = condition.intervalSeconds;
            if (interval <= 0f)
                continue;

            float elapsed = GetRuleElapsedSeconds(i) + deltaSeconds;
            int triggerCount = 0;
            while (elapsed >= interval)
            {
                elapsed -= interval;
                triggerCount++;
            }

            SetRuleElapsedSeconds(i, elapsed);

            for (int t = 0; t < triggerCount; t++)
                ApplyEffects(rule.effects);
        }
    }

    public void ResetRuntimeState()
    {
        triggerCounts.Clear();
        ruleElapsedSeconds.Clear();
    }

    public void AddSellValueBonus(int amount)
    {
        if (amount <= 0)
            return;

        SellValueBonus += amount;
    }

    void IncrementTriggerCount(ItemTriggerType trigger)
    {
        if (triggerCounts.TryGetValue(trigger, out var count))
            triggerCounts[trigger] = count + 1;
        else
            triggerCounts[trigger] = 1;
    }

    int GetTriggerCount(ItemTriggerType trigger)
    {
        return triggerCounts.TryGetValue(trigger, out var count) ? count : 0;
    }

    float GetRuleElapsedSeconds(int ruleIndex)
    {
        return ruleElapsedSeconds.TryGetValue(ruleIndex, out var elapsed) ? elapsed : 0f;
    }

    void SetRuleElapsedSeconds(int ruleIndex, float elapsed)
    {
        ruleElapsedSeconds[ruleIndex] = Mathf.Max(0f, elapsed);
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
