using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("opponentHealth", Required = Required.Always)]
        public int opponentHealth;

        [JsonProperty("plans", Required = Required.Always)]
        public List<EncounterPlanDef> plans = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterPlanDef
    {
        [JsonProperty("clashIndex", Required = Required.Always)]
        public int clashIndex;

        [JsonProperty("actions", Required = Required.Always)]
        public List<SummonActionRefDef> actions = new();
    }
}
