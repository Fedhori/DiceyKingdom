using UnityEngine;

public enum DamageSourceType
{
    Unknown = 0,
    Projectile,
    ItemEffect,
    Environment
}

public sealed class DamageContext
{
    public BlockController Target { get; }
    public ItemInstance SourceItem { get; }
    public DamageSourceType SourceType { get; }
    public Vector2? HitPosition { get; }
    public float DamageScale { get; }
    public bool ApplyStatusFromItem { get; }
    public bool AllowZeroDamage { get; }
    public object SourceOwner { get; }

    public DamageContext(
        BlockController target,
        ItemInstance sourceItem = null,
        DamageSourceType sourceType = DamageSourceType.Unknown,
        Vector2? hitPosition = null,
        bool applyStatusFromItem = true,
        object sourceOwner = null,
        float damageScale = 1f,
        bool allowZeroDamage = false)
    {
        Target = target;
        SourceItem = sourceItem;
        SourceType = sourceType;
        HitPosition = hitPosition;
        ApplyStatusFromItem = applyStatusFromItem;
        SourceOwner = sourceOwner;
        DamageScale = damageScale;
        AllowZeroDamage = allowZeroDamage;
    }
}

public sealed class DamageResult
{
    public int AppliedDamage { get; }
    public bool IsDead { get; }
    public bool StatusApplied { get; }
    public int CriticalLevel { get; }

    public DamageResult(int appliedDamage, bool isDead, bool statusApplied, int criticalLevel)
    {
        AppliedDamage = appliedDamage;
        IsDead = isDead;
        StatusApplied = statusApplied;
        CriticalLevel = criticalLevel;
    }
}
