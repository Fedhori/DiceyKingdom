using System;
using Data;
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

    public event Action<FlowPhase> OnPhaseChanged;
    private FlowPhase currentPhase = FlowPhase.None;

    public FlowPhase CurrentPhase
    {
        get => currentPhase;
        set
        {
            currentPhase = value;
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

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
            Debug.Log($"[FlowManager] stage not defined. stageIndex:{stageIndex}");
            return;
        }

        currentStage = new StageInstance(dto, stageIndex);
        currentRoundIndex = 0;
        currentStage.SetCurrentRoundIndex(currentRoundIndex);

        StageManager.Instance?.BindStage(currentStage);
        StageManager.Instance?.ShowRoundStartButton();

        CurrentPhase = FlowPhase.None;
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

        CurrentPhase = FlowPhase.Round;
        RoundManager.Instance?.StartRound(currentStage, currentRoundIndex);
    }

    public void OnRoundFinished()
    {
        if (currentPhase != FlowPhase.Round)
        {
            Debug.LogWarning($"[FlowManager] OnRoundFinished in phase {currentPhase}");
        }

        CurrencyManager.Instance?.AddCurrency(GameConfig.BaseRoundIncome);

        var pinsByRow = PinManager.Instance.PinsByRow;

        for (int row = 0; row < pinsByRow.Count; row++)
        {
            var rowList = pinsByRow[row];
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                var pin = rowList[col];
                if (pin != null && pin.Instance != null)
                {
                    pin.Instance.HandleRoundFinished();
                }
            }
        }

        bool isLastRound = currentStage != null &&
                           currentRoundIndex >= currentStage.RoundCount - 1;

        if (!isLastRound)
        {
            currentRoundIndex++;
            currentStage?.SetCurrentRoundIndex(currentRoundIndex);

            StageManager.Instance?.UpdateRound(currentRoundIndex);

            CurrentPhase = FlowPhase.Shop;
            ShopManager.Instance?.Open(currentStage, ShopOpenContext.BetweenRounds, currentRoundIndex);
            return;
        }

        var totalScore = ScoreManager.Instance != null
            ? ScoreManager.Instance.TotalScore
            : 0;

        bool cleared = currentStage != null && totalScore >= currentStage.NeedScore;

        if (!cleared)
        {
            GameManager.Instance?.HandleGameOver();
            return;
        }

        if (!StageRepository.TryGetByIndex(currentStageIndex + 1, out var dto))
        {
            GameManager.Instance?.HandleGameClear();
            return;
        }

        CurrentPhase = FlowPhase.Reward;
        RewardManager.Instance?.Open(currentStage, currentStage.StageIndex);
    }

    public void OnRewardClosed()
    {
        if (currentPhase != FlowPhase.Reward)
        {
            Debug.LogWarning($"[FlowManager] OnRewardClosed in phase {currentPhase}");
        }

        CurrentPhase = FlowPhase.Shop;
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
                CurrentPhase = FlowPhase.None;
                StageManager.Instance?.ShowRoundStartButton();
                break;

            case ShopOpenContext.AfterStage:
                CurrentPhase = FlowPhase.None;
                currentStageIndex++;
                StartStage(currentStageIndex);
                break;
        }
    }
}