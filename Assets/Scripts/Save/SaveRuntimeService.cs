using System;
using UnityEngine;




public sealed class SaveRuntimeService : MonoBehaviour
{
    public bool Save(string payloadJson)
    {
        var data = new SaveData
        {
            payloadJson = string.IsNullOrEmpty(payloadJson) ? "{}" : payloadJson
        };

        var result = SaveService.WriteSave(data);
        if (result.IsSuccess)
            return true;

        SaveLogger.LogError($"Save failed: {result.Message}");
        return false;
    }

    public bool TryLoad(out string payloadJson)
    {
        payloadJson = string.Empty;

        var data = SaveService.ReadSave(out var result);
        if (data == null)
        {
            SaveLogger.LogWarning($"Load failed: {result.Message}");
            return false;
        }

        payloadJson = data.payloadJson ?? string.Empty;
        return true;
    }
}


