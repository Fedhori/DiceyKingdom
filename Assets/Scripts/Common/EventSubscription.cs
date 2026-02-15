using System;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Wraps Unity event registration into IDisposable tokens for deterministic unsubscription.
/// </summary>
public static class EventSubscription
{
    public static IDisposable Create(Action subscribe, Action unsubscribe)
    {
        subscribe?.Invoke();
        return DisposableToken.Create(unsubscribe);
    }

    public static IDisposable Subscribe(Button button, UnityAction listener)
    {
        if (button == null || listener == null)
            return DisposableToken.Empty;

        button.onClick.RemoveListener(listener);
        button.onClick.AddListener(listener);
        return DisposableToken.Create(() => button.onClick.RemoveListener(listener));
    }

    public static IDisposable Subscribe(Slider slider, UnityAction<float> listener)
    {
        if (slider == null || listener == null)
            return DisposableToken.Empty;

        slider.onValueChanged.RemoveListener(listener);
        slider.onValueChanged.AddListener(listener);
        return DisposableToken.Create(() => slider.onValueChanged.RemoveListener(listener));
    }
}

