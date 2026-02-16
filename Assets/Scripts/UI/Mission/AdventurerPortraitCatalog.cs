using System;
using System.Collections.Generic;
using UnityEngine;

public static class AdventurerPortraitCatalog
{
    const string PortraitResourcePath = "Portraits/Adventurers";
    static readonly List<Sprite> portraits = new();
    static readonly HashSet<string> loggedErrors = new(StringComparer.Ordinal);
    static bool loaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        portraits.Clear();
        loggedErrors.Clear();
        loaded = false;
    }

    public static int GetPortraitCount()
    {
        EnsureLoaded();
        return portraits.Count;
    }

    public static Sprite ResolvePortrait(int portraitIndex)
    {
        EnsureLoaded();
        if (portraits.Count <= 0)
        {
            LogErrorOnce("portrait_pool_empty", $"[AdventurerPortrait] No portrait textures found at Resources/{PortraitResourcePath}.");
            return null;
        }

        if (portraitIndex < 0 || portraitIndex >= portraits.Count)
        {
            LogErrorOnce($"portrait_index:{portraitIndex}", $"[AdventurerPortrait] portraitIndex out of range ({portraitIndex}/{portraits.Count - 1}). Using index 0.");
            portraitIndex = 0;
        }

        Sprite sprite = portraits[portraitIndex];
        if (sprite != null)
            return sprite;

        LogErrorOnce($"portrait_null:{portraitIndex}", $"[AdventurerPortrait] Portrait sprite is null at index {portraitIndex}. Using index 0.");
        return portraits[0];
    }

    static void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;
        Texture2D[] textures = Resources.LoadAll<Texture2D>(PortraitResourcePath);
        if (textures == null || textures.Length <= 0)
            return;

        Array.Sort(textures, CompareTextureByName);
        for (int i = 0; i < textures.Length; i++)
        {
            Texture2D texture = textures[i];
            if (texture == null)
                continue;

            Rect rect = new Rect(0f, 0f, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, rect, new Vector2(0.5f, 0.5f), 100f);
            if (sprite != null)
                portraits.Add(sprite);
        }
    }

    static int CompareTextureByName(Texture2D left, Texture2D right)
    {
        string leftName = left == null ? string.Empty : left.name;
        string rightName = right == null ? string.Empty : right.name;
        return string.CompareOrdinal(leftName, rightName);
    }

    static void LogErrorOnce(string key, string message)
    {
        if (!loggedErrors.Add(key))
            return;

        Debug.LogError(message);
    }
}
