using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class PlayerStartDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("startingHonor", Required = Required.Always)]
        public int startingHonor;

        [JsonProperty("startingPlayerHealth", Required = Required.Always)]
        public int startingPlayerHealth;

        [JsonProperty("startingLoadoutAbilityIds", Required = Required.Always)]
        public List<string> startingLoadoutAbilityIds = new();
    }
}

