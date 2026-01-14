// StageData.cs
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class StageDto
{
    public int index = -1;
    public double difficulty = 100.0;
    public float spawnSecond = 15f;
}

[Serializable]
public sealed class StageRoot
{
    public List<StageDto> stages;
}

/// <summary>
/// Stage JSON 파싱은 네가 알아서 하고,
/// 파싱된 StageRoot 또는 IEnumerable<StageDto>를 여기 Initialize로 넘긴다고 가정.
/// </summary>
public static class StageRepository
{
    static readonly List<StageDto> stages = new();
    public static bool IsInitialized { get; private set; }

    public static IReadOnlyList<StageDto> All => stages;

    public static void Initialize(StageRoot root)
    {
        stages.Clear();

        if (root?.stages != null)
            stages.AddRange(root.stages);

        FinalizeInitialization();
    }

    public static void Initialize(IEnumerable<StageDto> items)
    {
        stages.Clear();

        if (items != null)
            stages.AddRange(items);

        FinalizeInitialization();
    }

    static void FinalizeInitialization()
    {
        if (!ValidateAndSortStages(stages))
        {
            stages.Clear();
            IsInitialized = false;
            return;
        }

        IsInitialized = stages.Count > 0;
    }

    static bool ValidateAndSortStages(List<StageDto> list)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogError("[StageRepository] Stage list is empty.");
            return false;
        }

        list.Sort((a, b) =>
        {
            int aIndex = a != null ? a.index : int.MaxValue;
            int bIndex = b != null ? b.index : int.MaxValue;
            return aIndex.CompareTo(bIndex);
        });

        for (int i = 0; i < list.Count; i++)
        {
            var s = list[i];
            if (s == null)
            {
                Debug.LogError($"[StageRepository] Stage at position {i} is null.");
                return false;
            }

            if (s.index != i)
            {
                Debug.LogError($"[StageRepository] Stage index must be contiguous starting at 0. Expected {i} but got {s.index}.");
                return false;
            }
        }

        return true;
    }

    public static bool TryGetByIndex(int index, out StageDto dto)
    {
        dto = null;

        if (!IsInitialized)
            return false;

        if (index < 0 || index >= stages.Count)
            return false;

        dto = stages[index];
        return dto != null;
    }

    public static int Count => stages.Count;
}
