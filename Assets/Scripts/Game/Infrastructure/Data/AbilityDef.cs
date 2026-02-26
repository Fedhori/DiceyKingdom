using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AbilityDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("type", Required = Required.Always)]
        public string type = AbilityType.Attack.ToString();

        [JsonProperty("buildCost", Required = Required.Always)]
        public int buildCost;

        [JsonProperty("cooldown", Required = Required.Default)]
        public int? cooldown;

        [JsonProperty("power", Required = Required.Always)]
        public int power;

        [JsonProperty("nameLocKey", Required = Required.Always)]
        public string nameLocKey = string.Empty;

        [JsonProperty("descLocKey", Required = Required.Always)]
        public string descLocKey = string.Empty;

        [JsonProperty("isPlayerObtainable", Required = Required.Always)]
        public bool isPlayerObtainable;

        [JsonProperty("iconId", Required = Required.Default)]
        public string iconId = string.Empty;

        [JsonProperty("effects", Required = Required.Default)]
        public List<TimedEffectDef> effects = new();

        public bool TryGetAbilityType(out AbilityType abilityType)
        {
            return Enum.TryParse(type, false, out abilityType);
        }

        public int ResolvePower()
        {
            return power;
        }

        public static int GetDefaultCooldownTurns(AbilityType abilityType)
        {
            return abilityType == AbilityType.Passive
                ? 0
                : 1;
        }

        public static int GetMinimumCooldownTurns(AbilityType abilityType)
        {
            return abilityType == AbilityType.Passive
                ? 0
                : 1;
        }

        public int ResolveCooldownTurns(AbilityType abilityType)
        {
            return cooldown ?? GetDefaultCooldownTurns(abilityType);
        }
    }
}
