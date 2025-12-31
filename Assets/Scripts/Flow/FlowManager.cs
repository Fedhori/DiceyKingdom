using System;
using Data;
using TMPro;
using UnityEngine;

public enum FlowPhase
{
    None,
    Ready,
    Play,
    Reward,
    Shop
}

public sealed class FlowManager : MonoBehaviour
{
    public static FlowManager Instance { get; private set; }

    // index는 stageInstance dto안에서 관리하게.
    int currentStageIndex;
    StageInstance currentStage;

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
    
    [SerializeField] TMP_Text stageText;

    public bool CanDragTokens => CurrentPhase != FlowPhase.Play;

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
        TokenManager.Instance.TriggerTokens(TokenTriggerType.OnStageStart);

        if (stageIndex == 0)
            OnStageReadyStart();
        else
            OpenShop();
    }

    bool IsLastStage =>
        StageRepository.TryGetByIndex(currentStageIndex + 1, out _);

    public void OnStageReadyStart()
    {
        if (currentStage == null)
        {
            Debug.LogError("[FlowManager] OnStageReadyStart but currentStage is null.");
            return;
        }

        CurrentPhase = FlowPhase.Ready;
        PlayManager.Instance?.StartPlay();
    }

    public void OnPlayStarted()
    {
        CurrentPhase = FlowPhase.Play;
    }
    
    public void OnPlayFinished()
    {
        if (currentPhase != FlowPhase.Play)
        {
            Debug.LogWarning($"[FlowManager] OnPlayFinished in phase {currentPhase}");
        }

        if (currentStage == null)
        {
            Debug.LogError("[FlowManager] OnPlayFinished but currentStage is null.");
            CurrentPhase = FlowPhase.None;
            return;
        }

        OpenReward();
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

        AdvanceToNextStage();
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

        OnStageReadyStart();
    }

    void OpenReward()
    {
        CurrentPhase = FlowPhase.Reward;
        RewardManager.Instance?.Open();
    }

    void OpenShop()
    {
        CurrentPhase = FlowPhase.Shop;
        ShopManager.Instance?.Open();
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

        PlayerManager.Instance.ResetPlayer();
        currentStageIndex = nextStageIndex;
        StartStage(currentStageIndex);
    }
}
