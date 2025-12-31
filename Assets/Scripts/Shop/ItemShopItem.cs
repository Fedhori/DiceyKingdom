using System;
using Data;
using UnityEngine;

public sealed class ItemShopItem : IShopItem
{
    public ShopItemType ItemType => ShopItemType.Item;
    public string Id => item?.id;
    public int Price { get; }
    public Sprite Icon { get; }
    public bool Sold { get; set; }

    public ItemInstance PreviewInstance { get; }

    readonly ItemDto item;

    public ItemShopItem(ItemDto dto)
    {
        item = dto ?? throw new ArgumentNullException(nameof(dto));
        PreviewInstance = new ItemInstance(dto);
        Price = item.price;
        Icon = SpriteCache.GetItemSprite(item.id);
        Sold = false;
    }
}
