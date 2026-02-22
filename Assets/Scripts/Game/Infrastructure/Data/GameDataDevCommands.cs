using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Infrastructure.Data
{
    public static class GameDataDevCommands
    {
        static bool isRegistered;
        static bool isHooked;
        static readonly Type[] registerParameterTypes = { typeof(string), typeof(Action<string[]>) };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            TryRegister();

            if (isHooked)
            {
                return;
            }

            SceneManager.sceneLoaded += OnSceneLoaded;
            isHooked = true;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryRegister();
        }

        static void TryRegister()
        {
            if (isRegistered)
            {
                return;
            }

            if (!IsDevCommandReady())
            {
                return;
            }

            if (!TryRegisterCommand())
            {
                return;
            }

            isRegistered = true;

            if (isHooked)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                isHooked = false;
            }
        }

        static bool IsDevCommandReady()
        {
            Type gameAppType = Type.GetType("GameApp, Assembly-CSharp");
            if (gameAppType == null)
            {
                return false;
            }

            PropertyInfo instanceProperty = gameAppType.GetProperty("I", BindingFlags.Public | BindingFlags.Static);
            if (instanceProperty == null)
            {
                return false;
            }

            object gameAppInstance = instanceProperty.GetValue(null, null);
            if (gameAppInstance == null)
            {
                return false;
            }

            PropertyInfo appProperty = gameAppType.GetProperty("App", BindingFlags.Public | BindingFlags.Instance);
            if (appProperty == null)
            {
                return false;
            }

            object appServices = appProperty.GetValue(gameAppInstance, null);
            if (appServices == null)
            {
                return false;
            }

            PropertyInfo devCommandProperty = appServices.GetType().GetProperty("DevCommand", BindingFlags.Public | BindingFlags.Instance);
            if (devCommandProperty == null)
            {
                return false;
            }

            return devCommandProperty.GetValue(appServices, null) != null;
        }

        static bool TryRegisterCommand()
        {
            Type devCommandServiceType = Type.GetType("DevCommandService, Assembly-CSharp");
            if (devCommandServiceType == null)
            {
                return false;
            }

            MethodInfo registerMethod = devCommandServiceType.GetMethod(
                "Register",
                BindingFlags.Public | BindingFlags.Static,
                null,
                registerParameterTypes,
                null);

            if (registerMethod == null)
            {
                return false;
            }

            try
            {
                registerMethod.Invoke(null, new object[] { "validate_data", (Action<string[]>)OnValidateDataCommand });
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[validate_data] Failed to register dev command: {exception.Message}");
                return false;
            }
        }

        static void OnValidateDataCommand(string[] args)
        {
            GameDataBuildMode mode = GameDataBuildMode.Development;
            if (args != null && args.Length > 0 && string.Equals(args[0], "release", System.StringComparison.OrdinalIgnoreCase))
            {
                mode = GameDataBuildMode.Release;
            }

            GameDataBuildResult result = GameDataRuntime.LoadAtStartup(mode);

            if (result.isSuccess)
            {
                Debug.Log($"[validate_data] OK | errors=0 | mode={mode}");
                return;
            }

            string fallbackSuffix = GameDataRuntime.IsUsingFallback ? " | fallback=active" : string.Empty;
            Debug.LogWarning($"[validate_data] FAILED | errors={result.report.ErrorCount} | mode={mode}{fallbackSuffix}");
        }
    }
}
