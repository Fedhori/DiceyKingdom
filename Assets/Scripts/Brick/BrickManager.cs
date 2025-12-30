using System.Collections.Generic;
using UnityEngine;

public sealed class BrickManager : MonoBehaviour
{
    public static BrickManager Instance { get; private set; }

    [SerializeField] private BrickFactory brickFactory;
    [SerializeField] private Transform playArea;
    [SerializeField] private Vector2 brickSize = new Vector2(128f, 64f);
    private int currentHp;
    [SerializeField] private float fallSpeed = 40f;

    [Header("Spawn Ramp")]
    [SerializeField] private float spawnDurationSeconds = 30f;
    [SerializeField] private float spawnRateStartPerSec = 1f;
    [SerializeField] private float spawnRateEndPerSec = 2f;
    private readonly List<BrickController> activeBricks = new();
    private Vector2 originTopLeft;
    private Vector2 originTopRight;
    float spawnElapsed;
    float spawnAccumulator;
    bool spawnWindowActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ComputeOrigin();
    }

    void Update()
    {
        // Play 중에만 하강
        var flow = FlowManager.Instance;
        if (flow == null || flow.CurrentPhase != FlowPhase.Play)
            return;

        float dy = fallSpeed * Time.deltaTime;
        if (dy <= 0f)
            return;

        MoveAllBricksDown(dy);

        UpdateSpawnRamp();
    }
    
    public void BeginSpawnRamp()
    {
        spawnWindowActive = true;
        spawnElapsed = 0f;
        spawnAccumulator = 0f;
    }

    void ComputeOrigin()
    {
        if (playArea == null)
        {
            originTopLeft = Vector2.zero;
            return;
        }

        var sr = playArea.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            var size = sr.bounds.size;
            var center = sr.bounds.center;
            originTopLeft = new Vector2(center.x - size.x * 0.5f, center.y + size.y * 0.5f);
            originTopRight = new Vector2(center.x + size.x * 0.5f, center.y + size.y * 0.5f);
            
        }
    }

    // TODO - 이게 manager가 아니라 brickController에서 관리되어야 할게 아닌가?
    void MoveAllBricksDown(float distance)
    {
        if (distance <= 0f)
            return;

        float delta = distance;
        for (int i = activeBricks.Count - 1; i >= 0; i--)
        {
            var brick = activeBricks[i];
            if (brick == null)
            {
                activeBricks.RemoveAt(i);
                continue;
            }

            brick.transform.position += new Vector3(0f, -delta, 0f);
        }
    }

    void SpawnRandomBrickFromTop()
    {
        if (brickFactory == null)
            return;

        float minX = originTopLeft.x + brickSize.x * 0.5f;
        float maxX = originTopRight.x - brickSize.x * 0.5f;
        float y = originTopLeft.y - brickSize.y * 0.5f;

        float x = Random.Range(minX, maxX);
        Vector3 worldPos = new Vector3(x, y, 0f);

        var brick = brickFactory.CreateBrick(currentHp, Vector2Int.zero, worldPos);
        if (brick != null && !activeBricks.Contains(brick))
            activeBricks.Add(brick);
    }

    public void NotifyBrickDestroyed(BrickController brick)
    {
        if (brick == null)
            return;

        activeBricks.Remove(brick);
        CheckClearCondition();
    }

    void UpdateSpawnRamp()
    {
        if (!spawnWindowActive)
            return;

        spawnElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(spawnElapsed / spawnDurationSeconds);
        float rate = Mathf.Lerp(spawnRateStartPerSec, spawnRateEndPerSec, t);

        spawnAccumulator += rate * Time.deltaTime;

        int spawnCount = Mathf.FloorToInt(spawnAccumulator);
        if (spawnCount > 0)
            spawnAccumulator -= spawnCount;

        for (int i = 0; i < spawnCount; i++)
            SpawnRandomBrickFromTop();

        if (spawnElapsed >= spawnDurationSeconds)
            spawnWindowActive = false;

        CheckClearCondition();
    }

    void CheckClearCondition()
    {
        if (spawnWindowActive)
            return;

        if (activeBricks.Count > 0)
            return;

        PlayManager.Instance?.FinishPlay();
    }
}
