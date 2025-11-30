// StageManager.cs
using UnityEngine;
using UnityEngine.UI;

public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private StageHudView stageHudView;
    [SerializeField] private Button roundStartButton;

    StageInstance currentStage;
    int currentStageIndex;
    bool runStarted;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// GameManager.Start() 등에서 한 번만 호출한다고 가정.
    /// </summary>
    public void StartRun()
    {
        if (!StageRepository.IsInitialized)
        {
            Debug.LogError("[StageManager] StageRepository not initialized.");
            return;
        }

        if (StageRepository.Count == 0)
        {
            Debug.LogError("[StageManager] No stages defined.");
            return;
        }

        runStarted = true;
        currentStageIndex = 0;
        StartStage(currentStageIndex);
    }

    void StartStage(int stageIndex)
    {
        if (!StageRepository.TryGetByIndex(stageIndex, out var dto))
        {
            Debug.Log("[StageManager] No more stages. Run clear or not defined.");
            GameManager.Instance?.RestartGame();
            return;
        }

        currentStage = new StageInstance(dto, stageIndex);
        currentStage.SetCurrentRoundIndex(0);

        UpdateHudForStage();
        UpdateHudForRound();

        PrepareRound();
    }

    void PrepareRound()
    {
        if (currentStage == null)
        {
            Debug.LogError("[StageManager] PrepareRound called with no current stage.");
            return;
        }
        
        roundStartButton.gameObject.SetActive(true);
    }

    public void StartRound()
    {
        roundStartButton.gameObject.SetActive(false);
        RoundManager.Instance?.StartRound(currentStage, currentStage.CurrentRoundIndex);
    }

    void UpdateHudForStage()
    {
        if (stageHudView == null || currentStage == null)
            return;

        var displayStageNumber = currentStage.StageIndex + 1;
        stageHudView.SetStageInfo(
            displayStageNumber,
            currentStage.NeedScore,
            currentStage.RoundCount
        );
    }

    void UpdateHudForRound()
    {
        if (stageHudView == null || currentStage == null)
            return;

        var displayRoundNumber = currentStage.CurrentRoundIndex + 1;
        stageHudView.SetRoundInfo(
            displayRoundNumber,
            currentStage.RoundCount
        );
    }

    /// <summary>
    /// RoundManager에서 라운드 종료 시점에 호출.
    /// </summary>
    public void HandleRoundFinished()
    {
        if (!runStarted || currentStage == null)
            return;
        
        CurrencyManager.Instance.AddCurrency(100);

        var nextRoundIndex = currentStage.CurrentRoundIndex + 1;

        if (nextRoundIndex < currentStage.RoundCount)
        {
            currentStage.SetCurrentRoundIndex(nextRoundIndex);
            UpdateHudForRound();
            ShopManager.Instance?.Open(currentStage, nextRoundIndex);
        }
        else
        {
            EvaluateStageResult();
        }
    }

    void EvaluateStageResult()
    {
        if (currentStage == null)
            return;

        var totalScore = ScoreManager.Instance != null
            ? ScoreManager.Instance.TotalScore
            : 0;

        if (totalScore >= currentStage.NeedScore)
        {
            RewardManager.Instance?.Open(currentStage, currentStage.StageIndex);
        }
        else
        {
            Debug.Log("[StageManager] Game Over. NeedScore not reached.");
            GameManager.Instance?.RestartGame();
        }
    }

    /// <summary>
    /// ShopManager.Close()에서 호출.
    /// </summary>
    public void HandleShopClosed()
    {
        if (!runStarted || currentStage == null)
            return;

        UpdateHudForRound();
        PrepareRound();
    }

    /// <summary>
    /// RewardManager.Close()에서 호출.
    /// </summary>
    public void HandleRewardClosed()
    {
        if (!runStarted)
            return;

        currentStageIndex++;
        StartStage(currentStageIndex);
    }
}
