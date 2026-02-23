namespace Game.Application.Duel
{
    public enum DuelPhaseFailureReason
    {
        None = 0,
        NotStarted = 1,
        InvalidPhase = 2,
        HonorInsufficient = 3,
        AlreadyEnded = 4
    }
}
