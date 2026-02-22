namespace Game.Application.Battle.Effects
{
    public readonly struct TroopTimedEffectRunResult
    {
        public int appliedCount { get; }
        public int failedCount { get; }
        public int skippedCount { get; }

        public TroopTimedEffectRunResult(int appliedCount, int failedCount, int skippedCount)
        {
            this.appliedCount = appliedCount;
            this.failedCount = failedCount;
            this.skippedCount = skippedCount;
        }
    }
}
