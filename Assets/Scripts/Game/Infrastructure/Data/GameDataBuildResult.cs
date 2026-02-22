namespace Game.Infrastructure.Data
{
    public sealed class GameDataBuildResult
    {
        public bool isSuccess;
        public bool shouldBlockStartup;
        public GameDatabase database = new();
        public GameDataValidationReport report = new();
    }
}
