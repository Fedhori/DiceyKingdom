using System.Collections.Generic;
using UnityEngine;

public static class SpriteCache
{
    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();

    public static Sprite Get(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            Debug.LogWarning($"path is empty - path: {path}");
            return null;
        }

        if (Cache.TryGetValue(path, out var sprite)) return sprite;

        sprite = Resources.Load<Sprite>(path);
        if (sprite != null) Cache[path] = sprite;
        
        if (sprite == null)
            Debug.LogWarning($"sprite is null - path: {path}");
        
        return sprite;
    }

    public static Sprite GetBallSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return Get($"Sprites/Ball/{id}");
    }
    
    public static Sprite GetEnemySprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return Get($"Sprites/Enemy/{id}");
    }
    
    public static Sprite GetEquipmentSprite(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        return Get($"Sprites/Equipment/{id}");
    }
}