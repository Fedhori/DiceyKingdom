using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DataIndexDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("configs", Required = Required.Always)]
        public List<string> configs = new();

        [JsonProperty("clashes", Required = Required.Always)]
        public List<string> clashes = new();

        [JsonProperty("actions", Required = Required.Always)]
        public List<string> actions = new();

        [JsonProperty("cards", Required = Required.Always)]
        public List<string> cards = new();

        [JsonProperty("skills", Required = Required.Always)]
        public List<string> skills = new();

        [JsonProperty("encounters", Required = Required.Always)]
        public List<string> encounters = new();
    }
}
