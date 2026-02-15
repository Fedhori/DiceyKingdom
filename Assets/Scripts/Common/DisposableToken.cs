using System;

public static class DisposableToken
{
    sealed class ActionDisposable : IDisposable
    {
        Action onDispose;

        public ActionDisposable(Action onDisposeAction)
        {
            onDispose = onDisposeAction;
        }

        public void Dispose()
        {
            var action = onDispose;
            if (action == null)
                return;

            onDispose = null;
            action.Invoke();
        }
    }

    sealed class EmptyDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }

    static readonly IDisposable empty = new EmptyDisposable();

    public static IDisposable Empty => empty;

    public static IDisposable Create(Action onDispose)
    {
        if (onDispose == null)
            return Empty;

        return new ActionDisposable(onDispose);
    }
}
