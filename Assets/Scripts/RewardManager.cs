using UnityEngine;

public sealed class RewardManager : MonoBehaviour
{
    public static RewardManager Instance { get; private set; }

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

        // TODO: 실제 보상 UI/선택 로직으로 교체 예정
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
}