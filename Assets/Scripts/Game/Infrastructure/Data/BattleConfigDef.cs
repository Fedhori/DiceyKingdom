using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BattleConfigDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("battlefieldCount", Required = Required.Always)]
        public int battlefieldCount;

        [JsonProperty("manaMax", Required = Required.Always)]
        public int manaMax;

        [JsonProperty("manaRegenPerTurn", Required = Required.Always)]
        public int manaRegenPerTurn;

        [JsonProperty("cooldownTickPerTurn", Required = Required.Always)]
        public int cooldownTickPerTurn;

        [JsonProperty("attackResultMin", Required = Required.Always)]
        public int attackResultMin;

        [JsonProperty("greatVictoryMultiplier", Required = Required.Always)]
        public int greatVictoryMultiplier;

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
