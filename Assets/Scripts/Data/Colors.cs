using Data;
using UnityEngine;

public static class Colors
{
    public static readonly Color Normal = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly Color Critical = new Color32(0xFF, 0x8A, 0x80, 0xFF);
    public static readonly Color OverCritical = new Color32(0xFF, 0x17, 0x44, 0xFF); //#FF1744
    
    public static readonly Color White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly Color Black = new Color32(0x00, 0x00, 0x00, 0xFF);
    
    public static readonly Color Common    = new Color32(0x87, 0x96, 0xA8, 0xFF); // #8796A8 (cool light gray)
    public static readonly Color Uncommon  = new Color32(0x3A, 0xB9, 0x79, 0xFF); // #3AB979 (muted green)
    public static readonly Color Rare      = new Color32(0x43, 0x97, 0xD1, 0xFF); // #4397D1 (muted sky blue)
    public static readonly Color Legendary = new Color32(0xD0, 0xA8, 0x3A, 0xFF); // #D0A83A (muted gold)
    
    public static readonly Color Upgrade = new Color32(0x7E, 0x5A, 0xA6, 0xFF); // #7E5AA6
    
    public static readonly Color Red = new Color32(0xE7, 0x4C, 0x3C, 0xFF); //#E74C3C
    public static readonly Color Blue = new Color32(0x64, 0x94, 0xFF, 0xFF); //#6494FF
    public static readonly Color Green = new Color32(0x27, 0xAE, 0x60, 0xFF);

    public static readonly Color DamageFlash = new Color32(0xFF, 0xFF, 0xFF, 0x66);
    public static readonly Color FreezeTint = new Color32(0x6F, 0xD8, 0xFF, 0x66);
    
    public static readonly Color HighlightColor = Color.cyan;

    public static Color GetCriticalColor(int criticalLevel)
    {
        switch (criticalLevel)
        {
            case 0:
                return OverCritical;
            case 1:
                return OverCritical;
            default:
                return OverCritical;
        }
    }

    public static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common:
                return Common;
            case ItemRarity.Uncommon:
                return Uncommon;
            case ItemRarity.Rare:
                return Rare;
            case ItemRarity.Legendary:
                return Legendary;
            default:
                return Common;
        }
    }
}
