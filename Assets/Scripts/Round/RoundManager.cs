// RoundManager.cs
using UnityEngine;

public sealed class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    StageInstance currentStage;
    int currentRoundIndex;
    bool roundActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartRound(StageInstance stage, int roundIndex)
    {
        currentStage = stage;
        currentRoundIndex = roundIndex;
        roundActive = true;

        PlayerManager.Instance.ResetPlayer();
        PinManager.Instance.ResetAllPins();
        BallManager.Instance.ResetForNewRound();
    }
    
    public void NotifyAllBallsDestroyed()
    {
        if (!roundActive)
            return;

        roundActive = false;
        Debug.Log("[RoundManager] All balls destroyed. Round finished.");

        StageManager.Instance?.HandleRoundFinished();
    }
}