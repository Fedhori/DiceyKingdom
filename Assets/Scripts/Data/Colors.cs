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
    
    // ─────────────────────────────────────
    // Stat 하이라이트용 컬러들
    // PlayerStatIds / PinStatIds 기준
    // ─────────────────────────────────────

    // score: “점수 그 자체” → 부드러운 골드
    // 예: "+100 점"
    public static readonly Color StatScore = new Color32(0xFD, 0xE6, 0x8A, 0xFF); // #FDE68A

    // scoreMultiplier / pin scoreMultiplier: 점수 배율 → 더 강한 오렌지
    // 예: "점수 배율 +1", "점수 x3"
    public static readonly Color StatScoreMultiplier = new Color32(0xFD, 0xBA, 0x74, 0xFF); // #FDBA74

    // criticalChance: 크리티컬 확률 → 초록/행운 느낌
    // 예: "치명타 확률 +1%"
    public static readonly Color StatCriticalChance = new Color32(0x4A, 0xDE, 0x80, 0xFF); // #4ADE80

    // criticalMultiplier: 크리티컬 배율 → 크리티컬 계열과 맞는 핑크/레드 톤
    // 예: "치명타 배율 +0.1"
    public static readonly Color StatCriticalMultiplier = new Color32(0xFB, 0x71, 0x85, 0xFF); // #FB7185

    // 앞으로 자주 쓸 가능성이 높은 효과용(속도/크기)
    // AddVelocity / IncreaseSize와 매칭해두면 좋음.

    // 속도 계열: 시원한 하늘색
    // 예: "볼 속도 x1.5"
    public static readonly Color StatSpeed = new Color32(0x38, 0xBD, 0xF8, 0xFF); // #38BDF8

    // 크기 계열: 부드러운 보라/인디고
    // 예: "볼 크기 x1.5"
    public static readonly Color StatSize = new Color32(0xA5, 0xB4, 0xFC, 0xFF); // #A5B4FC

    public static Color GetCriticalColor(CriticalType criticalType)
    {
        switch (criticalType)
        {
            case CriticalType.None:
                return NormalScore;
            case CriticalType.Critical:
                return CritScore;
            case CriticalType.OverCritical:
                return OverCrit;
        }

        return NormalScore;
    }
}