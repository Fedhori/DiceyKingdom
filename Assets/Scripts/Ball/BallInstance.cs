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
    const string CritChanceStatId = "critChance";
    const string CritMultiplierStatId = "critMultiplier";

    static readonly System.Random LocalRandom = new();

    public BallDto BaseDto { get; }
    public string Id => BaseDto.id;
    public int BaseScore => BaseDto.baseScore;

    public StatSet Stats { get; }

    public float CritChance => Stats.GetValue(CritChanceStatId);
    public float CriticalMultiplier => Stats.GetValue(CritMultiplierStatId);

    public int PersonalScore { get; private set; }

    public BallInstance(BallDto dto)
    {
        BaseDto = dto ?? throw new System.ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(StatIds.Score, BaseScore);
        Stats.SetBase(CritChanceStatId, BaseDto.critChance);
        Stats.SetBase(CritMultiplierStatId, BaseDto.criticalMultiplier);

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
        float critMultiplier = GetCriticalMultiplier(criticalType);

        var gained = Mathf.RoundToInt(GetScorePerHit() * critMultiplier);
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
        float chance = Mathf.Max(0f, CritChance);
        float overChance = Mathf.Max(0f, chance - 100f);
        float baseCritChance = Mathf.Min(chance, 100f);

        var rng = GameManager.Instance?.Rng ?? LocalRandom;
        double roll = rng.NextDouble() * 100.0;

        if (overChance > 0f && roll < overChance)
            return CriticalType.OverCritical;

        return roll < baseCritChance ? CriticalType.Critical : CriticalType.None;
    }

    float GetCriticalMultiplier(CriticalType criticalType)
    {
        float normalCrit = Mathf.Max(1f, CriticalMultiplier);
        float overCrit = normalCrit * 2f;

        return criticalType switch
        {
            CriticalType.None => 1f,
            CriticalType.Critical => normalCrit,
            CriticalType.OverCritical => overCrit,
            _ => 1f
        };
    }

    public void AddTemporaryScoreMultiplier(float ratio, object source)
    {
        if (Mathf.Approximately(ratio, 0f))
            return;

        Stats.AddModifier(new StatModifier(
            statId: StatIds.Score,
            opKind: StatOpKind.Mult,
            value: ratio,
            layer: StatLayer.Temporary,
            source: source));
    }

    public void AddPermanentScoreBonus(int amount, object source)
    {
        if (amount == 0)
            return;

        Stats.AddModifier(new StatModifier(
            statId: StatIds.Score,
            opKind: StatOpKind.Add,
            value: amount,
            layer: StatLayer.Permanent,
            source: source));
    }

    public void RemoveModifiersFromSource(object source)
    {
        if (source == null) return;
        Stats.RemoveModifiers(layer: null, source: source);
    }
}
