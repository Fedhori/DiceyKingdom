using System.Collections.Generic;
using Data;
using GameStats;
using UnityEngine;

public class PinEffectManager : MonoBehaviour
{
    public static PinEffectManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void OnBallDestroyed(BallInstance ball)
    {
        if (ball == null)
            return;

        var pinMgr = PinManager.Instance;
        if (pinMgr == null)
            return;

        var rows = pinMgr.PinsByRow;
        if (rows == null)
            return;

        for (int row = 0; row < rows.Count; row++)
        {
            var rowList = rows[row];
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                var controller = rowList[col];
                if (controller == null || controller.Instance == null)
                    continue;

                controller.Instance.HandleTrigger(
                    PinTriggerType.OnBallDestroyed,
                    ball,
                    Vector2.zero
                );
            }
        }
    }

    public void ApplyEffect(PinEffectDto dto, BallInstance ball, PinInstance pin, Vector2 position)
    {
        if (dto == null || pin == null)
            return;

        var player = PlayerManager.Instance?.Current;

        switch (dto.type)
        {
            case "modifyPlayerStat":
                if (player == null)
                    return;
                ModifyPlayerStat(dto, player, pin);
                break;

            case "modifySelfStat":
                ModifySelfStat(dto, pin);
                break;

            case "addVelocity":
                if (ball == null)
                    return;
                ball.PendingSpeedFactor = dto.value;
                break;

            case "increaseSize":
                if (ball == null)
                    return;
                ball.PendingSizeFactor = dto.value;
                break;

            case "addScore":
                if (player == null || ScoreManager.Instance == null)
                    return;
                ScoreManager.Instance.AddScore(
                    (int)(dto.value * player.ScoreMultiplier),
                    CriticalType.None,
                    position
                );
                break;

            default:
                Debug.LogWarning($"[PinEffectManager] Unsupported effect type: {dto.type}");
                break;
        }
    }

    void ModifyPlayerStat(PinEffectDto dto, PlayerInstance player, PinInstance pin)
    {
        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[PinEffectManager] modifyPlayerStat with empty statId.");
            return;
        }

        var opKind = dto.mode != null &&
                     dto.mode.Equals("Add", System.StringComparison.OrdinalIgnoreCase)
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
        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[PinEffectManager] modifySelfStat with empty statId.");
            return;
        }

        var opKind = dto.mode != null &&
                     dto.mode.Equals("Add", System.StringComparison.OrdinalIgnoreCase)
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
