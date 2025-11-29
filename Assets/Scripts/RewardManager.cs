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

        Debug.Log($"[RewardManager] Open reward for stage {stageIndex + 1}");
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