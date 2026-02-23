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

        [JsonProperty("duelStart", Required = Required.Always)]
        public CardDuelStartDef duelStart = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class CardDuelStartDef
    {
        [JsonProperty("summonActions", Required = Required.Default)]
        public List<SummonActionRefDef> summonActions = new();

        [JsonProperty("ops", Required = Required.Default)]
        public List<EffectOpDef> ops = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SummonActionRefDef
    {
        [JsonProperty("actionId", Required = Required.Always)]
        public string actionId = string.Empty;

        [JsonProperty("count", Required = Required.Always)]
        public int count;
    }
}
