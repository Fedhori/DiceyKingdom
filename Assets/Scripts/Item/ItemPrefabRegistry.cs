using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class ItemPrefabRegistry : MonoBehaviour
{
    [Serializable]
    public sealed class Entry
    {
        public string key;
        public GameObject prefab;
    }

    public static ItemPrefabRegistry Instance { get; private set; }

    [SerializeField] private List<Entry> entries = new();

    readonly Dictionary<string, GameObject> map = new(StringComparer.OrdinalIgnoreCase);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Rebuild();
    }

    public void Rebuild()
    {
        map.Clear();
        for (int i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.key) || entry.prefab == null)
                continue;

            map[entry.key] = entry.prefab;
        }
    }

    public bool TryGet(string key, out GameObject prefab)
    {
        prefab = null;
        if (string.IsNullOrEmpty(key))
            return false;

        return map.TryGetValue(key, out prefab);
    }
}
