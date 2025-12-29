using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    public static BallManager Instance { get; private set; }

    [SerializeField] private GameObject ballPrefab; // 현재는 BallFactory가 prefab을 들고 있지만, 인스펙터 용으로 유지
    [SerializeField] private float cycle = 0.1f;
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;

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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
                StageManager.Instance?.NotifyAllBallsDestroyed();
            }

            return;
        }

        isSpawning = true;
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (nextSpawnIndex < spawnSequence.Count)
        {
            var rarity = spawnSequence[nextSpawnIndex];
            nextSpawnIndex++;

            var pos = GetSpawnPosition();
            BallFactory.Instance.SpawnBall(rarity, pos + spawnOffset);

            NotifyRemainingCountChanged();
            yield return new WaitForSeconds(cycle);
        }

        isSpawning = false;
        spawnCoroutine = null;

        if (liveBallCount == 0)
        {
            StageManager.Instance?.NotifyAllBallsDestroyed();
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
            StageManager.Instance?.NotifyAllBallsDestroyed();
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

        var dir = launchDirection;
        if (dir == Vector2.zero)
            dir = Vector2.up;

        var pos = GetSpawnPosition() + spawnOffset;

        ball.transform.position = new Vector3(pos.x, pos.y, ball.transform.position.z);
        rb.simulated = true;
        rb.linearVelocity = dir.normalized * GameConfig.BallSpeed;
    }
}
