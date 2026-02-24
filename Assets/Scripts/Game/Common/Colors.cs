using UnityEngine;

public static class Colors
{
    public static class Primitive
{
    // Scale convention: 950 (darkest) -> 050 (lightest)

    // ----------------------------
    // Neutrals (Dark Fantasy base)
    // ----------------------------

    // Abyss: cool, deep background blacks (not pure black)
    public static readonly Color32 Abyss950 = new Color32(0x0F, 0x11, 0x16, 0xFF);
    public static readonly Color32 Abyss900 = new Color32(0x11, 0x16, 0x22, 0xFF);
    public static readonly Color32 Abyss800 = new Color32(0x15, 0x1C, 0x2A, 0xFF);
    public static readonly Color32 Abyss700 = new Color32(0x1C, 0x26, 0x38, 0xFF);
    public static readonly Color32 Abyss600 = new Color32(0x25, 0x31, 0x4A, 0xFF);
    public static readonly Color32 Abyss500 = new Color32(0x32, 0x41, 0x64, 0xFF);
    public static readonly Color32 Abyss400 = new Color32(0x46, 0x5A, 0x84, 0xFF);
    public static readonly Color32 Abyss300 = new Color32(0x66, 0x7D, 0xA6, 0xFF);
    public static readonly Color32 Abyss200 = new Color32(0x95, 0xA6, 0xC2, 0xFF);
    public static readonly Color32 Abyss100 = new Color32(0xCF, 0xD6, 0xE3, 0xFF);
    public static readonly Color32 Abyss050 = new Color32(0xF1, 0xF4, 0xFA, 0xFF);

    // Slate: cold stone/steel surfaces, panels, borders
    public static readonly Color32 Slate950 = new Color32(0x0F, 0x13, 0x1C, 0xFF);
    public static readonly Color32 Slate900 = new Color32(0x14, 0x1B, 0x28, 0xFF);
    public static readonly Color32 Slate800 = new Color32(0x1A, 0x22, 0x32, 0xFF);
    public static readonly Color32 Slate700 = new Color32(0x26, 0x32, 0x48, 0xFF);
    public static readonly Color32 Slate600 = new Color32(0x34, 0x43, 0x5E, 0xFF);
    public static readonly Color32 Slate500 = new Color32(0x47, 0x5A, 0x7A, 0xFF);
    public static readonly Color32 Slate400 = new Color32(0x63, 0x7A, 0xA0, 0xFF);
    public static readonly Color32 Slate300 = new Color32(0x88, 0x9F, 0xBE, 0xFF);
    public static readonly Color32 Slate200 = new Color32(0xB7, 0xC6, 0xDC, 0xFF);
    public static readonly Color32 Slate100 = new Color32(0xDC, 0xE4, 0xF0, 0xFF);
    public static readonly Color32 Slate050 = new Color32(0xF3, 0xF6, 0xFB, 0xFF);

    // Mist: foggy cool grays for text/disabled/secondary UI
    public static readonly Color32 Mist950 = new Color32(0x0E, 0x10, 0x14, 0xFF);
    public static readonly Color32 Mist900 = new Color32(0x12, 0x16, 0x1D, 0xFF);
    public static readonly Color32 Mist800 = new Color32(0x19, 0x20, 0x2B, 0xFF);
    public static readonly Color32 Mist700 = new Color32(0x23, 0x2E, 0x3D, 0xFF);
    public static readonly Color32 Mist600 = new Color32(0x2F, 0x3E, 0x52, 0xFF);
    public static readonly Color32 Mist500 = new Color32(0x3F, 0x52, 0x6B, 0xFF);
    public static readonly Color32 Mist400 = new Color32(0x5B, 0x6F, 0x8C, 0xFF);
    public static readonly Color32 Mist300 = new Color32(0x9A, 0xA6, 0xBA, 0xFF);
    public static readonly Color32 Mist200 = new Color32(0xC3, 0xCB, 0xD9, 0xFF);
    public static readonly Color32 Mist100 = new Color32(0xE0, 0xE5, 0xEE, 0xFF);
    public static readonly Color32 Mist050 = new Color32(0xF4, 0xF6, 0xFA, 0xFF);

    // Bone: parchment / warm off-whites (avoid harsh pure white)
    public static readonly Color32 Bone950 = new Color32(0x14, 0x13, 0x11, 0xFF);
    public static readonly Color32 Bone900 = new Color32(0x20, 0x1E, 0x1B, 0xFF);
    public static readonly Color32 Bone800 = new Color32(0x2E, 0x2A, 0x25, 0xFF);
    public static readonly Color32 Bone700 = new Color32(0x3F, 0x38, 0x31, 0xFF);
    public static readonly Color32 Bone600 = new Color32(0x54, 0x4C, 0x41, 0xFF);
    public static readonly Color32 Bone500 = new Color32(0x6E, 0x65, 0x58, 0xFF);
    public static readonly Color32 Bone400 = new Color32(0x8F, 0x84, 0x76, 0xFF);
    public static readonly Color32 Bone300 = new Color32(0xB4, 0xAB, 0x9D, 0xFF);
    public static readonly Color32 Bone200 = new Color32(0xD2, 0xCB, 0xBE, 0xFF);
    public static readonly Color32 Bone100 = new Color32(0xE7, 0xE2, 0xD7, 0xFF);
    public static readonly Color32 Bone050 = new Color32(0xF4, 0xEF, 0xE4, 0xFF);

    // ----------------------------
    // Accents (Dark Fantasy)
    // ----------------------------

    // Moss: muted nature / poison / stamina
    public static readonly Color32 Moss950 = new Color32(0x08, 0x14, 0x0B, 0xFF);
    public static readonly Color32 Moss900 = new Color32(0x0E, 0x1F, 0x13, 0xFF);
    public static readonly Color32 Moss800 = new Color32(0x16, 0x2F, 0x1F, 0xFF);
    public static readonly Color32 Moss700 = new Color32(0x22, 0x48, 0x2F, 0xFF);
    public static readonly Color32 Moss600 = new Color32(0x32, 0x5A, 0x3A, 0xFF);
    public static readonly Color32 Moss500 = new Color32(0x4A, 0x7A, 0x53, 0xFF);
    public static readonly Color32 Moss400 = new Color32(0x6B, 0x9A, 0x74, 0xFF);
    public static readonly Color32 Moss300 = new Color32(0x8F, 0xB8, 0x98, 0xFF);
    public static readonly Color32 Moss200 = new Color32(0xB8, 0xD4, 0xBF, 0xFF);
    public static readonly Color32 Moss100 = new Color32(0xD9, 0xEB, 0xDD, 0xFF);
    public static readonly Color32 Moss050 = new Color32(0xF0, 0xF7, 0xF2, 0xFF);

    // Blood: damage / hostility / critical
    public static readonly Color32 Blood950 = new Color32(0x1A, 0x07, 0x07, 0xFF);
    public static readonly Color32 Blood900 = new Color32(0x2A, 0x0B, 0x0B, 0xFF);
    public static readonly Color32 Blood800 = new Color32(0x3F, 0x12, 0x12, 0xFF);
    public static readonly Color32 Blood700 = new Color32(0x5B, 0x1E, 0x1E, 0xFF);
    public static readonly Color32 Blood600 = new Color32(0x8D, 0x2D, 0x2D, 0xFF);
    public static readonly Color32 Blood500 = new Color32(0xB7, 0x42, 0x42, 0xFF);
    public static readonly Color32 Blood400 = new Color32(0xD1, 0x60, 0x60, 0xFF);
    public static readonly Color32 Blood300 = new Color32(0xE0, 0x8C, 0x8C, 0xFF);
    public static readonly Color32 Blood200 = new Color32(0xF0, 0xBA, 0xBA, 0xFF);
    public static readonly Color32 Blood100 = new Color32(0xF8, 0xDD, 0xDD, 0xFF);
    public static readonly Color32 Blood050 = new Color32(0xFD, 0xF2, 0xF2, 0xFF);

    // Ember: fire / aggression (more red-orange than Amber)
    public static readonly Color32 Ember950 = new Color32(0x23, 0x13, 0x08, 0xFF);
    public static readonly Color32 Ember900 = new Color32(0x31, 0x19, 0x0B, 0xFF);
    public static readonly Color32 Ember800 = new Color32(0x45, 0x22, 0x12, 0xFF);
    public static readonly Color32 Ember700 = new Color32(0x5E, 0x30, 0x19, 0xFF);
    public static readonly Color32 Ember600 = new Color32(0x7A, 0x3F, 0x22, 0xFF);
    public static readonly Color32 Ember500 = new Color32(0xA2, 0x5E, 0x2E, 0xFF);
    public static readonly Color32 Ember400 = new Color32(0xC7, 0x7A, 0x44, 0xFF);
    public static readonly Color32 Ember300 = new Color32(0xE3, 0x9D, 0x6B, 0xFF);
    public static readonly Color32 Ember200 = new Color32(0xF0, 0xC4, 0xA2, 0xFF);
    public static readonly Color32 Ember100 = new Color32(0xF8, 0xE2, 0xD0, 0xFF);
    public static readonly Color32 Ember050 = new Color32(0xFC, 0xF3, 0xEC, 0xFF);

    // Amber: warning / ritual gold (brighter than Ember, still not neon)
    public static readonly Color32 Amber950 = new Color32(0x1F, 0x16, 0x07, 0xFF);
    public static readonly Color32 Amber900 = new Color32(0x2D, 0x20, 0x0A, 0xFF);
    public static readonly Color32 Amber800 = new Color32(0x3F, 0x2D, 0x0F, 0xFF);
    public static readonly Color32 Amber700 = new Color32(0x57, 0x40, 0x15, 0xFF);
    public static readonly Color32 Amber600 = new Color32(0x76, 0x57, 0x1E, 0xFF);
    public static readonly Color32 Amber500 = new Color32(0xD0, 0x9A, 0x3B, 0xFF);
    public static readonly Color32 Amber400 = new Color32(0xE0, 0xB4, 0x5E, 0xFF);
    public static readonly Color32 Amber300 = new Color32(0xEF, 0xCB, 0x88, 0xFF);
    public static readonly Color32 Amber200 = new Color32(0xF6, 0xE1, 0xB8, 0xFF);
    public static readonly Color32 Amber100 = new Color32(0xFB, 0xF0, 0xDA, 0xFF);
    public static readonly Color32 Amber050 = new Color32(0xFE, 0xFA, 0xF1, 0xFF);

    // Gold: premium / reward (antique feel)
    public static readonly Color32 Gold950 = new Color32(0x1B, 0x16, 0x07, 0xFF);
    public static readonly Color32 Gold900 = new Color32(0x26, 0x1F, 0x0A, 0xFF);
    public static readonly Color32 Gold800 = new Color32(0x37, 0x2C, 0x0E, 0xFF);
    public static readonly Color32 Gold700 = new Color32(0x4B, 0x3C, 0x15, 0xFF);
    public static readonly Color32 Gold600 = new Color32(0x64, 0x50, 0x1E, 0xFF);
    public static readonly Color32 Gold500 = new Color32(0xC9, 0xA2, 0x4D, 0xFF);
    public static readonly Color32 Gold400 = new Color32(0xDC, 0xBC, 0x6C, 0xFF);
    public static readonly Color32 Gold300 = new Color32(0xEA, 0xD3, 0x9B, 0xFF);
    public static readonly Color32 Gold200 = new Color32(0xF3, 0xE6, 0xC6, 0xFF);
    public static readonly Color32 Gold100 = new Color32(0xFA, 0xF2, 0xE1, 0xFF);
    public static readonly Color32 Gold050 = new Color32(0xFD, 0xFB, 0xF4, 0xFF);

    // Cobalt: arcane / intelligence / rare items
    public static readonly Color32 Cobalt950 = new Color32(0x0A, 0x10, 0x22, 0xFF);
    public static readonly Color32 Cobalt900 = new Color32(0x0E, 0x17, 0x35, 0xFF);
    public static readonly Color32 Cobalt800 = new Color32(0x14, 0x21, 0x4C, 0xFF);
    public static readonly Color32 Cobalt700 = new Color32(0x1C, 0x2E, 0x6B, 0xFF);
    public static readonly Color32 Cobalt600 = new Color32(0x2E, 0x45, 0x90, 0xFF);
    public static readonly Color32 Cobalt500 = new Color32(0x4A, 0x6E, 0xC2, 0xFF);
    public static readonly Color32 Cobalt400 = new Color32(0x6B, 0x90, 0xDD, 0xFF);
    public static readonly Color32 Cobalt300 = new Color32(0x93, 0xB4, 0xEE, 0xFF);
    public static readonly Color32 Cobalt200 = new Color32(0xC2, 0xD7, 0xF8, 0xFF);
    public static readonly Color32 Cobalt100 = new Color32(0xE2, 0xEC, 0xFD, 0xFF);
    public static readonly Color32 Cobalt050 = new Color32(0xF2, 0xF7, 0xFF, 0xFF);

    // Amethyst: forbidden magic / curse / mystery
    public static readonly Color32 Amethyst950 = new Color32(0x14, 0x0A, 0x22, 0xFF);
    public static readonly Color32 Amethyst900 = new Color32(0x1D, 0x0F, 0x33, 0xFF);
    public static readonly Color32 Amethyst800 = new Color32(0x2A, 0x16, 0x4B, 0xFF);
    public static readonly Color32 Amethyst700 = new Color32(0x3B, 0x1F, 0x66, 0xFF);
    public static readonly Color32 Amethyst600 = new Color32(0x55, 0x30, 0x90, 0xFF);
    public static readonly Color32 Amethyst500 = new Color32(0x7B, 0x5C, 0xCB, 0xFF);
    public static readonly Color32 Amethyst400 = new Color32(0x9B, 0x83, 0xE4, 0xFF);
    public static readonly Color32 Amethyst300 = new Color32(0xBD, 0xA9, 0xF2, 0xFF);
    public static readonly Color32 Amethyst200 = new Color32(0xDE, 0xD3, 0xFA, 0xFF);
    public static readonly Color32 Amethyst100 = new Color32(0xF1, 0xEC, 0xFE, 0xFF);
    public static readonly Color32 Amethyst050 = new Color32(0xF8, 0xF6, 0xFF, 0xFF);

    // Aether: mystic teal / alchemy / “otherworldly” effects
    public static readonly Color32 Aether950 = new Color32(0x07, 0x1A, 0x1B, 0xFF);
    public static readonly Color32 Aether900 = new Color32(0x0B, 0x26, 0x28, 0xFF);
    public static readonly Color32 Aether800 = new Color32(0x10, 0x38, 0x3B, 0xFF);
    public static readonly Color32 Aether700 = new Color32(0x16, 0x50, 0x53, 0xFF);
    public static readonly Color32 Aether600 = new Color32(0x1F, 0x6E, 0x72, 0xFF);
    public static readonly Color32 Aether500 = new Color32(0x3C, 0x9B, 0xA0, 0xFF);
    public static readonly Color32 Aether400 = new Color32(0x65, 0xBA, 0xC0, 0xFF);
    public static readonly Color32 Aether300 = new Color32(0x91, 0xD3, 0xD7, 0xFF);
    public static readonly Color32 Aether200 = new Color32(0xC0, 0xE8, 0xEA, 0xFF);
    public static readonly Color32 Aether100 = new Color32(0xE3, 0xF6, 0xF7, 0xFF);
    public static readonly Color32 Aether050 = new Color32(0xF3, 0xFB, 0xFB, 0xFF);

    // Umber: leather / wood / earthy UI elements
    public static readonly Color32 Umber950 = new Color32(0x1A, 0x12, 0x0B, 0xFF);
    public static readonly Color32 Umber900 = new Color32(0x24, 0x17, 0x0E, 0xFF);
    public static readonly Color32 Umber800 = new Color32(0x35, 0x21, 0x15, 0xFF);
    public static readonly Color32 Umber700 = new Color32(0x4A, 0x2E, 0x1E, 0xFF);
    public static readonly Color32 Umber600 = new Color32(0x5F, 0x3D, 0x28, 0xFF);
    public static readonly Color32 Umber500 = new Color32(0x7A, 0x5B, 0x3A, 0xFF);
    public static readonly Color32 Umber400 = new Color32(0x9A, 0x7B, 0x57, 0xFF);
    public static readonly Color32 Umber300 = new Color32(0xBB, 0xA7, 0x88, 0xFF);
    public static readonly Color32 Umber200 = new Color32(0xDD, 0xCF, 0xBF, 0xFF);
    public static readonly Color32 Umber100 = new Color32(0xF0, 0xE7, 0xDC, 0xFF);
    public static readonly Color32 Umber050 = new Color32(0xF8, 0xF4, 0xEF, 0xFF);
}
    
    public static class Semantic
{
    private static Color32 WithA(Color32 c, byte a) => new Color32(c.r, c.g, c.b, a);

    // ----------------------------
    // Text
    // ----------------------------

    // 기본(어두운 Surface 위)
    public static readonly Color32 TextPrimary   = Primitive.Bone050;
    public static readonly Color32 TextSecondary = Primitive.Mist200;
    public static readonly Color32 TextMuted     = Primitive.Mist300;
    public static readonly Color32 TextDisabled  = Primitive.Mist300;

    // 밝은(양피지/패치먼트 Surface 위)
    public static readonly Color32 TextOnLightPrimary   = Primitive.Bone900; // 잉크 느낌(따뜻한 블랙)
    public static readonly Color32 TextOnLightSecondary = Primitive.Bone700;
    public static readonly Color32 TextOnLightMuted     = Primitive.Bone600;
    public static readonly Color32 TextOnLightDisabled  = Primitive.Bone500;

    // 강한 Accent 면(금색 버튼 등) 위
    public static readonly Color32 TextOnAccentDark  = Primitive.Abyss900; // Gold 계열 위 추천
    public static readonly Color32 TextOnAccentLight = Primitive.Bone050;  // Blood/Moss/Cobalt 계열 위 추천

    // ----------------------------
    // Surfaces (Background / Panels / Cards)
    // ----------------------------

    public static readonly Color32 Background       = Primitive.Abyss950; // 최외곽 배경
    public static readonly Color32 SurfacePrimary   = Primitive.Abyss900; // 메인 패널
    public static readonly Color32 SurfaceSecondary = Primitive.Slate800; // 내부 패널
    public static readonly Color32 SurfaceTertiary  = Primitive.Slate700; // 칩/서브 블록
    public static readonly Color32 SurfaceInset     = Primitive.Abyss800; // 슬롯/인풋/인셋 영역

    // 보드게임 재질감(양피지/가죽)
    public static readonly Color32 SurfaceParchment      = Primitive.Bone100; // 카드/툴팁/설명 영역
    public static readonly Color32 SurfaceParchmentAlt   = Primitive.Bone050; // 강조된 패치먼트
    public static readonly Color32 SurfaceParchmentMuted = Primitive.Bone200; // 살짝 눌린 패치먼트

    public static readonly Color32 SurfaceLeather    = Primitive.Umber800; // 배너/헤더/가죽 패널
    public static readonly Color32 SurfaceLeatherAlt = Primitive.Umber700;

    // ----------------------------
    // Borders / Dividers
    // ----------------------------

    public static readonly Color32 BorderSubtle = Primitive.Slate700;
    public static readonly Color32 BorderNormal = Primitive.Slate600;
    public static readonly Color32 BorderStrong = Primitive.Slate500;

    public static readonly Color32 BorderParchment = Primitive.Umber500; // 패치먼트 위 테두리
    public static readonly Color32 BorderLeather   = Primitive.Umber600; // 가죽 위 테두리
    public static readonly Color32 BorderAccent    = Primitive.Gold600;  // 장식/강조 테두리(얇게)

    public static readonly Color32 Divider          = WithA(Primitive.Slate700, 0xA0);
    public static readonly Color32 DividerParchment = WithA(Primitive.Umber500, 0x88);

    // ----------------------------
    // Overlay / Shadows / Sheen
    // ----------------------------

    public static readonly Color32 OverlayDim   = WithA(Primitive.Abyss950, 0xCC); // 모달 딤
    public static readonly Color32 OverlayScrim = WithA(Primitive.Abyss950, 0x99); // 약한 딤

    public static readonly Color32 ShadowSoft = WithA(Primitive.Abyss950, 0x66);
    public static readonly Color32 ShadowHard = WithA(Primitive.Abyss950, 0xA8);

    // 보드게임 “인쇄/바니시” 느낌의 상단 살짝 빛
    public static readonly Color32 HighlightSheen = WithA(Primitive.Bone050, 0x22);

    // ----------------------------
    // Interaction (generic overlays)
    // ----------------------------

    // 어떤 Surface 위에도 얇게 덮어서 Hover/Pressed 효과로 쓰는 용도
    public static readonly Color32 HoverTint    = WithA(Primitive.Mist200, 0x18);
    public static readonly Color32 PressedTint  = WithA(Primitive.Mist200, 0x2A);
    public static readonly Color32 DisabledTint = WithA(Primitive.Abyss950, 0x66);

    // ----------------------------
    // Focus / Selection
    // ----------------------------

    public static readonly Color32 FocusRing        = Primitive.Gold400;
    public static readonly Color32 SelectionOutline = Primitive.Aether500;
    public static readonly Color32 SelectionFill    = WithA(Primitive.Aether500, 0x22);

    // ----------------------------
    // Actions (Buttons / CTAs)
    // ----------------------------

    // Primary (금색)
    public static readonly Color32 ActionPrimaryBg         = Primitive.Gold500;
    public static readonly Color32 ActionPrimaryBgHover    = Primitive.Gold400;
    public static readonly Color32 ActionPrimaryBgPressed  = Primitive.Gold600;
    public static readonly Color32 ActionPrimaryBgDisabled = Primitive.Gold600;
    public static readonly Color32 ActionPrimaryFg         = TextOnAccentDark;

    // Secondary (석재/강철)
    public static readonly Color32 ActionSecondaryBg         = Primitive.Slate700;
    public static readonly Color32 ActionSecondaryBgHover    = Primitive.Slate600;
    public static readonly Color32 ActionSecondaryBgPressed  = Primitive.Slate800;
    public static readonly Color32 ActionSecondaryBgDisabled = Primitive.Slate800;
    public static readonly Color32 ActionSecondaryFg         = TextPrimary;

    // Ghost (배경 투명, 텍스트만)
    public static readonly Color32 ActionGhostFg         = TextSecondary;
    public static readonly Color32 ActionGhostFgHover    = TextPrimary;
    public static readonly Color32 ActionGhostFgDisabled = TextDisabled;

    // Danger action (빨강)
    public static readonly Color32 ActionDangerBg         = Primitive.Blood500;
    public static readonly Color32 ActionDangerBgHover    = Primitive.Blood400;
    public static readonly Color32 ActionDangerBgPressed  = Primitive.Blood600;
    public static readonly Color32 ActionDangerBgDisabled = Primitive.Blood700;
    public static readonly Color32 ActionDangerFg         = TextOnAccentLight;

    // ----------------------------
    // Status / Feedback
    // ----------------------------

    public static readonly Color32 StatePositive = Primitive.Moss500;
    public static readonly Color32 StateWarning  = Primitive.Amber500;
    public static readonly Color32 StateDanger   = Primitive.Blood500;
    public static readonly Color32 StateInfo     = Primitive.Cobalt500;
    public static readonly Color32 StateMagic    = Primitive.Amethyst500;

    // 배경에 깔아주는 “틴트” (칩/배지/바 텍스쳐 없이도 상태감을 주기 좋음)
    public static readonly Color32 StatePositiveTint = WithA(Primitive.Moss500, 0x2A);
    public static readonly Color32 StateWarningTint  = WithA(Primitive.Amber500, 0x2A);
    public static readonly Color32 StateDangerTint   = WithA(Primitive.Blood500, 0x2A);
    public static readonly Color32 StateInfoTint     = WithA(Primitive.Cobalt500, 0x2A);
    public static readonly Color32 StateMagicTint    = WithA(Primitive.Amethyst500, 0x2A);

    // ----------------------------
    // Values (numbers / reward / penalty)
    // ----------------------------

    public static readonly Color32 ValueBase   = Primitive.Bone050;
    public static readonly Color32 ValueBonus  = Primitive.Moss500;
    public static readonly Color32 ValuePenalty= Primitive.Blood500;
    public static readonly Color32 ValueFinal  = Primitive.Gold500;
    public static readonly Color32 ValueArcane = Primitive.Cobalt500;
    public static readonly Color32 ValueCurse  = Primitive.Amethyst500;

    // ----------------------------
    // Inputs (fields / slots)
    // ----------------------------

    public static readonly Color32 FieldBg           = SurfaceInset;
    public static readonly Color32 FieldBorder       = BorderSubtle;
    public static readonly Color32 FieldBorderFocused= BorderAccent;
    public static readonly Color32 FieldPlaceholder  = TextMuted;

    // ----------------------------
    // Scroll/Handles (있으면 편한 범용 토큰)
    // ----------------------------

    public static readonly Color32 HandleIdle   = Primitive.Slate600;
    public static readonly Color32 HandleHover  = Primitive.Slate500;
    public static readonly Color32 HandleActive = Primitive.Gold600;

    // ----------------------------
    // Screen Aliases (component mapping)
    // ----------------------------

    // HUD
    public static readonly Color32 HudStatText = TextPrimary;

    // Generic list/card aliases
    public static readonly Color32 ListCardBg = SurfaceParchment;
    public static readonly Color32 ListCardSelectedFill = SurfaceParchmentAlt;
    public static readonly Color32 ListCardSelectedBorder = SelectionOutline;
    public static readonly Color32 ListCardTitleText = TextOnLightPrimary;
    public static readonly Color32 ListCardMetaText = TextOnLightSecondary;
    public static readonly Color32 ListCardValueTileBg = SurfaceParchmentAlt;
    public static readonly Color32 ListCardValueText = TextOnLightPrimary;
    public static readonly Color32 ListCardStatePositive = StatePositive;

    // Generic detail panel aliases
    public static readonly Color32 DetailPanelBg = SurfaceParchment;
    public static readonly Color32 DetailHeaderBg = SurfaceParchmentMuted;
    public static readonly Color32 DetailSectionBg = SurfaceParchmentAlt;
    public static readonly Color32 DetailTitleText = TextOnLightPrimary;
    public static readonly Color32 DetailMetaText = TextOnLightSecondary;
    public static readonly Color32 DetailTagText = TextOnLightSecondary;
    public static readonly Color32 DetailValueTileBg = SurfaceParchmentMuted;
    public static readonly Color32 DetailValueText = TextOnLightPrimary;
    public static readonly Color32 DetailPositivePanelBg = Primitive.Moss100;
    public static readonly Color32 DetailPositiveText = TextOnLightPrimary;
    public static readonly Color32 DetailNegativePanelBg = Primitive.Blood100;
    public static readonly Color32 DetailNegativeText = TextOnLightPrimary;
    public static readonly Color32 DetailPrimaryActionBg = ActionPrimaryBg;
    public static readonly Color32 DetailPrimaryActionBgDisabled = ActionPrimaryBgDisabled;
    public static readonly Color32 DetailPrimaryActionFg = ActionPrimaryFg;
    public static readonly Color32 DetailSecondaryActionBg = ActionSecondaryBg;
    public static readonly Color32 DetailSecondaryActionBgHover = ActionSecondaryBgHover;
    public static readonly Color32 DetailSecondaryActionBgPressed = ActionSecondaryBgPressed;
    public static readonly Color32 DetailSecondaryActionBgDisabled = ActionSecondaryBgDisabled;
    public static readonly Color32 DetailSecondaryActionFg = ActionSecondaryFg;

    // Generic slot/frame aliases
    public static readonly Color32 SlotFrameUsable = BorderNormal;
    public static readonly Color32 SlotFrameLocked = TextDisabled;
    public static readonly Color32 SlotPlus = TextDisabled;
    public static readonly Color32 SlotLockedOverlay = Primitive.Slate700;

    // Modal
    public static readonly Color32 ModalDimBackground = OverlayDim;
    public static readonly Color32 ModalPanelBg = SurfacePrimary;
    public static readonly Color32 ModalPanelBorder = BorderNormal;
    public static readonly Color32 ModalTitleText = TextPrimary;
    public static readonly Color32 ModalBodyText = TextSecondary;
    public static readonly Color32 ModalConfirmButtonBg = ActionPrimaryBg;
    public static readonly Color32 ModalConfirmButtonFg = ActionPrimaryFg;
    public static readonly Color32 ModalCancelButtonBg = ActionSecondaryBg;
    public static readonly Color32 ModalCancelButtonFg = ActionSecondaryFg;
}
}

