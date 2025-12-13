using UnityEngine;
using UnityEngine.UI;

public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private StageHudView stageHudView;
    [SerializeField] private Button roundStartButton;

    // Stage & Round 상태
    StageInstance currentStage;
    int currentRoundIndex;
    bool roundActive;

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

    /// <summary>
    /// 현재 스테이지 바인딩 + HUD 갱신.
    /// </summary>
    public void BindStage(StageInstance stage)
    {
        currentStage = stage;
        roundActive = false;

        if (stageHudView != null && stage != null)
        {
            var displayStageNumber = stage.StageIndex + 1;
            stageHudView.SetStageInfo(
                displayStageNumber,
                StageRepository.Count,
                stage.NeedScore
            );

            currentRoundIndex = stage.CurrentRoundIndex;
            UpdateRound(currentRoundIndex);
        }
    }

    /// <summary>
    /// 라운드 인덱스 갱신 + HUD 반영.
    /// </summary>
    public void UpdateRound(int roundIndex)
    {
        if (stageHudView == null || currentStage == null)
            return;

        currentRoundIndex = roundIndex;

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

    /// <summary>
    /// 라운드 시작: 기존 RoundManager.StartRound 역할.
    /// </summary>
    public void StartRound(StageInstance stage, int roundIndex)
    {
        ScoreManager.Instance.previousScore = ScoreManager.Instance.TotalScore;
        currentStage = stage;
        currentRoundIndex = roundIndex;
        roundActive = true;

        PlayerManager.Instance.ResetPlayer();
        PinManager.Instance.ResetAllPins();
        BallManager.Instance.ResetForNewRound();

        var player = PlayerManager.Instance.Current;
        if (player == null)
        {
            Debug.LogError("[StageManager] Player not created. Cannot build ball deck.");
            roundActive = false;
            return;
        }

        var rng = GameManager.Instance != null
            ? GameManager.Instance.Rng
            : new System.Random();

        var sequence = player.BallDeck.BuildSpawnSequence(rng);

        BallManager.Instance.PrepareSpawnSequence(sequence);
        BallManager.Instance.StartSpawning();

        // HUD에 현재 라운드 번호 반영
        UpdateRound(currentRoundIndex);
    }

    /// <summary>
    /// 모든 볼이 삭제되었을 때 호출: 기존 RoundManager.NotifyAllBallsDestroyed 역할.
    /// </summary>
    public void NotifyAllBallsDestroyed()
    {
        if (!roundActive)
            return;

        roundActive = false;

        FlowManager.Instance?.OnRoundFinished();
    }

    // 필요하다면 외부용 프로퍼티 추가
    public StageInstance CurrentStage => currentStage;
    public int CurrentRoundIndex => currentRoundIndex;
    public bool IsRoundActive => roundActive;
}
