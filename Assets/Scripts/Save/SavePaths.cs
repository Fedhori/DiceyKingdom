using System.IO;
using UnityEngine;

public static class SavePaths
{
    public const string SaveDirectoryName = "saves";
    public const string SaveFileName = "save.json";
    public const string BackupFileName = "save_backup.json";
    public const string InvalidFileName = "save_invalid.json";
    public const string TempFileName = "save.tmp";

    public static string SaveDirectoryPath =>
        System.IO.Path.Combine(Application.persistentDataPath, SaveDirectoryName);

    public static string SaveFilePath => Path.Combine(SaveDirectoryPath, SaveFileName);
    public static string BackupFilePath => Path.Combine(SaveDirectoryPath, BackupFileName);
    public static string InvalidFilePath => Path.Combine(SaveDirectoryPath, InvalidFileName);
    public static string TempFilePath => Path.Combine(SaveDirectoryPath, TempFileName);

    public static void EnsureDirectory()
    {
        if (!Directory.Exists(SaveDirectoryPath))
            Directory.CreateDirectory(SaveDirectoryPath);
    }
}
