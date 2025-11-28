using System;
using Data;
using GameStats;
using UnityEngine;

public static class PinEffectExecutor
{
    public static void Apply(PinEffectDto dto, BallInstance ball, PinInstance pin)
    {
        var player = PlayerManager.Instance?.Current;

        if (dto == null || player == null)
            return;

        switch (dto.type)
        {
            case "modifyPlayerStat":
                ModifyPlayerStat(dto, player, pin);
                break;

            default:
                Debug.LogWarning($"[PinEffectExecutor] Unsupported effect type: {dto.type}");
                break;
        }
    }

    static void ModifyPlayerStat(PinEffectDto dto, PlayerInstance player, PinInstance pin)
    {
        var opKind = dto.mode.Equals("Add", StringComparison.OrdinalIgnoreCase)
            ? StatOpKind.Add
            : StatOpKind.Mult;

        var layer = dto.temporary ? StatLayer.Temporary : StatLayer.Permanent;

        player.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: opKind,
            value: dto.value,
            layer: layer,
            source: pin
        ));
    }
}