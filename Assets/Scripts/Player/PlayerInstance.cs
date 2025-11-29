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
    public float ScoreMultiplier => Stats.GetValue(PlayerStatIds.ScoreMultiplier);
    public float CriticalChance => Stats.GetValue(PlayerStatIds.CriticalChance);
    public float CriticalMultiplier => Stats.GetValue(PlayerStatIds.CriticalMultiplier);

    // 플레이어가 들고 있는 볼 덱
    public BallDeck BallDeck { get; }

    public PlayerInstance(PlayerDto dto)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(PlayerStatIds.Score, BaseDto.scoreBase);
        Stats.SetBase(PlayerStatIds.ScoreMultiplier, BaseDto.scoreMultiplier);
        Stats.SetBase(PlayerStatIds.CriticalChance, BaseDto.critChance);
        Stats.SetBase(PlayerStatIds.CriticalMultiplier, BaseDto.criticalMultiplier);

        BallDeck = new BallDeck();

        if (BaseDto.ballDeck != null)
        {
            foreach (var entry in BaseDto.ballDeck)
            {
                if (entry == null || string.IsNullOrEmpty(entry.id))
                    continue;

                var count = Mathf.Max(0, entry.count);
                if (count <= 0)
                    continue;

                // 유효하지 않은 ballId는 무시
                if (!BallRepository.IsInitialized ||
                    !BallRepository.TryGet(entry.id, out _))
                {
                    Debug.LogWarning($"[PlayerInstance] Unknown ball id in deck: {entry.id}");
                    continue;
                }

                BallDeck.Add(entry.id, count);
            }
        }
    }

    public void ResetData()
    {
        Stats.RemoveModifiers(StatLayer.Temporary);
        // BallDeck은 '빌드' 개념이라 라운드 리셋에 따라 초기화하지 않는다.
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
