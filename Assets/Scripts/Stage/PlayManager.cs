using System;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

public sealed class PlayManager : MonoBehaviour
{
    public static PlayManager Instance { get; private set; }

    [SerializeField] private StageHudView stageHudView;

    // Stage 상태
    StageInstance currentStage;
    public StageInstance CurrentStage => currentStage;
    public bool playActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetStage(StageInstance stage)
    {
        currentStage = stage;
        playActive = false;
        ScoreManager.Instance.previousScore = ScoreManager.Instance.TotalScore;

        if (ScoreManager.Instance != null && stage != null)
            ScoreManager.Instance.SetNeedScoreForFontScale(stage.NeedScore);

        if (stageHudView != null && stage != null)
        {
            var displayStageNumber = stage.StageIndex + 1;
            stageHudView.SetStageInfo(
                displayStageNumber,
                StageRepository.Count,
                stage.NeedScore
            );

            UpdateStageHud();
        }
    }

    public void StartStagePlay(StageInstance stage)
    {
        currentStage = stage;

        ItemManager.Instance?.InitializeFromPlayer(PlayerManager.Instance.Current);
        playActive = true;
        BrickManager.Instance.BeginSpawnRamp();
        FlowManager.Instance?.OnPlayStarted();
    }

    public void FinishPlay()
    {
        playActive = false;
        ItemManager.Instance?.ClearAll();
        FlowManager.Instance?.OnPlayFinished();
    }

    void UpdateStageHud()
    {
        if (stageHudView == null || currentStage == null)
            return;

        var displayStageNumber = currentStage.StageIndex + 1;
        stageHudView.SetStageInfo(
            displayStageNumber,
            StageRepository.Count,
            currentStage.NeedScore
        );
    }
}
