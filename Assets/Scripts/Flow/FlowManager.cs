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

        currentStage = new StageInstance(dto, stageIndex);
        currentRoundIndex = 0;
        currentStage.SetCurrentRoundIndex(currentRoundIndex);

        StageManager.Instance?.BindStage(currentStage);
        OnRoundStartRequested();
    }

    bool IsLastRoundInCurrentStage =>
        currentStage != null && currentRoundIndex >= currentStage.RoundCount - 1;

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

        // 라운드 종료시 모든 핀에 통지
        var pinsByRow = PinManager.Instance.PinsByRow;
        for (int row = 0; row < pinsByRow.Count; row++)
        {
            var rowList = pinsByRow[row];
            if (rowList == null)
                continue;

            for (int col = 0; col < rowList.Count; col++)
            {
                var pin = rowList[col];
                if (pin?.Instance != null)
                {
                    pin.Instance.HandleRoundFinished();
                }
            }
        }

        bool isLastRound = IsLastRoundInCurrentStage;

        if (isLastRound)
        {
            // 마지막 라운드면 스테이지 클리어 여부 판정
            var totalScore = ScoreManager.Instance != null
                ? ScoreManager.Instance.TotalScore
                : 0;

            bool cleared = totalScore >= currentStage.NeedScore;
            if (!cleared)
            {
                // 마지막 라운드 + 점수 미달 → 즉시 게임 오버
                CurrentPhase = FlowPhase.None;
                GameManager.Instance?.HandleGameOver();
                return;
            }

            // 마지막 라운드 + 스테이지 클리어 → 클리어 보상 UI
            CurrentPhase = FlowPhase.Reward;
            StatisticsManager.Instance?.Open(true);
            return;
        }

        // 중간 라운드 → 항상 보상 단계로 넘어감
        CurrentPhase = FlowPhase.Reward;
        StatisticsManager.Instance?.Open(false);
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

        CurrentPhase = FlowPhase.Shop;
        // 라운드 인덱스는 currentRoundIndex 그대로 유지
        ShopManager.Instance?.Open(currentStage, -1);
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

        bool isLastRound = IsLastRoundInCurrentStage;

        if (isLastRound)
        {
            // 여기까지 왔다는 건: 마지막 라운드를 클리어했고, 보상/상점까지 끝난 상태
            int nextStageIndex = currentStageIndex + 1;

            if (!StageRepository.TryGetByIndex(nextStageIndex, out var _))
            {
                // 다음 스테이지 없음 → 게임 클리어
                CurrentPhase = FlowPhase.None;
                GameManager.Instance?.HandleGameClear();
                return;
            }

            // 다음 스테이지 시작
            currentStageIndex = nextStageIndex;
            StartStage(currentStageIndex);
            return;
        }

        // 마지막 라운드가 아니면: 다음 라운드 준비 상태로 돌아감
        currentRoundIndex++;
        currentStage.SetCurrentRoundIndex(currentRoundIndex);
        StageManager.Instance?.UpdateRound(currentRoundIndex);

        OnRoundStartRequested();
    }
}