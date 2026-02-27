namespace Game.Presentation.Localization
{
    public interface ILocalizedTextResolver
    {
        string Resolve(string tableName, string key, object arguments = null);
    }
}
