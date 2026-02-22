#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Game.Infrastructure.Data
{
    public static class GameDataValidationMenu
    {
        [MenuItem("Tools/Validate Game Data")]
        static void Validate()
        {
            GameDataBuildResult result = GameDatabaseLoader.LoadDefault(GameDataBuildMode.Development);

            if (result.isSuccess)
            {
                Debug.Log("[validate_data] OK | errors=0 | source=Tools/Validate Game Data");
                return;
            }

            Debug.LogError($"[validate_data] FAILED | errors={result.report.ErrorCount} | source=Tools/Validate Game Data");
        }
    }
}
#endif
