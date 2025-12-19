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
    bool waitingSpawnSelection;
    Vector2 pendingSpawnPoint;

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
        waitingSpawnSelection = false;

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

        var sequence = BuildRaritySequence(player, rng);

        BallManager.Instance.PrepareSpawnSequence(sequence);
        BeginSpawnSelection();

        // HUD에 현재 라운드 번호 반영
        UpdateRound(currentRoundIndex);
    }

    void BeginSpawnSelection()
    {
        var spawnPointManager = BallSpawnPointManager.Instance;
        if (spawnPointManager == null)
        {
            Debug.LogWarning("[StageManager] spawnPointManager not set. Spawning immediately.");
            BallManager.Instance.StartSpawning();
            return;
        }

        var pinMgr = PinManager.Instance;
        if (pinMgr == null)
        {
            Debug.LogWarning("[StageManager] PinManager missing. Spawning immediately.");
            BallManager.Instance.StartSpawning();
            return;
        }

        var points = pinMgr.GetBallSpawnPoints();
        if (points == null || points.Count == 0)
        {
            Debug.LogWarning("[StageManager] No spawn points. Spawning immediately.");
            BallManager.Instance.StartSpawning();
            return;
        }

        waitingSpawnSelection = true;
        spawnPointManager.OnPointSelected = OnSpawnPointSelected;
        spawnPointManager.ShowPoints(points);
    }

    void OnSpawnPointSelected(Vector2 pos)
    {
        if (!waitingSpawnSelection)
            return;

        pendingSpawnPoint = pos;
        waitingSpawnSelection = false;

        // TODO: 4.x에서 선택 지점 적용. 현재는 위치 적용 없이 스폰 시작.
        BallManager.Instance.StartSpawning();
    }

    static System.Collections.Generic.List<BallRarity> BuildRaritySequence(PlayerInstance player, System.Random rng)
    {
        var list = new System.Collections.Generic.List<BallRarity>();
        int count = Mathf.Max(0, player.InitialBallCount);
        var probs = player.RarityProbabilities;

        if (rng == null)
            rng = new System.Random();

        for (int i = 0; i < count; i++)
        {
            var rarity = RollRarity(probs, rng);
            list.Add(rarity);
        }

        return list;
    }

    static BallRarity RollRarity(System.Collections.Generic.IReadOnlyList<float> probs, System.Random rng)
    {
        if (probs == null || probs.Count == 0)
            return BallRarity.Common;

        double roll = rng.NextDouble() * 100.0;
        double acc = 0.0;
        for (int i = 0; i < probs.Count; i++)
        {
            acc += probs[i];
            if (roll <= acc)
                return (BallRarity)i;
        }

        return BallRarity.Common;
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
