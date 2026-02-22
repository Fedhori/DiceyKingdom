using System;
using UnityEngine;

namespace Game.Infrastructure.Data
{
    public static class GameDataRuntime
    {
        static GameDataBuildResult currentBuildResult = CreateInitialResult();
        static bool isUsingFallback;

        public static GameDataBuildResult CurrentBuildResult => currentBuildResult;
        public static GameDatabase CurrentDatabase => currentBuildResult.database;
        public static bool IsUsingFallback => isUsingFallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            currentBuildResult = CreateInitialResult();
            isUsingFallback = false;
        }

        public static GameDataBuildResult LoadAtStartup(GameDataBuildMode mode)
        {
            return LoadAtStartup(mode, () => GameDatabaseLoader.LoadDefault(mode));
        }

        public static GameDataBuildResult LoadAtStartup(GameDataBuildMode mode, Func<GameDataBuildResult> loadFunc)
        {
            if (loadFunc == null)
            {
                throw new ArgumentNullException(nameof(loadFunc));
            }

            GameDataBuildResult loaded = loadFunc.Invoke();
            if (loaded == null)
            {
                throw new InvalidOperationException("Game data load function returned null result.");
            }

            EnsureResultObjects(loaded);

            if (loaded.isSuccess)
            {
                isUsingFallback = false;
                currentBuildResult = loaded;
                return currentBuildResult;
            }

            if (mode == GameDataBuildMode.Development)
            {
                isUsingFallback = false;
                currentBuildResult = loaded;
                return currentBuildResult;
            }

            var fallbackResult = new GameDataBuildResult
            {
                isSuccess = false,
                shouldBlockStartup = false,
                database = GameDataFallbackFactory.CreateSafeFallbackDatabase(),
                report = loaded.report
            };

            currentBuildResult = fallbackResult;
            isUsingFallback = true;
            Debug.LogWarning(
                $"[GameDataRuntime] Release fallback is active. validationErrors={loaded.report.ErrorCount}");

            return currentBuildResult;
        }

        static GameDataBuildResult CreateInitialResult()
        {
            return new GameDataBuildResult
            {
                isSuccess = false,
                shouldBlockStartup = false,
                database = GameDataFallbackFactory.CreateSafeFallbackDatabase(),
                report = new GameDataValidationReport()
            };
        }

        static void EnsureResultObjects(GameDataBuildResult result)
        {
            if (result.database == null)
            {
                result.database = new GameDatabase();
            }

            if (result.report == null)
            {
                result.report = new GameDataValidationReport();
            }
        }
    }
}
