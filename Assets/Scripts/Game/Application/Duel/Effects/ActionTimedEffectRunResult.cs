namespace Game.Application.Duel.Effects
{
    public readonly struct ActionTimedEffectRunResult
    {
        public int appliedCount { get; }
        public int failedCount { get; }
        public int skippedCount { get; }

        public ActionTimedEffectRunResult(int appliedCount, int failedCount, int skippedCount)
        {
            this.appliedCount = appliedCount;
            this.failedCount = failedCount;
            this.skippedCount = skippedCount;
        }
    }
}
