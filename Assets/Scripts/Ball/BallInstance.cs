using Data;
using UnityEngine;
using System.Collections.Generic;
using GameStats;

public sealed class BallInstance
{
    static readonly System.Random LocalRandom = new();

    public BallDto BaseDto { get; }
    public string Id => BaseDto.id;

    public double ScoreMultiplier => Stats.GetValue(BallStatIds.ScoreMultiplier);
    public double CriticalMultiplier => Stats.GetValue(BallStatIds.CriticalMultiplier);
    public int life;
    
    public float PendingSpeedFactor { get; set; } = 1f;
    public float PendingSizeFactor { get; set; } = 1f;

    readonly System.Random localRandom = new();

    readonly List<BallRuleDto> rules;
    public IReadOnlyList<BallRuleDto> Rules => rules;

    public StatSet Stats { get; }

    public BallInstance(BallDto dto)
    {
        BaseDto = dto ?? throw new System.ArgumentNullException(nameof(dto));

        rules = dto.rules != null
            ? new List<BallRuleDto>(dto.rules)
            : new List<BallRuleDto>();

        Stats = new StatSet();
        Stats.SetBase(BallStatIds.ScoreMultiplier, BaseDto.ballScoreMultiplier);
        Stats.SetBase(BallStatIds.CriticalMultiplier, BaseDto.criticalMultiplier);
        life = BaseDto.life;
    }

    public void OnHitPin(PinInstance pin, Vector2 position)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] ScoreManager is null.");
            return;
        }

        ScoreManager.Instance.CalculateScore(this, pin, position);
        HandleTrigger(BallTriggerType.OnBallHitPin, null, pin, position);
    }

    public void OnHitBall(BallInstance other, Vector2 position)
    {
        if (other == null)
            return;

        HandleTrigger(BallTriggerType.OnBallHitBall, other, null, position);
    }

    void HandleTrigger(BallTriggerType trigger, BallInstance otherBall, PinInstance pin, Vector2 position)
    {
        if (rules == null || rules.Count == 0)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule == null)
                continue;

            if (rule.triggerType != trigger)
                continue;

            if (!IsConditionMet(rule.condition, trigger))
                continue;

            ApplyEffects(rule.effects, otherBall, pin, position);
        }
    }

    bool IsConditionMet(BallConditionDto cond, BallTriggerType trigger)
    {
        if (cond == null)
            return true;

        switch (cond.conditionKind)
        {
            case BallConditionKind.Always:
                return true;
            default:
                return false;
        }
    }

    void ApplyEffects(List<BallEffectDto> effects, BallInstance otherBall, PinInstance pin, Vector2 position)
    {
        if (effects == null || effects.Count == 0)
            return;

        var effectManager = BallEffectManager.Instance;
        if (effectManager == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            effectManager.ApplyEffect(effect, this, otherBall, pin, position);
        }
    }
}
