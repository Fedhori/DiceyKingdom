using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    [Serializable]
    public sealed class NailDto
    {
        public string id;
    }

    [Serializable]
    public sealed class NailRoot
    {
        public List<NailDto> nails;
    }

    public static class NailRepository
    {
        static readonly Dictionary<string, NailDto> map = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IEnumerable<NailDto> All => map.Values;

        public static void InitializeFromJson(string json)
        {
            map.Clear();

            NailRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<NailRoot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[NailRepository] Failed to deserialize Nails.json: {e}");
                initialized = false;
                return;
            }

            if (root?.nails != null)
            {
                foreach (var dto in root.nails)
                {
                    if (dto == null || string.IsNullOrEmpty(dto.id))
                        continue;

                    map[dto.id] = dto;
                }
            }

            initialized = true;
            Debug.Log($"[NailRepository] Loaded {map.Count} nail definitions.");
        }

        public static bool TryGet(string id, out NailDto dto)
        {
            if (!initialized)
            {
                Debug.LogError("[NailRepository] Not initialized.");
                dto = null;
                return false;
            }

            return map.TryGetValue(id, out dto);
        }

        public static NailDto GetOrThrow(string id)
        {
            if (!initialized)
                throw new InvalidOperationException("[NailRepository] Not initialized.");

            if (!map.TryGetValue(id, out var dto) || dto == null)
                throw new KeyNotFoundException($"[NailRepository] Nail id not found: {id}");

            return dto;
        }
    }
}
