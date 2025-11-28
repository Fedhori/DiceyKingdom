using System;
using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public class PinEffectManager: MonoBehaviour
{
    public static PinEffectManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    // eventId -> 등록된 (pin, effect) 목록.
    // TODO - 스테이지 초기화 시 이것도 초기화시켜야함~
    readonly Dictionary<string, List<(PinInstance pin, PinEffectDto dto)>> eventMap
        = new();

    public void RegisterEventEffects(PinInstance pin)
    {
        if (pin == null) return;

        foreach (var dto in pin.Effects)
        {
            if (string.IsNullOrEmpty(dto.eventId))
                continue; // 이벤트 없는 효과는 무시

            if (!eventMap.TryGetValue(dto.eventId, out var list))
            {
                list = new List<(PinInstance, PinEffectDto)>();
                eventMap[dto.eventId] = list;
            }

            list.Add((pin, dto));
        }
    }

    public void UnregisterEventEffects(PinInstance pin)
    {
        if (pin == null) return;

        foreach (var kvp in eventMap)
        {
            kvp.Value.RemoveAll(e => e.pin == pin);
        }
    }

    public void OnBallDestroyed(BallInstance ball)
    {
        if (!eventMap.TryGetValue("ballDestroyed", out var list))
            return;

        foreach (var (pin, dto) in list)
        {  
            Apply(dto, ball, pin);
        }
    }
    
    public void Apply(PinEffectDto dto, BallInstance ball, PinInstance pin)
    {
        var player = PlayerManager.Instance?.Current;

        if (dto == null)
            return;

        switch (dto.type)
        {
            case "modifyPlayerStat":
                if (player == null) return;
                ModifyPlayerStat(dto, player, pin);
                break;

            case "modifySelfStat":
                ModifySelfStat(dto, pin);
                break;
            
            case "addVelocity":
                ball.PendingSpeedFactor = dto.value;
                break;
            
            case "increaseSize":
                ball.PendingSizeFactor = dto.value;
                break;

            default:
                Debug.LogWarning($"[PinEffectManager] Unsupported effect type: {dto.type}");
                break;
        }
    }

    void ModifyPlayerStat(PinEffectDto dto, PlayerInstance player, PinInstance pin)
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

    void ModifySelfStat(PinEffectDto dto, PinInstance pin)
    {
        var opKind = dto.mode.Equals("Add", StringComparison.OrdinalIgnoreCase)
            ? StatOpKind.Add
            : StatOpKind.Mult;
        
        var layer = dto.temporary ? StatLayer.Temporary : StatLayer.Permanent;

        pin.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: opKind,
            value: dto.value,
            layer: layer,
            source: pin
        ));
    }
}