using System;
using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public sealed class PinInstance
{
    public PinDto BaseDto { get; }
    public string Id => BaseDto.id;

    public StatSet Stats { get; }

    public int Row { get; }
    public int Column { get; }

    readonly List<PinEffectDto> effects;
    public IReadOnlyList<PinEffectDto> Effects => effects;

    public float ScoreMultiplier => Stats.GetValue(PinStatIds.ScoreMultiplier);

    public event Action<int> OnRemainingHitsChanged;
    public int RemainingHits
    {
        get => remainingHits;
        set
        {
            remainingHits = value;
            OnRemainingHitsChanged?.Invoke(remainingHits);
        }
    }
    private int remainingHits;

    bool HasCounterGate => BaseDto.hitsToTrigger > 0;

    public PinInstance(PinDto dto, int row, int column)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));
        Row = row;
        Column = column;

        effects = dto.effects != null
            ? new List<PinEffectDto>(dto.effects)
            : new List<PinEffectDto>();

        Stats = new StatSet();
        Stats.SetBase(PinStatIds.ScoreMultiplier, BaseDto.scoreMultiplier);

        // 이벤트 기반 효과 등록
        PinEffectManager.Instance?.RegisterEventEffects(this);
    }

    public void InitializeAfterLink()
    {
        RemainingHits = BaseDto.hitsToTrigger;
    }
    
    public void OnHitByBall(BallInstance ball, Vector2 position)
    {
        if (ball == null || effects.Count == 0)
            return;

        // 1) hitsToTrigger 게이트 처리
        if (HasCounterGate)
        {
            // 지금은 항상 1씩 감소. 나중에 확장하려면 여기서 cost 변수로 분리.
            const int hitCost = 1;
            RemainingHits -= hitCost;

            if (RemainingHits > 0)
            {
                // 아직 발동 조건 미달 → 효과 실행 안 함
                return;
            }
            
            RemainingHits += BaseDto.hitsToTrigger;
        }

        // 2) eventId 없는 OnHit 효과들만 실행
        foreach (var effect in effects)
        {
            if (!string.IsNullOrEmpty(effect.eventId))
                continue;

            PinEffectManager.Instance?.Apply(effect, ball, this, position);
        }
    }
}