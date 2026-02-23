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

        [JsonProperty("enemy", Required = Required.Default)]
        public EncounterEnemyDef enemy = new();

        [JsonProperty("opponentHealth", Required = Required.Default)]
        public int opponentHealth;

        [JsonProperty("plans", Required = Required.Default)]
        public List<EncounterPlanDef> plans = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterEnemyDef
    {
        [JsonProperty("id", Required = Required.Always)]
        public string id = string.Empty;

        [JsonProperty("health", Required = Required.Always)]
        public int health;

        [JsonProperty("clashes", Required = Required.Always)]
        public List<EncounterEnemyClashDef> clashes = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterEnemyClashDef
    {
        [JsonProperty("clashId", Required = Required.Always)]
        public string clashId = string.Empty;

        [JsonProperty("abilityLoadout", Required = Required.Always)]
        public List<SummonAbilityRefDef> abilityLoadout = new();
    }

    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EncounterPlanDef
    {
        [JsonProperty("clashIndex", Required = Required.Always)]
        public int clashIndex;

        [JsonProperty("abilities", Required = Required.Default)]
        public List<SummonAbilityRefDef> abilities = new();

        [JsonProperty("actions", Required = Required.Default)]
        public List<SummonAbilityRefDef> legacyActions = new();

        public List<SummonAbilityRefDef> ResolveAbilities()
        {
            if (abilities != null && abilities.Count > 0)
            {
                return abilities;
            }

            return legacyActions ?? new List<SummonAbilityRefDef>();
        }
    }
}
