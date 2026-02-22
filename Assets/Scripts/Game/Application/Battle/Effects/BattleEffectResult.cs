namespace Game.Application.Battle.Effects
{
    public readonly struct BattleEffectResult
    {
        public bool isSuccess { get; }
        public BattleEffectFailureReason failureReason { get; }
        public string warningMessage { get; }

        BattleEffectResult(bool isSuccess, BattleEffectFailureReason failureReason, string warningMessage)
        {
            this.isSuccess = isSuccess;
            this.failureReason = failureReason;
            this.warningMessage = warningMessage;
        }

        public static BattleEffectResult Success()
        {
            return new BattleEffectResult(true, BattleEffectFailureReason.None, string.Empty);
        }

        public static BattleEffectResult Fail(BattleEffectFailureReason failureReason, string warningMessage)
        {
            return new BattleEffectResult(false, failureReason, warningMessage ?? string.Empty);
        }
    }
}
