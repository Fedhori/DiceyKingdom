using System;

/// <summary>
/// Factory for lightweight IDisposable tokens used to unregister callbacks.
/// </summary>
public static class DisposableToken
{
    /// <summary>
    /// Core class that defines action disposable responsibilities.
    /// </summary>
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

    /// <summary>
    /// Core class that defines empty disposable responsibilities.
    /// </summary>
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

