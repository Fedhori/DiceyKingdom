using Data;
using UnityEngine;

public sealed class BallInstance
{
    static readonly System.Random LocalRandom = new();

    public BallDto BaseDto { get; }
    public string Id => BaseDto.id;

    public float BallScoreMultiplier => BaseDto.ballScoreMultiplier;
    
    public float PendingSpeedFactor { get; set; } = 1f;
    public float PendingSizeFactor { get; set; } = 1f;

    readonly System.Random localRandom = new();

    readonly List<BallRuleDto> rules;
    public IReadOnlyList<BallRuleDto> Rules => rules;

    public BallInstance(BallDto dto)
    {
        BaseDto = dto ?? throw new System.ArgumentNullException(nameof(dto));

        rules = dto.rules != null
            ? new List<BallRuleDto>(dto.rules)
            : new List<BallRuleDto>();
    }

    public void OnHitPin(PinInstance pin, Vector2 position)
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] ScoreManager is null.");
            return;
        }

        var player = PlayerManager.Instance?.Current;
        if (player == null)
        {
            Debug.LogWarning("[BallInstance] PlayerManager.Current is null.");
            return;
        }

        var rng = GameManager.Instance?.Rng ?? localRandom;

        var criticalType = player.RollCriticalLevel(rng);
        float criticalMultiplier = player.GetCriticalMultiplier(criticalType);

        float rawScore = player.ScoreBase * player.ScoreMultiplier * BallScoreMultiplier * pin.ScoreMultiplier * criticalMultiplier;
        int gained = Mathf.RoundToInt(rawScore);

        ScoreManager.Instance.AddScore(gained, criticalType, position);
    }

    public void OnHitBall(BallInstance other)
    {
        // 나중에 Ball-Ball 충돌에 따른 효과를 추가하고 싶으면 여기서 처리
    }
}
