using UnityEngine;

public enum BlockStatusType
{
    Unknown = 0,
    Freeze
}

public static class BlockStatusSettings
{
    public static float GetDecayPerSecond(BlockStatusType type)
    {
        switch (type)
        {
            case BlockStatusType.Freeze:
                return 1f;
            default:
                return 0f;
        }
    }
}

public sealed class BlockStatusState
{
    public BlockStatusType Type { get; }
    public float Stack { get; private set; }
    public float DecayPerSecond { get; }

    public BlockStatusState(BlockStatusType type, float initialStack, float decayPerSecond)
    {
        Type = type;
        Stack = Mathf.Max(0f, initialStack);
        DecayPerSecond = Mathf.Max(0f, decayPerSecond);
    }

    public void AddStack(float amount)
    {
        if (amount <= 0f)
            return;

        Stack += amount;
    }

    public void Update(float deltaTime)
    {
        if (deltaTime <= 0f || DecayPerSecond <= 0f || Stack <= 0f)
            return;

        Stack = Mathf.Max(0f, Stack - DecayPerSecond * deltaTime);
    }

    public bool IsExpired => Stack <= 0f;
}
