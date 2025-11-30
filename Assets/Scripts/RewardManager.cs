// RewardManager.cs
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

        PlayerManager.Instance.Current.BallDeck.Add("ball.basic", 5);
        Close();
    }

    public void Close()
    {
        if (!isOpen)
            return;

        isOpen = false;
        Debug.Log("[RewardManager] Close reward");

        StageManager.Instance?.HandleRewardClosed();
    }
}