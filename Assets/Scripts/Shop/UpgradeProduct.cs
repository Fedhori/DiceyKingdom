using System;
using Data;
using UnityEngine;

public sealed class UpgradeProduct : IProduct
{
    public ProductType ProductType => ProductType.Upgrade;
    public string Id => upgrade?.id;
    public int Price { get; }
    public Sprite Icon { get; }
    public bool Sold { get; set; }

    public UpgradeInstance PreviewInstance { get; }

    readonly UpgradeDto upgrade;

    public UpgradeProduct(UpgradeDto dto)
    {
        upgrade = dto ?? throw new ArgumentNullException(nameof(dto));
        PreviewInstance = new UpgradeInstance(dto);
        Price = upgrade.price;
        Icon = SpriteCache.GetUpgradeSprite(upgrade.id);
        Sold = false;
    }
}
