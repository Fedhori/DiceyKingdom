using System;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    const double MinValue = 10;
    const double MaxValue = 10000;
    const float MinFontSize = 12f;
    const float MaxFontSize = 36f;

    public double previousScore;
    
    private double totalScore;

    public double TotalScore
    {
        get => totalScore;
        private set
        {
            totalScore = value;
            OnScoreChanged?.Invoke(TotalScore);
        }
    }

    float GetFontSizeForScore(double score)
    {
        // 점수 범위 밖 안전 처리 (clamp score to range)
        var value = Math.Clamp(score, MinValue, MaxValue);

        double t = 0;
        if (MinValue < MaxValue)
        {
            // 0~1로 정규화 (normalize to 0~1)
            t = (float)((value - MinValue) / (MaxValue - MinValue));
        }

        // 폰트 크기 보간 (lerp font size)
        return Mathf.Lerp(MinFontSize, MaxFontSize, (float)t);
    }

    public event Action<double> OnScoreChanged;

    void Awake()
    {
        Instance = this;
    }

    // NOTICE - 다른 오버로드 함수들도 대응해야함
    public void CalculateScore(BallInstance ball, PinInstance pin, Vector2 position)
    {
        var player = PlayerManager.Instance?.Current;
        if (player == null)
        {
            Debug.LogWarning("[BallInstance] PlayerManager.Current is null.");
            return;
        }

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[BallInstance] GameManager.Instance is null.");
            return;
        }

        var rng = GameManager.Instance.Rng;

        // TODO - 크리티컬 시스템 <- 제거하는건 어떨까? 아니면 Pin으로는 얻기 어렵고, Token으로만 얻거나 성장으로만 얻을 수 있게?
        //var criticalType = player.RollCriticalLevel(rng);
        var criticalType = 0;
        double criticalMultiplier = player.GetCriticalMultiplier(criticalType) * ball.CriticalMultiplier;

        double rarityMultiplier = Math.Pow(player.RarityGrowth, (int)ball.Rarity);

        var gained = player.ScoreBase * player.ScoreMultiplier * rarityMultiplier * pin.ScoreMultiplier *
                     criticalMultiplier;

        AddScore(gained, criticalType, position);
    }

    public void AddScore(double amount, int criticalLevel, Vector2 position)
    {
        if (amount == 0)
            return;

        var color = Colors.GetCriticalColor(criticalLevel);
        var postFix = "";
        if (criticalLevel == 1)
            postFix = "!";
        else if (criticalLevel >= 2)
            postFix = "!!";

        FloatingTextManager.Instance.ShowText(
            amount + postFix,
            color,
            GetFontSizeForScore(amount),
            1f,
            position
        );
        TotalScore += amount;
    }
}
