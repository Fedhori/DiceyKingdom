using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using GameStats;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Data
{
    public enum ItemTriggerType
    {
        Unknown = 0,
        OnStageStart,
        OnTick
    }

    public enum ItemConditionKind
    {
        Unknown = 0,
        Always
    }

    public enum ItemEffectType
    {
        Unknown = 0,
        ModifyStat,
        AddCurrency
    }

    public enum ItemRarity
    {
        Unknown = 0,
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    [Serializable]
    public sealed class ItemConditionDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemConditionKind conditionKind;
    }

    [Serializable]
    public sealed class ItemEffectDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemEffectType effectType;

        public string statId;

        [JsonConverter(typeof(StringEnumConverter))]
        public StatOpKind effectMode;

        public float value;
        public bool temporary = true;
    }

    [Serializable]
    public sealed class ItemRuleDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemTriggerType triggerType;

        public ItemConditionDto condition;
        public List<ItemEffectDto> effects;
    }

    public enum ProjectileHitBehavior
    {
        Unknown = 0,
        Destroy,
        Bounce,
        Pierce
    }

    [Serializable]
    public sealed class ItemProjectileData
    {
        public string key;
        public float size = 1f;
        public float speed = 1f;

        [JsonConverter(typeof(StringEnumConverter))]
        public ProjectileHitBehavior hitBehavior = ProjectileHitBehavior.Destroy;

        public int maxBounces = 0;
        public float lifeTime = 0f;
    }

    [Serializable]
    public sealed class ItemDto
    {
        public string id;
        public bool isObject;
        public int price;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemRarity rarity = ItemRarity.Common;

        public List<ItemRuleDto> rules;
        public float damageMultiplier = 1f;
        public float attackSpeed = 1f;
        public ItemProjectileData projectile;

        [JsonIgnore]
        public bool isValid = true;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            isValid = true;

            if (rules == null)
                rules = new List<ItemRuleDto>();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[ItemDto] id is null or empty.");
                isValid = false;
                return;
            }

            if (price < 0)
            {
                Debug.LogError($"[ItemDto] '{id}': price < 0 is not allowed.");
                isValid = false;
            }

            if (projectile != null)
            {
                if (projectile.size < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.size < 0 is not allowed.");
                    isValid = false;
                }

                if (projectile.speed < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.speed < 0 is not allowed.");
                    isValid = false;
                }

                if (projectile.maxBounces < 0)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.maxBounces < 0 is not allowed.");
                    isValid = false;
                }

                if (projectile.lifeTime < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.lifeTime < 0 is not allowed.");
                    isValid = false;
                }
            }

            for (int i = 0; i < rules.Count; i++)
            {
                var rule = rules[i];
                if (rule == null)
                {
                    Debug.LogError($"[ItemDto] '{id}': rules[{i}] is null.");
                    isValid = false;
                    continue;
                }

                if (rule.triggerType == ItemTriggerType.Unknown)
                {
                    Debug.LogError($"[ItemDto] '{id}': rules[{i}].triggerType is Unknown.");
                    isValid = false;
                }

                if (rule.effects == null || rule.effects.Count == 0)
                {
                    Debug.LogError($"[ItemDto] '{id}': rules[{i}] has no effects.");
                    isValid = false;
                }

                var cond = rule.condition;
                if (cond == null)
                {
                    Debug.LogError($"[ItemDto] '{id}': rules[{i}].condition is null.");
                    isValid = false;
                    continue;
                }

                if (cond.conditionKind == ItemConditionKind.Unknown)
                {
                    Debug.LogError($"[ItemDto] '{id}': rules[{i}].condition.conditionKind is Unknown.");
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
                            Debug.LogError($"[ItemDto] '{id}': rules[{i}].effects[{e}] is null.");
                            isValid = false;
                            continue;
                        }

                        if (effect.effectType == ItemEffectType.Unknown)
                        {
                            Debug.LogError($"[ItemDto] '{id}': rules[{i}].effects[{e}].effectType is Unknown.");
                            isValid = false;
                        }

                        if (effect.effectType == ItemEffectType.ModifyStat && string.IsNullOrEmpty(effect.statId))
                        {
                            Debug.LogError($"[ItemDto] '{id}': rules[{i}].effects[{e}].statId is empty.");
                            isValid = false;
                        }
                    }
                }
            }
        }
    }
}
