using UnityEngine;
using UnityEngine.UI;

public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private StageHudView stageHudView;
    [SerializeField] private Button roundStartButton;

    StageInstance currentStage;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (roundStartButton != null)
        {
            roundStartButton.onClick.RemoveAllListeners();
            roundStartButton.onClick.AddListener(OnRoundStartButtonClicked);
        }
    }

    public void BindStage(StageInstance stage)
    {
        currentStage = stage;

        if (stageHudView != null && stage != null)
        {
            var displayStageNumber = stage.StageIndex + 1;
            stageHudView.SetStageInfo(
                displayStageNumber,
                stage.NeedScore,
                stage.RoundCount
            );

            UpdateRound(stage.CurrentRoundIndex);
        }
    }

    public void UpdateRound(int roundIndex)
    {
        if (stageHudView == null || currentStage == null)
            return;

        var displayRoundNumber = roundIndex + 1;
        stageHudView.SetRoundInfo(displayRoundNumber, currentStage.RoundCount);
    }

    public void ShowRoundStartButton()
    {
        if (roundStartButton != null)
            roundStartButton.gameObject.SetActive(true);
    }

    public void HideRoundStartButton()
    {
        if (roundStartButton != null)
            roundStartButton.gameObject.SetActive(false);
    }

    void OnRoundStartButtonClicked()
    {
        HideRoundStartButton();
        FlowManager.Instance?.OnRoundStartRequested();
    }
}