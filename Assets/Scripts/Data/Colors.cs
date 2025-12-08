using UnityEngine;

public static class Colors
{
    public static readonly Color NormalScore = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly Color CritScore = new Color32(0xFF, 0x8A, 0x80, 0xFF);
    public static readonly Color OverCrit = new Color32(0xFF, 0x17, 0x44, 0xFF);
    
    public static readonly Color Black = new Color32(0x00, 0x00, 0x00, 0xFF); // #FFFFFF
    public static readonly Color Common = new Color32(0xFF, 0xFF, 0xFF, 0xFF); // #FFFFFF
    public static readonly Color Rare = new Color32(0x3B, 0x82, 0xF6, 0xFF); // #3B82F6
    public static readonly Color RareLight = new Color32(0xB1, 0xCD, 0xFB, 0xFF); // #B1CDFB
    public static readonly Color Unique = new Color32(0xA8, 0x55, 0xF7, 0xFF); // #A855F7
    public static readonly Color UniqueLight = new Color32(0xD8, 0xB4, 0xFE, 0xFF); // #D8B4FE
    public static readonly Color Legendary = new Color32(0xF5, 0x9E, 0x0B, 0xFF); // #F59E0B
    public static readonly Color LegendaryLight = new Color32(0xFB, 0xD8, 0x9D, 0xFF); // #FBD89D
    
    public static readonly Color Red = new Color32(0xE7, 0x4C, 0x3C, 0xFF); // #E74C3C (붉은색)
    public static readonly Color Blue = new Color32(0x34, 0x98, 0xDB, 0xFF); // #3498DB (파란색)
    public static readonly Color Green = new Color32(0x27, 0xAE, 0x60, 0xFF); // #27AE60 (초록색)

    public static readonly Color EnemyRed = new Color32(0xB3, 0x2A, 0x2A, 0xFF);
    
    public static readonly Color HighlightColor = Color.cyan;

    public static Color GetCriticalColor(int criticalLevel)
    {
        switch (criticalLevel)
        {
            case 0:
                return NormalScore;
            case 1:
                return CritScore;
            default:
                return OverCrit;
        }
    }
}