namespace Game.Application.Duel.Effects
{
    public readonly struct AbilityTimedEffectRunResult
    {
        public int appliedCount { get; }
        public int failedCount { get; }
        public int skippedCount { get; }

        public AbilityTimedEffectRunResult(int appliedCount, int failedCount, int skippedCount)
        {
            this.appliedCount = appliedCount;
            this.failedCount = failedCount;
            this.skippedCount = skippedCount;
        }
    }
}
