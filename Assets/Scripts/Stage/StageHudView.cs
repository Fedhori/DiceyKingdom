// StageHudView.cs

using TMPro;
using UnityEngine;

public sealed class StageHudView : MonoBehaviour
{
    [SerializeField] TMP_Text stageText;
    [SerializeField] TMP_Text roundText;
    [SerializeField] TMP_Text targetScoreText;
    [SerializeField] TMP_Text currentScoreText;

    void OnEnable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            HandleScoreChanged(ScoreManager.Instance.TotalScore);
        }
    }

    void OnDisable()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }
    }

    void HandleScoreChanged(int score)
    {
        currentScoreText.text = score.ToString();
    }

    public void SetStageInfo(int currentStage, int maxStage, int needScore)
    {
        stageText.text = $"{currentStage} / {maxStage}";
        targetScoreText.text = $"{needScore}";
    }

    public void SetRoundInfo(int currentRound, int totalRounds)
    {
        roundText.text = $"{currentRound} / {totalRounds}";
    }
}