using System;
using System.Threading.Tasks;
using Game.Data;
using Game.Infrastructure.Data;
using Game.Save;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.App
{
    [DefaultExecutionOrder(-20000)]
    public sealed class Bootstrap : MonoBehaviour
    {
        [SerializeField] GameObject managersRoot;

        enum BootstrapStage
        {
            ConfigureFrameRate = 0,
            DeactivateManagersRoot = 1,
            InitializeStreamingCache = 2,
            LoadGameConfig = 3,
            LoadGameData = 4,
            ActivateManagersRoot = 5,
            ResolveGameApp = 6,
            RebuildServices = 7,
            SyncWebGlSave = 8,
            LoadStartScene = 9
        }

        enum BootstrapFailurePolicy
        {
            Continue = 0,
            Stop = 1
        }

        async void Awake()
        {
            try
            {
                if (!RunStage(
                        BootstrapStage.ConfigureFrameRate,
                        BootstrapFailurePolicy.Continue,
                        () =>
                        {
                            UnityEngine.Application.targetFrameRate = 60;
                            return true;
                        }))
                {
                    return;
                }

                if (!RunStage(
                        BootstrapStage.DeactivateManagersRoot,
                        BootstrapFailurePolicy.Continue,
                        () =>
                        {
                            if (managersRoot != null && managersRoot.activeSelf)
                            {
                                managersRoot.SetActive(false);
                            }

                            return true;
                        }))
                {
                    return;
                }

                if (!await RunStageAsync(
                        BootstrapStage.InitializeStreamingCache,
                        BootstrapFailurePolicy.Continue,
                        async () =>
                        {
                            await SaCache.InitAsync(BuildSaOptions());
                            return true;
                        }))
                {
                    return;
                }

                if (!await RunStageAsync(
                        BootstrapStage.LoadGameConfig,
                        BootstrapFailurePolicy.Stop,
                        () => GameConfigProvider.LoadFromStreamingAssetsAsync()))
                {
                    return;
                }

                if (!RunLoadGameDataStage())
                {
                    return;
                }

                if (!RunStage(
                        BootstrapStage.ActivateManagersRoot,
                        BootstrapFailurePolicy.Stop,
                        () =>
                        {
                            if (managersRoot == null)
                            {
                                return false;
                            }

                            managersRoot.SetActive(true);
                            return true;
                        },
                        "managersRoot is missing."))
                {
                    return;
                }

                await Task.Yield();

                GameApp gameApp = null;
                if (!RunStage(
                        BootstrapStage.ResolveGameApp,
                        BootstrapFailurePolicy.Stop,
                        () =>
                        {
                            gameApp = managersRoot.GetComponentInChildren<GameApp>(true);
                            return gameApp != null;
                        },
                        "GameApp is missing under managersRoot."))
                {
                    return;
                }

                if (!RunStage(
                        BootstrapStage.RebuildServices,
                        BootstrapFailurePolicy.Stop,
                        () =>
                        {
                            gameApp.RebuildServices();
                            return true;
                        }))
                {
                    return;
                }

                if (!await RunStageAsync(
                        BootstrapStage.SyncWebGlSave,
                        BootstrapFailurePolicy.Continue,
                        () => SaveWebGlSync.SyncFromPersistentAsync()))
                {
                    return;
                }

                if (!await RunStageAsync(
                        BootstrapStage.LoadStartScene,
                        BootstrapFailurePolicy.Stop,
                        async () =>
                        {
                            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(SceneIds.TemplateStartScene);
                            if (loadOperation == null)
                            {
                                return false;
                            }

                            await loadOperation.AsTask();
                            return true;
                        },
                        () => $"failed to load scene({SceneIds.TemplateStartScene})."))
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bootstrap] Bootstrap failed with unhandled exception.\n{ex}");
            }
        }

        bool RunLoadGameDataStage()
        {
            GameDataBuildMode dataMode = ResolveDataMode();
            GameDataBuildResult dataResult = null;

            return RunStage(
                BootstrapStage.LoadGameData,
                BootstrapFailurePolicy.Stop,
                () =>
                {
                    dataResult = GameDataRuntime.LoadAtStartup(dataMode);
                    return dataResult != null && !dataResult.shouldBlockStartup;
                },
                () =>
                {
                    int errorCount = dataResult?.report == null ? -1 : dataResult.report.ErrorCount;
                    return $"game data validation blocked startup. mode={dataMode}, errors={errorCount}";
                });
        }

        static GameDataBuildMode ResolveDataMode()
        {
            return Debug.isDebugBuild
                ? GameDataBuildMode.Development
                : GameDataBuildMode.Release;
        }

        static SaOptions BuildSaOptions()
        {
            return new SaOptions
            {
                forceRefresh = Debug.isDebugBuild,
                refreshIfAppVersionChanged = true,
                verifyHash = true
            };
        }

        bool RunStage(
            BootstrapStage stage,
            BootstrapFailurePolicy policy,
            Func<bool> action,
            string failureReason = null)
        {
            return RunStage(stage, policy, action, () => failureReason);
        }

        bool RunStage(
            BootstrapStage stage,
            BootstrapFailurePolicy policy,
            Func<bool> action,
            Func<string> failureReasonFactory)
        {
            LogStageStart(stage, policy);

            try
            {
                bool success = action != null && action.Invoke();
                if (success)
                {
                    LogStageSuccess(stage);
                    return true;
                }

                string reason = failureReasonFactory?.Invoke();
                return HandleStageFailure(stage, policy, reason);
            }
            catch (Exception ex)
            {
                return HandleStageFailure(stage, policy, ex.Message, ex);
            }
        }

        Task<bool> RunStageAsync(
            BootstrapStage stage,
            BootstrapFailurePolicy policy,
            Func<Task<bool>> action,
            string failureReason = null)
        {
            return RunStageAsync(stage, policy, action, () => failureReason);
        }

        async Task<bool> RunStageAsync(
            BootstrapStage stage,
            BootstrapFailurePolicy policy,
            Func<Task<bool>> action,
            Func<string> failureReasonFactory)
        {
            LogStageStart(stage, policy);

            try
            {
                bool success = action != null && await action.Invoke();
                if (success)
                {
                    LogStageSuccess(stage);
                    return true;
                }

                string reason = failureReasonFactory?.Invoke();
                return HandleStageFailure(stage, policy, reason);
            }
            catch (Exception ex)
            {
                return HandleStageFailure(stage, policy, ex.Message, ex);
            }
        }

        static bool HandleStageFailure(
            BootstrapStage stage,
            BootstrapFailurePolicy policy,
            string reason,
            Exception exception = null)
        {
            string normalizedReason = string.IsNullOrWhiteSpace(reason)
                ? "no failure detail provided."
                : reason;
            string message = $"[Bootstrap][{stage}] FAILED | policy={policy} | reason={normalizedReason}";
            if (exception != null)
            {
                message += $"\n{exception}";
            }

            if (policy == BootstrapFailurePolicy.Stop)
            {
                Debug.LogError(message);
                return false;
            }

            Debug.LogWarning(message);
            return true;
        }

        static void LogStageStart(BootstrapStage stage, BootstrapFailurePolicy policy)
        {
            Debug.Log($"[Bootstrap][{stage}] START | policy={policy}");
        }

        static void LogStageSuccess(BootstrapStage stage)
        {
            Debug.Log($"[Bootstrap][{stage}] OK");
        }
    }

    public static class AsyncOperationExt
    {
        public static async Task AsTask(this AsyncOperation op)
        {
            while (!op.isDone)
            {
                await Task.Yield();
            }
        }
    }
}
