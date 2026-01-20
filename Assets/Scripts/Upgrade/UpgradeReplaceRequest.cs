using System;

public sealed class UpgradeReplaceRequest
{
    public ItemInstance TargetItem { get; }
    public UpgradeInstance PendingUpgrade { get; }
    public int TargetSlotIndex { get; }

    readonly Func<UpgradeInstance, bool> confirmHandler;
    readonly Action cancelHandler;

    public UpgradeReplaceRequest(
        ItemInstance targetItem,
        UpgradeInstance pendingUpgrade,
        int targetSlotIndex,
        Func<UpgradeInstance, bool> confirmHandler,
        Action cancelHandler)
    {
        TargetItem = targetItem;
        PendingUpgrade = pendingUpgrade;
        TargetSlotIndex = targetSlotIndex;
        this.confirmHandler = confirmHandler;
        this.cancelHandler = cancelHandler;
    }

    public bool Confirm(UpgradeInstance existingUpgrade)
    {
        return confirmHandler != null && confirmHandler(existingUpgrade);
    }

    public void Cancel()
    {
        cancelHandler?.Invoke();
    }
}
