namespace Game.Infrastructure.Data
{
    public static class GameDataErrorCode
    {
        public const string DuplicateId = "E001";
        public const string MissingFile = "E002";
        public const string ParseError = "E003";
        public const string MissingReference = "E004";
        public const string InvalidValue = "E005";
        public const string UnsupportedOpCode = "E006";
        public const string InvalidEnum = "E007";
        public const string InvalidSchemaVersion = "E008";
        public const string MissingRequiredConfig = "E009";
        public const string InvalidIndex = "E010";
    }
}
