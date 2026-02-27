using System.Collections.Generic;
using System;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class EnemyDef : IGameDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("id", Required = Required.Always)]
        public string id { get; private set; } = string.Empty;

        [JsonProperty("health", Required = Required.Always)]
        public int health;

        [JsonProperty("tier", Required = Required.Always)]
        public string tier = EnemyTier.Normal.ToString();

        [JsonProperty("abilityLoadout", Required = Required.Always)]
        public List<AbilityLoadoutEntryDef> abilityLoadout = new();

        public bool TryGetTier(out EnemyTier enemyTier)
        {
            return Enum.TryParse(tier, false, out enemyTier);
        }
    }
}
