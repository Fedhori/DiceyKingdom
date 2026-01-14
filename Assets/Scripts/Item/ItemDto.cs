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
        OnAcquire,
        OnPlayStart,
        OnPlayEnd,
        OnItemChanged,
        OnCurrencyChanged,
        OnProjectileSpawned,
        OnRewardOpen,
        OnBlockDestroyed,
        OnBlockStatusApplied,
        OnTimeChanged,
        OnTick
    }

    public enum ItemConditionKind
    {
        Unknown = 0,
        Always,
        PlayerIdle,
        EveryNthTrigger,
        Time
    }

    public enum ItemEffectType
    {
        Unknown = 0,
        ModifyStat,
        AddCurrency,
        SpawnProjectile,
        ApplyStatusToRandomBlocks,
        AddSellValue,
        ModifyItemStat,
        SetItemStat,
        SetStat,
        ModifyBaseIncome,
        ApplyDamageToAllBlocks,
        SetItemStatus,
        ModifyTriggerRepeat
    }

    public enum ItemEffectTarget
    {
        Unknown = 0,
        Self,
        Right
    }

    public enum ItemRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Legendary = 3
    }

    [Serializable]
    public sealed class ItemConditionDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemConditionKind conditionKind;

        public int count;
        public float intervalSeconds;
    }

    [Serializable]
    public sealed class ItemEffectDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ItemEffectType effectType;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemEffectTarget target = ItemEffectTarget.Self;

        public string statId;

        [JsonConverter(typeof(StringEnumConverter))]
        public StatOpKind effectMode;

        public float value;

        [JsonConverter(typeof(StringEnumConverter))]
        public StatLayer duration = StatLayer.Temporary;

        public string multiplier;
        public int threshold;

        [JsonConverter(typeof(StringEnumConverter))]
        public BlockStatusType statusType = BlockStatusType.Unknown;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemTriggerType triggerType = ItemTriggerType.Unknown;
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
        Normal,
        Bounce
    }

    [Serializable]
    public sealed class ItemProjectileData
    {
        public string key;
        public float size = 1f;
        public float speed = 1f;
        public int pelletCount = 1;
        public float spreadAngle = 0f;
        public float randomAngle = 0f;
        public float homingTurnRate = 0f;
        public float explosionRadius = 0f;

        [JsonConverter(typeof(StringEnumConverter))]
        public ProjectileHitBehavior hitBehavior = ProjectileHitBehavior.Normal;
        public int pierce = 0;
    }

    [Serializable]
    public sealed class ItemBeamData
    {
        public float thickness = 0f;
        public float duration = 0f;
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
        public float damageMultiplier = 0f;
        public float statusDamageMultiplier = 1f;
        public float attackSpeed = 0f;
        public ItemProjectileData projectile;
        public ItemBeamData beam;
        public int pierceBonus = 0;
        [JsonConverter(typeof(StringEnumConverter))]
        public BlockStatusType statusType = BlockStatusType.Unknown;

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

            if (damageMultiplier < 0f)
            {
                Debug.LogError($"[ItemDto] '{id}': damageMultiplier < 0 is not allowed.");
                isValid = false;
            }

            if (statusDamageMultiplier < 0f)
            {
                Debug.LogError($"[ItemDto] '{id}': statusDamageMultiplier < 0 is not allowed.");
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

                if (projectile.pierce < 0)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.pierce < 0 is not allowed.");
                    isValid = false;
                }

                if (projectile.pelletCount < 1)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.pelletCount < 1 is not allowed.");
                    isValid = false;
                }

                if (projectile.homingTurnRate < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.homingTurnRate < 0 is not allowed.");
                    isValid = false;
                }

                if (projectile.randomAngle < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.randomAngle < 0 is not allowed.");
                    isValid = false;
                }

                if (projectile.explosionRadius < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': projectile.explosionRadius < 0 is not allowed.");
                    isValid = false;
                }
            }

            if (beam != null)
            {
                if (beam.thickness < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': beam.thickness < 0 is not allowed.");
                    isValid = false;
                }

                if (beam.duration < 0f)
                {
                    Debug.LogError($"[ItemDto] '{id}': beam.duration < 0 is not allowed.");
                    isValid = false;
                }
            }

            if (pierceBonus < 0)
            {
                Debug.LogError($"[ItemDto] '{id}': pierceBonus < 0 is not allowed.");
                isValid = false;
            }

        }
    }
}
