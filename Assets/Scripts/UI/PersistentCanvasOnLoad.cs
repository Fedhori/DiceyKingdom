using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PersistentCanvasOnLoad : MonoBehaviour
{
    [SerializeField] string persistentId = string.Empty;
    [SerializeField] bool destroyDuplicate = true;

    static readonly HashSet<string> ActiveIds = new(StringComparer.Ordinal);

    string runtimeId;
    bool ownsRegistration;

    void Awake()
    {
        runtimeId = string.IsNullOrWhiteSpace(persistentId)
            ? gameObject.name
            : persistentId.Trim();

        if (destroyDuplicate)
        {
            if (ActiveIds.Contains(runtimeId))
            {
                Destroy(gameObject);
                return;
            }

            ActiveIds.Add(runtimeId);
            ownsRegistration = true;
        }

        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (!ownsRegistration)
            return;
        if (string.IsNullOrWhiteSpace(runtimeId))
            return;

        ActiveIds.Remove(runtimeId);
    }
}
