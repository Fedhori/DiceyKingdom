using System.Collections.Generic;

namespace Game.Infrastructure.Data
{
    public sealed class GameDatabase
    {
        public DuelConfigDef duelConfig;
        public string duelConfigSourcePath = string.Empty;

        public RunConfigDef runConfig;
        public string runConfigSourcePath = string.Empty;

        public PlayerStartDef playerStart;
        public string playerStartSourcePath = string.Empty;

        public Dictionary<string, ClashDef> clashesById = new();
        public Dictionary<string, string> clashSourcePathById = new();

        public Dictionary<string, AbilityDef> abilitiesById = new();
        public Dictionary<string, string> abilitySourcePathById = new();

        public Dictionary<string, EncounterDef> encountersById = new();
        public Dictionary<string, string> encounterSourcePathById = new();
    }
}
