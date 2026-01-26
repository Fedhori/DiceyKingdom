using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public static class SaveLogger
{
    public static string LogFilePath =>
        System.IO.Path.Combine(Application.persistentDataPath, "saves", "save_log.txt");

    public static void LogInfo(string message)
    {
        Write("INFO", message);
    }

    public static void LogWarning(string message)
    {
        Write("WARN", message);
    }

    public static void LogError(string message)
    {
        Write("ERROR", message);
    }

    public static void LogValidationErrors(IReadOnlyList<SaveValidationError> errors, string context)
    {
        if (errors == null || errors.Count == 0)
            return;

        var sb = new StringBuilder();
        sb.AppendLine($"[{TimestampUtc()}] [{context}] validation_errors={errors.Count}");
        for (int i = 0; i < errors.Count; i++)
        {
            var e = errors[i];
            sb.AppendLine($"- fieldPath='{e.FieldPath}', expected='{e.Expected}', actual='{e.Actual}', code='{e.Code}'");
        }

        WriteRaw(sb.ToString());
    }

    static void Write(string level, string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        string line = $"[{TimestampUtc()}] [{level}] {message}";
        WriteRaw(line);
    }

    static void WriteRaw(string message)
    {
        try
        {
            if (string.IsNullOrEmpty(message))
                return;

            if (!message.EndsWith(Environment.NewLine, StringComparison.Ordinal))
                message += Environment.NewLine;

            Debug.Log(message.TrimEnd());
            string dir = System.IO.Path.GetDirectoryName(LogFilePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.AppendAllText(LogFilePath, message);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveLogger] Failed to write log: {ex}");
        }
    }

    static string TimestampUtc()
    {
        return DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
    }
}
