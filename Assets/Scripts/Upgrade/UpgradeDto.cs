using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

namespace Data
{
    public enum UpgradeConditionKind
    {
        Unknown = 0,
        HasDamageMultiplier,
        HasAttackSpeed,
        HasProjectile,
        HasNoExplosionRadius,
        HasItemRarity,
        HasTriggerRule
    }

    [Serializable]
    public sealed class UpgradeConditionDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public UpgradeConditionKind conditionKind;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemRarity rarity;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemTriggerType triggerType = ItemTriggerType.Unknown;
    }

    [Serializable]
    public sealed class UpgradeDto
    {
        public string id;
        public int price;

        [JsonConverter(typeof(StringEnumConverter))]
        public ItemRarity rarity = ItemRarity.Common;

        public List<UpgradeConditionDto> conditions;
        public List<ItemEffectDto> effects;
        public List<ItemRuleDto> rules;
        public bool requiresSolo;

        [JsonIgnore]
        public bool isValid = true;

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            isValid = true;

            if (conditions == null)
                conditions = new List<UpgradeConditionDto>();

            if (effects == null)
                effects = new List<ItemEffectDto>();

            if (rules == null)
                rules = new List<ItemRuleDto>();

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogError("[UpgradeDto] id is null or empty.");
                isValid = false;
                return;
            }

            if (price < 0)
            {
                Debug.LogError($"[UpgradeDto] '{id}': price < 0 is not allowed.");
                isValid = false;
            }
        }
    }

    [Serializable]
    public sealed class UpgradeRoot
    {
        public List<UpgradeDto> upgrades;
    }
}
