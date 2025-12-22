using System;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.UI;

public sealed class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [SerializeField] private StageHudView stageHudView;
    [SerializeField] private LocalizeStringEvent stallNoticeText;
    [SerializeField] private TMP_Text ballCountText;
    [SerializeField] private Button startSpawnButton;

    // Stage 상태
    StageInstance currentStage;
    public bool playActive;
    
    [SerializeField] private float stallWarningTime = 60f;
    [SerializeField] private float stallForceTime = 90f;
    bool stallTimerRunning;
    float stallTimer;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        if (BallManager.Instance != null)
            BallManager.Instance.OnRemainingSpawnCountChanged += UpdateBallCount;
        
        var player = PlayerManager.Instance?.Current;
        if (player != null)
        {
            player.OnBallCountChanged += UpdateBallCount;
            UpdateBallCount(player.BallCount);
        }
    }

    void OnDisable()
    {
        if (BallManager.Instance != null)
            BallManager.Instance.OnRemainingSpawnCountChanged -= UpdateBallCount;
        
        var player = PlayerManager.Instance?.Current;
        if (player != null)
            player.OnBallCountChanged -= UpdateBallCount;
    }

    /// <summary>
    /// 현재 스테이지 바인딩 + HUD 갱신.
    /// </summary>
    public void SetStage(StageInstance stage)
    {
        currentStage = stage;
        playActive = false;
        ScoreManager.Instance.previousScore = ScoreManager.Instance.TotalScore;

        if (stageHudView != null && stage != null)
        {
            var displayStageNumber = stage.StageIndex + 1;
            stageHudView.SetStageInfo(
                displayStageNumber,
                StageRepository.Count,
                stage.NeedScore
            );

            UpdateStageHud();
        }
    }

    void Update()
    {
        if (!playActive)
            return;

        HandleStallTimer();
    }

    public void StartStagePlay(StageInstance stage)
    {
        currentStage = stage;
        ResetStallState();
        UpdateStartSpawnButton(false, false);
        
        BallManager.Instance.ResetForNewStage();

        var player = PlayerManager.Instance.Current;

        var rng = GameManager.Instance != null
            ? GameManager.Instance.Rng
            : new System.Random();

        var sequence = BuildRaritySequence(player, rng);

        BallManager.Instance.PrepareSpawnSequence(sequence);
        
        var points = PinManager.Instance.GetBallSpawnPoints();
        BallManager.Instance.SetSpawnPoints(points);
        UpdateStartSpawnButton(true, true);
    }

    void StartBallSpawning()
    {
        playActive = true;
        BallManager.Instance.StartSpawning();
    }

    public void OnStartSpawnButtonClicked()
    {
        UpdateStartSpawnButton(false, false);
        StartBallSpawning();
    }

    void UpdateStartSpawnButton(bool show, bool interactable)
    {
        if (startSpawnButton == null)
            return;

        startSpawnButton.gameObject.SetActive(show);
        startSpawnButton.interactable = interactable;
    }

    void HandleStallTimer()
    {
        if (!playActive)
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
            BallManager.Instance.DestroyAllBalls();
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

    void UpdateBallCount(int count)
    {
        if (ballCountText == null)
            return;

        if (count < 0)
            count = 0;

        ballCountText.text = $"x{count}";
    }

    static System.Collections.Generic.List<BallRarity> BuildRaritySequence(PlayerInstance player, System.Random rng)
    {
        var list = new System.Collections.Generic.List<BallRarity>();
        int count = Mathf.Max(0, player.BallCount);
        var probs = player.RarityProbabilities;

        rng ??= new System.Random();

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
    
    public void NotifyAllBallsDestroyed()
    {
        if (!playActive)
            return;

        FinishPlay();
    }

    void FinishPlay()
    {
        playActive = false;
        ResetStallState();
        UpdateBallCount(PlayerManager.Instance.Current.BallCount);
        FlowManager.Instance?.OnPlayFinished();
    }

    void ResetStallState()
    {
        stallTimerRunning = false;
        stallTimer = 0f;
        SetStallNoticeVisible(false);
        UpdateStartSpawnButton(false, false);
    }

    void UpdateStageHud()
    {
        if (stageHudView == null || currentStage == null)
            return;

        var displayStageNumber = currentStage.StageIndex + 1;
        stageHudView.SetStageInfo(
            displayStageNumber,
            StageRepository.Count,
            currentStage.NeedScore
        );
    }
}
