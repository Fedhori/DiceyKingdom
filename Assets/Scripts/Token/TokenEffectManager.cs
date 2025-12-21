using Data;
using GameStats;
using UnityEngine;

public sealed class TokenEffectManager : MonoBehaviour
{
    public static TokenEffectManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ApplyEffect(TokenEffectDto dto, TokenInstance token)
    {
        if (dto == null || token == null)
            return;

        switch (dto.effectType)
        {
            case TokenEffectType.ModifyPlayerStat:
                ApplyPlayerStat(dto, token);
                break;
            case TokenEffectType.AddCurrency:
                ApplyCurrency(dto);
                break;
            case TokenEffectType.AddScore:
                ApplyScore(dto);
                break;
            default:
                Debug.LogWarning($"[TokenEffectManager] Unsupported effect type: {dto.effectType}");
                break;
        }
    }

    void ApplyPlayerStat(TokenEffectDto dto, TokenInstance token)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return;

        if (string.IsNullOrEmpty(dto.statId))
        {
            Debug.LogWarning("[TokenEffectManager] ModifyPlayerStat with empty statId.");
            return;
        }

        var layer = dto.temporary ? StatLayer.Temporary : StatLayer.Permanent;

        player.Stats.AddModifier(new StatModifier(
            statId: dto.statId,
            opKind: dto.effectMode,
            value: dto.value,
            layer: layer,
            source: token
        ));
    }

    void ApplyCurrency(TokenEffectDto dto)
    {
        var currencyMgr = CurrencyManager.Instance;
        if (currencyMgr == null)
            return;

        currencyMgr.AddCurrency(Mathf.RoundToInt(dto.value));
    }

    void ApplyScore(TokenEffectDto dto)
    {
        var scoreMgr = ScoreManager.Instance;
        if (scoreMgr == null)
            return;

        var player = PlayerManager.Instance?.Current;
        float multiplier = player != null ? (float)player.ScoreMultiplier : 1f;
        double amount = dto.value * multiplier;

        scoreMgr.AddScore(amount, 0, Vector2.zero);
    }
}
