namespace Game.Infrastructure.Data
{
    public interface IGameDataSource
    {
        bool Exists(string relativePath);
        bool TryReadText(string relativePath, out string json, out string errorMessage);
    }
}
