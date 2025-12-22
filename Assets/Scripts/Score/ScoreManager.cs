using System;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private float minFontSize = 12f;
    [SerializeField] private float maxFontSize = 48f;

    double minValue = 10;
    double maxValue = 10000;

    public double previousScore;

    double totalScore;

    public double TotalScore
    {
        get => totalScore;
        private set
        {
            totalScore = value;
            OnScoreChanged?.Invoke(TotalScore);
        }
    }

    public event Action<double> OnScoreChanged;

    void Awake()
    {
        Instance = this;
    }

    public void SetNeedScoreForFontScale(double needScore)
    {
        if (needScore <= 0)
        {
            minValue = 10;
            maxValue = 10000;
            return;
        }

        maxValue = needScore;
        minValue = needScore / 100.0;

        if (minValue < 0)
            minValue = 0;

        if (maxValue <= minValue)
            maxValue = minValue + 1.0;
    }

    float GetFontSizeForScore(double score)
    {
        var value = Math.Clamp(score, minValue, maxValue);

        double t = (Math.Log(value) - Math.Log(minValue)) / (Math.Log(maxValue) - Math.Log(minValue));
        t = Math.Clamp(t, 0.0, 1.0);
        return Mathf.Lerp(minFontSize, maxFontSize, (float)t);
    }

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

        var criticalType = player.RollCriticalLevel(rng);
        double criticalMultiplier = player.GetCriticalMultiplier(criticalType) * ball.CriticalMultiplier;

        double rarityMultiplier = Math.Pow(player.RarityGrowth, (int)ball.Rarity);

        var gained = player.ScoreBase * player.ScoreMultiplier * rarityMultiplier * pin.ScoreMultiplier * criticalMultiplier;

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
