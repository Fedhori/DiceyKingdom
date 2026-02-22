using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Game.Infrastructure.Data
{
    public static class GameDataFallbackFactory
    {
        const string FallbackSourcePath = "fallback";

        public static GameDatabase CreateSafeFallbackDatabase()
        {
            var database = new GameDatabase();

            database.battleConfig = Deserialize<BattleConfigDef>(
@"{
  ""schemaVersion"": 1,
  ""id"": ""battle_config"",
  ""battlefieldCount"": 3,
  ""manaMax"": 5,
  ""manaRegenPerTurn"": 2,
  ""cooldownTickPerTurn"": -1,
  ""attackResultMin"": 1,
  ""greatVictoryMultiplier"": 2,
  ""p0Rules"": {
    ""disallowBaseAttackMutation"": true,
    ""defaultSlotLimit"": null
  }
}");
            database.battleConfigSourcePath = FallbackSourcePath;

            database.runConfig = Deserialize<RunConfigDef>(
@"{
  ""schemaVersion"": 1,
  ""id"": ""run_config"",
  ""startingStability"": 3,
  ""supplyLimit"": 5
}");
            database.runConfigSourcePath = FallbackSourcePath;

            database.playerStart = Deserialize<PlayerStartDef>(
@"{
  ""schemaVersion"": 1,
  ""id"": ""player_start"",
  ""startingStability"": 3,
  ""startingMana"": 5,
  ""startingPlayerMorale"": 10,
  ""startingSquadCardIds"": []
}");
            database.playerStartSourcePath = FallbackSourcePath;

            AddFallbackBattlefields(database);
            return database;
        }

        static void AddFallbackBattlefields(GameDatabase database)
        {
            for (int i = 0; i < 3; i++)
            {
                string id = $"fallback_bf_{i}";
                BattlefieldDef def = Deserialize<BattlefieldDef>(
$@"{{
  ""schemaVersion"": 1,
  ""id"": ""{id}"",
  ""slotLimit"": null,
  ""tags"": [],
  ""nameLocKey"": ""{id}_name"",
  ""descLocKey"": ""{id}_desc"",
  ""outcomeEffects"": {{
    ""GreatVictory"": [],
    ""Victory"": [],
    ""Draw"": [],
    ""Defeat"": [],
    ""GreatDefeat"": []
  }}
}}");

                database.battlefieldsById[id] = def;
                database.battlefieldSourcePathById[id] = FallbackSourcePath;
            }
        }

        static TDef Deserialize<TDef>(string json)
            where TDef : class
        {
            TDef parsed = JsonConvert.DeserializeObject<TDef>(json);
            if (parsed == null)
            {
                throw new InvalidOperationException($"Failed to deserialize fallback type '{typeof(TDef).Name}'.");
            }

            return parsed;
        }
    }
}
