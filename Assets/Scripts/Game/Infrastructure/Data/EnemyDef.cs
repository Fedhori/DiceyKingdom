using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("health", Required = Required.Always)]
        public int health;

        [JsonProperty("abilityLoadout", Required = Required.Always)]
        public List<SummonAbilityRefDef> abilityLoadout = new();
    }
}
