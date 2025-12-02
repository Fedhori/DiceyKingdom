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

    public int Row { get; private set; }
    public int Column { get; private set; }

    readonly List<PinEffectDto> effects;
    public IReadOnlyList<PinEffectDto> Effects => effects;

    public float ScoreMultiplier => Stats.GetValue(PinStatIds.ScoreMultiplier);

    public event Action<int> OnRemainingHitsChanged;

    int remainingHits;

    public int RemainingHits
    {
        get => remainingHits;
        set
        {
            remainingHits = value;
            OnRemainingHitsChanged?.Invoke(remainingHits);
        }
    }

    bool HasCounterGate => BaseDto.hitsToTrigger > 0;

    public PinInstance(PinDto dto, int row, int column, bool registerEventEffects = true)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));

        SetGridPosition(row, column);

        effects = dto.effects != null
            ? new List<PinEffectDto>(dto.effects)
            : new List<PinEffectDto>();

        Stats = new StatSet();
        Stats.SetBase(PinStatIds.ScoreMultiplier, BaseDto.scoreMultiplier);

        if (registerEventEffects)
            PinEffectManager.Instance?.RegisterEventEffects(this);
    }

    public void SetGridPosition(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public void ResetData()
    {
        RemainingHits = BaseDto.hitsToTrigger;
        Stats.RemoveModifiers(StatLayer.Temporary);
    }

    public void OnHitByBall(BallInstance ball, Vector2 position)
    {
        if (ball == null || effects.Count == 0)
            return;

        if (HasCounterGate)
        {
            const int hitCost = 1;
            RemainingHits -= hitCost;

            if (RemainingHits > 0)
                return;

            RemainingHits += BaseDto.hitsToTrigger;
        }

        for (int i = 0; i < effects.Count; i++)
        {
            var effect = effects[i];
            if (!string.IsNullOrEmpty(effect.eventId))
                continue;

            PinEffectManager.Instance?.Apply(effect, ball, this, position);
        }
    }
}
