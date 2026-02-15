using System;
using System.Collections.Generic;

public sealed class ObservableValue<T>
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

    public void PublishCurrent()
    {
        Publish(value);
    }

    void Publish(T current)
    {
        for (int i = 0; i < handlers.Count; i++)
        {
            handlers[i]?.Invoke(current);
        }
    }
}
