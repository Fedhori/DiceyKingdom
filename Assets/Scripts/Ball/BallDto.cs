using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

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
        ModifyOtherBallStat
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
        public bool isNotSell = false;
        public int price;

        public List<BallRuleDto> rules;

        [JsonIgnore] public bool isValid = true;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (rules == null)
                rules = new List<BallRuleDto>();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[BallDto] id is null or empty.");
                isValid = false;
                return;
            }

            if (!isNotSell && price <= 0)
            {
                Debug.LogError(
                    $"[BallDto] '{id}': isNotSell=false but price <= 0. (price={price})"
                );
                isValid = false;
            }

            if (isNotSell && price != 0)
            {
                Debug.LogError(
                    $"[BallDto] '{id}': isNotSell=true but price={price}. Force set to 0."
                );
                price = 0;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                {
                    Debug.LogError($"[BallDto] '{id}': rules[{i}] is null.");
                    isValid = false;
                    continue;
                }

                if (rule.triggerType == BallTriggerType.Unknown)
                {
                    Debug.LogError(
                        $"[BallDto] '{id}': rules[{i}].triggerType is Unknown. Use OnBallHitBall/OnBallHitPin."
                    );
                    isValid = false;
                }

                if (rule.effects == null || rule.effects.Count == 0)
                {
                    Debug.LogError(
                        $"[BallDto] '{id}': rules[{i}] effects empty. Need at least 1 effect."
                    );
                    isValid = false;
                }

                var cond = rule.condition;
                if (cond == null)
                {
                    Debug.LogError($"[BallDto] '{id}': rules[{i}].condition is null.");
                    isValid = false;
                    continue;
                }

                if (cond.conditionKind == BallConditionKind.Unknown)
                {
                    Debug.LogError(
                        $"[BallDto] '{id}': rules[{i}].condition.conditionKind is Unknown. Use Always."
                    );
                    isValid = false;
                }
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
