public enum BlockStatusType
{
    Unknown = 0,
    Freeze
}

public sealed class BlockStatusState
{
    public BlockStatusType Type { get; }

    public BlockStatusState(BlockStatusType type)
    {
        Type = type;
    }
}
