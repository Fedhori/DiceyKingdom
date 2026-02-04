using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class StaticDataManager : MonoBehaviour
{
    public static StaticDataManager Instance { get; private set; }

    [Serializable]
    public sealed class JsonEntry
    {
        public string key;
        public string relativePath;
    }

    [SerializeField] private List<JsonEntry> entries = new();

    readonly Dictionary<string, string> jsonCache = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, string> JsonCache => jsonCache;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadAll();
    }

    public void LoadAll()
    {
        jsonCache.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.relativePath))
                continue;

            var key = string.IsNullOrEmpty(entry.key) ? entry.relativePath : entry.key;
            TryLoad(entry.relativePath, key);
        }
    }

    public bool TryGetJson(string key, out string json)
    {
        if (string.IsNullOrEmpty(key))
        {
            json = string.Empty;
            return false;
        }

        return jsonCache.TryGetValue(key, out json);
    }

    bool TryLoad(string relativePath, string key)
    {
        try
        {
            string json = SaCache.ReadText(relativePath);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[StaticDataManager] Empty json: {relativePath}");
                return false;
            }

            jsonCache[key] = json;
            return true;
        }
        catch (IOException e)
        {
            Debug.LogWarning($"[StaticDataManager] Read failed: {relativePath} ({e.Message})");
            return false;
        }
    }
}
