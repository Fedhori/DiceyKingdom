using UnityEngine;

public class BallManager : MonoBehaviour
{
    public static BallManager Instance { get; private set; }

    [SerializeField] private GameObject ballPrefab;

    private float cycle = 0.1f;
    private float currentCycle = 0f;

    [SerializeField] private int spawnCount = 20;
    private int currentSpawnCount = 0;

    // 현재 필드에 살아있는 볼 수
    private int liveBallCount = 0;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // 야매임. 고쳐야 함 ㅋㅋ
        currentSpawnCount = spawnCount;
    }

    // 이번 라운드에서 소환해야 할 볼을 다 뽑았는지 여부
    private bool AllSpawned => currentSpawnCount >= spawnCount;

    void Update()
    {
        if (currentSpawnCount >= spawnCount)
            return;

        currentCycle += Time.deltaTime;

        if (currentCycle > cycle)
        {
            currentSpawnCount++;
            currentCycle -= cycle;

            if (currentSpawnCount % 10 == 0)
                BallFactory.Instance.SpawnBall("ball.gold");
            else
                BallFactory.Instance.SpawnBall("ball.basic");
        }
    }

    /// <summary>
    /// 볼이 Initialize를 완료했을 때 호출.
    /// </summary>
    public void RegisterBall(BallController controller)
    {
        if (controller == null)
            return;

        liveBallCount++;
    }

    /// <summary>
    /// 볼이 파괴/비활성화될 때 호출.
    /// </summary>
    public void UnregisterBall(BallController controller)
    {
        if (controller == null)
            return;

        liveBallCount--;
        if (liveBallCount < 0)
            liveBallCount = 0;

        // 모든 볼이 이미 소환되었고, 현재 살아있는 볼이 0이라면 라운드 종료
        if (AllSpawned && liveBallCount == 0)
        {
            RoundManager.Instance?.NotifyAllBallsDestroyed();
        }
    }
    
    public void ResetForNewRound()
    {
        currentSpawnCount = 0;
        currentCycle = 0f;
        liveBallCount = 0;
    }
}
