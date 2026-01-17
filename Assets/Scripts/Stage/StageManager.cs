using System;
using Data;
using TMPro;
using UnityEngine;

public enum StagePhase
{
    None,
    Play,
    Result,
    Shop
}

public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    StageInstance currentStage;
    public StageInstance CurrentStage
    {
        get => currentStage;
        private set
        {
            currentStage = value;
            UpdateStageText();
        }
    }
    public event Action<StagePhase> OnPhaseChanged;
    private StagePhase currentPhase = StagePhase.None;

    public StagePhase CurrentPhase
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

    public bool CanDragItems => CurrentPhase != StagePhase.Play;

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
            Debug.LogError("[StageManager] StageRepository not initialized.");
            return;
        }

        if (StageRepository.Count == 0)
        {
            Debug.LogError("[StageManager] No stages defined.");
            return;
        }

        StartStage(0);
    }

    void StartStage(int stageIndex)
    {
        if (!StageRepository.TryGetByIndex(stageIndex, out var dto))
        {
            Debug.LogError($"[StageManager] stage not defined. stageIndex:{stageIndex}");
            return;
        }

        CurrentStage = new StageInstance(dto);
        DamageTrackingManager.Instance?.ResetForStage();
        DamageTextManager.Instance?.SetMinMaxValue(CurrentStage.Difficulty);

        OnPlayStart();

        UpdateStageText();
    }

    bool IsLastStage =>
        CurrentStage != null && StageRepository.TryGetByIndex(CurrentStage.StageIndex + 1, out _);

    public void OnPlayStart()
    {
        if (CurrentStage == null)
        {
            Debug.LogError("[StageManager] OnPlayStart but currentStage is null.");
            return;
        }

        CurrentPhase = StagePhase.Play;
        PlayManager.Instance?.StartPlay();
        ItemManager.Instance?.TriggerAll(ItemTriggerType.OnPlayStart);
    }
    
    public void OnPlayFinished()
    {
        if (currentPhase != StagePhase.Play)
        {
            Debug.LogWarning($"[StageManager] OnPlayFinished in phase {currentPhase}");
        }

        if (CurrentStage == null)
        {
            Debug.LogError("[StageManager] OnPlayFinished but currentStage is null.");
            CurrentPhase = StagePhase.None;
            return;
        }

        OpenResult();
    }

    public void OnResultClosed()
    {
        if (currentPhase != StagePhase.Result)
        {
            Debug.LogWarning($"[StageManager] OnResultClosed in phase {currentPhase}");
        }

        if (CurrentStage == null)
        {
            Debug.LogError("[StageManager] OnResultClosed but currentStage is null.");
            CurrentPhase = StagePhase.None;
            return;
        }

        bool hasNextStage = StageRepository.TryGetByIndex(CurrentStage.StageIndex + 1, out _);
        if (!hasNextStage)
        {
            CurrentPhase = StagePhase.None;
            GameManager.Instance?.HandleGameClear();
            return;
        }

        OpenShop();
    }

    public void OnShopClosed()
    {
        if (currentPhase != StagePhase.Shop)
        {
            Debug.LogWarning($"[StageManager] OnShopClosed in phase {currentPhase}");
        }

        if (CurrentStage == null)
        {
            Debug.LogError("[StageManager] OnShopClosed but currentStage is null.");
            CurrentPhase = StagePhase.None;
            return;
        }

        ItemManager.Instance?.TriggerAll(ItemTriggerType.OnStageEnd);
        ToNextStage();
    }

    void OpenResult()
    {
        CurrentPhase = StagePhase.Result;
        ResultManager.Instance?.Open();
    }

    void OpenShop()
    {
        CurrentPhase = StagePhase.Shop;
        ShopManager.Instance?.Open();
    }

    void ToNextStage()
    {
        if (!IsLastStage)
        {
            CurrentPhase = StagePhase.None;
            GameManager.Instance?.HandleGameClear();
            return;
        }

        PlayerManager.Instance.ResetPlayer();
        StartStage(CurrentStage.StageIndex + 1);
    }

    void UpdateStageText()
    {
        if (stageText == null)
            return;

        int total = StageRepository.Count;
        int displayIndex = CurrentStage != null ? CurrentStage.StageIndex + 1 : 0;
        stageText.text = $"{displayIndex}/{total}";
    }
}
