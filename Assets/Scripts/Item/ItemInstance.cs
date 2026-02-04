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
    public float ProjectileSizeMultiplier => (float)Stats.GetValue(ItemStatIds.ProjectileSizeMultiplier);
    public float ProjectileSpeed { get; private set; }
    public float ProjectileStationaryStopSeconds { get; private set; }
    public bool IsStationaryProjectile => ProjectileStationaryStopSeconds >= 0f;
    public bool ProjectileAreaDamageTick { get; private set; }
    public bool ProjectileIsHoming => (float)Stats.GetValue(ItemStatIds.ProjectileIsHoming) > 0.5f;
    public string ProjectileKey { get; private set; }
    public ProjectileHitBehavior ProjectileHitBehavior { get; private set; }
    public float ProjectileExplosionLevel => Mathf.Max(0f, (float)Stats.GetValue(ItemStatIds.ProjectileExplosionRadius));
    public float ProjectileExplosionRadius =>
        ProjectileExplosionLevel * GameConfig.ProjectileExplosionRadiusUnit;
    public float ProjectileLifetimeSeconds { get; private set; }
    public float BeamThickness { get; private set; }
    public float BeamDuration { get; private set; }
    public int Pierce => Mathf.Max(0, Mathf.FloorToInt((float)Stats.GetValue(ItemStatIds.Pierce)));
    public int PelletCount { get; private set; }
    public float SpreadAngle { get; private set; }
    public float ProjectileRandomAngle => (float)Stats.GetValue(ItemStatIds.ProjectileRandomAngle);
    public bool IsObject { get; private set; }
    public int PierceBonus { get; private set; }
    public int SellValueBonus => Mathf.Max(0, Mathf.FloorToInt((float)Stats.GetValue(ItemStatIds.SellValueBonus)));
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
    public float CriticalChanceMultiplier => (float)Stats.GetValue(ItemStatIds.CriticalChanceMultiplier);

    private readonly List<ItemRuleDto> rules = new();
    public IReadOnlyList<ItemRuleDto> Rules => rules;
    private readonly List<ItemRuleDto> upgradeRules = new();
    private readonly List<string> upgradeRuleSourceUids = new();
    public IReadOnlyList<ItemRuleDto> UpgradeRules => upgradeRules;

    bool hasNextProjectileDamage;
    float nextProjectileDamageScale = 1f;

    public event Action<ItemEffectDto, ItemInstance, string> OnEffectTriggered;
    public event Action<ItemInstance> OnUpgradeChanged;
    public event Action<ItemInstance> OnUpgradesChanged;

    public float WorldProjectileSize => GameConfig.ItemBaseProjectileSize * Mathf.Max(0.1f, ProjectileSize);
    public float WorldProjectileSpeed =>
        GameConfig.ItemBaseProjectileSpeed * Mathf.Max(0.1f, ProjectileSpeed);
    public float WorldProjectileStationaryStartSpeed =>
        GameConfig.ItemBaseProjectileSpeed * Mathf.Max(0.1f, GameConfig.ProjectileStationaryStartSpeedMultiplier);

    public void SetUpgrades(IReadOnlyList<UpgradeInstance> newUpgrades)
    {
        var previousFirst = Upgrade;
        if (!ReplaceUpgrades(newUpgrades))
            return;

        var currentFirst = Upgrade;
        if (!ReferenceEquals(previousFirst, currentFirst))
            OnUpgradeChanged?.Invoke(this);

        OnUpgradesChanged?.Invoke(this);
        ClearNextProjectileDamage();
    }

    public void SetUpgradeRules(IReadOnlyList<ItemRuleDto> newRules)
    {
        if (newRules == null || newRules.Count == 0)
        {
            if (upgradeRules.Count == 0)
                return;

            upgradeRules.Clear();
            upgradeRuleSourceUids.Clear();
            upgradeRuleElapsedSeconds.Clear();
            ClearNextProjectileDamage();
            return;
        }

        upgradeRules.Clear();
        upgradeRuleSourceUids.Clear();
        for (int i = 0; i < newRules.Count; i++)
        {
            var rule = newRules[i];
            if (rule != null)
                upgradeRules.Add(rule);
        }

        upgradeRuleElapsedSeconds.Clear();
        ClearNextProjectileDamage();
    }

    public void SetUpgradeRuleSourceUids(List<string> sources)
    {
        upgradeRuleSourceUids.Clear();
        if (sources == null || sources.Count == 0)
            return;

        for (int i = 0; i < sources.Count; i++)
        {
            var value = sources[i];
            upgradeRuleSourceUids.Add(value);
        }
    }

    readonly Dictionary<ItemTriggerType, int> triggerCounts = new();
    readonly Dictionary<int, float> ruleElapsedSeconds = new();
    readonly Dictionary<int, float> upgradeRuleElapsedSeconds = new();
    readonly Dictionary<int, float> ruleCooldownRemaining = new();
    readonly Dictionary<int, float> upgradeRuleCooldownRemaining = new();
    readonly List<int> cooldownKeys = new();
    readonly StatSet triggerRepeatStats = new();
    readonly Dictionary<string, double> statStacks = new();

    public ItemInstance(ItemDto dto, string uniqueId = null)
    {
        UniqueId = string.IsNullOrEmpty(uniqueId) ? Guid.NewGuid().ToString() : uniqueId;
        Stats = new StatSet();
        Stats.SetBase(ItemStatIds.CriticalChanceMultiplier, 1d, 0d);
        Stats.SetBase(ItemStatIds.ProjectileSizeMultiplier, 0d, 0d);
        Stats.SetBase(ItemStatIds.ProjectileRandomAngle, 0d, 0d);
        Stats.SetBase(ItemStatIds.ProjectileIsHoming, 0d, 0d);
        Stats.SetBase(ItemStatIds.ProjectileExplosionRadius, 0d, 0d);
        Stats.SetBase(ItemStatIds.SellValueBonus, 0d, 0d);

        if (dto == null || string.IsNullOrEmpty(dto.id))
        {
            Debug.LogError("[ItemInstance] Invalid dto");
            Id = string.Empty;
            Stats.SetBase(ItemStatIds.DamageMultiplier, 0d, 0d);
            Stats.SetBase(ItemStatIds.AttackSpeed, 0d, 0d);
            ProjectileSize = 1f;
            ProjectileSpeed = 1f;
            ProjectileStationaryStopSeconds = -1f;
            ProjectileAreaDamageTick = false;
            ProjectileKey = string.Empty;
            ProjectileHitBehavior = ProjectileHitBehavior.Normal;
            ProjectileLifetimeSeconds = 0f;
            BeamThickness = 0f;
            BeamDuration = 0f;
            PelletCount = 1;
            SpreadAngle = 0f;
            IsObject = false;
            PierceBonus = 0;
            StatusDamageMultiplier = 1f;
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
            ProjectileStationaryStopSeconds = projectile.stationaryStopSeconds;
            ProjectileAreaDamageTick = projectile.areaDamageTick;
            ProjectileHitBehavior = projectile.hitBehavior;
            Stats.SetBase(ItemStatIds.ProjectileExplosionRadius, Mathf.Max(0f, projectile.explosion), 0d);
            ProjectileLifetimeSeconds = Mathf.Max(0f, projectile.lifetime);
            basePierce = projectile.pierce;
            PelletCount = Mathf.Max(1, projectile.pelletCount);
            SpreadAngle = Mathf.Max(0f, projectile.spreadAngle);
            Stats.SetBase(ItemStatIds.ProjectileRandomAngle, Mathf.Max(0f, projectile.randomAngle), 0d);
            Stats.SetBase(ItemStatIds.ProjectileIsHoming, projectile.isHoming ? 1d : 0d, 0d, 1d);
        }
        else
        {
            ProjectileKey = string.Empty;
            ProjectileSize = 1f;
            ProjectileSpeed = 1f;
            ProjectileStationaryStopSeconds = -1f;
            ProjectileAreaDamageTick = false;
            ProjectileHitBehavior = ProjectileHitBehavior.Normal;
            ProjectileLifetimeSeconds = 0f;
            PelletCount = 1;
            SpreadAngle = 0f;
            Stats.SetBase(ItemStatIds.ProjectileRandomAngle, 0d, 0d);
            Stats.SetBase(ItemStatIds.ProjectileIsHoming, 0d, 0d, 1d);
            Stats.SetBase(ItemStatIds.ProjectileExplosionRadius, 0d, 0d);
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
        if (rules.Count == 0 && upgradeRules.Count == 0)
            return;

        IncrementTriggerCount(trigger);

        HandleTriggerRules(rules, trigger);
        HandleTriggerRules(upgradeRules, trigger);
    }

    void HandleTriggerRules(List<ItemRuleDto> targetRules, ItemTriggerType trigger)
    {
        if (targetRules == null || targetRules.Count == 0)
            return;

        bool isUpgradeRule = ReferenceEquals(targetRules, upgradeRules);
        for (int i = 0; i < targetRules.Count; i++)
        {
            var rule = targetRules[i];
            if (rule == null)
                continue;

            if (rule.triggerType != trigger)
                continue;

            if (!IsConditionMet(rule.condition, trigger, i, isUpgradeRule))
                continue;

            string sourceUid = null;
            if (ReferenceEquals(targetRules, upgradeRules) && i < upgradeRuleSourceUids.Count)
                sourceUid = upgradeRuleSourceUids[i];

            ApplyEffects(rule.effects, sourceUid);
        }
    }

    bool IsConditionMet(ItemConditionDto condition, ItemTriggerType trigger, int ruleIndex, bool isUpgradeRule)
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
                if (trigger == ItemTriggerType.OnTimeChanged)
                    return false;

                float interval = condition.intervalSeconds;
                if (interval <= 0f)
                    return false;

                var cooldowns = isUpgradeRule ? upgradeRuleCooldownRemaining : ruleCooldownRemaining;
                if (cooldowns.TryGetValue(ruleIndex, out var remaining) && remaining > 0f)
                    return false;

                cooldowns[ruleIndex] = interval;
                return true;
            default:
                Debug.LogWarning($"[ItemInstance] Unsupported condition {condition.conditionKind} for trigger {trigger}");
                return false;
        }
    }

    public void HandleTime(float deltaSeconds)
    {
        if (deltaSeconds <= 0f)
            return;

        HandleTimeRules(rules, ruleElapsedSeconds, deltaSeconds);
        HandleTimeRules(upgradeRules, upgradeRuleElapsedSeconds, deltaSeconds);
        TickRuleCooldowns(ruleCooldownRemaining, deltaSeconds);
        TickRuleCooldowns(upgradeRuleCooldownRemaining, deltaSeconds);
    }

    void HandleTimeRules(List<ItemRuleDto> targetRules, Dictionary<int, float> elapsedSeconds, float deltaSeconds)
    {
        if (targetRules == null || targetRules.Count == 0)
            return;

        for (int i = 0; i < targetRules.Count; i++)
        {
            var rule = targetRules[i];
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

            float elapsed = GetRuleElapsedSeconds(elapsedSeconds, i) + deltaSeconds;
            int triggerCount = 0;
            while (elapsed >= interval)
            {
                elapsed -= interval;
                triggerCount++;
            }

            SetRuleElapsedSeconds(elapsedSeconds, i, elapsed);

            string sourceUid = null;
            if (ReferenceEquals(targetRules, upgradeRules) && i < upgradeRuleSourceUids.Count)
                sourceUid = upgradeRuleSourceUids[i];

            for (int t = 0; t < triggerCount; t++)
                ApplyEffects(rule.effects, sourceUid);
        }
    }

    public void ResetRuntimeState()
    {
        triggerCounts.Clear();
        ruleElapsedSeconds.Clear();
        upgradeRuleElapsedSeconds.Clear();
        ruleCooldownRemaining.Clear();
        upgradeRuleCooldownRemaining.Clear();
        statStacks.Clear();
        ClearNextProjectileDamage();
    }

    public double ModifyStatStack(string statId, StatOpKind opKind, double value, double? minValue, double? maxValue)
    {
        if (string.IsNullOrEmpty(statId))
            return 0d;

        if (!statStacks.TryGetValue(statId, out var current))
            current = 0d;

        switch (opKind)
        {
            case StatOpKind.Add:
                current += value;
                break;
            case StatOpKind.Override:
                current = value;
                break;
            case StatOpKind.Mult:
                current *= (1d + value);
                break;
            default:
                current += value;
                break;
        }

        if (minValue.HasValue)
            current = Math.Max(minValue.Value, current);
        if (maxValue.HasValue)
            current = Math.Min(maxValue.Value, current);

        statStacks[statId] = current;
        return current;
    }

    void TickRuleCooldowns(Dictionary<int, float> cooldowns, float deltaSeconds)
    {
        if (cooldowns == null || cooldowns.Count == 0 || deltaSeconds <= 0f)
            return;

        cooldownKeys.Clear();
        foreach (var entry in cooldowns)
            cooldownKeys.Add(entry.Key);

        for (int i = 0; i < cooldownKeys.Count; i++)
        {
            int key = cooldownKeys[i];
            float remaining = cooldowns[key] - deltaSeconds;
            if (remaining <= 0f)
                cooldowns.Remove(key);
            else
                cooldowns[key] = remaining;
        }
    }

    public void TryChargeNextProjectileDamage()
    {
        if (hasNextProjectileDamage)
            return;

        float scale = GetTotalNextProjectileDamageScale();
        if (scale <= 1f)
            return;

        nextProjectileDamageScale = scale;
        hasNextProjectileDamage = true;
    }


    public float ConsumeNextProjectileDamageScale()
    {
        if (!hasNextProjectileDamage)
            return 1f;

        float scale = nextProjectileDamageScale;
        ClearNextProjectileDamage();
        return scale;
    }

    public bool IsWeapon()
    {
        return DamageMultiplier > 0f;
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

    public void AddTriggerRepeatModifier(ItemTriggerType trigger, StatOpKind opKind, double value, StatLayer layer, string source, int priority = 0)
    {
        if (trigger == ItemTriggerType.Unknown)
            return;

        var statId = GetTriggerRepeatStatId(trigger);
        triggerRepeatStats.SetBase(statId, 1d, 1d, null);
        triggerRepeatStats.AddModifier(new StatModifier(statId, opKind, value, layer, source, priority));
    }

    public void RemoveTriggerRepeatModifiers(StatLayer? layer = null, string source = null)
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

    float GetRuleElapsedSeconds(Dictionary<int, float> elapsedSeconds, int ruleIndex)
    {
        if (elapsedSeconds == null)
            return 0f;

        return elapsedSeconds.TryGetValue(ruleIndex, out var elapsed) ? elapsed : 0f;
    }

    void SetRuleElapsedSeconds(Dictionary<int, float> elapsedSeconds, int ruleIndex, float elapsed)
    {
        if (elapsedSeconds == null)
            return;

        elapsedSeconds[ruleIndex] = Mathf.Max(0f, elapsed);
    }

    void ClearNextProjectileDamage()
    {
        hasNextProjectileDamage = false;
        nextProjectileDamageScale = 1f;
    }

    float GetTotalNextProjectileDamageScale()
    {
        float total = 0f;
        AccumulateNextProjectileDamageScale(rules, ref total);
        AccumulateNextProjectileDamageScale(upgradeRules, ref total);
        return total;
    }

    static void AccumulateNextProjectileDamageScale(List<ItemRuleDto> targetRules, ref float total)
    {
        if (targetRules == null || targetRules.Count == 0)
            return;

        for (int i = 0; i < targetRules.Count; i++)
        {
            var rule = targetRules[i];
            if (rule == null || rule.effects == null || rule.effects.Count == 0)
                continue;

            for (int j = 0; j < rule.effects.Count; j++)
            {
                var effect = rule.effects[j];
                if (effect == null || effect.effectType != ItemEffectType.ChargeNextProjectileDamage)
                    continue;

                total += Mathf.Max(0f, effect.value);
            }
        }
    }

    void ApplyEffects(List<ItemEffectDto> effects, string sourceUid = null)
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

            OnEffectTriggered?.Invoke(effect, this, sourceUid);
        }
    }
}
