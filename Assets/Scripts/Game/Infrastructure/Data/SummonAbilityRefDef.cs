using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SummonAbilityRefDef
    {
        [JsonProperty("abilityId", Required = Required.Always)]
        public string abilityId = string.Empty;

        [JsonProperty("count", Required = Required.Always)]
        public int count;
    }
}
