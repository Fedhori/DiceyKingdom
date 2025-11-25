using Data;
using GameStats;
using UnityEngine;

public sealed class BallInstance
{
    public BallDto BaseDto { get; }
    public string Id => BaseDto.id;
    public int BaseScore => BaseDto.baseScore;

    public StatSet Stats { get; }

    public int PersonalScore { get; private set; }

    public BallInstance(BallDto dto)
    {
        BaseDto = dto ?? throw new System.ArgumentNullException(nameof(dto));

        Stats = new StatSet();
        Stats.SetBase(StatIds.Score, BaseScore);

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

        var gained = GetScorePerHit();
        PersonalScore += gained;
        ScoreManager.Instance.AddScore(gained, position);
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
