using System.Collections.Generic;
using UnityEngine;

public sealed class BlockInstance
{
    readonly Dictionary<BlockStatusType, BlockStatusState> statuses = new();

    public int MaxHp { get; private set; }
    public int Hp { get; private set; }

    public Vector2Int GridPos { get; private set; }

    public IReadOnlyDictionary<BlockStatusType, BlockStatusState> Statuses => statuses;

    public BlockInstance(int hp, Vector2Int gridPos)
    {
        MaxHp = Mathf.Max(1, hp);
        Hp = MaxHp;
        GridPos = gridPos;
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
            return;

        Hp = Mathf.Max(0, Hp - amount);
    }

    public BlockStatusState GetStatus(BlockStatusType type)
    {
        if (type == BlockStatusType.Unknown)
            return null;

        statuses.TryGetValue(type, out var status);
        return status;
    }

    public bool HasStatus(BlockStatusType type)
    {
        return GetStatus(type) != null;
    }

    public bool TryApplyStatus(BlockStatusType type)
    {
        if (type == BlockStatusType.Unknown)
            return false;

        if (statuses.TryGetValue(type, out var _))
            return false;

        statuses[type] = new BlockStatusState(type);
        return true;
    }

    public void RemoveStatus(BlockStatusType type)
    {
        if (type == BlockStatusType.Unknown)
            return;

        statuses.Remove(type);
    }

    public bool IsDead => Hp <= 0;
}
