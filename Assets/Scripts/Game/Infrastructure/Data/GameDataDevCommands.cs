using System;
using UnityEngine;

namespace Game.Infrastructure.Data
{
    public static class GameDataDevCommands
    {
        static bool isRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            isRegistered = false;
        }

        public static bool TryRegister(Action<string, Action<string[]>> registerCommand)
        {
            if (isRegistered)
            {
                return true;
            }

            if (registerCommand == null)
            {
                return false;
            }

            try
            {
                registerCommand.Invoke("validate_data", OnValidateDataCommand);
                isRegistered = true;
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
