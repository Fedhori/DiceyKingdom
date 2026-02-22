namespace Game.Infrastructure.Data
{
    public sealed class GameDataValidationEntry
    {
        public string code = string.Empty;
        public string path = string.Empty;
        public string id = string.Empty;
        public string message = string.Empty;

        public string ToLogLine()
        {
            return $"{code}|{path}|{id}|{message}";
        }
    }
}
