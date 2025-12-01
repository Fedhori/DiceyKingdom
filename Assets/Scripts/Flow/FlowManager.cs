using UnityEngine;

public enum FlowPhase
{
    None,
    Round,
    Reward,
    Shop
}

public enum ShopOpenContext
{
    BetweenRounds,
    AfterStage
}

public sealed class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    int currentStageIndex;
    StageInstance currentStage;
    int currentRoundIndex;
    FlowPhase currentPhase = FlowPhase.None;

    // 핀 드래그 허용 여부: 준비 상태(라운드 전)에서만 허용
    public bool CanDragPins => currentPhase != FlowPhase.Round;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartRun()
    {
        if (!StageRepository.IsInitialized)
        {
            Debug.LogError("[FlowManager] StageRepository not initialized.");
            return;
        }

        if (StageRepository.Count == 0)
        {
            Debug.LogError("[FlowManager] No stages defined.");
            return;
        }

        currentStageIndex = 0;
        StartStage(currentStageIndex);
    }

    void StartStage(int stageIndex)
    {
        if (!StageRepository.TryGetByIndex(stageIndex, out var dto))
        {
            Debug.Log("[FlowManager] No more stages. Run clear or not defined.");
            GameManager.Instance?.RestartGame();
            return;
        }

        currentStage = new StageInstance(dto, stageIndex);
        currentRoundIndex = 0;
        currentStage.SetCurrentRoundIndex(currentRoundIndex);

        StageManager.Instance?.BindStage(currentStage);
        StageManager.Instance?.ShowRoundStartButton();

        currentPhase = FlowPhase.None;
    }

    public void OnRoundStartRequested()
    {
        if (currentStage == null)
        {
            Debug.LogError("[FlowManager] OnRoundStartRequested but currentStage is null.");
            return;
        }

        if (currentPhase != FlowPhase.None)
        {
            Debug.LogWarning($"[FlowManager] OnRoundStartRequested in phase {currentPhase}");
        }

        currentPhase = FlowPhase.Round;
        RoundManager.Instance?.StartRound(currentStage, currentRoundIndex);
    }

    public void OnRoundFinished()
    {
        if (currentPhase != FlowPhase.Round)
        {
            Debug.LogWarning($"[FlowManager] OnRoundFinished in phase {currentPhase}");
        }

        CurrencyManager.Instance?.AddCurrency(100);

        bool isLastRound = currentStage != null &&
                           currentRoundIndex >= currentStage.RoundCount - 1;

        if (!isLastRound)
        {
            currentRoundIndex++;
            currentStage.SetCurrentRoundIndex(currentRoundIndex);

            StageManager.Instance?.UpdateRound(currentRoundIndex);

            currentPhase = FlowPhase.Shop;
            ShopManager.Instance?.Open(currentStage, ShopOpenContext.BetweenRounds, currentRoundIndex);
            return;
        }

        int totalScore = ScoreManager.Instance != null
            ? ScoreManager.Instance.TotalScore
            : 0;

        bool cleared = currentStage != null && totalScore >= currentStage.NeedScore;

        if (!cleared)
        {
            Debug.Log("[FlowManager] Game Over. NeedScore not reached.");
            GameManager.Instance?.RestartGame();
            return;
        }

        currentPhase = FlowPhase.Reward;
        RewardManager.Instance?.Open(currentStage, currentStage.StageIndex);
    }

    public void OnRewardClosed()
    {
        if (currentPhase != FlowPhase.Reward)
        {
            Debug.LogWarning($"[FlowManager] OnRewardClosed in phase {currentPhase}");
        }

        currentPhase = FlowPhase.Shop;
        ShopManager.Instance?.Open(currentStage, ShopOpenContext.AfterStage, -1);
    }

    public void OnShopClosed(ShopOpenContext context)
    {
        if (currentPhase != FlowPhase.Shop)
        {
            Debug.LogWarning($"[FlowManager] OnShopClosed in phase {currentPhase}");
        }

        switch (context)
        {
            case ShopOpenContext.BetweenRounds:
                currentPhase = FlowPhase.None;
                StageManager.Instance?.ShowRoundStartButton();
                break;

            case ShopOpenContext.AfterStage:
                currentPhase = FlowPhase.None;
                currentStageIndex++;
                StartStage(currentStageIndex);
                break;
        }
    }
}
