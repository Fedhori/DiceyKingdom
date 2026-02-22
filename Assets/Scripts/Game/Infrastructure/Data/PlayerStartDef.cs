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

        [JsonProperty("startingStability", Required = Required.Always)]
        public int startingStability;

        [JsonProperty("startingMana", Required = Required.Always)]
        public int startingMana;

        [JsonProperty("startingPlayerMorale", Required = Required.Always)]
        public int startingPlayerMorale;

        [JsonProperty("startingSquadCardIds", Required = Required.Always)]
        public List<string> startingSquadCardIds = new();
    }
}
