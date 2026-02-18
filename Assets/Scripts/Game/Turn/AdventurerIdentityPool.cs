using System;
using System.Collections.Generic;
using UnityEngine;

public static class AdventurerIdentityPool
{
    const string NamesResourcePath = "GameData/adventurer_names_ko";
    static readonly HashSet<string> loggedErrors = new(StringComparer.Ordinal);
    static string[] cachedNames = Array.Empty<string>();
    static bool namesLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic()
    {
        loggedErrors.Clear();
        cachedNames = Array.Empty<string>();
        namesLoaded = false;
    }

    public static void AssignIdentity(RunState runState, AdventurerInstance adventurer)
    {
        if (adventurer == null)
            return;

        adventurer.displayName = PickDisplayName(runState);
        adventurer.portraitIndex = PickPortraitIndex(runState);
    }

    static string PickDisplayName(RunState runState)
    {
        EnsureNamesLoaded();
        if (cachedNames.Length <= 0)
        {
            LogErrorOnce("name_pool_missing", "[AdventurerIdentity] Name pool is empty. Using fallback name.");
            return "모험가";
        }

        var used = new HashSet<string>(StringComparer.Ordinal);
        CollectUsedNames(runState, used);

        var availableIndices = new List<int>(cachedNames.Length);
        for (int i = 0; i < cachedNames.Length; i++)
        {
            string candidate = cachedNames[i];
            if (string.IsNullOrWhiteSpace(candidate))
                continue;

            if (used.Contains(candidate))
                continue;

            availableIndices.Add(i);
        }

        if (availableIndices.Count <= 0)
        {
            LogErrorOnce("name_pool_exhausted", "[AdventurerIdentity] Name pool exhausted. Using index 0.");
            return cachedNames[0];
        }

        int index = availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
        return cachedNames[index];
    }

    static int PickPortraitIndex(RunState runState)
    {
        int portraitCount = AdventurerPortraitCatalog.GetPortraitCount();
        if (portraitCount <= 0)
        {
            LogErrorOnce("portrait_pool_missing", "[AdventurerIdentity] Portrait pool is empty. Using index 0.");
            return 0;
        }

        var usedIndices = new HashSet<int>();
        CollectUsedPortraitIndices(runState, usedIndices, portraitCount);

        var availableIndices = new List<int>(portraitCount);
        for (int i = 0; i < portraitCount; i++)
        {
            if (usedIndices.Contains(i))
                continue;

            availableIndices.Add(i);
        }

        if (availableIndices.Count <= 0)
        {
            LogErrorOnce("portrait_pool_exhausted", "[AdventurerIdentity] Portrait pool exhausted. Using index 0.");
            return 0;
        }

        return availableIndices[UnityEngine.Random.Range(0, availableIndices.Count)];
    }

    static void EnsureNamesLoaded()
    {
        if (namesLoaded)
            return;

        namesLoaded = true;
        TextAsset namesAsset = Resources.Load<TextAsset>(NamesResourcePath);
        if (namesAsset == null)
        {
            LogErrorOnce("name_asset_missing", $"[AdventurerIdentity] Missing TextAsset at Resources/{NamesResourcePath}.txt");
            cachedNames = Array.Empty<string>();
            return;
        }

        string[] lines = namesAsset.text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        var normalized = new List<string>(lines.Length);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < lines.Length; i++)
        {
            string entry = lines[i]?.Trim();
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            if (!seen.Add(entry))
                continue;

            normalized.Add(entry);
        }

        cachedNames = normalized.ToArray();
        if (cachedNames.Length <= 0)
            LogErrorOnce("name_asset_empty", $"[AdventurerIdentity] Name TextAsset is empty: Resources/{NamesResourcePath}.txt");
    }

    static void CollectUsedNames(RunState runState, ISet<string> used)
    {
        if (used == null || runState == null)
            return;

        CollectUsedNames(runState.candidates, used);
        CollectUsedNames(runState.adventurers, used);
        CollectUsedNames(runState.graveyard, used);
    }

    static void CollectUsedNames(IReadOnlyList<AdventurerInstance> adventurers, ISet<string> used)
    {
        if (adventurers == null || used == null)
            return;

        for (int i = 0; i < adventurers.Count; i++)
        {
            AdventurerInstance adventurer = adventurers[i];
            if (adventurer == null || string.IsNullOrWhiteSpace(adventurer.displayName))
                continue;

            used.Add(adventurer.displayName.Trim());
        }
    }

    static void CollectUsedPortraitIndices(RunState runState, ISet<int> used, int portraitCount)
    {
        if (used == null || runState == null || portraitCount <= 0)
            return;

        CollectUsedPortraitIndices(runState.candidates, used, portraitCount);
        CollectUsedPortraitIndices(runState.adventurers, used, portraitCount);
        CollectUsedPortraitIndices(runState.graveyard, used, portraitCount);
    }

    static void CollectUsedPortraitIndices(IReadOnlyList<AdventurerInstance> adventurers, ISet<int> used, int portraitCount)
    {
        if (adventurers == null || used == null || portraitCount <= 0)
            return;

        for (int i = 0; i < adventurers.Count; i++)
        {
            AdventurerInstance adventurer = adventurers[i];
            if (adventurer == null)
                continue;

            if (adventurer.portraitIndex < 0 || adventurer.portraitIndex >= portraitCount)
                continue;

            used.Add(adventurer.portraitIndex);
        }
    }

    static void LogErrorOnce(string key, string message)
    {
        if (!loggedErrors.Add(key))
            return;

        Debug.LogError(message);
    }
}
