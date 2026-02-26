using System;
using System.Collections.Generic;
using System.IO;
using Game.Infrastructure.Data;
using UnityEngine;

namespace Game.Presentation.Battle
{
    public sealed class BattleAbilityIconCache : IDisposable
    {
        readonly Dictionary<string, Sprite> spritesByIconId = new(StringComparer.Ordinal);
        readonly HashSet<string> missingLogGuard = new(StringComparer.Ordinal);
        readonly List<Texture2D> runtimeTextures = new();
        readonly List<Sprite> runtimeSprites = new();

        Sprite defaultSprite;

        public void Rebuild(GameDatabase database)
        {
            Clear();

            defaultSprite = TryLoadSprite(AbilityIconPathPolicy.DefaultIconPath, AbilityIconPathPolicy.DefaultIconId);
            if (defaultSprite == null)
            {
                Debug.LogError(
                    $"[DuelAbilityIconCache] Missing default icon file: {AbilityIconPathPolicy.DefaultIconPath}. Fallback texture will be used.");
                defaultSprite = CreateGeneratedFallbackSprite();
            }

            spritesByIconId[AbilityIconPathPolicy.DefaultIconId] = defaultSprite;

            if (database?.abilitiesById == null)
            {
                return;
            }

            foreach (KeyValuePair<string, AbilityDef> pair in database.abilitiesById)
            {
                AbilityDef abilityDef = pair.Value;
                if (abilityDef == null || string.IsNullOrWhiteSpace(abilityDef.iconId))
                {
                    continue;
                }

                if (spritesByIconId.ContainsKey(abilityDef.iconId))
                {
                    continue;
                }

                if (!AbilityIconPathPolicy.TryBuildPath(abilityDef.iconId, out string iconPath))
                {
                    Debug.LogError(
                        $"[DuelAbilityIconCache] Invalid iconId('{abilityDef.iconId}') on ability('{pair.Key}').");
                    spritesByIconId[abilityDef.iconId] = defaultSprite;
                    continue;
                }

                Sprite sprite = TryLoadSprite(iconPath, abilityDef.iconId);
                if (sprite == null)
                {
                    Debug.LogError(
                        $"[DuelAbilityIconCache] Failed to load icon file for iconId('{abilityDef.iconId}') path('{iconPath}'). Default icon will be used.");
                    sprite = defaultSprite;
                }

                spritesByIconId[abilityDef.iconId] = sprite;
            }
        }

        public Sprite ResolveOrDefault(string iconId)
        {
            if (!string.IsNullOrWhiteSpace(iconId) &&
                spritesByIconId.TryGetValue(iconId, out Sprite found) &&
                found != null)
            {
                return found;
            }

            string safeIconId = string.IsNullOrWhiteSpace(iconId) ? "<empty>" : iconId;
            if (!missingLogGuard.Contains(safeIconId))
            {
                missingLogGuard.Add(safeIconId);
                Debug.LogError(
                    $"[DuelAbilityIconCache] Icon cache miss for iconId('{safeIconId}'). Default icon will be used.");
            }

            return defaultSprite;
        }

        public void Dispose()
        {
            Clear();
        }

        void Clear()
        {
            missingLogGuard.Clear();
            spritesByIconId.Clear();
            defaultSprite = null;

            for (int i = 0; i < runtimeSprites.Count; i++)
            {
                Sprite sprite = runtimeSprites[i];
                if (sprite != null)
                {
                    DestroyObject(sprite);
                }
            }

            runtimeSprites.Clear();

            for (int i = 0; i < runtimeTextures.Count; i++)
            {
                Texture2D texture = runtimeTextures[i];
                if (texture != null)
                {
                    DestroyObject(texture);
                }
            }

            runtimeTextures.Clear();
        }

        Sprite TryLoadSprite(string relativePath, string iconId)
        {
            string fullPath = ResolveExistingPath(relativePath);
            if (string.IsNullOrWhiteSpace(fullPath))
            {
                return null;
            }

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(fullPath);
            }
            catch (Exception exception)
            {
                Debug.LogError(
                    $"[DuelAbilityIconCache] Failed to read icon file for iconId('{iconId}') path('{relativePath}'): {exception.Message}");
                return null;
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            bool loaded = texture.LoadImage(bytes, false);
            if (!loaded)
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            sprite.name = $"icon::{iconId}";

            runtimeTextures.Add(texture);
            runtimeSprites.Add(sprite);
            return sprite;
        }

        static void DestroyObject(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            if (UnityEngine.Application.isPlaying)
            {
                UnityEngine.Object.Destroy(target);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(target);
            }
        }

        static string ResolveExistingPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return string.Empty;
            }

            string normalized = relativePath.Replace('\\', '/');
            string streamingPath = Path.Combine(UnityEngine.Application.streamingAssetsPath, normalized);
            if (File.Exists(streamingPath))
            {
                return streamingPath;
            }

            string persistentPath = Path.Combine(UnityEngine.Application.persistentDataPath, normalized);
            if (File.Exists(persistentPath))
            {
                return persistentPath;
            }

            return string.Empty;
        }

        Sprite CreateGeneratedFallbackSprite()
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Color32 color = Colors.Primitive.Bone300;
            texture.SetPixels32(new[] { color, color, color, color });
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "icon::generated.default";

            runtimeTextures.Add(texture);
            runtimeSprites.Add(sprite);
            return sprite;
        }
    }
}
