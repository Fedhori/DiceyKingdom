using System;
using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public sealed class PinInstance
{
    private PinDto BaseDto { get; }
    public string Id => BaseDto.id;

    public StatSet Stats { get; }

    public int Row { get; private set; }
    public int Column { get; private set; }

    readonly List<PinRuleDto> rules;
    public IReadOnlyList<PinRuleDto> Rules => rules;

    public float ScoreMultiplier => Stats.GetValue(PinStatIds.ScoreMultiplier);
    
    public event Action<int> OnHitCountChanged;
    private int hitCount = 0;
    public int HitCount
    {
        get => hitCount;
        set
        {
            hitCount = value;
            OnHitCountChanged?.Invoke(hitCount);
        }
    }

    int remainingHits;
   
    bool hasCharge;

    public int Price => BaseDto.price;

    public event Action<int> OnRemainingHitsChanged;
    int chargeMax;
    public int RemainingHits
    {
        get => remainingHits;
        private set
        {
            remainingHits = value;
            OnRemainingHitsChanged?.Invoke(remainingHits);
        }
    }

    public int RoundCount = 0;

    public PinInstance(PinDto dto, int row, int column, bool registerEventEffects = true)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        SetGridPosition(row, column);

        rules = dto.rules != null
            ? new List<PinRuleDto>(dto.rules)
            : new List<PinRuleDto>();

        Stats = new StatSet();
        Stats.SetBase(PinStatIds.ScoreMultiplier, BaseDto.scoreMultiplier);

        InitializeChargeState();
    }

    void InitializeChargeState()
    {
        hasCharge = false;
        chargeMax = 0;

        if (rules == null)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            var cond = rule?.condition;
            if (cond == null)
                continue;

            if (cond.conditionKind == PinConditionKind.Charge)
            {
                if (!hasCharge)
                {
                    hasCharge = true;
                    chargeMax = Mathf.Max(1, cond.hits);
                }
                else
                {
                    Debug.LogWarning(
                        $"[PinInstance] '{Id}': Charge 조건이 2개 이상 발견되었습니다. 첫 번째 것만 사용합니다."
                    );
                }
            }
        }
    }

    public void SetGridPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public void ResetData(int hitCnt)
    {
        if (hasCharge && chargeMax > 0)
            RemainingHits = chargeMax;
        else
            RemainingHits = -1;

        Stats.RemoveModifiers(StatLayer.Temporary);
        HitCount = hitCnt;
    }

    public void OnHitByBall(BallInstance ball, Vector2 position)
    {
        if (ball == null)
            return;

        HitCount++;

        HandleTrigger(PinTriggerType.OnBallHit, ball, position);
    }

    public void HandleTrigger(PinTriggerType trigger, BallInstance ball, Vector2 position)
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

            ApplyEffects(rule.effects, ball, position);
        }
    }
    
    bool IsConditionMet(PinConditionDto cond, PinTriggerType trigger)
    {
        if (cond == null)
            return true;
        
        switch (cond.conditionKind)
        {
            case PinConditionKind.Always:
                return true;

            case PinConditionKind.Charge:
                if (!hasCharge || chargeMax <= 0)
                    return false;

                if (trigger != PinTriggerType.OnBallHit)
                    return false;

                RemainingHits -= 1;

                if (RemainingHits > 0)
                    return false;

                RemainingHits += chargeMax;
                return true;

            case PinConditionKind.RoundCount:
            {
                return RoundCount >= cond.round;
            }

            default:
                return false;
        }
    }

    void ApplyEffects(List<PinEffectDto> effects, BallInstance ball, Vector2 position)
    {
        if (effects == null || effects.Count == 0)
            return;

        var effectManager = PinEffectManager.Instance;
        if (effectManager == null)
            return;

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (effect == null)
                continue;

            effectManager.ApplyEffect(effect, ball, this, position);
        }
    }
}
