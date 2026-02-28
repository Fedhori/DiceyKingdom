using System;

namespace Game.Data
{
    [Serializable]
    public sealed class GameConfigData
    {
        public const int CurrentSchemaVersion = 2;

        public int schemaVersion = CurrentSchemaVersion;
        public string templateName = "TemplateProject";
        public int defaultRunSeed = 1001;
        public int startingPrimaryValue = 0;
        public int startingSecondaryValue = 0;
        public int ticksPerAutoSave = 5;
    }

}
