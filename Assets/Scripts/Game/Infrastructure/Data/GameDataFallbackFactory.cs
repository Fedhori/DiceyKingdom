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

            database.duelConfig = Deserialize<DuelConfigDef>(
@"{
  ""schemaVersion"": 1,
  ""id"": ""duel.config"",
  ""clashCount"": 3,
  ""focusMax"": 5,
  ""focusRegenPerTurn"": 2,
  ""cooldownTickPerTurn"": -1,
  ""attackResultMin"": 1,
  ""p0Rules"": {
    ""disallowBaseAttackMutation"": true,
    ""defaultSlotLimit"": null
  }
}");
            database.duelConfigSourcePath = FallbackSourcePath;

            database.runConfig = Deserialize<RunConfigDef>(
@"{
  ""schemaVersion"": 1,
  ""id"": ""run.config"",
  ""startingHonor"": 3,
  ""supplyLimit"": 5
}");
            database.runConfigSourcePath = FallbackSourcePath;

            database.playerStart = Deserialize<PlayerStartDef>(
@"{
  ""schemaVersion"": 1,
  ""id"": ""player.start"",
  ""startingHonor"": 3,
  ""startingFocus"": 5,
  ""startingPlayerHealth"": 10,
  ""startingSquadCardIds"": []
}");
            database.playerStartSourcePath = FallbackSourcePath;

            AddFallbackClashes(database);
            return database;
        }

        static void AddFallbackClashes(GameDatabase database)
        {
            for (int i = 0; i < 3; i++)
            {
                string id = $"fallback.clash.{i}";
                ClashDef def = Deserialize<ClashDef>(
$@"{{
  ""schemaVersion"": 1,
  ""id"": ""{id}"",
  ""slotLimit"": null,
  ""damage"": 1,
  ""tags"": [],
  ""nameLocKey"": ""{id}_name"",
  ""descLocKey"": ""{id}_desc"",
  ""outcomeEffects"": {{
    ""Victory"": [],
    ""Draw"": [],
    ""Defeat"": []
  }}
}}");

                database.clashesById[id] = def;
                database.clashSourcePathById[id] = FallbackSourcePath;
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
