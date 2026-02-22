namespace Game.Infrastructure.Data
{
    public interface IGameDef
    {
        int schemaVersion { get; }
        string id { get; }
    }
}
