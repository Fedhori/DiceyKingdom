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
            BlockManager.Instance?.HandleBlockDestroyed(context.Target);
            Destroy(context.Target.gameObject);

            if (context.AllowOverflow && result.OverflowDamage > 0)
                TryApplyOverflow(context, result.OverflowDamage);
        }

        return result;
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
