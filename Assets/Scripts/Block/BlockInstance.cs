using UnityEngine;

public sealed class BlockInstance
{
    public int MaxHp { get; private set; }
    public int Hp { get; private set; }

    public Vector2Int GridPos { get; private set; }

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
