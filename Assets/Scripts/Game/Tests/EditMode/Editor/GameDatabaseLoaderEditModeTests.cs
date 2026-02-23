using System.Collections.Generic;
using System.Linq;
using Game.Infrastructure.Data;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Game.Tests.EditMode
{
    public sealed class GameDatabaseLoaderEditModeTests
    {
        [Test]
        public void LoadDefaultDataIndex_FromSaCacheSource_Succeeds()
        {
            GameDataBuildResult result = GameDatabaseLoader.LoadDefault(GameDataBuildMode.Development);

            Assert.IsTrue(result.isSuccess);
            Assert.NotNull(result.database);
            Assert.AreEqual(0, result.report.ErrorCount);
            Assert.NotNull(result.database.duelConfig);
            Assert.NotNull(result.database.runConfig);
            Assert.NotNull(result.database.playerStart);
            Assert.AreEqual(3, result.database.clashesById.Count);
            Assert.Greater(result.database.actionsById.Count, 0);
        }

        [Test]
        public void Load_FailsWhenActionReferenceIsMissing()
        {
            Dictionary<string, string> files = CreateValidDataSet();
            files["Data/encounters/encounter.1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""encounter.1"",
  ""opponentHealth"": 10,
  ""plans"": [
    { ""clashIndex"": 0, ""actions"": [ { ""actionId"": ""missing_action"", ""count"": 1 } ] }
  ]
}";

            var loader = new GameDatabaseLoader(new InMemoryGameDataSource(files));
            LogAssert.ignoreFailingMessages = true;
            try
            {
                GameDataBuildResult result = loader.Load(new GameDataLoadOptions
                {
                    dataIndexPath = "Data/DataIndex.json",
                    mode = GameDataBuildMode.Development
                });

                Assert.IsFalse(result.isSuccess);
                Assert.IsTrue(result.report.Errors.Any(e => e.code == GameDataErrorCode.MissingReference));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void Load_FailsWhenUnknownFieldExists()
        {
            Dictionary<string, string> files = CreateValidDataSet();
            files["Data/actions/action.1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""action.1"",
  ""attack"": 2,
  ""nameLocKey"": ""action.1_name"",
  ""descLocKey"": ""action.1_desc"",
  ""effects"": [],
  ""unknownField"": 999
}";

            var loader = new GameDatabaseLoader(new InMemoryGameDataSource(files));
            LogAssert.ignoreFailingMessages = true;
            try
            {
                GameDataBuildResult result = loader.Load(new GameDataLoadOptions
                {
                    dataIndexPath = "Data/DataIndex.json",
                    mode = GameDataBuildMode.Development
                });

                Assert.IsFalse(result.isSuccess);
                Assert.IsTrue(result.report.Errors.Any(e => e.code == GameDataErrorCode.ParseError));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        [Test]
        public void Load_FailsWhenModifierLayerIsNotCaseSensitiveMatch()
        {
            Dictionary<string, string> files = CreateValidDataSet();
            files["Data/actions/action.1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""action.1"",
  ""attack"": 2,
  ""nameLocKey"": ""action.1_name"",
  ""descLocKey"": ""action.1_desc"",
  ""effects"": [
    {
      ""timing"": ""TurnEnd"",
      ""condition"": { ""type"": ""IsInActionHolder"" },
      ""ops"": [
        { ""op"": ""AddAttackModifier"", ""target"": ""Attack"", ""layer"": ""duel"", ""mode"": ""Add"", ""value"": 1 }
      ]
    }
  ]
}";

            var loader = new GameDatabaseLoader(new InMemoryGameDataSource(files));
            LogAssert.ignoreFailingMessages = true;
            try
            {
                GameDataBuildResult result = loader.Load(new GameDataLoadOptions
                {
                    dataIndexPath = "Data/DataIndex.json",
                    mode = GameDataBuildMode.Development
                });

                Assert.IsFalse(result.isSuccess);
                Assert.IsTrue(result.report.Errors.Any(e => e.code == GameDataErrorCode.InvalidEnum));
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        static Dictionary<string, string> CreateValidDataSet()
        {
            return new Dictionary<string, string>
            {
                ["Data/DataIndex.json"] =
@"{
  ""schemaVersion"": 1,
  ""configs"": [""Data/duel.config.json"", ""Data/run.config.json"", ""Data/player.start.json""],
  ""clashes"": [""Data/clashes/clash.1.json""],
  ""actions"": [""Data/actions/action.1.json""],
  ""cards"": [""Data/cards/card.squad.1.json""],
  ""skills"": [],
  ""encounters"": [""Data/encounters/encounter.1.json""]
}",
                ["Data/duel.config.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""duel.config"",
  ""clashCount"": 1,
  ""focusMax"": 5,
  ""focusRegenPerTurn"": 2,
  ""cooldownTickPerTurn"": -1,
  ""attackResultMin"": 1,
  ""greatVictoryMultiplier"": 2,
  ""p0Rules"": {
    ""disallowBaseAttackMutation"": true,
    ""defaultSlotLimit"": null
  }
}",
                ["Data/run.config.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""run.config"",
  ""startingHonor"": 3,
  ""supplyLimit"": 5
}",
                ["Data/player.start.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""player.start"",
  ""startingHonor"": 3,
  ""startingFocus"": 5,
  ""startingPlayerHealth"": 10,
  ""startingSquadCardIds"": [""card.squad.1""]
}",
                ["Data/clashes/clash.1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""clash.1"",
  ""slotLimit"": null,
  ""tags"": [],
  ""nameLocKey"": ""clash.1_name"",
  ""descLocKey"": ""clash.1_desc"",
  ""outcomeEffects"": {
    ""GreatVictory"": [],
    ""Victory"": [],
    ""Draw"": [],
    ""Defeat"": [],
  ""GreatDefeat"": []
  }
}",
                ["Data/cards/card.squad.1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""card.squad.1"",
  ""type"": ""Squad"",
  ""supplyCost"": 1,
  ""nameLocKey"": ""card.squad.1_name"",
  ""descLocKey"": ""card.squad.1_desc"",
  ""duelStart"": {
    ""summonActions"": [ { ""actionId"": ""action.1"", ""count"": 1 } ],
    ""ops"": []
  }
}",
                ["Data/actions/action.1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""action.1"",
  ""attack"": 2,
  ""nameLocKey"": ""action.1_name"",
  ""descLocKey"": ""action.1_desc"",
  ""effects"": []
}",
                ["Data/encounters/encounter.1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""encounter.1"",
  ""opponentHealth"": 10,
  ""plans"": [
    { ""clashIndex"": 0, ""actions"": [ { ""actionId"": ""action.1"", ""count"": 1 } ] }
  ]
}"
            };
        }

        sealed class InMemoryGameDataSource : IGameDataSource
        {
            readonly Dictionary<string, string> files;

            public InMemoryGameDataSource(Dictionary<string, string> files)
            {
                this.files = files;
            }

            public bool Exists(string relativePath)
            {
                return files.ContainsKey(relativePath);
            }

            public bool TryReadText(string relativePath, out string json, out string errorMessage)
            {
                if (!files.TryGetValue(relativePath, out json!))
                {
                    json = string.Empty;
                    errorMessage = $"Not found: {relativePath}";
                    return false;
                }

                errorMessage = string.Empty;
                return true;
            }
        }
    }
}
