namespace Game.Application.Duel.Effects
{
    public enum DuelEffectFailureReason
    {
        None = 0,
        DuelEnded = 1,
        UnsupportedOpCode = 2,
        MissingField = 3,
        InvalidTarget = 4,
        InvalidIndex = 5,
        SlotLimitExceeded = 6,
        MissingOutcomeContext = 7
    }
}
