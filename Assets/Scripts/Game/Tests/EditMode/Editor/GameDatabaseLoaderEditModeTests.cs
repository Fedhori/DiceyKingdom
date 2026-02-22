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
            Assert.NotNull(result.database.battleConfig);
            Assert.NotNull(result.database.runConfig);
            Assert.NotNull(result.database.playerStart);
            Assert.AreEqual(3, result.database.battlefieldsById.Count);
            Assert.Greater(result.database.troopsById.Count, 0);
        }

        [Test]
        public void Load_FailsWhenTroopReferenceIsMissing()
        {
            Dictionary<string, string> files = CreateValidDataSet();
            files["Data/encounters/enc_1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""enc_1"",
  ""enemyMorale"": 10,
  ""plans"": [
    { ""battlefieldIndex"": 0, ""troops"": [ { ""troopId"": ""missing_troop"", ""count"": 1 } ] }
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
            files["Data/troops/troop_1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""troop_1"",
  ""attack"": 2,
  ""nameLocKey"": ""troop_1_name"",
  ""descLocKey"": ""troop_1_desc"",
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
            files["Data/troops/troop_1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""troop_1"",
  ""attack"": 2,
  ""nameLocKey"": ""troop_1_name"",
  ""descLocKey"": ""troop_1_desc"",
  ""effects"": [
    {
      ""timing"": ""TurnEnd"",
      ""condition"": { ""type"": ""IsInCamp"" },
      ""ops"": [
        { ""op"": ""AddAttackModifier"", ""target"": ""Attack"", ""layer"": ""battle"", ""mode"": ""Add"", ""value"": 1 }
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
  ""configs"": [""Data/battle_config.json"", ""Data/run_config.json"", ""Data/player_start.json""],
  ""battlefields"": [""Data/battlefields/bf_1.json""],
  ""troops"": [""Data/troops/troop_1.json""],
  ""cards"": [""Data/cards/card_squad_1.json""],
  ""skills"": [],
  ""encounters"": [""Data/encounters/enc_1.json""]
}",
                ["Data/battle_config.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""battle_config"",
  ""battlefieldCount"": 1,
  ""manaMax"": 5,
  ""manaRegenPerTurn"": 2,
  ""cooldownTickPerTurn"": -1,
  ""attackResultMin"": 1,
  ""greatVictoryMultiplier"": 2,
  ""p0Rules"": {
    ""disallowBaseAttackMutation"": true,
    ""defaultSlotLimit"": null
  }
}",
                ["Data/run_config.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""run_config"",
  ""startingStability"": 3,
  ""supplyLimit"": 5
}",
                ["Data/player_start.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""player_start"",
  ""startingStability"": 3,
  ""startingMana"": 5,
  ""startingPlayerMorale"": 10,
  ""startingSquadCardIds"": [""card_squad_1""]
}",
                ["Data/battlefields/bf_1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""bf_1"",
  ""slotLimit"": null,
  ""tags"": [],
  ""nameLocKey"": ""bf_1_name"",
  ""descLocKey"": ""bf_1_desc"",
  ""outcomeEffects"": {
    ""GreatVictory"": [],
    ""Victory"": [],
    ""Draw"": [],
    ""Defeat"": [],
  ""GreatDefeat"": []
  }
}",
                ["Data/cards/card_squad_1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""card_squad_1"",
  ""type"": ""Squad"",
  ""supplyCost"": 1,
  ""nameLocKey"": ""card_squad_1_name"",
  ""descLocKey"": ""card_squad_1_desc"",
  ""battleStart"": {
    ""summonTroops"": [ { ""troopId"": ""troop_1"", ""count"": 1 } ],
    ""ops"": []
  }
}",
                ["Data/troops/troop_1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""troop_1"",
  ""attack"": 2,
  ""nameLocKey"": ""troop_1_name"",
  ""descLocKey"": ""troop_1_desc"",
  ""effects"": []
}",
                ["Data/encounters/enc_1.json"] =
@"{
  ""schemaVersion"": 1,
  ""id"": ""enc_1"",
  ""enemyMorale"": 10,
  ""plans"": [
    { ""battlefieldIndex"": 0, ""troops"": [ { ""troopId"": ""troop_1"", ""count"": 1 } ] }
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
