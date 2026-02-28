using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    /// <summary>
    /// Index for duel runtime game data loaded by <see cref="GameDatabaseLoader"/>.
    /// Includes only:
    /// - configs (duel.config / run.config / player.start)
    /// - abilities
    /// - enemies
    /// Excludes app bootstrap config: Data/GameConfig.json.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public sealed class DataIndexDef
    {
        [JsonProperty("schemaVersion", Required = Required.Always)]
        public int schemaVersion { get; private set; }

        [JsonProperty("configs", Required = Required.Always)]
        public List<string> configs = new();

        [JsonProperty("abilities", Required = Required.Always)]
        public List<string> abilities = new();

        [JsonProperty("enemies", Required = Required.Always)]
        public List<string> enemies = new();
    }
}
