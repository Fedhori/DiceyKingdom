using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class AbilityLoadoutEntryDef
    {
        [JsonProperty("abilityId", Required = Required.Always)]
        public string abilityId = string.Empty;

        [JsonProperty("count", Required = Required.Always)]
        public int count;

        [JsonProperty("power", Required = Required.Default)]
        public int? power;

        [JsonProperty("cooldown", Required = Required.Default)]
        public int? cooldown;
    }
}
