using Data;
using UnityEngine;

public sealed class PinShopItem : IShopItem
{
    public ShopItemType ItemType => ShopItemType.Pin;
    public string Id => pin?.id;
    public int Price { get; }
    public Sprite Icon { get; }
    public bool Sold { get; set; }

    public PinInstance PreviewInstance { get; }

    readonly PinDto pin;

    public PinShopItem(PinDto dto)
    {
        pin = dto ?? throw new System.ArgumentNullException(nameof(dto));
        PreviewInstance = new PinInstance(dto, -1, -1, registerEventEffects: false);
        Price = PreviewInstance.Price;
        Icon = SpriteCache.GetPinSprite(pin.id);
        Sold = false;
    }
}
