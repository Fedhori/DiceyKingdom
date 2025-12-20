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

    Vector2 spawnPosition = Vector2.zero;
    readonly List<Vector2> spawnPoints = new();

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

    /// <summary>
    /// 라운드 시작 시 초기화.
    /// </summary>
    public void ResetForNewRound()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        spawnSequence.Clear();
        nextSpawnIndex = 0;
        isSpawning = false;
        liveBallCount = 0;
        spawnPosition = Vector2.zero;
        spawnPoints.Clear();
        NotifyRemainingCountChanged();
    }

    public void SetSpawnPosition(Vector2 position)
    {
        spawnPosition = position;
    }

    public void SetSpawnPoints(IReadOnlyList<Vector2> points)
    {
        spawnPoints.Clear();

        if (points == null)
            return;

        for (int i = 0; i < points.Count; i++)
        {
            spawnPoints.Add(points[i]);
        }
    }
    
    public bool IsSpawning => isSpawning;
    public int RemainingSpawnCount => Mathf.Max(0, spawnSequence.Count - nextSpawnIndex);

    Vector2 GetSpawnPosition()
    {
        if (spawnPoints.Count > 0)
        {
            int idx = Random.Range(0, spawnPoints.Count);
            return spawnPoints[idx];
        }

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
}
