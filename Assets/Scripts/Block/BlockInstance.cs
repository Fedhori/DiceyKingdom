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

    public void SetGridPos(Vector2Int pos)
    {
        GridPos = pos;
    }

    public void ApplyDamage(int amount)
    {
        if (amount <= 0)
            return;

        Hp = Mathf.Max(0, Hp - amount);
    }

    public bool IsDead => Hp <= 0;
}
