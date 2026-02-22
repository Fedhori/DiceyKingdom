namespace Game.Infrastructure.Data
{
    public sealed class GameDataLoadOptions
    {
        public string dataIndexPath = GameDataConstants.DefaultDataIndexPath;
        public GameDataBuildMode mode = GameDataBuildMode.Development;
    }
}
