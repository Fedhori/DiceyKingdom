using System;
using System.Collections.Generic;

public sealed class BallDeck
{
    readonly Dictionary<string, int> counts = new();

    public IReadOnlyDictionary<string, int> Counts => counts;

    public void Set(string ballId, int count)
    {
        if (string.IsNullOrEmpty(ballId))
            return;

        count = Math.Max(0, count);

        if (count == 0)
        {
            counts.Remove(ballId);
            return;
        }

        counts[ballId] = count;
    }

    public void Add(string ballId, int delta)
    {
        if (string.IsNullOrEmpty(ballId))
            return;

        if (!counts.TryGetValue(ballId, out var current))
            current = 0;

        var next = current + delta;
        next = Math.Max(0, next);

        if (next == 0)
            counts.Remove(ballId);
        else
            counts[ballId] = next;
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