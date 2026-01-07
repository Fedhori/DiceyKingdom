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

    public void TickStatuses(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || statuses.Count == 0)
            return;

        List<BlockStatusType> toRemove = null;

        foreach (var pair in statuses)
        {
            var status = pair.Value;
            if (status == null || !status.UsesDuration)
                continue;

            float next = status.RemainingSeconds - deltaSeconds;
            if (next <= 0f)
            {
                toRemove ??= new List<BlockStatusType>();
                toRemove.Add(pair.Key);
            }
            else
            {
                status.SetRemainingSeconds(next);
            }
        }

        if (toRemove == null)
            return;

        for (int i = 0; i < toRemove.Count; i++)
            statuses.Remove(toRemove[i]);
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

    public bool TryApplyStatus(BlockStatusType type, float durationSeconds)
    {
        if (type == BlockStatusType.Unknown)
            return false;

        bool usesDuration = durationSeconds > 0f;
        if (statuses.TryGetValue(type, out var existing))
        {
            if (existing.UsesDuration && usesDuration)
            {
                if (durationSeconds > existing.RemainingSeconds)
                    existing.SetRemainingSeconds(durationSeconds);
            }

            return false;
        }

        statuses[type] = new BlockStatusState(type, durationSeconds, usesDuration);
        return true;
    }

    public bool TryUpdateStatusDuration(BlockStatusType type, float durationSeconds)
    {
        if (type == BlockStatusType.Unknown)
            return false;

        if (!statuses.TryGetValue(type, out var existing))
            return false;

        if (!existing.UsesDuration)
            return false;

        if (durationSeconds <= existing.RemainingSeconds)
            return false;

        existing.SetRemainingSeconds(durationSeconds);
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
