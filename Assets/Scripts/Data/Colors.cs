using UnityEngine;

public static class Colors
{
    public static readonly Color NormalScore = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    public static readonly Color CritScore = new Color32(0xFF, 0x8A, 0x80, 0xFF);
    public static readonly Color OverCrit = new Color32(0xFF, 0x17, 0x44, 0xFF);
    
    public static readonly Color Black = new Color32(0x00, 0x00, 0x00, 0xFF); // #FFFFFF
    
    public static readonly Color Common = new Color32(0xFF, 0xFF, 0xFF, 0xFF);     // #FFFFFF
    public static readonly Color Uncommon = new Color32(0x00, 0xFF, 0x66, 0xFF);   // #00FF66
    public static readonly Color Rare = new Color32(0x00, 0xC8, 0xFF, 0xFF);       // #00C8FF
    public static readonly Color Epic = new Color32(0xB3, 0x00, 0xFF, 0xFF);       // #B300FF
    public static readonly Color Legendary = new Color32(0xFF, 0xD4, 0x00, 0xFF);  // #FFD400
    
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