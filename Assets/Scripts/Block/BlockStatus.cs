using UnityEngine;

public enum BlockStatusType
{
    Unknown = 0,
    Freeze
}

public sealed class BlockStatusState
{
    public BlockStatusType Type { get; }
    public float RemainingSeconds { get; private set; }
    public bool UsesDuration { get; }

    public BlockStatusState(BlockStatusType type, float remainingSeconds, bool usesDuration)
    {
        Type = type;
        UsesDuration = usesDuration;
        RemainingSeconds = usesDuration ? Mathf.Max(0f, remainingSeconds) : 0f;
    }

    public void SetRemainingSeconds(float remainingSeconds)
    {
        if (!UsesDuration)
            return;

        RemainingSeconds = Mathf.Max(0f, remainingSeconds);
    }
}
