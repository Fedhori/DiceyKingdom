using System;
using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public sealed class PlayerInstance
{
    public PlayerDto BaseDto { get; }
    public string Id => BaseDto.id;

    public StatSet Stats { get; }

    public double Power => Stats.GetValue(PlayerStatIds.Power);
    public double CriticalChance => Stats.GetValue(PlayerStatIds.CriticalChance);
    public double CriticalMultiplier => Stats.GetValue(PlayerStatIds.CriticalMultiplier);
    public double MoveSpeed => Stats.GetValue(PlayerStatIds.MoveSpeed);
    public double ProjectileSizeMultiplier => Stats.GetValue(PlayerStatIds.ProjectileSizeMultiplier);
    public double ProjectileRandomAngleMultiplier => Stats.GetValue(PlayerStatIds.ProjectileRandomAngleMultiplier);
    public double ProjectileDamageMultiplier => Stats.GetValue(PlayerStatIds.ProjectileDamageMultiplier);
    public bool IsDryIceEnabled => Stats.GetValue(PlayerStatIds.IsDryIceEnabled) > 0.5d;
    public int BaseIncomeBonus => Mathf.FloorToInt((float)Stats.GetValue(PlayerStatIds.BaseIncomeBonus));
    public int PierceBonus => Mathf.Max(0, Mathf.FloorToInt((float)Stats.GetValue(PlayerStatIds.PierceBonus)));
    public int WallBounceCount => Mathf.Max(0, Mathf.FloorToInt((float)Stats.GetValue(PlayerStatIds.WallBounceCount)));
    public IReadOnlyList<string> ItemIds => itemIds;
    public float WorldMoveSpeed => GameConfig.PlayerBaseMoveSpeed * Mathf.Max(0.1f, (float)MoveSpeed);

    // 상점/보상에 사용하는 통화
    public int Currency { get; private set; }
    int guaranteedCriticalHitsRemaining;
    int timedModifierSequence;
    readonly List<TimedModifier> timedModifiers = new();

    public event Action OnCurrencyChanged;
    readonly List<string> itemIds;

    public PlayerInstance(PlayerDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(PlayerStatIds.Power, BaseDto.power, 0d);
        Stats.SetBase(PlayerStatIds.CriticalChance, BaseDto.critChance, 0d, 200d);
        Stats.SetBase(PlayerStatIds.CriticalMultiplier, BaseDto.criticalMultiplier, 1d);
        Stats.SetBase(PlayerStatIds.MoveSpeed, Mathf.Max(0.1f, BaseDto.moveSpeed), 0.1d);
        Stats.SetBase(PlayerStatIds.ProjectileSizeMultiplier, 0d, 0d);
        Stats.SetBase(PlayerStatIds.ProjectileRandomAngleMultiplier, 1d, 0d);
        Stats.SetBase(PlayerStatIds.ProjectileDamageMultiplier, 1d, 0d);
        Stats.SetBase(PlayerStatIds.IsDryIceEnabled, 0d, 0d, 1d);
        Stats.SetBase(PlayerStatIds.BaseIncomeBonus, 0d);
        Stats.SetBase(PlayerStatIds.PierceBonus, 0d, 0d);
        Stats.SetBase(PlayerStatIds.WallBounceCount, 0d, 0d);

        itemIds = BaseDto.itemIds != null ? new List<string>(BaseDto.itemIds) : new List<string>();

        // 시작 통화 셋업
        Currency = Mathf.Max(0, BaseDto.startCurrency);
    }

    public void ResetData()
    {
        // 라운드 단위로 날아가는 임시 버프만 초기화
        Stats.RemoveModifiers(StatLayer.Temporary);
        timedModifiers.Clear();
        timedModifierSequence = 0;
    }

    public void AddGuaranteedCriticalHits(int count)
    {
        if (count <= 0)
            return;

        guaranteedCriticalHitsRemaining = Mathf.Max(0, guaranteedCriticalHitsRemaining) + count;
    }

    public void SetGuaranteedCriticalHits(int count)
    {
        guaranteedCriticalHitsRemaining = Mathf.Max(0, count);
    }

    public void AddTimedModifier(string statId, StatOpKind opKind, double value, float durationSeconds)
    {
        if (string.IsNullOrEmpty(statId) || durationSeconds <= 0f)
            return;

        string source = $"timed:{timedModifierSequence++}";
        Stats.AddModifier(new StatModifier(statId, opKind, value, StatLayer.Temporary, source));
        timedModifiers.Add(new TimedModifier(statId, source, durationSeconds));
    }

    public void TickTimedModifiers(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || timedModifiers.Count == 0)
            return;

        for (int i = timedModifiers.Count - 1; i >= 0; i--)
        {
            var entry = timedModifiers[i];
            entry.RemainingSeconds -= deltaSeconds;
            if (entry.RemainingSeconds <= 0f)
            {
                Stats.RemoveModifiers(entry.StatId, StatLayer.Temporary, entry.Source);
                timedModifiers.RemoveAt(i);
            }
            else
            {
                timedModifiers[i] = entry;
            }
        }
    }

    public bool TryConsumeGuaranteedCriticalHit()
    {
        if (guaranteedCriticalHitsRemaining <= 0)
            return false;

        guaranteedCriticalHitsRemaining--;
        return true;
    }

    struct TimedModifier
    {
        public readonly string StatId;
        public readonly string Source;
        public float RemainingSeconds;

        public TimedModifier(string statId, string source, float remainingSeconds)
        {
            StatId = statId;
            Source = source;
            RemainingSeconds = remainingSeconds;
        }
    }

    public int RollCriticalLevel(System.Random rng, double criticalChance)
    {
        if (rng == null)
            rng = new System.Random();

        if (criticalChance < 0f)
            criticalChance = 0f;

        int criticalLevel = (int)(criticalChance / 100f);
        double chance = Math.Max(0f, criticalChance - criticalLevel * 100f);

        double roll = rng.NextDouble() * 100.0;

        if (chance >= roll)
            criticalLevel++;

        return criticalLevel;
    }

    public double GetCriticalMultiplier(int criticalLevel)
    {
        return Mathf.Max(1f, criticalLevel * (float)CriticalMultiplier);
    }

    public void AddCurrency(int amount)
    {
        if (amount == 0)
            return;

        var newValue = Currency + amount;
        if (newValue < 0)
            newValue = 0;

        if (newValue == Currency)
            return;

        Currency = newValue;
        OnCurrencyChanged?.Invoke();
    }

    public bool TrySpendCurrency(int cost)
    {
        if (cost <= 0)
            return true;

        if (Currency < cost)
            return false;

        Currency -= cost;
        OnCurrencyChanged?.Invoke();
        return true;
    }

    public void SetCurrency(int value)
    {
        int newValue = Mathf.Max(0, value);
        if (newValue == Currency)
            return;

        Currency = newValue;
        OnCurrencyChanged?.Invoke();
    }
}
