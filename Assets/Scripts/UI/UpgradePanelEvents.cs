using System;

public static class UpgradePanelEvents
{
    public static event Action<ItemInstance> OnToggleRequested;

    public static void RaiseToggleRequested(ItemInstance item)
    {
        OnToggleRequested?.Invoke(item);
    }
}
