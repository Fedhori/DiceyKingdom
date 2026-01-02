using UnityEngine;

public sealed class PlayManager : MonoBehaviour
{
    public static PlayManager Instance { get; private set; }

    [SerializeField] private UIGaugeBar spawnTimeGauge;

    bool spawnUiVisible;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (spawnTimeGauge != null)
            spawnTimeGauge.SetLabelVisible(false);
    }

    public void StartPlay()
    {
        ItemManager.Instance?.BeginPlay();
        BlockManager.Instance.BeginSpawnRamp();
    }

    public void FinishPlay()
    {
        ItemManager.Instance?.EndPlay();
        ProjectileFactory.Instance?.ClearAllProjectiles();
        StageManager.Instance?.OnPlayFinished();
    }

    void Update()
    {
        if (spawnTimeGauge == null)
            return;

        bool isPlay = StageManager.Instance != null && StageManager.Instance.CurrentPhase == StagePhase.Play;
        if (!isPlay)
        {
            SetSpawnUiVisible(false);
            return;
        }

        SetSpawnUiVisible(true);

        var block = BlockManager.Instance;
        if (block == null)
            return;

        float max = block.SpawnDurationSeconds;
        float elapsed = block.SpawnElapsedSeconds;

        spawnTimeGauge.UpdateFill(elapsed, max);
    }

    void SetSpawnUiVisible(bool visible)
    {
        if (spawnUiVisible == visible)
            return;

        spawnUiVisible = visible;

        if (spawnTimeGauge != null)
            spawnTimeGauge.gameObject.SetActive(visible);
    }
}
