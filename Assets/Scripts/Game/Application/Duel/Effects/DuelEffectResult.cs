namespace Game.Application.Duel.Effects
{
    public readonly struct DuelEffectResult
    {
        public bool isSuccess { get; }
        public DuelEffectFailureReason failureReason { get; }
        public string warningMessage { get; }

        DuelEffectResult(bool isSuccess, DuelEffectFailureReason failureReason, string warningMessage)
        {
            this.isSuccess = isSuccess;
            this.failureReason = failureReason;
            this.warningMessage = warningMessage;
        }

        public static DuelEffectResult Success()
        {
            return new DuelEffectResult(true, DuelEffectFailureReason.None, string.Empty);
        }

        public static DuelEffectResult Fail(DuelEffectFailureReason failureReason, string warningMessage)
        {
            return new DuelEffectResult(false, failureReason, warningMessage ?? string.Empty);
        }
    }
}
