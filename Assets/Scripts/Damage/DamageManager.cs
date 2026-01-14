using Data;
using UnityEngine;

public sealed class DamageManager : MonoBehaviour
{
    public static DamageManager Instance { get; private set; }

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

        var blockManager = BlockManager.Instance;
        if (blockManager == null)
            return 0;

        var targets = blockManager.GetActiveBlocksSnapshot();
        if (targets.Count == 0)
            return 0;

        float sqrRadius = radius * radius;
        int applied = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            var block = targets[i];
            if (block == null)
                continue;

            Vector2 pos = block.transform.position;
            if ((pos - center).sqrMagnitude > sqrRadius)
                continue;

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
