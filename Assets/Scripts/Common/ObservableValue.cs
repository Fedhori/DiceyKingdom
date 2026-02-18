using System;
using System.Collections.Generic;

public interface IReadOnlyObservableValue<T>
{
    T Value { get; }
    IDisposable Subscribe(Action<T> handler, bool pushCurrent = true);
    void PublishCurrent();
}




public sealed class ObservableValue<T> : IReadOnlyObservableValue<T>
{
    readonly List<Action<T>> handlers = new();
    T value;

    public ObservableValue()
    {
        value = default;
    }

    public ObservableValue(T initialValue)
    {
        value = initialValue;
    }

    public T Value
    {
        get => value;
        set
        {
            if (EqualityComparer<T>.Default.Equals(this.value, value))
                return;

            this.value = value;
            Publish(this.value);
        }
    }

    public IDisposable Subscribe(Action<T> handler, bool pushCurrent = true)
    {
        if (handler == null)
            return DisposableToken.Empty;

        handlers.Remove(handler);
        handlers.Add(handler);

        if (pushCurrent)
            handler.Invoke(value);

        return DisposableToken.Create(() => handlers.Remove(handler));
    }

    public void ClearListeners()
    {
        handlers.Clear();
    }

    public void PublishCurrent()
    {
        Publish(value);
    }

    void Publish(T current)
    {
        if (handlers.Count == 0)
            return;

        Action<T>[] snapshot = handlers.ToArray();
        for (int i = 0; i < snapshot.Length; i++)
        {
            snapshot[i]?.Invoke(current);
        }
    }
}

