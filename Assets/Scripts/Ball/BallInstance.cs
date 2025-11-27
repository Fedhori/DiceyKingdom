using Data;
using GameStats;
using UnityEngine;

public enum CriticalType
{
    None,
    Critical,
    OverCritical,
}

public sealed class BallInstance
{
    static readonly System.Random LocalRandom = new();

    public BallDto BaseDto { get; }
    public string Id => BaseDto.id;
    public int BaseScore => BaseDto.baseScore;

    public StatSet Stats { get; }

    public float CriticalChance => Stats.GetValue(StatIds.CriticalChance);
    public float CriticalDamage => Stats.GetValue(StatIds.CriticalDamage);

    public int PersonalScore { get; private set; }

    public BallInstance(BallDto dto)
    {
        BaseDto = dto ?? throw new System.ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(StatIds.Score, BaseScore);
        Stats.SetBase(StatIds.CriticalChance, BaseDto.critChance);
        Stats.SetBase(StatIds.CriticalDamage, BaseDto.criticalDamage);

        PersonalScore = 0;
    }

    int GetScorePerHit()
    {
        var value = Stats.GetValue(StatIds.Score);
        return Mathf.RoundToInt(value);
    }

    public void OnHitPin(PinInstance pin, Vector2 position)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] ScoreManager is null.");
            return;
        }

        var criticalType = RollCriticalType();
        float criticalDamage = GetCriticalDamage(criticalType);

        var gained = Mathf.RoundToInt(GetScorePerHit() * criticalDamage);
        PersonalScore += gained;
        ScoreManager.Instance.AddScore(gained, criticalType, position);
    }

    public void OnHitBall(BallInstance other)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] ScoreManager is null.");
            return;
        }

        // 나중에 Ball-Ball 충돌에 따른 스탯 효과 추가 가능
    }

    public CriticalType RollCriticalType()
    {
        float chance = Mathf.Max(0f, CriticalChance);
        float overChance = Mathf.Max(0f, chance - 100f);
        float baseCritChance = Mathf.Min(chance, 100f);

        var rng = GameManager.Instance?.Rng ?? LocalRandom;
        double roll = rng.NextDouble() * 100.0;

        if (overChance > 0f && roll < overChance)
            return CriticalType.OverCritical;

        return roll < baseCritChance ? CriticalType.Critical : CriticalType.None;
    }

    float GetCriticalDamage(CriticalType criticalType)
    {
        float normalCrit = Mathf.Max(1f, CriticalDamage);
        float overCrit = normalCrit * 2f;

        return criticalType switch
        {
            CriticalType.None => 1f,
            CriticalType.Critical => normalCrit,
            CriticalType.OverCritical => overCrit,
            _ => 1f
        };
    }
}