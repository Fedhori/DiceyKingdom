using System.Collections.Generic;
using Data;
using UnityEngine;
using UnityEngine.Serialization;

public sealed class BlockManager : MonoBehaviour
{
    public static BlockManager Instance { get; private set; }

    [SerializeField] private Transform playArea;
    [SerializeField] private Vector2 blockSize = new Vector2(128f, 128f);
    private int currentHp;

    [Header("Spawn Ramp")] [SerializeField]
    private float spawnWindowSeconds = 30f;

    [SerializeField] private float spawnBudgetRateStart = 10f;
    [SerializeField] private float spawnBudgetRateEnd = 30f;

    private readonly List<BlockController> activeBlocks = new();
    private Vector2 originTopLeft;
    private Vector2 originTopRight;
    float spawnElapsedSeconds;
    double spawnBudget;
    bool hasPendingPattern;
    float pendingPatternSize;
    double pendingPatternCost;
    bool isSpawning;

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

        UpdateSpawnRamp();
    }

    public void BeginSpawnRamp()
    {
        var stage = StageManager.Instance?.CurrentStage;
        float duration = stage != null ? stage.SpawnSecond : spawnWindowSeconds;
        spawnWindowSeconds = Mathf.Max(1f, duration);

        isSpawning = true;
        spawnElapsedSeconds = 0f;
        spawnBudget = 0.0;
        hasPendingPattern = false;
        pendingPatternSize = 1f;
        pendingPatternCost = 0.0;
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

    void SpawnBlock(double blockHealth, float scale)
    {
        float halfWidth = blockSize.x * 0.5f * scale;
        float halfHeight = blockSize.y * 0.5f * scale;
        float minX = originTopLeft.x + halfWidth;
        float maxX = originTopRight.x - halfWidth;
        float y = originTopLeft.y + halfHeight;

        float x = minX <= maxX ? Random.Range(minX, maxX) : (originTopLeft.x + originTopRight.x) * 0.5f;
        Vector3 worldPos = new Vector3(x, y, 0f);

        var block = BlockFactory.Instance.CreateBlock(blockHealth, Vector2Int.zero, worldPos);
        if (block != null)
        {
            var currentScale = block.transform.localScale;
            block.transform.localScale = new Vector3(
                currentScale.x * scale,
                currentScale.y * scale,
                currentScale.z);

            if (!activeBlocks.Contains(block))
                activeBlocks.Add(block);
        }
    }

    public void HandleBlockDestroyed(BlockController block)
    {
        if (block == null)
            return;

        ItemManager.Instance?.TriggerAll(ItemTriggerType.OnBlockDestroyed);
        activeBlocks.Remove(block);
        CheckClearCondition();
    }

    public BlockController GetRandomActiveBlock()
    {
        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            if (activeBlocks[i] == null)
                activeBlocks.RemoveAt(i);
        }

        if (activeBlocks.Count == 0)
            return null;

        int index = Random.Range(0, activeBlocks.Count);
        return activeBlocks[index];
    }

    void UpdateSpawnRamp()
    {
        if (!isSpawning)
            return;

        float delta = Time.deltaTime;
        if (delta <= 0f)
            return;

        float remaining = spawnWindowSeconds - spawnElapsedSeconds;
        if (remaining <= 0f)
        {
            isSpawning = false;
            CheckClearCondition();
            return;
        }

        float effectiveDelta = Mathf.Min(delta, remaining);
        spawnElapsedSeconds += effectiveDelta;

        double budgetScale = StageManager.Instance?.CurrentStage?.SpawnBudgetScale ?? 0.0;
        if (budgetScale > 0.0)
        {
            AccumulateSpawnBudget(budgetScale, effectiveDelta);

            int safety = 0;
            while (TrySpawnPendingPattern(budgetScale))
            {
                safety++;
                if (safety >= 1000)
                {
                    spawnBudget = 0.0;
                    hasPendingPattern = false;
                    break;
                }
            }
        }

        if (spawnElapsedSeconds >= spawnWindowSeconds)
            isSpawning = false;

        CheckClearCondition();
    }

    bool TrySpawnPendingPattern(double budgetScale)
    {
        if (!hasPendingPattern)
            SelectPendingPattern(budgetScale);

        if (!hasPendingPattern || pendingPatternCost <= 0.0)
        {
            hasPendingPattern = false;
            return false;
        }

        if (spawnBudget < pendingPatternCost)
            return false;

        spawnBudget -= pendingPatternCost;
        SpawnBlock(pendingPatternCost, pendingPatternSize);
        hasPendingPattern = false;
        return true;
    }

    void SelectPendingPattern(double budgetScale)
    {
        if (budgetScale <= 0.0)
        {
            hasPendingPattern = false;
            pendingPatternCost = 0.0;
            pendingPatternSize = 1f;
            return;
        }

        bool isLarge = Random.value < 0.1f;
        pendingPatternSize = isLarge ? 2f : 1f;
        pendingPatternCost = budgetScale * pendingPatternSize * pendingPatternSize;
        hasPendingPattern = true;
    }

    void AccumulateSpawnBudget(double budgetScale, float deltaSeconds)
    {
        float t = Mathf.Clamp01(spawnElapsedSeconds / spawnWindowSeconds);
        double rateStart = budgetScale * spawnBudgetRateStart;
        double rateEnd = budgetScale * spawnBudgetRateEnd;
        double rate = rateStart + (rateEnd - rateStart) * t;

        double stageMultiplier = 1.0;
        var stage = StageManager.Instance?.CurrentStage;
        int totalStages = StageRepository.Count;
        if (stage != null && totalStages > 1)
        {
            double stageT = stage.StageIndex / (double)(totalStages - 1);
            stageMultiplier = 1.0 + 3.0 * stageT;
        }

        rate *= stageMultiplier;
        spawnBudget += rate * deltaSeconds;
    }

    void CheckClearCondition()
    {
        if (isSpawning)
            return;

        if (activeBlocks.Count > 0)
            return;

        PlayManager.Instance?.FinishPlay();
    }

    public bool TryGetPlayAreaBounds(out Bounds bounds)
    {
        bounds = default;

        if (playArea == null)
            return false;

        var sr = playArea.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            bounds = sr.bounds;
            return true;
        }

        var p = playArea.position;
        bounds = new Bounds(p, new Vector3(1000f, 1000f, 0f));
        return true;
    }

    public Vector3 GetRandomPositionInPlayArea()
    {
        if (!TryGetPlayAreaBounds(out var bounds))
            return Vector3.zero;

        float x = Random.Range(bounds.min.x, bounds.max.x);
        float y = Random.Range(bounds.min.y, bounds.max.y);
        return new Vector3(x, y, 0f);
    }

    public void ClearAllBlocks()
    {
        isSpawning = false;
        spawnElapsedSeconds = 0f;
        spawnBudget = 0.0;
        hasPendingPattern = false;
        pendingPatternSize = 1f;
        pendingPatternCost = 0.0;

        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            var block = activeBlocks[i];
            if (block == null)
                continue;

            Destroy(block.gameObject);
        }

        activeBlocks.Clear();
    }

    public BlockController GetLowestBlock(Vector3 fromPosition)
    {
        BlockController best = null;
        float bestY = float.PositiveInfinity;
        float bestDist = float.PositiveInfinity;
        const float yEpsilon = 0.01f;

        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            var block = activeBlocks[i];
            if (block == null)
            {
                activeBlocks.RemoveAt(i);
                continue;
            }

            float y = block.transform.position.y;
            if (y < bestY - yEpsilon)
            {
                bestY = y;
                bestDist = (block.transform.position - fromPosition).sqrMagnitude;
                best = block;
                continue;
            }

            if (Mathf.Abs(y - bestY) <= yEpsilon)
            {
                float dist = (block.transform.position - fromPosition).sqrMagnitude;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = block;
                }
            }
        }

        return best;
    }

    public int ApplyStatusToRandomBlocks(BlockStatusType type, int count, int stackAmount)
    {
        if (type == BlockStatusType.Unknown || count <= 0 || stackAmount <= 0)
            return 0;

        List<BlockController> candidates = null;

        for (int i = activeBlocks.Count - 1; i >= 0; i--)
        {
            var block = activeBlocks[i];
            if (block == null)
            {
                activeBlocks.RemoveAt(i);
                continue;
            }

            var inst = block.Instance;
            if (inst == null)
                continue;

            candidates ??= new List<BlockController>();
            candidates.Add(block);
        }

        if (candidates == null || candidates.Count == 0)
            return 0;

        int appliedCount = 0;
        for (int i = 0; i < count; i++)
        {
            var target = candidates[Random.Range(0, candidates.Count)];
            if (target == null)
                continue;

            bool applied = target.ApplyStatus(type, stackAmount);
            if (applied)
            {
                ItemManager.Instance?.TriggerAll(ItemTriggerType.OnBlockStatusApplied);
                appliedCount++;
            }
        }

        return appliedCount;
    }

    public int ApplyDamageToAllBlocks(float damageScale, ItemInstance sourceItem)
    {
        if (damageScale <= 0f)
            return 0;

        if (activeBlocks.Count == 0)
            return 0;

        var targets = new List<BlockController>(activeBlocks);
        int applied = 0;
        var damageManager = DamageManager.Instance;
        if (damageManager == null)
            return 0;

        for (int i = 0; i < targets.Count; i++)
        {
            var block = targets[i];
            if (block == null)
                continue;

            var context = new DamageContext(
                block,
                sourceItem: sourceItem,
                sourceType: DamageSourceType.ItemEffect,
                hitPosition: block.transform.position,
                applyStatusFromItem: true,
                damageScale: damageScale,
                allowZeroDamage: false);
            var result = damageManager.ApplyDamage(context);
            if (result != null && result.AppliedDamage > 0)
                applied++;
        }

        return applied;
    }

}
