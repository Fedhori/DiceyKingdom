using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Data;

public sealed class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }
    [SerializeField] private int ballRewardCount = 3;
    [SerializeField] private GameObject ballRewardOverlay;
    [SerializeField] private GameObject ballRewardPrefab;
    [SerializeField] private RectTransform ballRewardParent;
    [SerializeField] private Button ballRerollButton;
    [SerializeField] private TMP_Text ballRerollCostText;

    [SerializeField] private int baseBallRerollCost = 1;
    [SerializeField] private int ballRerollCostIncrement = 1;
    private int currentBallRerollCost;

    readonly List<BallRewardData> currentBallRewards = new();

    bool isOpen;
    StageInstance currentStage;
    int stageIndex;

    sealed class BallRewardData
    {
        public string BallId;
        public int BallCount;
        public BallDto BallDto;
    }

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
        SubscribePlayer();
        SetupRerollButton();
    }

    public void Open(StageInstance stage, int stageIndex)
    {
        currentStage = stage;
        this.stageIndex = stageIndex;
        isOpen = true;

        currentBallRerollCost = baseBallRerollCost;
        ClearBallRewards();

        BuildBallRewardSelection();
        InstantiateBallRewardViews();
        UpdateRerollUI();

        if (ballRewardOverlay != null)
            ballRewardOverlay.SetActive(true);
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Debug.Log("[RewardManager] Close reward");

        if (ballRewardOverlay != null)
            ballRewardOverlay.SetActive(false);

        FlowManager.Instance?.OnRewardClosed();
    }

    void SubscribePlayer()
    {
        UnsubscribePlayer();

        var pm = PlayerManager.Instance;
        if (pm == null)
            return;

        var player = pm.Current;
        if (player == null)
            return;

        player.OnCurrencyChanged += HandleCurrencyChanged;
    }

    void UnsubscribePlayer()
    {
        var pm = PlayerManager.Instance;
        if (pm == null)
            return;

        var player = pm.Current;
        if (player == null)
            return;

        player.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    void SetupRerollButton()
    {
        if (ballRerollButton == null)
            return;

        ballRerollButton.onClick.RemoveAllListeners();
        ballRerollButton.onClick.AddListener(BallRewardReroll);
    }
    
    void HandleCurrencyChanged(int value)
    {
        UpdateRerollUI();
    }

    void ClearBallRewards()
    {
        if (ballRewardParent == null)
            return;

        currentBallRewards.Clear();

        for (int i = ballRewardParent.childCount - 1; i >= 0; i--)
        {
            var child = ballRewardParent.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    void BuildBallRewardSelection()
    {
        currentBallRewards.Clear();

        if (ballRewardCount <= 0)
            return;

        if (!BallRepository.IsInitialized)
        {
            Debug.LogError("[RewardManager] BallRepository not initialized. Cannot build ball rewards.");
            return;
        }

        var candidates = new List<BallDto>();
        foreach (var dto in BallRepository.All)
        {
            if (dto == null)
                continue;

            if (dto.isNotReward)
                continue;

            candidates.Add(dto);
        }

        if (candidates.Count == 0)
        {
            Debug.LogError("[RewardManager] No eligible ball rewards found.");
            return;
        }

        var usedIds = new HashSet<string>();
        int uniqueTarget = Mathf.Min(ballRewardCount, candidates.Count);

        while (currentBallRewards.Count < uniqueTarget)
        {
            var dto = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (usedIds.Contains(dto.id))
                continue;

            usedIds.Add(dto.id);
            currentBallRewards.Add(new BallRewardData
            {
                BallId = dto.id,
                BallDto = dto,
                BallCount = CalculateBallRewardCount(dto)
            });
        }

        while (currentBallRewards.Count < ballRewardCount)
        {
            var dto = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            currentBallRewards.Add(new BallRewardData
            {
                BallId = dto.id,
                BallDto = dto,
                BallCount = CalculateBallRewardCount(dto)
            });
        }
    }

    public void BallRewardReroll()
    {
        if (!isOpen)
            return;

        var currencyManager = CurrencyManager.Instance;
        if (currencyManager == null)
        {
            Debug.LogError("[RewardManager] CurrencyManager not found. Cannot reroll.");
            return;
        }

        int cost = Mathf.Max(1, currentBallRerollCost);
        if (!currencyManager.TrySpend(cost))
        {
            SetRerollButtonState(false);
            return;
        }

        currentBallRerollCost = Mathf.Max(1, currentBallRerollCost + Mathf.Max(1, ballRerollCostIncrement));

        ClearBallRewards();
        BuildBallRewardSelection();
        InstantiateBallRewardViews();
        UpdateRerollUI();
    }

    void InstantiateBallRewardViews()
    {
        if (ballRewardPrefab == null || ballRewardParent == null)
        {
            Debug.LogError("[RewardManager] ballRewardPrefab or ballRewardParent missing.");
            return;
        }

        foreach (var reward in currentBallRewards)
        {
            var go = Instantiate(ballRewardPrefab, ballRewardParent);
            var controller = go.GetComponent<BallRewardController>();
            if (controller == null)
            {
                Debug.LogError("[RewardManager] BallRewardController not found on prefab.");
                continue;
            }

            controller.Initialize(reward.BallId, reward.BallCount, HandleBallRewardSelected);
        }
    }

    int CalculateBallRewardCount(BallDto dto)
    {
        if (dto == null || dto.cost <= 0)
            return 0;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
            return 0;

        float baseCost = player.BallCost;
        float multiplier = UnityEngine.Random.Range(1.0f, 1.5f);
        float adjustedCost = baseCost * multiplier;
        int count = Mathf.CeilToInt(adjustedCost / dto.cost);
        return Mathf.Max(1, count);
    }

    void UpdateRerollUI()
    {
        int currency = CurrencyManager.Instance?.CurrentCurrency ?? 0;
        bool canReroll = currency >= currentBallRerollCost;
        SetRerollButtonState(canReroll);
    }

    void SetRerollButtonState(bool canReroll)
    {
        if (ballRerollCostText != null)
        {
            ballRerollCostText.text = currentBallRerollCost.ToString();
            ballRerollCostText.color = canReroll ? Colors.Black : Colors.Red;
        }

        if (ballRerollButton != null)
            ballRerollButton.interactable = canReroll;
    }

    void HandleBallRewardSelected(string ballId, int count)
    {
        if (!isOpen)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null)
        {
            Debug.LogError("[RewardManager] Player not found. Cannot apply reward.");
            return;
        }

        player.BallDeck.Add(ballId, count);
        Close();
    }
}
