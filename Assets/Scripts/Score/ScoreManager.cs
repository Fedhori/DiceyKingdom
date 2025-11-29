using System;
using System.Globalization;
using TMPro;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int totalScore;

    const float MinValue = 10f;
    const float MaxValue = 10000f;
    const float MinFontSize = 12f;
    const float MaxFontSize = 36f;

    float GetFontSizeForScore(int score)
    {
        // 점수 범위 밖 안전 처리 (clamp score to range)
        float value = Mathf.Clamp(score, MinValue, MaxValue);

        // 0~1로 정규화 (normalize to 0~1)
        float t = Mathf.InverseLerp(MinValue, MaxValue, value);

        // 폰트 크기 보간 (lerp font size)
        return Mathf.Lerp(MinFontSize, MaxFontSize, t);
    }

    public int TotalScore
    {
        get => totalScore;
        private set
        {
            totalScore = value;
            OnScoreChanged?.Invoke(TotalScore);
        }
    }

    [SerializeField] private TMP_Text scoreText;

    public event Action<int> OnScoreChanged;

    void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        OnScoreChanged += UpdateScoreText;
    }

    private void OnDisable()
    {
        OnScoreChanged -= UpdateScoreText;
    }

    public void AddScore(int amount, CriticalType criticalType, Vector2 position)
    {
        if (amount == 0)
            return;

        var color = Colors.GetCriticalColor(criticalType);
        var postFix = "";
        if (criticalType == CriticalType.Critical)
            postFix = "!";
        else if (criticalType == CriticalType.OverCritical)
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

    private void UpdateScoreText(int score)
    {
        scoreText.text = score.ToString();
    }
}