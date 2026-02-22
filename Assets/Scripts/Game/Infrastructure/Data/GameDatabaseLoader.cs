using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Game.Infrastructure.Data
{
    public sealed class GameDatabaseLoader
    {
        readonly IGameDataSource dataSource;
        readonly GameDataValidator validator = new();
        readonly JsonSerializerSettings strictJsonSettings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Error
        };
        readonly JsonSerializerSettings relaxedJsonSettings = new()
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        public GameDatabaseLoader(IGameDataSource dataSource)
        {
            this.dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        }

        public static GameDataBuildResult LoadDefault(GameDataBuildMode mode = GameDataBuildMode.Development)
        {
            var loader = new GameDatabaseLoader(new SaCacheGameDataSource());
            return loader.Load(new GameDataLoadOptions
            {
                mode = mode,
                dataIndexPath = GameDataConstants.DefaultDataIndexPath
            });
        }

        public GameDataBuildResult Load(GameDataLoadOptions options = null)
        {
            options ??= new GameDataLoadOptions();

            var report = new GameDataValidationReport();
            var database = new GameDatabase();

            if (!TryReadJson(options.dataIndexPath, report, string.Empty, out string dataIndexJson))
            {
                return BuildFailure(database, report, options.mode);
            }

            DataIndexDef dataIndex = ParseStrict<DataIndexDef>(dataIndexJson, options.dataIndexPath, string.Empty, report);
            if (dataIndex == null)
            {
                return BuildFailure(database, report, options.mode);
            }

            ValidateSchemaVersion(dataIndex.schemaVersion, options.dataIndexPath, "data_index", report);

            ParseConfigs(database, dataIndex.configs, report);
            ParseDefCollection(dataIndex.battlefields, database.battlefieldsById, database.battlefieldSourcePathById, report);
            ParseDefCollection(dataIndex.troops, database.troopsById, database.troopSourcePathById, report);
            ParseDefCollection(dataIndex.cards, database.cardsById, database.cardSourcePathById, report);
            ParseDefCollection(dataIndex.skills, database.skillsById, database.skillSourcePathById, report);
            ParseDefCollection(dataIndex.encounters, database.encountersById, database.encounterSourcePathById, report);

            validator.Validate(database, dataIndex, report);

            if (report.HasErrors)
            {
                return BuildFailure(database, report, options.mode);
            }

            return new GameDataBuildResult
            {
                isSuccess = true,
                shouldBlockStartup = false,
                database = database,
                report = report
            };
        }

        void ParseConfigs(GameDatabase database, IReadOnlyList<string> configPaths, GameDataValidationReport report)
        {
            for (int i = 0; i < configPaths.Count; i++)
            {
                string path = configPaths[i];
                if (!TryReadJson(path, report, string.Empty, out string json))
                {
                    continue;
                }

                ConfigHeaderDef header = Parse<ConfigHeaderDef>(json, path, string.Empty, report, relaxedJsonSettings);
                if (header == null)
                {
                    continue;
                }

                ValidateSchemaVersion(header.schemaVersion, path, header.id, report);

                if (string.Equals(header.id, "battle_config", StringComparison.Ordinal))
                {
                    if (database.battleConfig != null)
                    {
                        report.AddError(
                            GameDataErrorCode.DuplicateId,
                            path,
                            header.id,
                            "Duplicate battle_config is not allowed.");
                        continue;
                    }

                    BattleConfigDef battleConfig = ParseStrict<BattleConfigDef>(json, path, header.id, report);
                    if (battleConfig == null)
                    {
                        continue;
                    }

                    ValidateSchemaVersion(battleConfig.schemaVersion, path, battleConfig.id, report);
                    database.battleConfig = battleConfig;
                    database.battleConfigSourcePath = path;
                    continue;
                }

                if (string.Equals(header.id, "run_config", StringComparison.Ordinal))
                {
                    if (database.runConfig != null)
                    {
                        report.AddError(
                            GameDataErrorCode.DuplicateId,
                            path,
                            header.id,
                            "Duplicate run_config is not allowed.");
                        continue;
                    }

                    RunConfigDef runConfig = ParseStrict<RunConfigDef>(json, path, header.id, report);
                    if (runConfig == null)
                    {
                        continue;
                    }

                    ValidateSchemaVersion(runConfig.schemaVersion, path, runConfig.id, report);
                    database.runConfig = runConfig;
                    database.runConfigSourcePath = path;
                    continue;
                }

                report.AddError(
                    GameDataErrorCode.InvalidValue,
                    path,
                    header.id,
                    $"Unknown config id '{header.id}'. Allowed: battle_config, run_config.");
            }
        }

        void ParseDefCollection<TDef>(
            IReadOnlyList<string> paths,
            Dictionary<string, TDef> defsById,
            Dictionary<string, string> sourcePathById,
            GameDataValidationReport report)
            where TDef : class, IGameDef
        {
            for (int i = 0; i < paths.Count; i++)
            {
                string path = paths[i];
                if (!TryReadJson(path, report, string.Empty, out string json))
                {
                    continue;
                }

                TDef def = ParseStrict<TDef>(json, path, string.Empty, report);
                if (def == null)
                {
                    continue;
                }

                ValidateSchemaVersion(def.schemaVersion, path, def.id, report);

                if (string.IsNullOrWhiteSpace(def.id))
                {
                    report.AddError(
                        GameDataErrorCode.InvalidValue,
                        path,
                        string.Empty,
                        "id must not be empty.");
                    continue;
                }

                if (defsById.ContainsKey(def.id))
                {
                    report.AddError(
                        GameDataErrorCode.DuplicateId,
                        path,
                        def.id,
                        $"Duplicate id '{def.id}' is not allowed.");
                    continue;
                }

                defsById.Add(def.id, def);
                sourcePathById[def.id] = path;
            }
        }

        bool TryReadJson(string path, GameDataValidationReport report, string id, out string json)
        {
            json = string.Empty;

            if (!dataSource.Exists(path))
            {
                report.AddError(
                    GameDataErrorCode.MissingFile,
                    path,
                    id,
                    $"File does not exist: {path}");
                return false;
            }

            if (!dataSource.TryReadText(path, out json, out string errorMessage))
            {
                report.AddError(
                    GameDataErrorCode.MissingFile,
                    path,
                    id,
                    $"Failed to read file: {errorMessage}");
                return false;
            }

            return true;
        }

        TDef ParseStrict<TDef>(string json, string path, string id, GameDataValidationReport report)
            where TDef : class
        {
            return Parse<TDef>(json, path, id, report, strictJsonSettings);
        }

        TDef Parse<TDef>(
            string json,
            string path,
            string id,
            GameDataValidationReport report,
            JsonSerializerSettings settings)
            where TDef : class
        {
            try
            {
                TDef def = JsonConvert.DeserializeObject<TDef>(json, settings);
                if (def == null)
                {
                    report.AddError(
                        GameDataErrorCode.ParseError,
                        path,
                        id,
                        $"Failed to parse {typeof(TDef).Name}: null result.");
                }

                return def;
            }
            catch (Exception exception)
            {
                report.AddError(
                    GameDataErrorCode.ParseError,
                    path,
                    id,
                    $"Failed to parse {typeof(TDef).Name}: {exception.Message}");
                return null;
            }
        }

        static void ValidateSchemaVersion(int schemaVersion, string path, string id, GameDataValidationReport report)
        {
            if (schemaVersion == GameDataConstants.CurrentSchemaVersion)
            {
                return;
            }

            report.AddError(
                GameDataErrorCode.InvalidSchemaVersion,
                path,
                id,
                $"schemaVersion({schemaVersion}) is not supported. Expected {GameDataConstants.CurrentSchemaVersion}.");
        }

        static GameDataBuildResult BuildFailure(
            GameDatabase database,
            GameDataValidationReport report,
            GameDataBuildMode mode)
        {
            report.LogErrorsToConsole();

            bool shouldBlock = mode == GameDataBuildMode.Development;
            if (shouldBlock)
            {
                Debug.LogError("[validate_data] Validation failed in Development mode. Startup should be blocked.");
            }
            else
            {
                Debug.LogWarning("[validate_data] Validation failed in Release mode. Startup should continue with safe fallback.");
            }

            return new GameDataBuildResult
            {
                isSuccess = false,
                shouldBlockStartup = shouldBlock,
                database = database,
                report = report
            };
        }
    }
}
