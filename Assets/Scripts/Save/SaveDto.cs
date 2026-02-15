using System;

[Serializable]
/// <summary>
/// Data model for save values.
/// </summary>
public sealed class SaveData
{
    public SaveMeta meta = new();
    public string payloadJson = "{}";
}

[Serializable]
/// <summary>
/// Core class that defines save meta responsibilities.
/// </summary>
public sealed class SaveMeta
{
    public const int CurrentSchemaVersion = 1;

    public int schemaVersion = CurrentSchemaVersion;
    public string appVersion = string.Empty;
    public long timestampUtc;
    public string checksum = string.Empty;
}

