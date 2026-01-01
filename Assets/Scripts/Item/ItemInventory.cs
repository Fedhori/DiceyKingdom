using System;
using System.Collections.Generic;

public sealed class ItemInventory
{
    readonly ItemInstance[] slots;

    public int SlotCount => slots.Length;
    public IReadOnlyList<ItemInstance> Slots => slots;

    public event Action<int, ItemInstance, ItemInstance> OnSlotChanged;
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
            NotifySlotChanged(i, previous, null);
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
        NotifySlotChanged(index, previous, instance);
        NotifyInventoryChanged();
        return true;
    }

    public bool TryAdd(ItemInstance instance, out int index)
    {
        index = -1;
        if (instance == null)
            return false;

        if (!TryGetFirstEmptySlot(out index))
            return false;

        slots[index] = instance;
        NotifySlotChanged(index, null, instance);
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
        NotifySlotChanged(index, removed, null);
        NotifyInventoryChanged();
        return true;
    }

    public bool TryMove(int fromIndex, int toIndex)
    {
        if (!IsValidIndex(fromIndex) || !IsValidIndex(toIndex))
            return false;

        if (fromIndex == toIndex)
            return false;

        if (slots[fromIndex] == null || slots[toIndex] != null)
            return false;

        var moving = slots[fromIndex];
        slots[toIndex] = moving;
        slots[fromIndex] = null;
        NotifySlotChanged(fromIndex, moving, null);
        NotifySlotChanged(toIndex, null, moving);
        NotifyInventoryChanged();
        return true;
    }

    public bool TrySwap(int indexA, int indexB)
    {
        if (!IsValidIndex(indexA) || !IsValidIndex(indexB))
            return false;

        if (indexA == indexB)
            return false;

        var a = slots[indexA];
        var b = slots[indexB];
        slots[indexA] = b;
        slots[indexB] = a;
        NotifySlotChanged(indexA, a, b);
        NotifySlotChanged(indexB, b, a);
        NotifyInventoryChanged();
        return true;
    }

    bool IsValidIndex(int index)
    {
        return index >= 0 && index < slots.Length;
    }

    void NotifySlotChanged(int index, ItemInstance previous, ItemInstance current)
    {
        OnSlotChanged?.Invoke(index, previous, current);
    }

    void NotifyInventoryChanged()
    {
        OnInventoryChanged?.Invoke();
    }
}
