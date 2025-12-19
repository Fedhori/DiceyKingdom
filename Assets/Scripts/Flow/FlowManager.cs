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

public sealed class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    // index는 stageInstance dto안에서 관리하게.
    int currentStageIndex;
    StageInstance currentStage;
    int currentRoundIndex;

    public event Action<FlowPhase> OnPhaseChanged;
    private FlowPhase currentPhase = FlowPhase.None;

    public FlowPhase CurrentPhase
    {
        get => currentPhase;
        private set
        {
            if (currentPhase == value) return;
            currentPhase = value;
            OnPhaseChanged?.Invoke(currentPhase);
        }
    }

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
            Debug.LogError($"[FlowManager] stage not defined. stageIndex:{stageIndex}");
            return;
        }

        currentStage = new StageInstance(dto);
        currentRoundIndex = 0;
        currentStage.SetCurrentRoundIndex(currentRoundIndex);

        StageManager.Instance?.BindStage(currentStage);
        OnRoundStart();
    }

    bool IsLastRound =>
        currentStage != null && currentRoundIndex >= currentStage.RoundCount - 1;
    bool IsLastStage =>
        StageRepository.TryGetByIndex(currentStageIndex + 1, out _);

    public void OnRoundStart()
    {
        if (currentStage == null)
        {
            Debug.LogError("[FlowManager] OnRoundStart but currentStage is null.");
            return;
        }

        CurrentPhase = FlowPhase.Round;
        StageManager.Instance?.StartRound(currentStage, currentRoundIndex);
    }

    /// <summary>
    /// StageManager에서 모든 볼이 파괴되었을 때 호출.
    /// </summary>
    public void OnRoundFinished()
    {
        if (currentPhase != FlowPhase.Round)
        {
            Debug.LogWarning($"[FlowManager] OnRoundFinished in phase {currentPhase}");
        }

        if (currentStage == null)
        {
            Debug.LogError("[FlowManager] OnRoundFinished but currentStage is null.");
            CurrentPhase = FlowPhase.None;
            return;
        }

        PinManager.Instance.HandleRoundFinished();

        bool isLastRound = IsLastRound;

        if (!isLastRound)
        {
            AdvanceToNextRound();
            return;
        }

        if (!IsStageCleared())
        {
            CurrentPhase = FlowPhase.None;
            GameManager.Instance?.HandleGameOver();
            return;
        }

        OpenReward(true);
    }

    public void OnRewardClosed()
    {
        if (currentPhase != FlowPhase.Reward)
        {
            Debug.LogWarning($"[FlowManager] OnRewardClosed in phase {currentPhase}");
        }

        if (currentStage == null)
        {
            Debug.LogError("[FlowManager] OnRewardClosed but currentStage is null.");
            CurrentPhase = FlowPhase.None;
            return;
        }

        OpenShop();
    }

    public void OnShopClosed()
    {
        if (currentPhase != FlowPhase.Shop)
        {
            Debug.LogWarning($"[FlowManager] OnShopClosed in phase {currentPhase}");
        }

        if (currentStage == null)
        {
            Debug.LogError("[FlowManager] OnShopClosed but currentStage is null.");
            CurrentPhase = FlowPhase.None;
            return;
        }

        AdvanceToNextStage();
    }

    bool IsStageCleared()
    {
        if (currentStage == null)
            return false;

        var totalScore = ScoreManager.Instance != null
            ? ScoreManager.Instance.TotalScore
            : 0;

        return totalScore >= currentStage.NeedScore;
    }

    void OpenReward(bool isStageClear)
    {
        CurrentPhase = FlowPhase.Reward;
        StatisticsManager.Instance?.Open(isStageClear);
    }

    void OpenShop()
    {
        CurrentPhase = FlowPhase.Shop;
        ShopManager.Instance?.Open(currentStage, -1);
    }

    void AdvanceToNextRound()
    {
        currentRoundIndex++;
        currentStage.SetCurrentRoundIndex(currentRoundIndex);
        StageManager.Instance?.UpdateRound(currentRoundIndex);

        OnRoundStart();
    }

    void AdvanceToNextStage()
    {
        int nextStageIndex = currentStageIndex + 1;

        if (!IsLastStage)
        {
            CurrentPhase = FlowPhase.None;
            GameManager.Instance?.HandleGameClear();
            return;
        }

        PinManager.Instance.HandleStageFinished();
        PinManager.Instance.ResetAllPins();
        PlayerManager.Instance.ResetPlayer();

        currentStageIndex = nextStageIndex;
        StartStage(currentStageIndex);
    }
}
