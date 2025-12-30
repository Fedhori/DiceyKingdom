using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    public static BallManager Instance { get; private set; }

    [SerializeField] private GameObject ballPrefab; // 현재는 BallFactory가 prefab을 들고 있지만, 인스펙터 용으로 유지
    [SerializeField] private float cycle = 0.1f;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;
    [SerializeField] private Transform playArea;

    // 이번 라운드에 스폰할 희귀도 시퀀스
    readonly List<BallRarity> spawnSequence = new();
    int nextSpawnIndex = 0;
    bool isSpawning = false;
    public event System.Action<int> OnRemainingSpawnCountChanged;

    // 현재 필드에 살아있는 볼 수
    int liveBallCount = 0;

    Coroutine spawnCoroutine;
    Coroutine relaunchCoroutine;

    Vector2 spawnPosition = Vector2.zero;
    Vector2 launchDirection = Vector2.up;

    readonly Queue<BallController> relaunchQueue = new();
    readonly HashSet<BallController> queuedSet = new();

    Bounds playBounds;
    bool hasPlayBounds;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CachePlayAreaBounds();
    }

    // 이번 라운드에서 소환해야 할 볼을 다 뽑았는지 여부
    private bool AllSpawned => !isSpawning && nextSpawnIndex >= spawnSequence.Count;

    /// <summary>
    /// 라운드 시작 전, 이번 라운드에서 사용할 스폰 시퀀스를 세팅.
    /// </summary>
    public void PrepareSpawnSequence(IReadOnlyList<BallRarity> sequence)
    {
        // 이전 코루틴 정리
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        spawnSequence.Clear();

        if (sequence != null)
        {
            for (int i = 0; i < sequence.Count; i++)
            {
                spawnSequence.Add(sequence[i]);
            }
        }

        nextSpawnIndex = 0;
        isSpawning = false;
        NotifyRemainingCountChanged();
    }

    /// <summary>
    /// 스폰 시작. 시퀀스가 비어 있으면 바로 라운드 종료 판정까지 갈 수 있음.
    /// </summary>
    public void StartSpawning()
    {
        // 기존 코루틴이 돌고 있으면 정지
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        if (spawnSequence.Count == 0)
        {
            isSpawning = false;

            if (liveBallCount == 0)
            {
                PlayManager.Instance?.NotifyAllBallsDestroyed();
            }

            return;
        }

        isSpawning = true;

        // 한 번에 모두 생성 (랜덤 위치/방향)
        for (int i = nextSpawnIndex; i < spawnSequence.Count; i++)
        {
            var rarity = spawnSequence[i];
            var pos = GetRandomSpawnPosition();
            var dir = GetRandomDirection();
            BallFactory.Instance.SpawnBall(rarity, pos + spawnOffset, dir);
        }

        nextSpawnIndex = spawnSequence.Count;
        isSpawning = false;
        spawnCoroutine = null;

        if (liveBallCount == 0)
        {
            PlayManager.Instance?.NotifyAllBallsDestroyed();
        }
    }
    
    public void ResetForNewStage()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
        if (relaunchCoroutine != null)
        {
            StopCoroutine(relaunchCoroutine);
            relaunchCoroutine = null;
        }

        spawnSequence.Clear();
        nextSpawnIndex = 0;
        isSpawning = false;
        liveBallCount = 0;
        spawnPosition = Vector2.zero;
        relaunchQueue.Clear();
        queuedSet.Clear();
        NotifyRemainingCountChanged();
    }

    public void SetSpawnPosition(Vector2 position)
    {
        spawnPosition = position;
    }
    
    public void SetLaunchDirection(Vector2 direction)
    {
        if (direction.sqrMagnitude <= 0f)
            return;

        launchDirection = direction.normalized;
    }

    public Vector2 LaunchDirection => launchDirection;
    
    public bool IsSpawning => isSpawning;
    public int RemainingSpawnCount => Mathf.Max(0, spawnSequence.Count - nextSpawnIndex);

    Vector2 GetSpawnPosition()
    {
        return spawnPosition;
    }

    void NotifyRemainingCountChanged()
    {
        int remaining = Mathf.Max(0, spawnSequence.Count - nextSpawnIndex);
        OnRemainingSpawnCountChanged?.Invoke(remaining);
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
            PlayManager.Instance?.NotifyAllBallsDestroyed();
        }
    }

    void OnDisable()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        isSpawning = false;
    }
    
    public void DestroyAllBalls()
    {
        var balls = FindObjectsByType<BallController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var ball in balls)
        {
            if (ball != null)
                Destroy(ball.gameObject);
        }
    }

    public void QueueForRelaunch(BallController ball)
    {
        if (ball == null)
            return;

        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        if (queuedSet.Contains(ball))
            return;

        queuedSet.Add(ball);
        relaunchQueue.Enqueue(ball);

        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        rb.simulated = false;

        if (relaunchCoroutine == null)
            relaunchCoroutine = StartCoroutine(RelaunchRoutine());
    }

    IEnumerator RelaunchRoutine()
    {
        while (relaunchQueue.Count > 0)
        {
            var ball = relaunchQueue.Dequeue();
            queuedSet.Remove(ball);

            RelaunchBall(ball);

            yield return new WaitForSeconds(cycle);
        }

        relaunchCoroutine = null;
    }

    void RelaunchBall(BallController ball)
    {
        if (ball == null)
            return;

        var rb = ball.GetComponent<Rigidbody2D>();
        if (rb == null)
            return;

        var dir = GetRandomDirection();
        var pos = GetRandomSpawnPosition() + spawnOffset;

        ball.transform.position = new Vector3(pos.x, pos.y, ball.transform.position.z);
        rb.simulated = true;
        rb.linearVelocity = dir.normalized * GameConfig.BallSpeed;
    }

    void CachePlayAreaBounds()
    {
        hasPlayBounds = false;

        if (playArea == null)
            return;

        var sr = playArea.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            playBounds = sr.bounds;
            hasPlayBounds = true;
            return;
        }

        playBounds = new Bounds(playArea.position, Vector3.one * 10f);
        hasPlayBounds = true;
    }

    Vector2 GetRandomSpawnPosition()
    {
        if (!hasPlayBounds)
            CachePlayAreaBounds();

        if (!hasPlayBounds)
            return GetSpawnPosition();

        var min = playBounds.min;
        var max = playBounds.max;

        float x = UnityEngine.Random.Range(min.x, max.x);
        float y = UnityEngine.Random.Range(min.y, max.y);
        return new Vector2(x, y);
    }

    Vector2 GetRandomDirection()
    {
        var rng = GameManager.Instance != null ? GameManager.Instance.Rng : new System.Random();
        // random angle 0~360
        double angle = rng.NextDouble() * 360.0;
        float rad = (float)(angle * Mathf.Deg2Rad);
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        if (dir.sqrMagnitude < 0.0001f)
            dir = Vector2.up;
        return dir.normalized;
    }
}
