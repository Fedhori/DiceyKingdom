using System;
using Data;
using TMPro;
using UnityEngine;

public enum StagePhase
{
    None,
    Play,
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

    public void StartRunFromIndex(int stageIndex)
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

        StartStage(stageIndex);
    }

    void StartStage(int stageIndex)
    {
        if (!StageRepository.TryGetByIndex(stageIndex, out var dto))
        {
            Debug.LogError($"[StageManager] stage not defined. stageIndex:{stageIndex}");
            return;
        }

        CurrentStage = new StageInstance(dto);
        PlayerManager.Instance?.ResetPlayer();
        DamageTrackingManager.Instance?.ResetForStage();
        DamageTextManager.Instance?.SetMinMaxValue(CurrentStage.SpawnBudgetScale);

        if (CurrentStage.StageIndex > 0)
            SaveManager.Instance?.SaveOnStageStart();
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

        PlayerController.Instance?.ResetToStart();
        OpenShopWithResult();
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
        var upgradeManager = UpgradeManager.Instance;
        if (upgradeManager != null)
            upgradeManager.TryBreakUpgradesOnStageEnd(ItemManager.Instance?.Inventory);
        ToNextStage();
    }

    void OpenShop()
    {
        CurrentPhase = StagePhase.Shop;
        ShopManager.Instance?.Open();
    }

    void OpenShopWithResult()
    {
        OpenShop();
        var income = CalculateStageIncome();
        ResultManager.Instance?.OpenWithIncome(income);
    }

    ResultManager.IncomeBreakdown CalculateStageIncome()
    {
        int baseIncome = GameConfig.BaseIncome;
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            baseIncome += player.BaseIncomeBonus;

        int currentCurrency = CurrencyManager.Instance != null
            ? CurrencyManager.Instance.CurrentCurrency
            : 0;
        int interestStep = Mathf.Max(1, GameConfig.InterestCurrencyPerUnit);
        int interestCap = Mathf.Max(0, GameConfig.InterestMax);
        int interestIncome = Mathf.Clamp(currentCurrency / interestStep, 0, interestCap);
        int totalIncome = Mathf.Max(0, baseIncome + interestIncome);

        return new ResultManager.IncomeBreakdown(
            Mathf.Max(0, baseIncome),
            interestIncome,
            totalIncome,
            interestStep,
            interestCap);
    }

    void ToNextStage()
    {
        if (!IsLastStage)
        {
            CurrentPhase = StagePhase.None;
            GameManager.Instance?.HandleGameClear();
            return;
        }

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
