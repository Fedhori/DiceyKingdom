using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class BattlefieldDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("slotLimit", Required = Required.AllowNull)]
        public int? slotLimit;

        [JsonProperty("tags", Required = Required.Default)]
        public List<string> tags = new();

        [JsonProperty("nameLocKey", Required = Required.Always)]
        public string nameLocKey = string.Empty;

        [JsonProperty("descLocKey", Required = Required.Always)]
        public string descLocKey = string.Empty;

        [JsonProperty("outcomeEffects", Required = Required.Always)]
        public Dictionary<string, List<EffectBlockDef>> outcomeEffects = new();
    }
}
