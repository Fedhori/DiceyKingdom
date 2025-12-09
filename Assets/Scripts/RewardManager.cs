using System;
using UnityEngine;

public sealed class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }
    [SerializeField] private int ballRewardCount = 3;
    [SerializeField] private GameObject ballRewardOverlay;
    [SerializeField] private GameObject ballRewardPrefab;
    [SerializeField] private RectTransform ballRewardParent;

    [SerializeField] private int baseBallRerollCost = 1;
    [SerializeField] private int ballRerollCostIncrement = 1;
    private int currentBallRerollCost;

    bool isOpen;
    StageInstance currentStage;
    int stageIndex;

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
    }

    public void Open(StageInstance stage, int stageIndex)
    {
        currentStage = stage;
        this.stageIndex = stageIndex;
        isOpen = true;

        currentBallRerollCost = baseBallRerollCost;
        ClearBallRewards();

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
    
    void HandleCurrencyChanged(int value)
    {
        // TODO
    }

    void ClearBallRewards()
    {
        if (ballRewardParent == null)
            return;

        for (int i = ballRewardParent.childCount - 1; i >= 0; i--)
        {
            var child = ballRewardParent.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }
}
