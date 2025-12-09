using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using UnityEngine;

namespace Data
{
    [Serializable]
    public sealed class BallDto
    {
        public string id;
        public float ballScoreMultiplier = 1f;
        public int cost;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (cost <= 0)
            {
                Debug.LogError($"[BallDto] cost is same or below 0. cost: {cost}, id: {id}");
                return;
            }
        }
    }

    [Serializable]
    public sealed class BallRoot
    {
        public List<BallDto> balls;
    }

    public static class BallRepository
    {
        static readonly Dictionary<string, BallDto> map = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IEnumerable<BallDto> All => map.Values;

        public static void InitializeFromJson(string json)
        {
            map.Clear();

            BallRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<BallRoot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[BallRepository] Failed to deserialize Balls.json: {e}");
                initialized = false;
                return;
            }

            if (root?.balls != null)
            {
                foreach (var dto in root.balls)
                {
                    if (dto == null || string.IsNullOrEmpty(dto.id))
                        continue;

                    map[dto.id] = dto;
                }
            }

            initialized = true;
        }

        public static bool TryGet(string id, out BallDto dto)
        {
            if (!initialized)
            {
                Debug.LogError("[BallRepository] Not initialized.");
                dto = null;
                return false;
            }

            return map.TryGetValue(id, out dto);
        }

        public static BallDto GetOrThrow(string id)
        {
            if (!initialized)
                throw new InvalidOperationException("[BallRepository] Not initialized.");

            if (!map.TryGetValue(id, out var dto) || dto == null)
                throw new KeyNotFoundException($"[BallRepository] Ball id not found: {id}");

            return dto;
        }
    }
}