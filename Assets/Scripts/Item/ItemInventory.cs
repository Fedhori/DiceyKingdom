using System;

public sealed class ItemInventory
{
    readonly ItemInstance[] slots;

    public int SlotCount => slots.Length;
    public ItemInstance[] Slots => slots;

    public ItemInventory()
        : this(GameConfig.TokenSlotCount)
    {
    }

    public ItemInventory(int slotCount)
    {
        int count = Math.Max(0, slotCount);
        slots = new ItemInstance[count];
    }
}
