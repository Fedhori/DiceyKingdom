using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class GameConfigProvider
{
    public const string RelativePath = "Data/GameConfig.json";

    static GameConfigData current;

    public static bool IsLoaded => current != null;

    public static GameConfigData Current
    {
        get
        {
            if (current == null)
            {
                Debug.LogError("[GameConfigProvider] Game config not loaded. Returning empty config.");
                current = new GameConfigData();
            }

            return current;
        }
    }

    public static async Task<bool> LoadFromStreamingAssetsAsync()
    {
        try
        {
            string json = await ReadStreamingAssetTextAsync(RelativePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"[GameConfigProvider] Empty config json: {RelativePath}");
                return false;
            }

            var parsed = JsonUtility.FromJson<GameConfigData>(json);
            if (parsed == null)
            {
                Debug.LogError($"[GameConfigProvider] Failed to parse config json: {RelativePath}");
                return false;
            }

            current = parsed;
            Debug.Log("[GameConfigProvider] Loaded GameConfig.json");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[GameConfigProvider] Load failed: {RelativePath}\n{ex}");
            return false;
        }
    }

    static async Task<string> ReadStreamingAssetTextAsync(string relativePath)
    {
        string sourcePath = Path.Combine(Application.streamingAssetsPath, relativePath).Replace("\\", "/");
        if (sourcePath.Contains("://") || sourcePath.Contains("jar:"))
        {
            using var request = UnityWebRequest.Get(sourcePath);
            var op = request.SendWebRequest();
            while (!op.isDone)
                await Task.Yield();

            if (request.result != UnityWebRequest.Result.Success)
                throw new IOException($"UnityWebRequest failed ({request.error})");

            return request.downloadHandler.text;
        }

        return File.ReadAllText(sourcePath);
    }
}
