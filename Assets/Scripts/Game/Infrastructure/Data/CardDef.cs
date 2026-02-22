using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CardDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("type", Required = Required.Always)]
        public string type = string.Empty;

        [JsonProperty("supplyCost", Required = Required.Always)]
        public int supplyCost;

        [JsonProperty("nameLocKey", Required = Required.Always)]
        public string nameLocKey = string.Empty;

        [JsonProperty("descLocKey", Required = Required.Always)]
        public string descLocKey = string.Empty;

        [JsonProperty("battleStart", Required = Required.Always)]
        public CardBattleStartDef battleStart = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CardBattleStartDef
    {
        [JsonProperty("summonTroops", Required = Required.Default)]
        public List<SummonTroopRefDef> summonTroops = new();

        [JsonProperty("ops", Required = Required.Default)]
        public List<EffectOpDef> ops = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SummonTroopRefDef
    {
        [JsonProperty("troopId", Required = Required.Always)]
        public string troopId = string.Empty;

        [JsonProperty("count", Required = Required.Always)]
        public int count;
    }
}
