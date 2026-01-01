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
        OnRewardOpen,
        OnBlockDestroyed,
        OnTick
    }

    public enum ItemConditionKind
    {
        Unknown = 0,
        Always,
        PlayerIdle
    }

    public enum ItemEffectType
    {
        Unknown = 0,
        ModifyStat,
        AddCurrency,
        SpawnProjectile
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
        public int pelletCount = 1;
        public float spreadAngle = 0f;

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
        public bool isNotSell;
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

                if (projectile.pelletCount < 1)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.pelletCount < 1 is not allowed.");
                    isValid = false;
                }
            }
        }
    }
}
