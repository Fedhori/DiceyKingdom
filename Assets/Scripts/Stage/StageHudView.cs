// StageHudView.cs
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class StageHudView : MonoBehaviour
{
    [SerializeField] LocalizeStringEvent stageText;
    [SerializeField] LocalizeStringEvent targetScoreText;
    [SerializeField] LocalizeStringEvent currentScoreText;
    [SerializeField] LocalizeStringEvent roundText;

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
        if (currentScoreText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = score.ToString();
    }

    public void SetStageInfo(int stageNumber, int needScore, int roundCount)
    {
        if (stageText.StringReference.TryGetValue("value", out var v1) && v1 is StringVariable sv1)
            sv1.Value = stageNumber.ToString();
        
        if (targetScoreText.StringReference.TryGetValue("value", out var v2) && v2 is StringVariable sv2)
            sv2.Value = needScore.ToString();
    }

    public void SetRoundInfo(int currentRound, int totalRounds)
    {
        if (roundText.StringReference.TryGetValue("currentRound", out var v1) && v1 is StringVariable sv1)
            sv1.Value = currentRound.ToString();
        
        if (roundText.StringReference.TryGetValue("totalRound", out var v2) && v1 is StringVariable sv2)
            sv2.Value = totalRounds.ToString();
    }
}