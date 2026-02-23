using System.Collections.Generic;
using System;
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
        public int cooldown;

        [JsonProperty("damage", Required = Required.Always)]
        public int damage;

        [JsonProperty("tags", Required = Required.Default)]
        public List<string> tags = new();

        [JsonProperty("nameLocKey", Required = Required.Always)]
        public string nameLocKey = string.Empty;

        [JsonProperty("descLocKey", Required = Required.Always)]
        public string descLocKey = string.Empty;

        [JsonProperty("effects", Required = Required.Default)]
        public List<TimedEffectDef> effects = new();

        public bool TryGetAbilityType(out AbilityType abilityType)
        {
            return Enum.TryParse(type, false, out abilityType);
        }

        public int ResolveDamage()
        {
            return damage;
        }
    }
}
