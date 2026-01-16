using UnityEngine;

public enum BlockStatusType
{
    Unknown = 0,
    Freeze
}

public sealed class BlockStatusState
{
    public BlockStatusType Type { get; }
    public int Stack { get; private set; }
    float decayTimer;

    public BlockStatusState(BlockStatusType type, int initialStack)
    {
        Type = type;
        Stack = Mathf.Max(0, initialStack);
        decayTimer = 0f;
    }

    public void AddStack(int amount)
    {
        if (amount <= 0)
            return;

        Stack += amount;
    }

    public void Update(float deltaTime)
    {
        if (deltaTime <= 0f || Stack <= 0)
            return;

        decayTimer += deltaTime;
        while (decayTimer >= 1f && Stack > 0)
        {
            Stack -= 1;
            decayTimer -= 1f;
        }
    }

    public bool IsExpired => Stack <= 0;
}
