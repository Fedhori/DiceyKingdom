using System;
using Data;
using GameStats;
using UnityEngine;

public sealed class PlayerInstance
{
    public PlayerDto BaseDto { get; }
    public string Id => BaseDto.id;

    public StatSet Stats { get; }

    public float ScoreBase => Stats.GetValue(PlayerStatIds.Score);
    public float CriticalChance => Stats.GetValue(PlayerStatIds.CriticalChance);
    public float CriticalMultiplier => Stats.GetValue(PlayerStatIds.CriticalMultiplier);

    public PlayerInstance(PlayerDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(PlayerStatIds.Score, BaseDto.scoreBase);
        Stats.SetBase(PlayerStatIds.CriticalChance, BaseDto.critChance);
        Stats.SetBase(PlayerStatIds.CriticalMultiplier, BaseDto.criticalMultiplier);
    }

    public CriticalType RollCriticalType(System.Random rng)
    {
        if (rng == null)
            rng = new System.Random();

        float chance = Mathf.Max(0f, CriticalChance);
        float overChance = Mathf.Max(0f, chance - 100f);
        float baseCritChance = Mathf.Min(chance, 100f);

        double roll = rng.NextDouble() * 100.0;

        if (overChance > 0f && roll < overChance)
            return CriticalType.OverCritical;

        return roll < baseCritChance ? CriticalType.Critical : CriticalType.None;
    }

    public float GetCriticalMultiplier(CriticalType criticalType)
    {
        float normalCrit = Mathf.Max(1f, CriticalMultiplier);
        float overCrit = normalCrit * 2f;

        switch (criticalType)
        {
            case CriticalType.None:
                return 1f;
            case CriticalType.Critical:
                return normalCrit;
            case CriticalType.OverCritical:
                return overCrit;
            default:
                return 1f;
        }
    }
}