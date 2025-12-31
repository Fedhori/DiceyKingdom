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
        ItemManager.Instance?.BeginPlay();
        BlockManager.Instance.BeginSpawnRamp();
    }

    public void FinishPlay()
    {
        ItemManager.Instance?.EndPlay();
        StageManager.Instance?.OnPlayFinished();
    }
}
