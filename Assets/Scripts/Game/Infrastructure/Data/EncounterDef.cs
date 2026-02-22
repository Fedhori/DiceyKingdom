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

        [JsonProperty("enemyMorale", Required = Required.Always)]
        public int enemyMorale;

        [JsonProperty("plans", Required = Required.Always)]
        public List<EncounterPlanDef> plans = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterPlanDef
    {
        [JsonProperty("battlefieldIndex", Required = Required.Always)]
        public int battlefieldIndex;

        [JsonProperty("troops", Required = Required.Always)]
        public List<SummonTroopRefDef> troops = new();
    }
}
