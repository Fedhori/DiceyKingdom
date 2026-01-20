using System;
using System.Collections.Generic;
using UnityEngine;
using Data;
using GameStats;

public sealed class ItemInstance
{
    public string Id { get; }
    public string UniqueId { get; }

    public float StatusDamageMultiplier { get; private set; }
    public float ProjectileSize { get; private set; }
    public float ProjectileSpeed { get; private set; }
    public string ProjectileKey { get; private set; }
    public ProjectileHitBehavior ProjectileHitBehavior { get; private set; }
    public float ProjectileExplosionRadius { get; private set; }
    public float BeamThickness { get; private set; }
    public float BeamDuration { get; private set; }
    public int Pierce => Mathf.Max(0, Mathf.FloorToInt((float)Stats.GetValue(ItemStatIds.Pierce)));
    public int PelletCount { get; private set; }
    public float SpreadAngle { get; private set; }
    public float ProjectileRandomAngle { get; private set; }
    public bool IsObject { get; private set; }
    public int PierceBonus { get; private set; }
    public float ProjectileHomingTurnRate { get; private set; }
    public int SellValueBonus { get; private set; }
    public ItemRarity Rarity { get; private set; }
    public UpgradeInstance Upgrade
    {
        get => upgrades.Count > 0 ? upgrades[0] : null;
        set
        {
            if (value == null)
            {
                SetUpgrades(null);
                return;
            }

            SetUpgrades(new[] { value });
        }
    }
    public IReadOnlyList<UpgradeInstance> Upgrades => upgrades;
    readonly List<UpgradeInstance> upgrades = new();

    public StatSet Stats { get; }
    public float DamageMultiplier => (float)Stats.GetValue(ItemStatIds.DamageMultiplier);
    public float AttackSpeed => (float)Stats.GetValue(ItemStatIds.AttackSpeed);

    private readonly List<ItemRuleDto> rules = new();
    public IReadOnlyList<ItemRuleDto> Rules => rules;

    public event Action<ItemEffectDto, ItemInstance> OnEffectTriggered;
    public event Action<ItemInstance> OnUpgradeChanged;
    public event Action<ItemInstance> OnUpgradesChanged;

    public float WorldProjectileSize => GameConfig.ItemBaseProjectileSize * Mathf.Max(0.1f, ProjectileSize);
    public float WorldProjectileSpeed => GameConfig.ItemBaseProjectileSpeed * Mathf.Max(0.1f, ProjectileSpeed);

    public void SetUpgrades(IReadOnlyList<UpgradeInstance> newUpgrades)
    {
        var previousFirst = Upgrade;
        if (!ReplaceUpgrades(newUpgrades))
            return;

        var currentFirst = Upgrade;
        if (!ReferenceEquals(previousFirst, currentFirst))
            OnUpgradeChanged?.Invoke(this);

        OnUpgradesChanged?.Invoke(this);
    }

    readonly Dictionary<ItemTriggerType, int> triggerCounts = new();
    readonly Dictionary<int, float> ruleElapsedSeconds = new();
    readonly StatSet triggerRepeatStats = new();

    public ItemInstance(ItemDto dto)
    {
        UniqueId = Guid.NewGuid().ToString();
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
            ProjectileExplosionRadius = 0f;
            BeamThickness = 0f;
            BeamDuration = 0f;
            PelletCount = 1;
            SpreadAngle = 0f;
            ProjectileRandomAngle = 0f;
            IsObject = false;
            PierceBonus = 0;
            ProjectileHomingTurnRate = 0f;
            StatusDamageMultiplier = 1f;
            SellValueBonus = 0;
            Rarity = ItemRarity.Common;
            Stats.SetBase(ItemStatIds.Pierce, 0d, 0d);
            return;
        }

        Id = dto.id;
        StatusDamageMultiplier = Mathf.Max(0f, dto.statusDamageMultiplier);
        Stats.SetBase(ItemStatIds.DamageMultiplier, Mathf.Max(0f, dto.damageMultiplier), 0d);
        Stats.SetBase(ItemStatIds.AttackSpeed, Mathf.Max(0f, dto.attackSpeed), 0d);
        IsObject = dto.isObject;
        PierceBonus = Mathf.Max(0, dto.pierceBonus);
        SellValueBonus = 0;
        Rarity = dto.rarity;

        var statusKeys = StatusUtil.Keys;
        for (int i = 0; i < statusKeys.Count; i++)
        {
            string key = statusKeys[i];
            int value = StatusUtil.GetItemStatusBaseValue(dto, key);
            Stats.SetBase(key, Mathf.Max(0, value), 0d);
        }

        if (dto.rules != null)
            rules.AddRange(dto.rules);

        var projectile = dto.projectile;
        int basePierce = 0;
        if (projectile != null)
        {
            ProjectileKey = projectile.key ?? string.Empty;
            ProjectileSize = Mathf.Max(0.1f, projectile.size);
            ProjectileSpeed = Mathf.Max(0.1f, projectile.speed);
            ProjectileHitBehavior = projectile.hitBehavior;
            ProjectileExplosionRadius = Mathf.Max(0f, projectile.explosionRadius);
            basePierce = projectile.pierce;
            PelletCount = Mathf.Max(1, projectile.pelletCount);
            SpreadAngle = Mathf.Max(0f, projectile.spreadAngle);
            ProjectileRandomAngle = Mathf.Max(0f, projectile.randomAngle);
            ProjectileHomingTurnRate = Mathf.Max(0f, projectile.homingTurnRate);
        }
        else
        {
            ProjectileKey = string.Empty;
            ProjectileSize = 1f;
            ProjectileSpeed = 1f;
            ProjectileHitBehavior = ProjectileHitBehavior.Normal;
            ProjectileExplosionRadius = 0f;
            PelletCount = 1;
            SpreadAngle = 0f;
            ProjectileRandomAngle = 0f;
            ProjectileHomingTurnRate = 0f;
        }

        if (dto.beam != null)
        {
            BeamThickness = Mathf.Max(0f, dto.beam.thickness);
            BeamDuration = Mathf.Max(0f, dto.beam.duration);
        }
        else
        {
            BeamThickness = 0f;
            BeamDuration = 0f;
        }

        Stats.SetBase(ItemStatIds.Pierce, Mathf.Max(0, basePierce), 0d);
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

    public bool IsWeapon()
    {
        return DamageMultiplier > 0f;
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

    public int GetTriggerRepeat(ItemTriggerType trigger)
    {
        if (trigger == ItemTriggerType.Unknown)
            return 1;

        var statId = GetTriggerRepeatStatId(trigger);
        triggerRepeatStats.SetBase(statId, 1d, 1d, null);
        double value = triggerRepeatStats.GetValue(statId);
        return Mathf.Max(1, Mathf.FloorToInt((float)value));
    }

    public void AddTriggerRepeatModifier(ItemTriggerType trigger, StatOpKind opKind, double value, StatLayer layer, object source, int priority = 0)
    {
        if (trigger == ItemTriggerType.Unknown)
            return;

        var statId = GetTriggerRepeatStatId(trigger);
        triggerRepeatStats.SetBase(statId, 1d, 1d, null);
        triggerRepeatStats.AddModifier(new StatModifier(statId, opKind, value, layer, source, priority));
    }

    public void RemoveTriggerRepeatModifiers(StatLayer? layer = null, object source = null)
    {
        triggerRepeatStats.RemoveModifiers(layer, source);
    }

    static string GetTriggerRepeatStatId(ItemTriggerType trigger)
    {
        return $"triggerRepeat.{trigger}";
    }

    bool ReplaceUpgrades(IReadOnlyList<UpgradeInstance> newUpgrades)
    {
        if (newUpgrades == null || newUpgrades.Count == 0)
        {
            if (upgrades.Count == 0)
                return false;

            upgrades.Clear();
            return true;
        }

        bool changed = upgrades.Count != newUpgrades.Count;
        if (!changed)
        {
            for (int i = 0; i < upgrades.Count; i++)
            {
                if (!ReferenceEquals(upgrades[i], newUpgrades[i]))
                {
                    changed = true;
                    break;
                }
            }
        }

        if (!changed)
            return false;

        upgrades.Clear();
        for (int i = 0; i < newUpgrades.Count; i++)
        {
            var upgrade = newUpgrades[i];
            if (upgrade != null)
                upgrades.Add(upgrade);
        }

        return true;
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
