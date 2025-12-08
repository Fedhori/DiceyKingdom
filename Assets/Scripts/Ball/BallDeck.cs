using System;
using System.Collections.Generic;

public sealed class BallDeck
{
    readonly Dictionary<string, int> counts = new();

    public IReadOnlyDictionary<string, int> Counts => counts;

    // 파라미터 없는 Event
    public event Action OnDeckChanged;

    public void Set(string ballId, int count)
    {
        if (string.IsNullOrEmpty(ballId))
            return;

        count = Math.Max(0, count);

        var had = counts.TryGetValue(ballId, out var prev);

        if (count == 0)
        {
            // 기존에 있었는데 제거되는 경우만 이벤트
            if (had)
            {
                counts.Remove(ballId);
                OnDeckChanged?.Invoke();
            }
            return;
        }

        // 새로 추가되거나, 값이 달라질 때만 이벤트
        if (!had || prev != count)
        {
            counts[ballId] = count;
            OnDeckChanged?.Invoke();
        }
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
