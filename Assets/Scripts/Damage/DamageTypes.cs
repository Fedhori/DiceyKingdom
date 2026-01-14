using UnityEngine;

public enum DamageSourceType
{
    Unknown = 0,
    Projectile,
    ItemEffect,
    Overflow,
    Environment
}

public sealed class DamageContext
{
    public BlockController Target { get; }
    public ItemInstance SourceItem { get; }
    public DamageSourceType SourceType { get; }
    public Vector2? HitPosition { get; }
    public int? BaseDamage { get; }
    public float DamageScale { get; }
    public bool AllowOverflow { get; }
    public bool ApplyStatusFromItem { get; }
    public bool AllowZeroDamage { get; }
    public object SourceOwner { get; }

    public DamageContext(
        BlockController target,
        int? baseDamage = null,
        ItemInstance sourceItem = null,
        DamageSourceType sourceType = DamageSourceType.Unknown,
        Vector2? hitPosition = null,
        bool allowOverflow = true,
        bool applyStatusFromItem = true,
        object sourceOwner = null,
        float damageScale = 1f,
        bool allowZeroDamage = false)
    {
        Target = target;
        BaseDamage = baseDamage;
        SourceItem = sourceItem;
        SourceType = sourceType;
        HitPosition = hitPosition;
        AllowOverflow = allowOverflow;
        ApplyStatusFromItem = applyStatusFromItem;
        SourceOwner = sourceOwner;
        DamageScale = damageScale;
        AllowZeroDamage = allowZeroDamage;
    }
}

public sealed class DamageResult
{
    public int AppliedDamage { get; }
    public int OverflowDamage { get; }
    public bool IsDead { get; }
    public bool StatusApplied { get; }

    public DamageResult(int appliedDamage, int overflowDamage, bool isDead, bool statusApplied)
    {
        AppliedDamage = appliedDamage;
        OverflowDamage = overflowDamage;
        IsDead = isDead;
        StatusApplied = statusApplied;
    }
}
