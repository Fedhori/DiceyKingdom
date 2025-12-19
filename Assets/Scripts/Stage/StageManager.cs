using UnityEngine;
using UnityEngine.UI;

public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private StageHudView stageHudView;
    [SerializeField] private GameObject spawnSelectHintUI;
    [SerializeField] private GameObject stallEndButton;
    [SerializeField] private float noScoreTimeoutSeconds = 5f;
    [SerializeField] private float stallButtonDelaySeconds = 60f;
    [SerializeField] private float autoEndSeconds = 180f;

    // Stage & Round 상태
    StageInstance currentStage;
    int currentRoundIndex;
    bool roundActive;
    bool waitingSpawnSelection;
    Vector2 pendingSpawnPoint;
    float roundElapsed;
    float noScoreElapsed;
    bool stallButtonShown;
    bool forceEndTriggered;

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

    void Update()
    {
        if (!roundActive || forceEndTriggered)
            return;

        roundElapsed += Time.deltaTime;
        noScoreElapsed += Time.deltaTime;

        if (!stallButtonShown &&
            (noScoreElapsed >= noScoreTimeoutSeconds || roundElapsed >= stallButtonDelaySeconds))
        {
            ShowStallEndButton();
        }

        if (roundElapsed >= autoEndSeconds)
        {
            ForceEndRound(applyAutoBonus: true);
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
    
    public void StartRound(StageInstance stage, int roundIndex)
    {
        ScoreManager.Instance.previousScore = ScoreManager.Instance.TotalScore;
        currentStage = stage;
        currentRoundIndex = roundIndex;
        roundActive = true;
        waitingSpawnSelection = false;
        forceEndTriggered = false;
        ResetStallState();

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
        ToggleSpawnSelectHint(true);
        spawnPointManager.OnPointSelected = OnSpawnPointSelected;
        spawnPointManager.ShowPoints(points);
    }

    void OnSpawnPointSelected(Vector2 pos)
    {
        if (!waitingSpawnSelection)
            return;

        pendingSpawnPoint = pos;
        waitingSpawnSelection = false;

        ToggleSpawnSelectHint(false);
        BallManager.Instance.SetSpawnPosition(pos);
        BallManager.Instance.StartSpawning();
    }

    void ToggleSpawnSelectHint(bool show)
    {
        if (spawnSelectHintUI != null)
            spawnSelectHintUI.SetActive(show);
    }

    void ToggleStallEndButton(bool show)
    {
        if (stallEndButton != null)
            stallEndButton.SetActive(show);
    }

    void ShowStallEndButton()
    {
        stallButtonShown = true;
        ToggleStallEndButton(true);
    }

    void ResetStallState()
    {
        roundElapsed = 0f;
        noScoreElapsed = 0f;
        stallButtonShown = false;
        ToggleStallEndButton(false);
    }

    void CancelSpawnSelection()
    {
        waitingSpawnSelection = false;
        ToggleSpawnSelectHint(false);
        BallSpawnPointManager.Instance?.HidePoints();
    }

    public void ResetNoScoreTimer()
    {
        noScoreElapsed = 0f;
    }

    public void OnStallEndButtonClicked()
    {
        ForceEndRound(applyAutoBonus: false);
    }

    void ForceEndRound(bool applyAutoBonus)
    {
        if (!roundActive || forceEndTriggered)
            return;

        forceEndTriggered = true;
        roundActive = false;

        CancelSpawnSelection();
        ToggleStallEndButton(false);

        BallManager.Instance?.StopSpawning();
        DestroyAllBallsInField();

        if (applyAutoBonus)
        {
            ScoreManager.Instance?.MultiplyScore(10.0);
        }

        FlowManager.Instance?.OnRoundFinished();
        
        ModalManager.Instance.ShowInfo(
            titleTable: "modal", titleKey: "modal.forceendround.title",
            messageTable: "modal", messageKey: "modal.forceendround.desc",
            onConfirm: () => { }
        );
    }

    void DestroyAllBallsInField()
    {
        var balls = FindObjectsByType<BallController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < balls.Length; i++)
        {
            if (balls[i] != null)
                Destroy(balls[i].gameObject);
        }
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
        ToggleStallEndButton(false);
        stallButtonShown = false;

        FlowManager.Instance?.OnRoundFinished();
    }

    // 필요하다면 외부용 프로퍼티 추가
    public StageInstance CurrentStage => currentStage;
    public int CurrentRoundIndex => currentRoundIndex;
    public bool IsRoundActive => roundActive;
}
