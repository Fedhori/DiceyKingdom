namespace Game.Application.Battle
{
    public enum BattlePhaseFailureReason
    {
        None = 0,
        NotStarted = 1,
        InvalidPhase = 2,
        StabilityInsufficient = 3,
        AlreadyEnded = 4
    }
}
