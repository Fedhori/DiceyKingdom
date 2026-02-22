using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class RunConfigDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("startingStability", Required = Required.Always)]
        public int startingStability;

        [JsonProperty("supplyLimit", Required = Required.Always)]
        public int supplyLimit;
    }
}
