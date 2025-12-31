using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GameStats;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Data
{
    public enum TokenTriggerType
    {
        Unknown = 0,
        OnStageStart
    }

    public enum TokenConditionKind
    {
        Unknown = 0,
        Always
    }

    public enum TokenEffectType
    {
        Unknown = 0,
        ModifyStat,
        AddCurrency
    }

    public enum TokenRarity
    {
        Unknown = 0,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public sealed class TokenConditionDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenConditionKind conditionKind;
    }

    [Serializable]
    public sealed class TokenEffectDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenEffectType effectType;

        public string statId;

        [JsonConverter(typeof(StringEnumConverter))]
        public StatOpKind effectMode;

        public float value;
        public bool temporary = true;
    }

    [Serializable]
    public sealed class TokenRuleDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TokenTriggerType triggerType;

        public TokenConditionDto condition;
        public List<TokenEffectDto> effects;
    }

    [Serializable]
    public sealed class TokenDto
    {
        public string id;
        public int price;

        [JsonConverter(typeof(StringEnumConverter))]
        public TokenRarity rarity = TokenRarity.Common;

        public List<TokenRuleDto> rules;

        [JsonIgnore]
        public bool isValid = true;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            if (rules == null)
                rules = new List<TokenRuleDto>();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[TokenDto] id is null or empty.");
                isValid = false;
                return;
            }

            if (price < 0)
            {
                Debug.LogError($"[TokenDto] '{id}': price < 0 is not allowed.");
                isValid = false;
            }

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                {
                    Debug.LogError($"[TokenDto] '{id}': rules[{i}] is null.");
                    isValid = false;
                    continue;
                }

                if (rule.triggerType == TokenTriggerType.Unknown)
                {
                    Debug.LogError($"[TokenDto] '{id}': rules[{i}].triggerType is Unknown.");
                    isValid = false;
                }

                if (rule.effects == null || rule.effects.Count == 0)
                {
                    Debug.LogError($"[TokenDto] '{id}': rules[{i}] has no effects.");
                    isValid = false;
                }

                var cond = rule.condition;
                if (cond == null)
                {
                    Debug.LogError($"[TokenDto] '{id}': rules[{i}].condition is null.");
                    isValid = false;
                    continue;
                }

                if (cond.conditionKind == TokenConditionKind.Unknown)
                {
                    Debug.LogError($"[TokenDto] '{id}': rules[{i}].condition.conditionKind is Unknown.");
                    isValid = false;
                    continue;
                }

                if (rule.effects != null)
                {
                    for (int e = 0; e < rule.effects.Count; e++)
                    {
                        var effect = rule.effects[e];
                        if (effect == null)
                        {
                            Debug.LogError($"[TokenDto] '{id}': rules[{i}].effects[{e}] is null.");
                            isValid = false;
                            continue;
                        }

                        if (effect.effectType == TokenEffectType.Unknown)
                        {
                            Debug.LogError($"[TokenDto] '{id}': rules[{i}].effects[{e}].effectType is Unknown.");
                            isValid = false;
                        }
                    }
                }
            }
        }
    }

    [Serializable]
    public sealed class TokenRoot
    {
        public List<TokenDto> tokens;
    }

    public static class TokenRepository
    {
        static readonly Dictionary<string, TokenDto> map = new();
        static bool initialized;

        public static bool IsInitialized => initialized;
        public static IEnumerable<TokenDto> All => map.Values;

        public static void InitializeFromJson(string json)
        {
            map.Clear();
            initialized = false;

            TokenRoot root;
            try
            {
                root = JsonConvert.DeserializeObject<TokenRoot>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[TokenRepository] Failed to deserialize Tokens.json: {e}");
                return;
            }

            if (root?.tokens != null)
            {
                foreach (var dto in root.tokens)
                {
                    if (dto == null)
                        continue;

                    if (!dto.isValid)
                    {
                        Debug.LogError($"[TokenRepository] Skipping invalid token definition. id='{dto.id ?? "(null)"}'.");
                        continue;
                    }

                    if (string.IsNullOrEmpty(dto.id))
                    {
                        Debug.LogError("[TokenRepository] Token with empty id encountered. Skipped.");
                        continue;
                    }

                    map[dto.id] = dto;
                }
            }

            initialized = true;
        }

        public static bool TryGet(string id, out TokenDto dto)
        {
            if (!initialized)
            {
                Debug.LogError("[TokenRepository] Not initialized.");
                dto = null;
                return false;
            }

            return map.TryGetValue(id, out dto);
        }

    }
}
