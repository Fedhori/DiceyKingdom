using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public sealed class SaveServiceResult
{
    public bool IsSuccess { get; private set; }
    public string Message { get; private set; }

    public static SaveServiceResult Success() => new SaveServiceResult { IsSuccess = true };
    public static SaveServiceResult Fail(string message) => new SaveServiceResult { IsSuccess = false, Message = message ?? string.Empty };
}

public static class SaveService
{
    public static SaveServiceResult WriteSave(SaveData data)
    {
        if (data == null)
            return SaveServiceResult.Fail("SaveData is null.");

        SavePaths.EnsureDirectory();

        data.meta ??= new SaveMeta();
        data.meta.schemaVersion = SaveMeta.CurrentSchemaVersion;
        data.meta.appVersion = Application.version;
        data.meta.timestampUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        data.meta.checksum = string.Empty;

        string payload = SaveJson.SerializeForChecksum(data);
        data.meta.checksum = SaveJson.ComputeChecksum(payload);
        string json = SaveJson.Serialize(data);

        try
        {
            File.WriteAllText(SavePaths.TempFilePath, json);
            if (!ValidateTempFile(SavePaths.TempFilePath, out var validationMessage))
                return SaveServiceResult.Fail(validationMessage);

            if (File.Exists(SavePaths.SaveFilePath))
            {
                File.Copy(SavePaths.SaveFilePath, SavePaths.BackupFilePath, overwrite: true);
            }

            File.Copy(SavePaths.TempFilePath, SavePaths.SaveFilePath, overwrite: true);
            File.Delete(SavePaths.TempFilePath);
            SaveWebGlSync.SyncToPersistent();
            return SaveServiceResult.Success();
        }
        catch (Exception ex)
        {
            return SaveServiceResult.Fail($"Save write failed: {ex}");
        }
    }

    public static SaveData ReadSave(out SaveServiceResult result)
    {
        result = SaveServiceResult.Success();

        if (!File.Exists(SavePaths.SaveFilePath))
        {
            result = SaveServiceResult.Fail("Save file does not exist.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePaths.SaveFilePath);
            var data = JsonConvert.DeserializeObject<SaveData>(json);
            if (!VerifyChecksum(data))
            {
                result = SaveServiceResult.Fail("Checksum mismatch.");
                return null;
            }

            var validation = SaveValidator.Validate(data);
            if (!validation.IsValid)
            {
                SaveLogger.LogValidationErrors(validation.Errors, "read_save");
                result = SaveServiceResult.Fail($"Save validation failed. errors={validation.Errors.Count}");
                return null;
            }
            return data;
        }
        catch (Exception ex)
        {
            result = SaveServiceResult.Fail($"Save read failed: {ex}");
            return null;
        }
    }

    public static SaveData ReadSaveWithBackup(out SaveServiceResult result, out bool usedBackup)
    {
        usedBackup = false;
        var data = ReadSave(out result);
        if (data != null)
            return data;

        MoveInvalidToQuarantine();

        var backup = ReadBackup(out var backupResult);
        if (backup == null)
        {
            result = backupResult;
            return null;
        }

        usedBackup = true;
        result = SaveServiceResult.Success();
        return backup;
    }

    public static SaveServiceResult DeleteAllSaves()
    {
        try
        {
            if (File.Exists(SavePaths.SaveFilePath))
                File.Delete(SavePaths.SaveFilePath);
            if (File.Exists(SavePaths.BackupFilePath))
                File.Delete(SavePaths.BackupFilePath);
            if (File.Exists(SavePaths.InvalidFilePath))
                File.Delete(SavePaths.InvalidFilePath);
            if (File.Exists(SavePaths.TempFilePath))
                File.Delete(SavePaths.TempFilePath);
            SaveWebGlSync.SyncToPersistent();
            return SaveServiceResult.Success();
        }
        catch (Exception ex)
        {
            return SaveServiceResult.Fail($"Delete saves failed: {ex}");
        }
    }

    public static bool HasValidSave()
    {
        SavePaths.EnsureDirectory();

        if (TryValidateFile(SavePaths.SaveFilePath, "has_valid_save") != null)
            return true;

        if (TryValidateFile(SavePaths.BackupFilePath, "has_valid_backup") != null)
            return true;

        return false;
    }

    public static bool VerifyChecksum(SaveData data)
    {
        if (data == null || data.meta == null)
            return false;

        string expected = data.meta.checksum ?? string.Empty;
        data.meta.checksum = string.Empty;
        string payload = SaveJson.SerializeForChecksum(data);
        string actual = SaveJson.ComputeChecksum(payload);
        data.meta.checksum = expected;
        return string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase);
    }

    static bool ValidateTempFile(string path, out string message)
    {
        message = string.Empty;
        try
        {
            string json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<SaveData>(json);
            if (data == null)
            {
                message = "Temp save deserialize returned null.";
                return false;
            }

            if (!VerifyChecksum(data))
            {
                message = "Temp save checksum mismatch.";
                return false;
            }

            var validation = SaveValidator.Validate(data);
            if (!validation.IsValid)
            {
                message = $"Temp save validation failed. errors={validation.Errors.Count}";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            message = $"Temp save validation exception: {ex}";
            return false;
        }
    }

    static SaveData ReadBackup(out SaveServiceResult result)
    {
        result = SaveServiceResult.Success();

        if (!File.Exists(SavePaths.BackupFilePath))
        {
            result = SaveServiceResult.Fail("Backup file does not exist.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePaths.BackupFilePath);
            var data = JsonConvert.DeserializeObject<SaveData>(json);
            if (!VerifyChecksum(data))
            {
                result = SaveServiceResult.Fail("Backup checksum mismatch.");
                return null;
            }

            var validation = SaveValidator.Validate(data);
            if (!validation.IsValid)
            {
                SaveLogger.LogValidationErrors(validation.Errors, "read_backup");
                result = SaveServiceResult.Fail($"Backup validation failed. errors={validation.Errors.Count}");
                return null;
            }
            return data;
        }
        catch (Exception ex)
        {
            result = SaveServiceResult.Fail($"Backup read failed: {ex}");
            return null;
        }
    }

    static void MoveInvalidToQuarantine()
    {
        try
        {
            if (!File.Exists(SavePaths.SaveFilePath))
                return;

            SavePaths.EnsureDirectory();
            File.Copy(SavePaths.SaveFilePath, SavePaths.InvalidFilePath, overwrite: true);
            File.Delete(SavePaths.SaveFilePath);
        }
        catch (Exception ex)
        {
            SaveLogger.LogWarning($"Move invalid save failed: {ex}");
        }
    }

    static SaveData TryValidateFile(string path, string context)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<SaveData>(json);
            if (!VerifyChecksum(data))
                return null;

            var validation = SaveValidator.Validate(data);
            if (!validation.IsValid)
            {
                SaveLogger.LogValidationErrors(validation.Errors, context);
                return null;
            }

            return data;
        }
        catch (Exception ex)
        {
            SaveLogger.LogWarning($"{context} failed: {ex}");
            return null;
        }
    }
}
