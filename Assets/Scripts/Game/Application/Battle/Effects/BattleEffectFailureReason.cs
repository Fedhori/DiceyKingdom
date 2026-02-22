namespace Game.Application.Battle.Effects
{
    public enum BattleEffectFailureReason
    {
        None = 0,
        BattleEnded = 1,
        UnsupportedOpCode = 2,
        MissingField = 3,
        InvalidTarget = 4,
        InvalidIndex = 5,
        SlotLimitExceeded = 6,
        MissingOutcomeContext = 7
    }
}
