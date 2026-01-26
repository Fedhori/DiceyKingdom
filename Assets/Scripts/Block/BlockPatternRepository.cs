using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    [Serializable]
    public sealed class BlockPatternDto
    {
        public string id;
        public float cost = 1f;
        public float size = 1f;
        public float speed = 1f;
        public float health = 1f;
        public int count = 1;

        [JsonIgnore]
        public bool isValid = true;

        public void Revalidate()
        {
            OnDeserialized(default);
        }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            isValid = true;

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[BlockPatternDto] id is null or empty.");
                isValid = false;
                return;
            }

            if (cost <= 0f)
            {
                Debug.LogError($"[BlockPatternDto] '{id}': cost must be > 0.");
                isValid = false;
            }

            size = Mathf.Max(0f, size);
            speed = Mathf.Max(0f, speed);
            health = Mathf.Max(0f, health);
            if (count < 1)
                count = 1;
        }
    }

    [Serializable]
    public sealed class BlockPatternRoot
    {
        public List<BlockPatternDto> patterns;
    }

    public static class BlockPatternRepository
    {
        static readonly Dictionary<string, BlockPatternDto> dict = new();
        static readonly List<BlockPatternDto> list = new();
        static bool initialized;
        static readonly Dictionary<string, float> weights = new();

        public static bool IsInitialized => initialized;
        public static IReadOnlyDictionary<string, BlockPatternDto> All => dict;
        public static IReadOnlyList<BlockPatternDto> List => list;

        public static float GetWeight(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0f;

            return weights.TryGetValue(id, out var value) ? value : 0f;
        }

        public static void SetWeight(string id, float weight)
        {
            if (string.IsNullOrEmpty(id))
                return;

            weights[id] = Mathf.Max(0f, weight);
        }

        public static bool ApplyUpdate(BlockPatternDto updated)
        {
            if (!initialized || updated == null || string.IsNullOrEmpty(updated.id))
                return false;

            updated.Revalidate();
            if (!updated.isValid)
                return false;

            if (!dict.ContainsKey(updated.id))
                return false;

            dict[updated.id] = updated;
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i]?.id, updated.id, StringComparison.Ordinal))
                {
                    list[i] = updated;
                    break;
                }
            }

            return true;
        }

        public static bool TryAdd(BlockPatternDto dto)
        {
            if (dto == null || string.IsNullOrEmpty(dto.id))
                return false;

            dto.Revalidate();
            if (!dto.isValid)
                return false;

            dict[dto.id] = dto;
            list.Add(dto);
            initialized = dict.Count > 0;
            return true;
        }

        public static bool TryRemove(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;

            bool removed = dict.Remove(id);
            if (!removed)
                return false;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (string.Equals(list[i]?.id, id, StringComparison.Ordinal))
                    list.RemoveAt(i);
            }

            initialized = dict.Count > 0;
            return true;
        }

        public static void LoadFromJson(TextAsset jsonAsset)
        {
            dict.Clear();
            list.Clear();
            initialized = false;

            if (jsonAsset == null)
            {
                Debug.LogError("[BlockPatternRepository] json asset is null");
                return;
            }

            BlockPatternRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<BlockPatternRoot>(jsonAsset.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BlockPatternRepository] json parse error: {e}");
                return;
            }

            if (root?.patterns == null)
            {
                Debug.LogError("[BlockPatternRepository] json has no patterns array");
                return;
            }

            for (int i = 0; i < root.patterns.Count; i++)
            {
                var dto = root.patterns[i];
                if (dto == null)
                    continue;

                if (!dto.isValid)
                {
                    Debug.LogError($"[BlockPatternRepository] Skipping invalid pattern. id='{dto.id ?? "(null)"}'.");
                    continue;
                }

                dict[dto.id] = dto;
                list.Add(dto);
            }

            InitializeDefaultWeights(list);
            initialized = dict.Count > 0;
        }

        public static bool TryGet(string id, out BlockPatternDto dto)
        {
            if (!initialized || string.IsNullOrEmpty(id))
            {
                dto = null;
                return false;
            }

            return dict.TryGetValue(id, out dto);
        }

        static void InitializeDefaultWeights(IReadOnlyList<BlockPatternDto> patterns)
        {
            weights.Clear();
            SetWeight("normal", 10f);
            SetWeight("big", 1f);
            SetWeight("fast", 1f);
            SetWeight("large", 1f);
            SetWeight("many", 1f);

            if (patterns == null)
                return;

            for (int i = 0; i < patterns.Count; i++)
            {
                var pattern = patterns[i];
                if (pattern == null || string.IsNullOrEmpty(pattern.id))
                    continue;

                if (!weights.ContainsKey(pattern.id))
                    weights[pattern.id] = 1f;
            }
        }
    }
}
