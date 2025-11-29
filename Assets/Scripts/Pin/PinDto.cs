using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    [Serializable]
    public sealed class PinEffectDto
    {
        public string eventId;
        public string type;
        public string statId;
        public string mode;
        public float value;
        public bool temporary = true;
    }

    [Serializable]
    public sealed class PinDto
    {
        public string id;
        public float scoreMultiplier = 1f;
        public int hitsToTrigger = -1;

        public List<PinEffectDto> effects;
    }

    [Serializable]
    public sealed class PinRoot
    {
        public List<PinDto> pins;
    }

    public static class PinRepository
    {
        static readonly Dictionary<string, PinDto> map = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IEnumerable<PinDto> All => map.Values;

        public static void InitializeFromJson(string json)
        {
            map.Clear();

            PinRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<PinRoot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PinRepository] Failed to deserialize Pins.json: {e}");
                initialized = false;
                return;
            }

            if (root?.pins != null)
            {
                foreach (var dto in root.pins)
                {
                    if (dto == null || string.IsNullOrEmpty(dto.id))
                        continue;

                    map[dto.id] = dto;
                }
            }

            initialized = true;
        }

        public static bool TryGet(string id, out PinDto dto)
        {
            if (!initialized)
            {
                Debug.LogError("[PinRepository] Not initialized.");
                dto = null;
                return false;
            }

            return map.TryGetValue(id, out dto);
        }

        public static PinDto GetOrThrow(string id)
        {
            if (!initialized)
                throw new InvalidOperationException("[PinRepository] Not initialized.");

            if (!map.TryGetValue(id, out var dto) || dto == null)
                throw new KeyNotFoundException($"[PinRepository] Pin id not found: {id}");

            return dto;
        }
    }
}
