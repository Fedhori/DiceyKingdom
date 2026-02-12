using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GameStaticDataValidationMenu
{
    const string MenuPath = "Tools/Game/Validate Static Data";

    [MenuItem(MenuPath)]
    public static void ValidateStaticData()
    {
        try
        {
            var dataSet = GameStaticDataLoader.LoadAll(logWarningsOnce: false);
            var warnings = GameStaticDataLoader.CollectAdventurerLocalizationWarnings(dataSet.adventurerDefs);
            LogValidationResult(warnings);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[GameStaticDataValidationMenu] Validation failed: {exception.Message}");
            throw;
        }
    }

    static void LogValidationResult(IReadOnlyList<string> warnings)
    {
        if (warnings == null || warnings.Count == 0)
        {
            Debug.Log("[GameStaticDataValidationMenu] Validation passed with no warnings.");
            return;
        }

        Debug.LogWarning(
            "[GameStaticDataValidationMenu] Validation warnings\n- " +
            string.Join("\n- ", warnings));
    }
}
