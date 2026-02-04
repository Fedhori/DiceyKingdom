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
            return new DamageResult(0, false, false, 0);

        var result = context.Target.ApplyDamage(context);
        if (result.AppliedDamage <= 0)
            return result;

        if (context.SourceItem != null)
            DamageTrackingManager.Instance?.RecordDamage(context.SourceItem, result.AppliedDamage);

        context.Target.PlayHitFlash();

        Vector2 pos = context.HitPosition ?? (Vector2)context.Target.transform.position;
        DamageTextManager.Instance?.ShowDamageText(result.AppliedDamage, result.CriticalLevel, pos);

        if (result.StatusApplied)
            ItemManager.Instance?.TriggerAll(ItemTriggerType.OnBlockStatusApplied);

        if (result.CriticalLevel > 0)
            ItemManager.Instance?.TriggerAll(ItemTriggerType.OnCriticalHit);

        if (result.IsDead)
        {
            if (context.SourceItem != null)
                ItemManager.Instance?.TriggerItem(context.SourceItem, ItemTriggerType.OnBlockDestroyedByItem);

            AudioManager.Instance?.Play("Pop");
            float scale = Mathf.Max(0.01f, context.Target.transform.lossyScale.x);
            ParticleManager.Instance?.PlayBlockDestroy(context.Target.transform.position, scale);
            BlockManager.Instance?.HandleBlockDestroyed(context.Target);
            Destroy(context.Target.gameObject);
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
                sourceItem: sourceItem,
                sourceType: sourceType,
                hitPosition: pos,
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

}
