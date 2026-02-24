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
            Assert.Greater(result.database.abilitiesById.Count, 0);
            Assert.Greater(result.database.enemiesById.Count, 0);
        }

        [Test]
        public void Load_FailsWhenEnemyAbilityReferenceIsMissing()
        {
            Dictionary<string, string> files = CreateValidDataSet();
            files["Data/enemies/enemy.1.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""enemy.1"",
  ""health"": 10,
  ""abilityLoadout"": [
    { ""abilityId"": ""ability.missing"", ""count"": 1 }
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
            files["Data/abilities/ability.1.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""ability.1"",
  ""type"": ""Attack"",
  ""buildCost"": 0,
  ""cooldown"": 0,
  ""power"": 2,
  ""nameLocKey"": ""ability.1_name"",
  ""descLocKey"": ""ability.1_desc"",
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
            files["Data/abilities/ability.1.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""ability.1"",
  ""type"": ""Attack"",
  ""buildCost"": 0,
  ""cooldown"": 0,
  ""power"": 2,
  ""nameLocKey"": ""ability.1_name"",
  ""descLocKey"": ""ability.1_desc"",
  ""effects"": [
    {
      ""timing"": ""TurnEnd"",
      ""condition"": { ""type"": ""IsInLoadout"" },
      ""ops"": [
        { ""op"": ""AddPowerModifier"", ""target"": ""Power"", ""layer"": ""duel"", ""mode"": ""Add"", ""value"": 1 }
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
  ""schemaVersion"": 2,
  ""configs"": [""Data/duel.config.json"", ""Data/run.config.json"", ""Data/player.start.json""],
  ""abilities"": [""Data/abilities/ability.1.json""],
  ""enemies"": [""Data/enemies/enemy.1.json""]
}",
                ["Data/duel.config.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""duel.config"",
  ""cooldownTickPerTurn"": -1,
  ""powerResultMin"": 1,
  ""p0Rules"": {
    ""disallowBasePowerMutation"": true,
    ""defaultSlotLimit"": null
  }
}",
                ["Data/run.config.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""run.config"",
  ""startingHonor"": 3,
  ""capacity"": 5
}",
                ["Data/player.start.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""player.start"",
  ""startingHonor"": 3,
  ""startingPlayerHealth"": 10,
  ""startingLoadoutAbilityIds"": [""ability.1""]
}",
                ["Data/abilities/ability.1.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""ability.1"",
  ""type"": ""Attack"",
  ""buildCost"": 0,
  ""cooldown"": 0,
  ""power"": 2,
  ""nameLocKey"": ""ability.1_name"",
  ""descLocKey"": ""ability.1_desc"",
  ""effects"": []
}",
                ["Data/enemies/enemy.1.json"] =
@"{
  ""schemaVersion"": 2,
  ""id"": ""enemy.1"",
  ""health"": 10,
  ""abilityLoadout"": [
    { ""abilityId"": ""ability.1"", ""count"": 1 }
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
                if (!files.TryGetValue(relativePath, out json))
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
