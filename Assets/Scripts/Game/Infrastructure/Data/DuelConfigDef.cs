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

        [JsonProperty("clashCount", Required = Required.Always)]
        public int clashCount;

        [JsonProperty("focusMax", Required = Required.Always)]
        public int focusMax;

        [JsonProperty("focusRegenPerTurn", Required = Required.Always)]
        public int focusRegenPerTurn;

        [JsonProperty("cooldownTickPerTurn", Required = Required.Always)]
        public int cooldownTickPerTurn;

        [JsonProperty("attackResultMin", Required = Required.Always)]
        public int attackResultMin;

        [JsonProperty("p0Rules", Required = Required.Always)]
        public P0RulesDef p0Rules = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class P0RulesDef
    {
        [JsonProperty("disallowBaseAttackMutation", Required = Required.Always)]
        public bool disallowBaseAttackMutation;

        [JsonProperty("defaultSlotLimit", Required = Required.AllowNull)]
        public int? defaultSlotLimit;
    }
}
