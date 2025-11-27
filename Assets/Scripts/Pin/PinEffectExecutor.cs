using System;
using Data;
using GameStats;
using UnityEngine;

public static class PinEffectExecutor
{
    public static void Apply(PinEffectDto dto, BallInstance ball, PinInstance pin)
    {
        if (dto == null || ball == null)
            return;

        switch (dto.type)
        {
            case "statmodifier":
                ApplyStatModifier(dto, ball, pin);
                break;
            
            default:
                Debug.LogWarning($"[PinEffectExecutor] Unsupported effect type: {dto.type}");
                break;
        }
    }

    static void ApplyStatModifier(PinEffectDto dto, BallInstance ball, PinInstance pin)
    {
        var opKind = dto.mode.Equals("Add", StringComparison.OrdinalIgnoreCase)
            ? StatOpKind.Add
            : StatOpKind.Mult;

        var layer = dto.temporary ? StatLayer.Temporary : StatLayer.Permanent;

        ball.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: opKind,
            value: dto.value,
            layer: layer,
            source: pin
        ));
    }
}