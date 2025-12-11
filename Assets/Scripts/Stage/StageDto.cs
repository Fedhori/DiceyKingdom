// StageData.cs
using System;
using System.Collections.Generic;

[Serializable]
public sealed class StageDto
{
    public string id;
    public double needScore;
    public int roundCount = 3;
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

        IsInitialized = stages.Count > 0;
    }

    public static void Initialize(IEnumerable<StageDto> items)
    {
        stages.Clear();

        if (items != null)
            stages.AddRange(items);

        IsInitialized = stages.Count > 0;
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

    public static bool TryGetById(string id, out StageDto dto)
    {
        dto = null;

        if (!IsInitialized || string.IsNullOrEmpty(id))
            return false;

        for (int i = 0; i < stages.Count; i++)
        {
            var s = stages[i];
            if (s != null && s.id == id)
            {
                dto = s;
                return true;
            }
        }

        return false;
    }

    public static int Count => stages.Count;
}