using Data;
using UnityEngine;

public static class Colors
{
    public static readonly Color32 White = new Color32(0xFF, 0xFF, 0xFF, 0xFF); // #FFFFFF

    // Neutral / UI Depth (Dark tone)
    public static readonly Color32 UiBackdrop        = new Color32(0x3A, 0x44, 0x54, 0xFF); // #3A4454
    public static readonly Color32 UiPanelBg         = new Color32(0x33, 0x3D, 0x4A, 0xFF); // #333D4A
    public static readonly Color32 UiPanelHeaderBg   = new Color32(0x2A, 0x33, 0x3F, 0xFF); // #2A333F
    public static readonly Color32 UiPanelBorder     = new Color32(0x5A, 0x66, 0x78, 0xFF); // #5A6678
    public static readonly Color32 UiShadow          = new Color32(0x2E, 0x36, 0x44, 0xFF); // #2E3644

    public static readonly Color32 UiCardBackground  = new Color32(0x42, 0x4C, 0x5C, 0xFF); // #424C5C
    public static readonly Color32 UiLabelText       = new Color32(0xCE, 0xD6, 0xDE, 0xFF); // #CED6DE

    public static readonly Color32 UiTextOnDark      = new Color32(0xF9, 0xFB, 0xFD, 0xFF); // #F9FBFD
    public static readonly Color32 UiTextOnDarkMuted = new Color32(0xCE, 0xD6, 0xDE, 0xFF); // #CED6DE
    public static readonly Color32 UiTextOnLight     = new Color32(0x1F, 0x26, 0x30, 0xFF); // #1F2630

    public static readonly Color32 UiDimOverlay      = new Color32(0x00, 0x00, 0x00, 0x4B); // #000000

    // Buttons
    public static readonly Color32 UiButtonBg          = new Color32(0xE7, 0xEC, 0xF2, 0xFF); // #E7ECF2
    public static readonly Color32 UiButtonText        = new Color32(0x1F, 0x26, 0x30, 0xFF); // #1F2630

    public static readonly Color32 PrimaryAction       = new Color32(0x3D, 0x7D, 0xFF, 0xFF); // #3D7DFF
    public static readonly Color32 PrimaryActionText   = new Color32(0xF9, 0xFB, 0xFD, 0xFF); // #F9FBFD

    public static readonly Color32 Destructive         = new Color32(0xC0, 0x39, 0x2B, 0xFF); // #C0392B
    public static readonly Color32 DestructiveText     = new Color32(0xF9, 0xFB, 0xFD, 0xFF); // #F9FBFD
    
    public static readonly Color32 DataBarDamage = new Color32(0xAD, 0x41, 0x3C, 0xFF); // #AD413C
    public static readonly Color32 DataTrackBg   = new Color32(0x2A, 0x33, 0x3F, 0xFF); // #2A333F

    // HUD / Stage label
    public static readonly Color32 StageText           = new Color32(0x9A, 0xB2, 0xF5, 0xFF); // #9AB2F5

    // Rarity (existing) + Header Fill (new)
    public static readonly Color32 Common    = new Color32(0x87, 0x96, 0xA8, 0xFF); // #8796A8
    public static readonly Color32 Uncommon  = new Color32(0x3A, 0xB9, 0x79, 0xFF); // #3AB979
    public static readonly Color32 Rare      = new Color32(0x43, 0x97, 0xD1, 0xFF); // #4397D1

    // Type / System
    public static readonly Color32 Item     = new Color32(0x2A, 0xB0, 0xA3, 0xFF); // #2AB0A3
    public static readonly Color32 Upgrade  = new Color32(0x7E, 0x5A, 0xA6, 0xFF); // #7E5AA6

    // Semantic numbers
    public static readonly Color32 Value    = new Color32(0x34, 0xC7, 0x59, 0xFF); // #34C759
    public static readonly Color32 Currency = new Color32(0xD0, 0xA8, 0x3A, 0xFF); // #D0A83A

    // Combat
    public static readonly Color32 Power        = new Color32(0xD1, 0x88, 0x3F, 0xFF); // #D1883F
    public static readonly Color32 DamageText   = new Color32(0xE0, 0x9A, 0x3A, 0xFF); // #E09A3A
    public static readonly Color32 Critical     = new Color32(0xE7, 0x4C, 0x3C, 0xFF); // #E74C3C
    public static readonly Color32 Invalid      = new Color32(0xC0, 0x39, 0x2B, 0xFF); // #C0392B

    public static readonly Color32 DamageFlash  = new Color32(0xFF, 0xFF, 0xFF, 0x66); // #FFFFFF
    public static readonly Color32 FreezeTint   = new Color32(0x6F, 0xD8, 0xFF, 0x66); // #6FD8FF

    // Keyword colors (you confirmed only these two)
    public static readonly Color32 FreezeKeyword    = new Color32(0x6F, 0xD8, 0xFF, 0xFF); // #6FD8FF
    public static readonly Color32 ExplosionKeyword = new Color32(0xFF, 0x4D, 0x6D, 0xFF); // #FF4D6D

    public static Color GetCriticalColor(int criticalLevel)
    {
        return criticalLevel <= 0 ? DamageText : Critical;
    }

    public static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return Common;
            case ItemRarity.Uncommon: return Uncommon;
            case ItemRarity.Rare: return Rare;
            default: return Common;
        }
    }
}
