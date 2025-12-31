using Data;
using UnityEngine;

public sealed class TokenShopItem : IShopItem
{
    public ShopItemType ItemType => ShopItemType.Item;
    public string Id => token?.id;
    public int Price { get; }
    public Sprite Icon { get; }
    public bool Sold { get; set; }

    public TokenInstance PreviewInstance { get; }

    readonly TokenDto token;

    public TokenShopItem(TokenDto dto)
    {
        token = dto ?? throw new System.ArgumentNullException(nameof(dto));
        PreviewInstance = new TokenInstance(dto);
        Price = token.price;
        Icon = SpriteCache.GetTokenSprite(token.id);
        Sold = false;
    }
}
