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

        [JsonProperty("abilities", Required = Required.Always)]
        public List<string> abilities = new();

        [JsonProperty("encounters", Required = Required.Always)]
        public List<string> encounters = new();
    }
}
