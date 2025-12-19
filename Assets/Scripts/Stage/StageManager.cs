using System;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;

public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private StageHudView stageHudView;
    [SerializeField] private GameObject spawnSelectHintUI;
    [SerializeField] private LocalizeStringEvent stallNoticeText;
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private float stallWarningTime = 60f;
    [SerializeField] private float stallForceTime = 90f;

    // Stage & Round 상태
    StageInstance currentStage;
    int currentRoundIndex;
    bool roundActive;
    bool waitingSpawnSelection;
    bool stallTimerRunning;
    bool spawnStarted;
    float stallTimer;
    bool subscribedBallCount;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SubscribeBallCountEvent();
    }

    void OnDestroy()
    {
        UnsubscribeBallCountEvent();
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
        if (!roundActive)
            return;

        HandleStallTimer();
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
        ResetStallState();
        RefreshBallCountImmediate();

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
            StartBallSpawning();
            return;
        }

        var pinMgr = PinManager.Instance;
        if (pinMgr == null)
        {
            Debug.LogWarning("[StageManager] PinManager missing. Spawning immediately.");
            StartBallSpawning();
            return;
        }

        var points = pinMgr.GetBallSpawnPoints();
        if (points == null || points.Count == 0)
        {
            Debug.LogWarning("[StageManager] No spawn points. Spawning immediately.");
            StartBallSpawning();
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

        waitingSpawnSelection = false;

        ToggleSpawnSelectHint(false);
        BallManager.Instance.SetSpawnPosition(pos);
        StartBallSpawning();
    }

    void StartBallSpawning()
    {
        spawnStarted = true;
        BallManager.Instance.StartSpawning();
    }

    void ToggleSpawnSelectHint(bool show)
    {
        if (spawnSelectHintUI != null)
            spawnSelectHintUI.SetActive(show);
    }

    void HandleStallTimer()
    {
        if (!spawnStarted)
            return;

        var ballMgr = BallManager.Instance;
        bool stillSpawning = ballMgr != null && ballMgr.IsSpawning;

        if (!stallTimerRunning)
        {
            if (stillSpawning)
                return;

            StartStallTimer();
        }

        stallTimer += Time.deltaTime;

        if (stallTimer >= stallWarningTime)
        {
            float remaining = Mathf.Max(0f, stallForceTime - stallTimer);
            UpdateStallNotice(remaining);
        }

        if (stallTimer >= stallForceTime)
        {
            ForceFinishRound();
        }
    }

    void StartStallTimer()
    {
        stallTimerRunning = true;
        stallTimer = 0f;
        SetStallNoticeVisible(false);
    }

    void UpdateStallNotice(float remainingSeconds)
    {
        if (stallNoticeText == null)
            return;

        int seconds = Mathf.CeilToInt(remainingSeconds);
        if (seconds < 0)
            seconds = 0;

        if (stallNoticeText.StringReference.TryGetValue("value", out var v) && v is StringVariable sv)
            sv.Value = seconds.ToString();
        SetStallNoticeVisible(true);
    }

    void SetStallNoticeVisible(bool show)
    {
        if (stallNoticeText != null)
            stallNoticeText.gameObject.SetActive(show);
    }

    void ForceFinishRound()
    {
        if (!roundActive)
            return;

        FinishRound();
    }

    void RefreshBallCountImmediate()
    {
        if (BallManager.Instance == null)
            return;

        UpdateBallCount(BallManager.Instance.RemainingSpawnCount);
    }

    void SubscribeBallCountEvent()
    {
        var ballMgr = BallManager.Instance;
        if (ballMgr == null || subscribedBallCount)
            return;

        ballMgr.OnRemainingSpawnCountChanged += UpdateBallCount;
        subscribedBallCount = true;
    }

    void UnsubscribeBallCountEvent()
    {
        var ballMgr = BallManager.Instance;
        if (ballMgr == null || !subscribedBallCount)
            return;

        ballMgr.OnRemainingSpawnCountChanged -= UpdateBallCount;
        subscribedBallCount = false;
    }

    void UpdateBallCount(int count)
    {
        if (ballCountText == null)
            return;

        ballCountText.text = $"x{count}";
    }

    static System.Collections.Generic.List<BallRarity> BuildRaritySequence(PlayerInstance player, System.Random rng)
    {
        var list = new System.Collections.Generic.List<BallRarity>();
        int count = Mathf.Max(0, player.BallCount);
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

        FinishRound();
    }

    void FinishRound()
    {
        roundActive = false;
        ResetStallState();
        FlowManager.Instance?.OnRoundFinished();
    }

    void ResetStallState()
    {
        stallTimerRunning = false;
        spawnStarted = false;
        stallTimer = 0f;
        SetStallNoticeVisible(false);
    }
}
