// StageHudView.cs

using System.Globalization;
using TMPro;
using UnityEngine;

public sealed class StageHudView : MonoBehaviour
{
    [SerializeField] TMP_Text stageText;
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

    void HandleScoreChanged(double score)
    {
        currentScoreText.text = $"{score:N0}";
    }

    public void SetStageInfo(int currentStage, int maxStage, double needScore)
    {
        stageText.text = $"{currentStage} / {maxStage}";
        targetScoreText.text = $"{needScore:N0}";
    }
}
