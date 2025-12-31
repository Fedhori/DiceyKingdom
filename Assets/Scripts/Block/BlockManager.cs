using System.Collections.Generic;
using UnityEngine;

public sealed class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [SerializeField] private Transform playArea;
    [SerializeField] private Vector2 blockSize = new Vector2(128f, 64f);
    private int currentHp;
    [SerializeField] private float fallSpeed = 40f;

    [Header("Spawn Ramp")]
    [SerializeField] private float spawnDurationSeconds = 30f;
    [SerializeField] private float spawnRateStartPerSec = 1f;
    [SerializeField] private float spawnRateEndPerSec = 2f;
    private readonly List<BlockController> activeBlocks = new();
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
        if (StageManager.Instance.CurrentPhase != StagePhase.Play)
            return;

        float dy = fallSpeed * Time.deltaTime;
        if (dy <= 0f)
            return;

        MoveAllBlocksDown(dy);

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

    // TODO - 이게 manager가 아니라 Controller서에서 관리되어야 할게 아닌가?
    void MoveAllBlocksDown(float distance)
    {
        if (distance <= 0f)
            return;

        float delta = distance;
        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            var block = activeBlocks[i];
            if (block == null)
            {
                activeBlocks.RemoveAt(i);
                continue;
            }

            block.transform.position += new Vector3(0f, -delta, 0f);
        }
    }

    void SpawnBlock()
    {
        float minX = originTopLeft.x + blockSize.x * 0.5f;
        float maxX = originTopRight.x - blockSize.x * 0.5f;
        float y = originTopLeft.y - blockSize.y * 0.5f;

        float x = Random.Range(minX, maxX);
        Vector3 worldPos = new Vector3(x, y, 0f);

        var block = BlockFactory.Instance.CreateBlock(currentHp, Vector2Int.zero, worldPos);
        if (block != null && !activeBlocks.Contains(block))
            activeBlocks.Add(block);
    }

    public void HandleBlockDestroyed(BlockController block)
    {
        if (block == null)
            return;

        activeBlocks.Remove(block);
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
            SpawnBlock();

        if (spawnElapsed >= spawnDurationSeconds)
            spawnWindowActive = false;

        CheckClearCondition();
    }

    void CheckClearCondition()
    {
        if (spawnWindowActive)
            return;

        if (activeBlocks.Count > 0)
            return;

        PlayManager.Instance?.FinishPlay();
    }
}
