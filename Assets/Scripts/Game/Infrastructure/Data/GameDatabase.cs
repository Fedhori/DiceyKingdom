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

        public Dictionary<string, ActionDef> actionsById = new();
        public Dictionary<string, string> actionSourcePathById = new();

        public Dictionary<string, CardDef> cardsById = new();
        public Dictionary<string, string> cardSourcePathById = new();

        public Dictionary<string, SkillDef> skillsById = new();
        public Dictionary<string, string> skillSourcePathById = new();

        public Dictionary<string, EncounterDef> encountersById = new();
        public Dictionary<string, string> encounterSourcePathById = new();
    }
}
