using UnityEngine;
using GameStats;

public sealed class BallEffectManager : MonoBehaviour
{
    public static BallEffectManager Instance { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void ApplyEffect(BallEffectDto dto, BallInstance self, BallInstance otherBall, PinInstance pin, Vector2 position)
    {
        if (dto == null || self == null)
            return;

        switch (dto.effectType)
        {
            case BallEffectType.ModifySelfStat:
                ModifyStat(dto, self.Stats, self);
                break;

            case BallEffectType.ModifyOtherBallStat:
                if (otherBall == null)
                    return;
                ModifyStat(dto, otherBall.Stats, self);
                break;

            default:
                Debug.LogWarning($"[BallEffectManager] Unsupported effect type: {dto.effectType}");
                break;
        }
    }

    void ModifyStat(BallEffectDto dto, StatSet stats, object source)
    {
        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[BallEffectManager] modifyStat with empty statId.");
            return;
        }

        var layer = dto.temporary ? StatLayer.Temporary : StatLayer.Permanent;

        stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: dto.effectMode,
            value: dto.value,
            layer: layer,
            source: source
        ));
    }
}
