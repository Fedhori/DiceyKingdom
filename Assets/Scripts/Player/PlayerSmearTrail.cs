using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerSmearTrail : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sourceRenderer;
    [SerializeField] private Transform poolRoot;
    [SerializeField] private float spawnIntervalSeconds = 0.05f;
    [SerializeField] private float lifetimeSeconds = 0.15f;
    [SerializeField] private float minSpeed = 20f;
    [SerializeField] private float offsetDistance = 0.1f;
    [SerializeField] private float startAlpha = 0.5f;
    [SerializeField] private int poolSize = 16;
    
    readonly List<SmearSprite> pool = new();
    int poolCursor;
    float spawnTimer;
    Vector3 lastPosition;
    bool hasLastPosition;

    void Awake()
    {
        if (sourceRenderer == null)
            sourceRenderer = GetComponentInChildren<SpriteRenderer>();

        EnsurePool();
    }

    void OnEnable()
    {
        hasLastPosition = false;
        spawnTimer = 0f;
    }

    void Update()
    {
        if (sourceRenderer == null || sourceRenderer.sprite == null || !sourceRenderer.enabled)
            return;

        float dt = Time.deltaTime;
        if (dt <= 0f)
            return;

        Vector3 pos = transform.position;
        if (!hasLastPosition)
        {
            lastPosition = pos;
            hasLastPosition = true;
            return;
        }

        Vector3 delta = pos - lastPosition;
        lastPosition = pos;

        float speed = delta.magnitude / dt;
        if (speed < minSpeed)
        {
            spawnTimer = 0f;
            return;
        }

        spawnTimer += dt;
        if (spawnTimer < spawnIntervalSeconds)
            return;
        spawnTimer = 0f;

        Vector3 dir = delta.normalized;
        Vector3 spawnPos = pos - dir * offsetDistance;

        SpawnSmear(spawnPos);
    }

    void EnsurePool()
    {
        if (poolRoot == null)
        {
            var root = new GameObject("PlayerSmearPool");
            poolRoot = root.transform;
            poolRoot.SetParent(transform.parent, worldPositionStays: true);
        }

        while (pool.Count < poolSize)
        {
            var go = new GameObject("Smear");
            go.SetActive(false);
            go.transform.SetParent(poolRoot, worldPositionStays: true);
            var renderer = go.AddComponent<SpriteRenderer>();
            var smear = go.AddComponent<SmearSprite>();
            pool.Add(smear);
        }
    }

    void SpawnSmear(Vector3 position)
    {
        if (pool.Count == 0)
            return;

        var smear = pool[poolCursor];
        poolCursor = (poolCursor + 1) % pool.Count;
        if (smear == null)
            return;

        var target = smear.transform;
        target.position = position;
        target.rotation = sourceRenderer.transform.rotation;
        target.localScale = sourceRenderer.transform.localScale;

        int sortingLayerId = sourceRenderer.sortingLayerID;
        int sortingOrder = sourceRenderer.sortingOrder - 1;
        Vector2 size = ResolveLocalSize(sourceRenderer);

        smear.Initialize(
            sourceRenderer.sprite,
            sourceRenderer.color,
            sortingLayerId,
            sortingOrder,
            lifetimeSeconds,
            startAlpha,
            size);
    }

    static Vector2 ResolveLocalSize(SpriteRenderer renderer)
    {
        if (renderer == null || renderer.sprite == null)
            return Vector2.one;

        if (renderer.drawMode == SpriteDrawMode.Simple)
            return renderer.sprite.bounds.size;

        return renderer.size;
    }
}
