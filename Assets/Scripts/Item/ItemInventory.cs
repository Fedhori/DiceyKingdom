using System;
using System.Collections.Generic;

public sealed class ItemInventory
{
    readonly ItemInstance[] slots;

    public int SlotCount => slots.Length;
    public IReadOnlyList<ItemInstance> Slots => slots;

    public enum SlotChangeType
    {
        Add,
        Remove,
        Move,
        Swap
    }

    public event Action<int, ItemInstance, ItemInstance, SlotChangeType> OnSlotChanged;
    public event Action OnInventoryChanged;

    public ItemInventory()
        : this(GameConfig.ItemSlotCount)
    {
    }

    public ItemInventory(int slotCount)
    {
        int count = Math.Max(0, slotCount);
        slots = new ItemInstance[count];
    }

    public void Clear()
    {
        bool changed = false;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            var previous = slots[i];
            slots[i] = null;
            NotifySlotChanged(i, previous, null, SlotChangeType.Remove);
            changed = true;
        }

        if (changed)
            NotifyInventoryChanged();
    }

    public ItemInstance GetSlot(int index)
    {
        return IsValidIndex(index) ? slots[index] : null;
    }

    public bool IsSlotEmpty(int index)
    {
        return IsValidIndex(index) && slots[index] == null;
    }

    public bool TryGetFirstEmptySlot(out int index)
    {
        index = -1;
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                index = i;
                return true;
            }
        }

        return false;
    }

    public bool TrySetSlot(int index, ItemInstance instance)
    {
        if (!IsValidIndex(index))
            return false;

        if (ReferenceEquals(slots[index], instance))
            return false;

        var previous = slots[index];
        slots[index] = instance;
        NotifySlotChanged(index, previous, instance, SlotChangeType.Add);
        NotifyInventoryChanged();
        return true;
    }

    public bool TryRemoveAt(int index, out ItemInstance removed)
    {
        removed = null;
        if (!IsValidIndex(index))
            return false;

        if (slots[index] == null)
            return false;

        removed = slots[index];
        slots[index] = null;
        NotifySlotChanged(index, removed, null, SlotChangeType.Remove);
        NotifyInventoryChanged();
        return true;
    }

    public bool TrySwap(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex))
            return false;

        if (fromIndex == toIndex)
            return false;

        var from = slots[fromIndex];
        var to = slots[toIndex];
        slots[fromIndex] = to;
        slots[toIndex] = from;
        NotifySlotChanged(fromIndex, from, to, SlotChangeType.Swap);
        NotifySlotChanged(toIndex, to, from, SlotChangeType.Swap);
        NotifyInventoryChanged();
        return true;
    }

    bool IsValidIndex(int index)
    {
        return index >= 0 && index < slots.Length;
    }

    void NotifySlotChanged(int index, ItemInstance previous, ItemInstance current, SlotChangeType changeType)
    {
        OnSlotChanged?.Invoke(index, previous, current, changeType);
    }

    void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
