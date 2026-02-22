using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SkillDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("manaCost", Required = Required.Always)]
        public int manaCost;

        [JsonProperty("cooldown", Required = Required.Always)]
        public int cooldown;

        [JsonProperty("timing", Required = Required.Always)]
        public string timing = string.Empty;

        [JsonProperty("target", Required = Required.Always)]
        public SkillTargetDef target = new();

        [JsonProperty("nameLocKey", Required = Required.Always)]
        public string nameLocKey = string.Empty;

        [JsonProperty("descLocKey", Required = Required.Always)]
        public string descLocKey = string.Empty;

        [JsonProperty("ops", Required = Required.Always)]
        public List<EffectOpDef> ops = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class SkillTargetDef
    {
        [JsonProperty("type", Required = Required.Always)]
        public string type = string.Empty;

        [JsonProperty("count", Required = Required.Default)]
        public int? count;
    }
}
