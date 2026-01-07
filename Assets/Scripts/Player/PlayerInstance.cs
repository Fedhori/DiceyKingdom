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
    public bool IsOverflowDamageEnabled => Stats.GetValue(PlayerStatIds.IsOverflowDamage) > 0.5d;
    public bool IsSideWallCollisionEnabled => Stats.GetValue(PlayerStatIds.SideWallCollisionEnabled) > 0.5d;
    public int PierceBonus => Mathf.Max(0, Mathf.FloorToInt((float)Stats.GetValue(PlayerStatIds.PierceBonus)));
    public IReadOnlyList<string> ItemIds => itemIds;
    public float WorldMoveSpeed => GameConfig.PlayerBaseMoveSpeed * Mathf.Max(0.1f, (float)MoveSpeed);

    // 상점/보상에 사용하는 통화
    public int Currency { get; private set; }

    public event Action<int> OnCurrencyChanged;
    readonly List<string> itemIds;

    public PlayerInstance(PlayerDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(PlayerStatIds.Power, BaseDto.power, 0d);
        Stats.SetBase(PlayerStatIds.CriticalChance, BaseDto.critChance, 0d, 200d);
        Stats.SetBase(PlayerStatIds.CriticalMultiplier, BaseDto.criticalMultiplier, 1d);
        Stats.SetBase(PlayerStatIds.MoveSpeed, Mathf.Max(0.1f, BaseDto.moveSpeed), 0.1d);
        Stats.SetBase(PlayerStatIds.ProjectileSizeMultiplier, 1d, 0.1d);
        Stats.SetBase(PlayerStatIds.IsOverflowDamage, 0d, 0d, 1d);
        Stats.SetBase(PlayerStatIds.SideWallCollisionEnabled, 0d, 0d, 1d);
        Stats.SetBase(PlayerStatIds.PierceBonus, 0d, 0d);

        itemIds = BaseDto.itemIds != null ? new List<string>(BaseDto.itemIds) : new List<string>();

        // 시작 통화 셋업
        Currency = Mathf.Max(0, BaseDto.startCurrency);
    }

    public void ResetData()
    {
        // 라운드 단위로 날아가는 임시 버프만 초기화
        Stats.RemoveModifiers(StatLayer.Temporary);
    }

    public int RollCriticalLevel(System.Random rng)
    {
        if (rng == null)
            rng = new System.Random();

        int criticalLevel = (int)(CriticalChance / 100f);
        double chance = Math.Max(0f, CriticalChance - criticalLevel * 100f);

        double roll = rng.NextDouble() * 100.0;

        if (chance >= roll)
            criticalLevel++;

        return criticalLevel;
    }

    public double GetCriticalMultiplier(int criticalLevel)
    {
        return Mathf.Max(1f, criticalLevel * 2f);
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
        OnCurrencyChanged?.Invoke(Currency);
    }

    public bool TrySpendCurrency(int cost)
    {
        if (cost <= 0)
            return true;

        if (Currency < cost)
            return false;

        Currency -= cost;
        OnCurrencyChanged?.Invoke(Currency);
        return true;
    }
}
