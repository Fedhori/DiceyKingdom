using System;

[Serializable]
public sealed class SaveData
{
    public SaveMeta meta = new();
    public string payloadJson = "{}";
}

[Serializable]
public sealed class SaveMeta
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;
    public string appVersion = string.Empty;
    public long timestampUtc;
    public string checksum = string.Empty;
}
