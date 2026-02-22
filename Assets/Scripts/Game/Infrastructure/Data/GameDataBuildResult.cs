namespace Game.Infrastructure.Data
{
    public sealed class GameDataBuildResult
    {
        public bool isSuccess;
        public bool shouldBlockStartup;
        public GameDatabase database;
        public GameDataValidationReport report;
    }
}
