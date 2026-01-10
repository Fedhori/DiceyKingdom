using System;

public static class UiSelectionEvents
{
    public static event Action OnSelectionCleared;

    public static void RaiseSelectionCleared()
    {
        OnSelectionCleared?.Invoke();
    }
}
