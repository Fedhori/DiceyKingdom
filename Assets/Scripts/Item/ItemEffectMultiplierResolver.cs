using Data;
using UnityEngine;

public static class ItemEffectMultiplierResolver
{
    public static double Resolve(
        ItemEffectDto dto,
        ItemInstance sourceItem,
        ItemInventory inventory = null,
        PlayerInstance player = null,
        bool useFirstEmptySlotForAdjacent = false)
    {
        if (dto == null || string.IsNullOrEmpty(dto.multiplier))
            return 1d;

        inventory ??= ItemManager.Instance?.Inventory;
        player ??= PlayerManager.Instance?.Current;

        switch (dto.multiplier)
        {
            case "normalItemCount":
                return GetNormalItemCount(inventory);
            case "currencyAtMost":
                return GetCurrencyAtMostMultiplier(player, dto.threshold);
            case "adjacentEmptySlotCount":
                return GetAdjacentEmptySlotCount(inventory, sourceItem, useFirstEmptySlotForAdjacent);
            case "weaponCount":
                return GetWeaponCount(inventory);
            default:
                Debug.LogWarning($"[ItemEffectMultiplierResolver] Unknown multiplier '{dto.multiplier}'.");
                return 1d;
        }
    }

    static int GetNormalItemCount(ItemInventory inventory)
    {
        if (inventory == null)
            return 0;

        int count = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var inst = inventory.GetSlot(i);
            if (inst == null)
                continue;

            if (inst.Rarity == ItemRarity.Common)
                count++;
        }

        return count;
    }

    static int GetWeaponCount(ItemInventory inventory)
    {
        if (inventory == null)
            return 0;

        int count = 0;
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var inst = inventory.GetSlot(i);
            if (inst == null)
                continue;

            if (inst.IsWeapon())
                count++;
        }

        return count;
    }

    static double GetCurrencyAtMostMultiplier(PlayerInstance player, int threshold)
    {
        if (player == null)
            return 0d;

        if (threshold < 0)
        {
            Debug.LogWarning($"[ItemEffectMultiplierResolver] currencyAtMost threshold < 0: {threshold}");
            return 0d;
        }

        return player.Currency <= threshold ? 1d : 0d;
    }

    static int GetAdjacentEmptySlotCount(ItemInventory inventory, ItemInstance sourceItem, bool useFirstEmptySlot)
    {
        if (inventory == null)
            return 0;

        int sourceIndex = -1;
        if (sourceItem != null)
            sourceIndex = FindItemIndex(inventory, sourceItem);
        else if (useFirstEmptySlot && inventory.TryGetFirstEmptySlot(out var emptyIndex))
            sourceIndex = emptyIndex;

        if (sourceIndex < 0)
            return 0;

        int slotsPerRow = Mathf.Max(1, GameConfig.ItemSlotsPerRow);
        int count = 0;

        if (sourceIndex % slotsPerRow != 0)
        {
            int left = sourceIndex - 1;
            if (left >= 0 && inventory.IsSlotEmpty(left))
                count++;
        }

        if ((sourceIndex + 1) % slotsPerRow != 0)
        {
            int right = sourceIndex + 1;
            if (right < inventory.SlotCount && inventory.IsSlotEmpty(right))
                count++;
        }

        int up = sourceIndex - slotsPerRow;
        if (up >= 0 && inventory.IsSlotEmpty(up))
            count++;

        int down = sourceIndex + slotsPerRow;
        if (down < inventory.SlotCount && inventory.IsSlotEmpty(down))
            count++;

        return count;
    }

    static int FindItemIndex(ItemInventory inventory, ItemInstance item)
    {
        if (inventory == null || item == null)
            return -1;

        for (int i = 0; i < inventory.SlotCount; i++)
        {
            if (ReferenceEquals(inventory.GetSlot(i), item))
                return i;
        }

        return -1;
    }
}
