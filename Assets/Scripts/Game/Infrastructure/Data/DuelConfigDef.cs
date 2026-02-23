using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DuelConfigDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("cooldownTickPerTurn", Required = Required.Always)]
        public int cooldownTickPerTurn;

        [JsonProperty("powerResultMin", Required = Required.Always)]
        public int powerResultMin;

        [JsonProperty("p0Rules", Required = Required.Always)]
        public P0RulesDef p0Rules = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class P0RulesDef
    {
        [JsonProperty("disallowBasePowerMutation", Required = Required.Always)]
        public bool disallowBasePowerMutation;

        [JsonProperty("defaultSlotLimit", Required = Required.AllowNull)]
        public int? defaultSlotLimit;
    }
}
