namespace Game.Presentation.Localization
{
    public interface ILocalizedTextResolver
    {
        string ResolveRequired(string tableName, string key, object arguments = null);
        string ResolveOptional(string tableName, string key, object arguments = null, bool warnIfMissing = false);
    }
}
