using UnityEngine;

public sealed class PlayManager : MonoBehaviour
{
    public static PlayManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartPlay()
    {
        ItemManager.Instance?.InitializeFromPlayer(PlayerManager.Instance.Current);
        BlockManager.Instance.BeginSpawnRamp();
    }

    public void FinishPlay()
    {
        ItemManager.Instance?.ClearAll();
        StageManager.Instance?.OnPlayFinished();
    }
}
