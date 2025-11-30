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
    BetweenRounds, // 라운드 사이 상점
    AfterStage     // 스테이지 끝난 뒤 상점
}

public sealed class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    int currentStageIndex;
    StageInstance currentStage;
    int currentRoundIndex;
    FlowPhase currentPhase = FlowPhase.None;

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
    /// GameManager에서 런 시작 시점에 호출.
    /// </summary>
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

    /// <summary>
    /// 라운드 시작 버튼 클릭 시 StageManager → FlowManager로 들어오는 엔트리.
    /// </summary>
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

    /// <summary>
    /// RoundManager에서 라운드 종료 시점에 호출.
    /// </summary>
    public void OnRoundFinished()
    {
        if (currentPhase != FlowPhase.Round)
        {
            Debug.LogWarning($"[FlowManager] OnRoundFinished in phase {currentPhase}");
        }

        // 공통 라운드 종료 보상
        CurrencyManager.Instance?.AddCurrency(100);

        bool isLastRound = currentStage != null &&
                           currentRoundIndex >= currentStage.RoundCount - 1;

        // 1) 마지막 라운드가 아니면: 다음 라운드 준비 + 라운드 사이 상점
        if (!isLastRound)
        {
            currentRoundIndex++;
            currentStage.SetCurrentRoundIndex(currentRoundIndex);

            StageManager.Instance?.UpdateRound(currentRoundIndex);

            currentPhase = FlowPhase.Shop;
            ShopManager.Instance?.Open(currentStage, ShopOpenContext.BetweenRounds, currentRoundIndex);
            return;
        }

        // 2) 마지막 라운드라면 스테이지 결과 평가
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

        // 스테이지 클리어 → 보상 Phase
        currentPhase = FlowPhase.Reward;
        RewardManager.Instance?.Open(currentStage, currentStage.StageIndex);
    }

    /// <summary>
    /// RewardManager.Close() → FlowManager로 들어오는 콜백.
    /// </summary>
    public void OnRewardClosed()
    {
        if (currentPhase != FlowPhase.Reward)
        {
            Debug.LogWarning($"[FlowManager] OnRewardClosed in phase {currentPhase}");
        }

        currentPhase = FlowPhase.Shop;
        // 스테이지 끝난 뒤 상점
        ShopManager.Instance?.Open(currentStage, ShopOpenContext.AfterStage, -1);
    }

    /// <summary>
    /// ShopManager.Close(context) → FlowManager로 들어오는 콜백.
    /// </summary>
    public void OnShopClosed(ShopOpenContext context)
    {
        if (currentPhase != FlowPhase.Shop)
        {
            Debug.LogWarning($"[FlowManager] OnShopClosed in phase {currentPhase}");
        }

        switch (context)
        {
            case ShopOpenContext.BetweenRounds:
                // 라운드 사이 상점 종료 → 같은 스테이지, 다음 라운드 준비
                currentPhase = FlowPhase.None;
                StageManager.Instance?.ShowRoundStartButton();
                break;

            case ShopOpenContext.AfterStage:
                // 스테이지 끝난 뒤 상점 종료 → 다음 스테이지로
                currentPhase = FlowPhase.None;
                currentStageIndex++;
                StartStage(currentStageIndex);
                break;
        }
    }
}
