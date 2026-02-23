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

        [JsonProperty("startingFocus", Required = Required.Default)]
        public int startingFocus;

        [JsonProperty("startingPlayerHealth", Required = Required.Always)]
        public int startingPlayerHealth;

        [JsonProperty("startingBagAbilityIds", Required = Required.Default)]
        public List<string> startingBagAbilityIds = new();

        [JsonProperty("startingAbilityDeckIds", Required = Required.Default)]
        public List<string> legacyStartingAbilityDeckIds = new();

        public List<string> ResolveStartingBagAbilityIds()
        {
            if (startingBagAbilityIds != null && startingBagAbilityIds.Count > 0)
            {
                return startingBagAbilityIds;
            }

            return legacyStartingAbilityDeckIds ?? new List<string>();
        }
    }
}
