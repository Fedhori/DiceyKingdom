using System;
using System.Collections.Generic;

public sealed class DisposableBag : IDisposable
{
    readonly List<IDisposable> tokens = new();

    public void Add(IDisposable token)
    {
        if (token == null)
            return;

        tokens.Add(token);
    }

    public void Clear()
    {
        for (int i = tokens.Count - 1; i >= 0; i--)
        {
            tokens[i]?.Dispose();
        }

        tokens.Clear();
    }

    public void Dispose()
    {
        Clear();
    }
}
