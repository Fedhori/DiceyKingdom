using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

/*
 * 신규 볼 추가 체크리스트
 * - Balls.json에 신규 볼 추가
 * - ball table에 {id}.name, {id}.effect{index} 추가
 * - {id}.png 파일 추가
 */

namespace Data
{
    public enum BallTriggerType
    {
        Unknown = 0,
        OnBallHitBall,
        OnBallHitPin
    }

    public enum BallConditionKind
    {
        Unknown = 0,
        Always
    }

    public enum BallEffectType
    {
        Unknown = 0,
        ModifySelfStat,
        ModifyOtherBallStat,
        AddScore,
    }

    [Serializable]
    public sealed class BallConditionDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BallConditionKind conditionKind;
    }

    [Serializable]
    public sealed class BallEffectDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BallEffectType effectType;

        public string statId;

        [JsonConverter(typeof(StringEnumConverter))]
        public GameStats.StatOpKind effectMode;

        public float value;
        public bool temporary = true;
    }

    [Serializable]
    public sealed class BallRuleDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public BallTriggerType triggerType;

        public BallConditionDto condition;
        public List<BallEffectDto> effects;
    }

    [Serializable]
    public sealed class BallDto
    {
        public string id;
        public float ballScoreMultiplier = 1f;
        public bool isNotSell;
        public float price;
        public float criticalMultiplier = 1f;
        public int life = 0;

        public List<BallRuleDto> rules;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (rules == null)
                rules = new List<BallRuleDto>();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[BallDto] id is null or empty.");
                return;
            }

            if (!isNotSell && price <= 0)
            {
                Debug.LogError(
                    $"[BallDto] '{id}': isNotSell=false but price <= 0. (price={price})"
                );
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
        private static readonly Dictionary<string, BallDto> Map = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IEnumerable<BallDto> All => Map.Values;

        public static void InitializeFromJson(string json)
        {
            Map.Clear();

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

                    Map[dto.id] = dto;
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

            return Map.TryGetValue(id, out dto);
        }

        public static BallDto GetOrThrow(string id)
        {
            if (!initialized)
                throw new InvalidOperationException("[BallRepository] Not initialized.");

            if (!Map.TryGetValue(id, out var dto) || dto == null)
                throw new KeyNotFoundException($"[BallRepository] Ball id not found: {id}");

            return dto;
        }
    }
}
