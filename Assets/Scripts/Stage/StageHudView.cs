// StageHudView.cs
using TMPro;
using UnityEngine;

public sealed class StageHudView : MonoBehaviour
{
    [SerializeField] TMP_Text stageText;
    [SerializeField] TMP_Text targetScoreText;
    [SerializeField] TMP_Text currentScoreText;
    [SerializeField] TMP_Text roundText;

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
        if (currentScoreText != null)
            currentScoreText.text = score.ToString();
    }

    public void SetStageInfo(int stageNumber, int needScore, int roundCount)
    {
        if (stageText != null)
            stageText.text = $"Stage {stageNumber}";

        if (targetScoreText != null)
            targetScoreText.text = needScore.ToString();

        // roundCount는 SetRoundInfo에서도 쓰기 때문에 여기서는 따로 사용 안 해도 됨.
    }

    public void SetRoundInfo(int currentRound, int totalRounds)
    {
        if (roundText != null)
            roundText.text = $"Round {currentRound} / {totalRounds}";
    }
}