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

        var player = PlayerManager.Instance.Current;
        if (player == null)
        {
            Debug.LogError("[RoundManager] Player not created. Cannot build ball deck.");
            return;
        }

        var rng = GameManager.Instance != null
            ? GameManager.Instance.Rng
            : new System.Random();

        var sequence = player.BallDeck.BuildSpawnSequence(rng);

        BallManager.Instance.PrepareSpawnSequence(sequence);
        BallManager.Instance.StartSpawning();
    }

    public void NotifyAllBallsDestroyed()
    {
        if (!roundActive)
            return;

        roundActive = false;

        FlowManager.Instance?.OnRoundFinished();
    }
}