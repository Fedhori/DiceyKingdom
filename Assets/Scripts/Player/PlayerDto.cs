using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    [Serializable]
    public sealed class PlayerDto
    {
        public string id;

        public float scoreBase = 10f;
        public float scoreMultiplier = 1f;

        public float critChance = 5f;
        public float criticalMultiplier = 2f;
    }

    [Serializable]
    public sealed class PlayerRoot
    {
        public List<PlayerDto> players;
    }

    public static class PlayerRepository
    {
        static readonly Dictionary<string, PlayerDto> map = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IEnumerable<PlayerDto> All => map.Values;

        public static void InitializeFromJson(string json)
        {
            map.Clear();

            PlayerRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<PlayerRoot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[PlayerRepository] Failed to deserialize Players.json: {e}");
                initialized = false;
                return;
            }

            if (root?.players != null)
            {
                foreach (var dto in root.players)
                {
                    if (dto == null || string.IsNullOrEmpty(dto.id))
                        continue;

                    map[dto.id] = dto;
                }
            }

            initialized = true;
        }

        public static bool TryGet(string id, out PlayerDto dto)
        {
            if (!initialized)
            {
                Debug.LogError("[PlayerRepository] Not initialized.");
                dto = null;
                return false;
            }

            return map.TryGetValue(id, out dto);
        }

        public static PlayerDto GetOrThrow(string id)
        {
            if (!initialized)
                throw new InvalidOperationException("[PlayerRepository] Not initialized.");

            if (!map.TryGetValue(id, out var dto) || dto == null)
                throw new KeyNotFoundException($"[PlayerRepository] Player id not found: {id}");

            return dto;
        }
    }
}
