using System;
using System.Collections.Generic;

public sealed class BallDeck
{
    readonly Dictionary<string, int> counts = new();

    public IReadOnlyDictionary<string, int> Counts => counts;

    // 파라미터 없는 Event
    public event Action OnDeckChanged;

    public bool TryReplace(string ballId, int delta)
    {
        if (string.IsNullOrEmpty(ballId))
            return false;

        if (delta <= 0)
            return false;

        var basicId = GameConfig.BasicBallId;

        if (!counts.TryGetValue(basicId, out var basicCount) || basicCount < delta)
            return false;

        var newBasicCount = basicCount - delta;
        if (newBasicCount <= 0)
            counts.Remove(basicId);
        else
            counts[basicId] = newBasicCount;

        var targetCount = counts.GetValueOrDefault(ballId, 0);

        var newTargetCount = targetCount + delta;
        if (newTargetCount <= 0)
            counts.Remove(ballId);
        else
            counts[ballId] = newTargetCount;

        OnDeckChanged?.Invoke(); 

        return true;
    }


    public void Add(string ballId, int delta)
    {
        if (string.IsNullOrEmpty(ballId))
            return;

        if (!counts.TryGetValue(ballId, out var current))
            current = 0;

        var next = current + delta;
        next = Math.Max(0, next);

        // 값이 그대로면 아무 일도 안 함
        if (next == current)
            return;

        if (next == 0)
        {
            // 0이 되면 딕셔너리에서 제거
            if (counts.Remove(ballId))
                OnDeckChanged?.Invoke();
        }
        else
        {
            counts[ballId] = next;
            OnDeckChanged?.Invoke();
        }
    }

    public int GetCount(string ballId)
    {
        if (string.IsNullOrEmpty(ballId))
            return 0;

        return counts.TryGetValue(ballId, out var value) ? value : 0;
    }

    public int GetTotalCount()
    {
        var total = 0;
        foreach (var kv in counts)
        {
            if (kv.Value > 0)
                total += kv.Value;
        }
        return total;
    }

    public List<string> BuildSpawnSequence(System.Random rng)
    {
        var result = new List<string>();

        foreach (var kv in counts)
        {
            var id = kv.Key;
            var count = kv.Value;
            if (string.IsNullOrEmpty(id) || count <= 0)
                continue;

            for (int i = 0; i < count; i++)
                result.Add(id);
        }

        if (result.Count <= 1)
            return result;

        if (rng == null)
            rng = new System.Random();

        for (int i = result.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (result[i], result[j]) = (result[j], result[i]);
        }

        return result;
    }
}
