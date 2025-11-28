using Data;
using UnityEngine;

public enum CriticalType
{
    None,
    Critical,
    OverCritical,
}

public sealed class BallInstance
{
    static readonly System.Random LocalRandom = new();

    public BallDto BaseDto { get; }
    public string Id => BaseDto.id;

    public float BallScoreMultiplier => BaseDto.ballScoreMultiplier;

    public BallInstance(BallDto dto)
    {
        BaseDto = dto ?? throw new System.ArgumentNullException(nameof(dto));
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

        var rng = GameManager.Instance?.Rng ?? LocalRandom;

        var criticalType = player.RollCriticalType(rng);
        float criticalMultiplier = player.GetCriticalMultiplier(criticalType);

        float baseScore = player.ScoreBase;

        float rawScore = baseScore * BallScoreMultiplier * pin.ScoreMultiplier * criticalMultiplier;
        int gained = Mathf.RoundToInt(rawScore);

        ScoreManager.Instance.AddScore(gained, criticalType, position);
    }

    public void OnHitBall(BallInstance other)
    {
        // 나중에 Ball-Ball 충돌에 따른 효과를 추가하고 싶으면 여기서 처리
    }
}