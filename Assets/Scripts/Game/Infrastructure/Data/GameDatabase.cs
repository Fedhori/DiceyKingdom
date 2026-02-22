using System.Collections.Generic;

namespace Game.Infrastructure.Data
{
    public sealed class GameDatabase
    {
        public BattleConfigDef battleConfig;
        public string battleConfigSourcePath = string.Empty;

        public RunConfigDef runConfig;
        public string runConfigSourcePath = string.Empty;

        public PlayerStartDef playerStart;
        public string playerStartSourcePath = string.Empty;

        public Dictionary<string, BattlefieldDef> battlefieldsById = new();
        public Dictionary<string, string> battlefieldSourcePathById = new();

        public Dictionary<string, TroopDef> troopsById = new();
        public Dictionary<string, string> troopSourcePathById = new();

        public Dictionary<string, CardDef> cardsById = new();
        public Dictionary<string, string> cardSourcePathById = new();

        public Dictionary<string, SkillDef> skillsById = new();
        public Dictionary<string, string> skillSourcePathById = new();

        public Dictionary<string, EncounterDef> encountersById = new();
        public Dictionary<string, string> encounterSourcePathById = new();
    }
}
