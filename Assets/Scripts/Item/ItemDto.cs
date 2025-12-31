using System;
using System.Collections.Generic;
using GameStats;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
        public string prefabKey;
        public int price;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemRarity rarity = ItemRarity.Common;

        public List<ItemRuleDto> rules;
        public float damageMultiplier = 1f;
        public float attackSpeed = 1f;
        public ItemProjectileData projectile;
    }
}
