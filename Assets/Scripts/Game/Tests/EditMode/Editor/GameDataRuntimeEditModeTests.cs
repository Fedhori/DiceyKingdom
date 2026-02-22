using Game.Infrastructure.Data;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public sealed class GameDataRuntimeEditModeTests
    {
        [Test]
        public void LoadAtStartup_DevelopmentFailure_DoesNotUseFallback()
        {
            var failedResult = new GameDataBuildResult
            {
                isSuccess = false,
                shouldBlockStartup = true,
                database = new GameDatabase(),
                report = CreateReportWithSingleError()
            };

            GameDataBuildResult loaded = GameDataRuntime.LoadAtStartup(
                GameDataBuildMode.Development,
                () => failedResult);

            Assert.AreSame(failedResult, loaded);
            Assert.AreSame(failedResult, GameDataRuntime.CurrentBuildResult);
            Assert.IsFalse(GameDataRuntime.IsUsingFallback);
            Assert.IsTrue(loaded.shouldBlockStartup);
        }

        [Test]
        public void LoadAtStartup_ReleaseFailure_UsesFallbackDatabase()
        {
            var failedResult = new GameDataBuildResult
            {
                isSuccess = false,
                shouldBlockStartup = false,
                database = new GameDatabase(),
                report = CreateReportWithSingleError()
            };

            GameDataBuildResult loaded = GameDataRuntime.LoadAtStartup(
                GameDataBuildMode.Release,
                () => failedResult);

            Assert.IsTrue(GameDataRuntime.IsUsingFallback);
            Assert.AreSame(loaded, GameDataRuntime.CurrentBuildResult);
            Assert.IsFalse(loaded.shouldBlockStartup);
            Assert.IsNotNull(loaded.database.battleConfig);
            Assert.IsNotNull(loaded.database.runConfig);
            Assert.AreEqual(3, loaded.database.battlefieldsById.Count);
            Assert.AreEqual(1, loaded.report.ErrorCount);
        }

        [Test]
        public void LoadAtStartup_ReleaseSuccess_UsesLoadedResult()
        {
            var successResult = new GameDataBuildResult
            {
                isSuccess = true,
                shouldBlockStartup = false,
                database = new GameDatabase(),
                report = new GameDataValidationReport()
            };

            GameDataBuildResult loaded = GameDataRuntime.LoadAtStartup(
                GameDataBuildMode.Release,
                () => successResult);

            Assert.AreSame(successResult, loaded);
            Assert.AreSame(successResult, GameDataRuntime.CurrentBuildResult);
            Assert.IsFalse(GameDataRuntime.IsUsingFallback);
        }

        static GameDataValidationReport CreateReportWithSingleError()
        {
            var report = new GameDataValidationReport();
            report.AddError(
                GameDataErrorCode.ParseError,
                "Data/fail.json",
                "bad_def",
                "intentional failure");
            return report;
        }
    }
}
