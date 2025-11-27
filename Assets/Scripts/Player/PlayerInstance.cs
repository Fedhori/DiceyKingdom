using System;
using Data;
using GameStats;
using UnityEngine;

public sealed class PlayerInstance
{
    public PlayerDto BaseDto { get; }
    public string Id => BaseDto.id;

    public StatSet Stats { get; }

    public float ScoreBase => Stats.GetValue(StatIds.ScoreBase);
    public float ScoreMultiplier => Stats.GetValue(StatIds.ScoreMultiplier);
    public float CriticalChance => Stats.GetValue(StatIds.CriticalChance);
    public float CriticalDamage => Stats.GetValue(StatIds.CriticalDamage);

    public PlayerInstance(PlayerDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(StatIds.ScoreBase, BaseDto.scoreBase);
        Stats.SetBase(StatIds.ScoreMultiplier, BaseDto.scoreMultiplier);
        Stats.SetBase(StatIds.CriticalChance, BaseDto.critChance);
        Stats.SetBase(StatIds.CriticalDamage, BaseDto.criticalDamage);
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

    public float GetCriticalDamage(CriticalType criticalType)
    {
        float normalCrit = Mathf.Max(1f, CriticalDamage);
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