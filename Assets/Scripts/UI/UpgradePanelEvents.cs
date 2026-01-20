using System;

public static class UpgradePanelEvents
{
    public static event Action<ItemInstance> OnToggleRequested;
    public static event Action OnTooltipDismissRequested;

    public static void RaiseToggleRequested(ItemInstance item)
    {
        OnToggleRequested?.Invoke(item);
    }

    public static void RaiseTooltipDismissRequested()
    {
        OnTooltipDismissRequested?.Invoke();
    }
}
