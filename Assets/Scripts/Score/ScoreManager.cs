using System;
using TMPro;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    private int totalScore;

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

    public void AddScore(int amount, Vector2 position)
    {
        if (amount == 0)
            return;

        FloatingTextManager.Instance.ShowText(amount.ToString(), Colors.Legendary, 16f, 1f, position);
        TotalScore += amount;
    }

    private void UpdateScoreText(int score)
    {
        scoreText.text = score.ToString();
    }
}