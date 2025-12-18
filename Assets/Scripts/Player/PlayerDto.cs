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

        public float scoreBase = 10f;
        public float scoreMultiplier = 1f;

        public float critChance = 5f;
        public float criticalMultiplier = 2f;

        // 런 시작 시 지급되는 통화
        public int startCurrency = 0;

        // 희귀도 기반 볼 생성 파라미터
        public int initialBallCount = 50;
        public float rarityGrowth = 2f;
        public List<float> rarityProbabilities;
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

                    EnsureDefaults(dto);
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

        static void EnsureDefaults(PlayerDto dto)
        {
            if (dto.initialBallCount <= 0)
            {
                Debug.LogError($"[PlayerRepository] initialBallCount is invalid (<=0) for player '{dto.id}'.");
                throw new InvalidOperationException("[PlayerRepository] Invalid initialBallCount.");
            }

            if (dto.rarityGrowth <= 0f)
            {
                Debug.LogError($"[PlayerRepository] rarityGrowth is invalid (<=0) for player '{dto.id}'.");
                throw new InvalidOperationException("[PlayerRepository] Invalid rarityGrowth.");
            }

            dto.rarityProbabilities = NormalizeProbabilities(dto.rarityProbabilities);
        }

        static List<float> NormalizeProbabilities(List<float> input)
        {
            if (input == null)
            {
                Debug.LogError("[PlayerRepository] rarityProbabilities is null.");
                throw new InvalidOperationException("[PlayerRepository] rarityProbabilities is null.");
            }

            var probs = new List<float>(input);

            if (probs.Count != 5)
            {
                Debug.LogError($"[PlayerRepository] rarityProbabilities must have 5 entries. count={probs.Count}");
                throw new InvalidOperationException("[PlayerRepository] rarityProbabilities length invalid.");
            }

            for (int i = 0; i < probs.Count; i++)
            {
                if (probs[i] < 0f)
                {
                    Debug.LogError($"[PlayerRepository] rarityProbabilities contains negative value at {i}.");
                    throw new InvalidOperationException("[PlayerRepository] rarityProbabilities contains negative value.");
                }
            }

            float sum = probs.Sum();
            if (sum <= 0.0001f)
            {
                Debug.LogError("[PlayerRepository] rarityProbabilities sum <= 0.");
                throw new InvalidOperationException("[PlayerRepository] rarityProbabilities sum invalid.");
            }

            if (Mathf.Abs(sum - 100f) > 0.001f)
            {
                Debug.LogError($"[PlayerRepository] rarityProbabilities sum != 100 (sum={sum}). Normalizing.");
                for (int i = 0; i < probs.Count; i++)
                    probs[i] = probs[i] / sum * 100f;
            }

            return probs;
        }
    }
}
