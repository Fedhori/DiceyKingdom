using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    [Serializable]
    public sealed class PlayerDto
    {
        public string id;

        public float power = 10f;

        public float critChance = 5f;
        public float criticalMultiplier = 2f;

        public float moveSpeed = 1f;

        // 런 시작 시 지급되는 통화
        public int startCurrency = 0;

        public List<string> itemIds;
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
