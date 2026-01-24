using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BlockInstance
{
    readonly Dictionary<BlockStatusType, BlockStatusState> statuses = new();

    public double MaxHp { get; private set; }
    public double Hp { get; private set; }
    public float SpeedMultiplier { get; private set; }

    public Vector2Int GridPos { get; private set; }

    public IReadOnlyDictionary<BlockStatusType, BlockStatusState> Statuses => statuses;

    public BlockInstance(double hp, Vector2Int gridPos, float speedMultiplier = 1f)
    {
        MaxHp = Math.Max(1.0, hp);
        Hp = MaxHp;
        GridPos = gridPos;
        SpeedMultiplier = Mathf.Max(0f, speedMultiplier);
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
            return;

        Hp = Math.Max(0.0, Hp - amount);
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
        var status = GetStatus(type);
        return status != null && status.Stack > 0;
    }

    public bool TryApplyStatus(BlockStatusType type)
    {
        return AddStatusStack(type, 1);
    }

    public bool AddStatusStack(BlockStatusType type, int stackAmount)
    {
        if (type == BlockStatusType.Unknown || stackAmount <= 0)
            return false;

        if (statuses.TryGetValue(type, out var status))
        {
            status.AddStack(stackAmount);
            return true;
        }

        statuses[type] = new BlockStatusState(type, stackAmount);
        return true;
    }

    public void UpdateStatuses(float deltaTime)
    {
        if (deltaTime <= 0f || statuses.Count == 0)
            return;

        List<BlockStatusType> expired = null;

        foreach (var pair in statuses)
        {
            var status = pair.Value;
            if (status == null)
                continue;

            status.Update(deltaTime);
            if (status.IsExpired)
            {
                expired ??= new List<BlockStatusType>();
                expired.Add(pair.Key);
            }
        }

        if (expired == null)
            return;

        for (int i = 0; i < expired.Count; i++)
            statuses.Remove(expired[i]);
    }

    public void RemoveStatus(BlockStatusType type)
    {
        if (type == BlockStatusType.Unknown)
            return;

        statuses.Remove(type);
    }

    public bool IsDead => Hp <= 0.0;
}
