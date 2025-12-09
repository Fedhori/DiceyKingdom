using UnityEngine;

public sealed class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }
    private int ballRewardCount = 3;
    [SerializeField] private GameObject ballRewardOverlay;
    [SerializeField] private GameObject ballRewardPrefab;
    [SerializeField] private RectTransform ballRewardParent;

    private int baseBallRerollCost = 1;
    private int ballRerollCostIncrement = 1;
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

    public void Open(StageInstance stage, int stageIndex)
    {
        currentStage = stage;
        this.stageIndex = stageIndex;
        isOpen = true;
        PlayerManager.Instance.Current.BallDeck.Add("ball.basic", 5);

        // 지금은 흐름 테스트용으로 즉시 닫는다.
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Debug.Log("[RewardManager] Close reward");

        FlowManager.Instance?.OnRewardClosed();
    }

    public void BallRewardReroll()
    {
        
    }
}