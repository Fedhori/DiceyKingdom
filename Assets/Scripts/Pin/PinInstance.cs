using System;
using System.Collections.Generic;
using Data;

public sealed class PinInstance
{
    public PinDto BaseDto { get; }
    public string Id => BaseDto.id;

    public int HitCount { get; private set; }

    public int Row { get; }
    public int Column { get; }

    readonly List<PinEffectDto> effects;
    public IReadOnlyList<PinEffectDto> Effects => effects;

    public PinInstance(PinDto dto, int row, int column)
    {
        BaseDto = dto ?? throw new ArgumentNullException(nameof(dto));
        Row = row;
        Column = column;
        HitCount = 0;

        effects = dto.effects != null
            ? new List<PinEffectDto>(dto.effects)
            : new List<PinEffectDto>();
    }

    public void OnHitByBall(BallInstance ball)
    {
        HitCount++;

        if (ball == null || effects.Count == 0)
            return;

        foreach (var effect in effects)
        {
            PinEffectExecutor.Apply(effect, ball, this);
        }
    }
}