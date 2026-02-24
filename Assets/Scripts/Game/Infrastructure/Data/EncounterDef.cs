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

        [JsonProperty("enemy", Required = Required.Always)]
        public EncounterEnemyDef enemy = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterEnemyDef
    {
        [JsonProperty("id", Required = Required.Always)]
        public string id = string.Empty;

        [JsonProperty("health", Required = Required.Always)]
        public int health;

        [JsonProperty("startPatternId", Required = Required.Always)]
        public string startPatternId = string.Empty;

        [JsonProperty("patterns", Required = Required.Always)]
        public List<EncounterEnemyPatternDef> patterns = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterEnemyPatternDef
    {
        [JsonProperty("patternId", Required = Required.Always)]
        public string patternId = string.Empty;

        [JsonProperty("clashes", Required = Required.Always)]
        public List<EncounterEnemyClashDef> clashes = new();

        [JsonProperty("nextPatterns", Required = Required.Always)]
        public List<EncounterEnemyPatternTransitionDef> nextPatterns = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterEnemyClashDef
    {
        [JsonProperty("clashId", Required = Required.Always)]
        public string clashId = string.Empty;

        [JsonProperty("maxPlayerAssignments", Required = Required.Default)]
        public int? maxPlayerAssignments;

        [JsonProperty("abilityLoadout", Required = Required.Always)]
        public List<SummonAbilityRefDef> abilityLoadout = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterEnemyPatternTransitionDef
    {
        [JsonProperty("patternId", Required = Required.Always)]
        public string patternId = string.Empty;

        [JsonProperty("probability", Required = Required.Always)]
        public double probability;
    }
}

