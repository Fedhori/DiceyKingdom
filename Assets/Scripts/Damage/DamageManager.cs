using System.Collections.Generic;
using Data;
using UnityEngine;

public sealed class DamageManager : MonoBehaviour
{
    public static DamageManager Instance { get; private set; }

    readonly List<Collider2D> areaOverlapResults = new();
    readonly HashSet<BlockController> areaTargets = new();
    ContactFilter2D areaFilter;
    bool areaFilterInitialized;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public DamageResult ApplyDamage(DamageContext context)
    {
        if (context == null || context.Target == null || context.Target.Instance == null)
            return new DamageResult(0, 0, false, false);

        var result = context.Target.ApplyDamage(context);
        if (result.AppliedDamage <= 0)
            return result;

        if (context.SourceItem != null)
            DamageTrackingManager.Instance?.RecordDamage(context.SourceItem, result.AppliedDamage);

        context.Target.PlayHitFlash();

        Vector2 pos = context.HitPosition ?? (Vector2)context.Target.transform.position;
        DamageTextManager.Instance?.ShowDamageText(result.AppliedDamage, 0, pos);

        if (result.StatusApplied)
            ItemManager.Instance?.TriggerAll(ItemTriggerType.OnBlockStatusApplied);

        if (result.IsDead)
        {
            AudioManager.Instance?.Play("Pop");
            ParticleManager.Instance?.PlayBlockDestroy(context.Target.transform.position);
            BlockManager.Instance?.HandleBlockDestroyed(context.Target);
            Destroy(context.Target.gameObject);

            if (context.AllowOverflow && result.OverflowDamage > 0)
                TryApplyOverflow(context, result.OverflowDamage);
        }

        return result;
    }

    public int ApplyAreaDamage(
        Vector2 center,
        float radius,
        ItemInstance sourceItem,
        DamageSourceType sourceType,
        float damageScale = 1f,
        object sourceOwner = null)
    {
        if (radius <= 0f)
            return 0;

        EnsureAreaFilter();
        if (!areaFilterInitialized)
            return 0;

        areaOverlapResults.Clear();
        Physics2D.OverlapCircle(center, radius, areaFilter, areaOverlapResults);
        if (areaOverlapResults.Count == 0)
            return 0;

        int applied = 0;
        areaTargets.Clear();

        for (int i = 0; i < areaOverlapResults.Count; i++)
        {
            var hit = areaOverlapResults[i];
            if (hit == null)
                continue;

            var block = hit.GetComponent<BlockController>();
            if (block == null || block.Instance == null)
                continue;

            if (!areaTargets.Add(block))
                continue;

            Vector2 pos = block.transform.position;

            var context = new DamageContext(
                block,
                baseDamage: null,
                sourceItem: sourceItem,
                sourceType: sourceType,
                hitPosition: pos,
                allowOverflow: true,
                applyStatusFromItem: true,
                sourceOwner: sourceOwner,
                damageScale: damageScale,
                allowZeroDamage: false);

            var result = ApplyDamage(context);
            if (result != null && result.AppliedDamage > 0)
                applied++;
        }

        return applied;
    }

    void EnsureAreaFilter()
    {
        if (areaFilterInitialized)
            return;

        int blockLayer = LayerMask.NameToLayer("Block");
        if (blockLayer < 0)
        {
            Debug.LogWarning("[DamageManager] Block layer not found.");
            return;
        }

        areaFilter = new ContactFilter2D
        {
            useLayerMask = true,
            layerMask = 1 << blockLayer,
            useTriggers = true
        };

        areaFilterInitialized = true;
    }

    void TryApplyOverflow(DamageContext sourceContext, int overflowDamage)
    {
        if (overflowDamage <= 0)
            return;

        var player = PlayerManager.Instance?.Current;
        if (player == null || !player.IsOverflowDamageEnabled)
            return;

        var blockManager = BlockManager.Instance;
        if (blockManager == null)
            return;

        var target = blockManager.GetRandomActiveBlock();
        if (target == null)
            return;

        var context = new DamageContext(
            target,
            overflowDamage,
            sourceContext.SourceItem,
            DamageSourceType.Overflow,
            target.transform.position,
            allowOverflow: true,
            applyStatusFromItem: false,
            sourceOwner: sourceContext.SourceOwner);

        ApplyDamage(context);
    }
}
