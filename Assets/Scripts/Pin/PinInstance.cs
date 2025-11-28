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

    public int HitCount { get; private set; }

    public int Row { get; }
    public int Column { get; }

    readonly List<PinEffectDto> effects;
    public IReadOnlyList<PinEffectDto> Effects => effects;

    public float ScoreMultiplier => Stats.GetValue(PinStatIds.ScoreMultiplier);

    // --- hitsToTrigger 관련 런타임 상태 ---
    int remainingHits;

    bool HasCounterGate => BaseDto.hitsToTrigger > 0;

    public PinInstance(PinDto dto, int row, int column)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));
        Row = row;
        Column = column;
        HitCount = 0;

        effects = dto.effects != null
            ? new List<PinEffectDto>(dto.effects)
            : new List<PinEffectDto>();

        Stats = new StatSet();
        Stats.SetBase(PinStatIds.ScoreMultiplier, BaseDto.scoreMultiplier);

        // hitsToTrigger 초기화
        remainingHits = BaseDto.hitsToTrigger;

        // 이벤트 기반 효과 등록
        PinEffectManager.Instance?.RegisterEventEffects(this);
    }

    /// <summary>
    /// 핀이 볼과 맞았을 때 호출.
    /// 앞으로 확장성을 위해 hitCost를 따로 두고 싶으면
    /// 이 메서드 안에서 상수 1 대신 파라미터로 바꾸면 된다.
    /// </summary>
    public void OnHitByBall(BallInstance ball, Vector2 position)
    {
        HitCount++;

        if (ball == null || effects.Count == 0)
            return;

        // 1) hitsToTrigger 게이트 처리
        if (HasCounterGate)
        {
            // 지금은 항상 1씩 감소. 나중에 확장하려면 여기서 cost 변수로 분리.
            const int hitCost = 1;
            remainingHits -= hitCost;

            if (remainingHits > 0)
            {
                // 아직 발동 조건 미달 → 효과 실행 안 함
                return;
            }
            
            remainingHits += BaseDto.hitsToTrigger;
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