using Data;
using UnityEngine;

public static class Colors
{
    public static readonly Color DamageText = new Color32(0xE7, 0x4C, 0x3C, 0xFF); //#E74C3C
    
    public static readonly Color White = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    
    public static readonly Color Common    = new Color32(0x87, 0x96, 0xA8, 0xFF); // #8796A8 (cool light gray)
    public static readonly Color Uncommon  = new Color32(0x3A, 0xB9, 0x79, 0xFF); // #3AB979 (muted green)
    public static readonly Color Rare      = new Color32(0x43, 0x97, 0xD1, 0xFF); // #4397D1 (muted sky blue)
    
    public static readonly Color Item = new Color32(0xD1, 0x88, 0x3F, 0xFF); // #D1883F (muted orange)
    public static readonly Color Upgrade = new Color32(0x7E, 0x5A, 0xA6, 0xFF); // #7E5AA6
    
    public static readonly Color Invalid = new Color32(0xE7, 0x4C, 0x3C, 0xFF); //#E74C3C
    public static readonly Color Value = new Color32(0x34, 0xC7, 0x59, 0xFF); // #34C759  (더 또렷함, 약간 노란기) - 툴팁 수치 표기용으로 사용중
    
    public static readonly Color Power = new Color32(0xE7, 0x4C, 0x3C, 0xFF); //#E74C3C
    public static readonly Color Currency = new Color32(0xD0, 0xA8, 0x3A, 0xFF); // #D0A83A (muted gold)

    public static readonly Color DamageFlash = new Color32(0xFF, 0xFF, 0xFF, 0x66);
    public static readonly Color FreezeTint = new Color32(0x6F, 0xD8, 0xFF, 0x66);

    public static Color GetCriticalColor(int criticalLevel)
    {
        switch (criticalLevel)
        {
            case 0:
                return DamageText;
            case 1:
                return DamageText;
            default:
                return DamageText;
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
            default:
                return Common;
        }
    }
}
