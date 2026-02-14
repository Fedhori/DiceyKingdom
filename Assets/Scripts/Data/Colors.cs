using UnityEngine;

public static class Colors
{
    // Primitive palette: dark fantasy + serious tone.
    public static class Primitive
    {
        public static readonly Color32 Abyss950 = new Color32(0x0B, 0x0D, 0x12, 0xFF); // #0B0D12
        public static readonly Color32 Abyss900 = new Color32(0x11, 0x16, 0x22, 0xFF); // #111622
        public static readonly Color32 Slate800 = new Color32(0x1A, 0x22, 0x32, 0xFF); // #1A2232
        public static readonly Color32 Slate700 = new Color32(0x26, 0x32, 0x48, 0xFF); // #263248

        public static readonly Color32 Mist300 = new Color32(0x9A, 0xA6, 0xBA, 0xFF);  // #9AA6BA
        public static readonly Color32 Mist200 = new Color32(0xC3, 0xCB, 0xD9, 0xFF);  // #C3CBD9
        public static readonly Color32 Bone100 = new Color32(0xE7, 0xE2, 0xD7, 0xFF);  // #E7E2D7
        public static readonly Color32 Bone050 = new Color32(0xF4, 0xEF, 0xE4, 0xFF);  // #F4EFE4

        public static readonly Color32 Moss600 = new Color32(0x32, 0x5A, 0x3A, 0xFF);  // #325A3A
        public static readonly Color32 Moss500 = new Color32(0x4A, 0x7A, 0x53, 0xFF);  // #4A7A53
        public static readonly Color32 Amber500 = new Color32(0xD0, 0x9A, 0x3B, 0xFF); // #D09A3B
        public static readonly Color32 Gold500 = new Color32(0xC9, 0xA2, 0x4D, 0xFF);  // #C9A24D
        public static readonly Color32 Blood600 = new Color32(0x8D, 0x2D, 0x2D, 0xFF); // #8D2D2D
        public static readonly Color32 Blood500 = new Color32(0xB7, 0x42, 0x42, 0xFF); // #B74242
    }

    // Semantic tokens: always mapped from Primitive.
    public static class Semantic
    {
        public static readonly Color32 TextPrimary = Primitive.Bone050;
        public static readonly Color32 TextSecondary = Primitive.Mist200;
        public static readonly Color32 TextDisabled = Primitive.Mist300;

        public static readonly Color32 SurfacePrimary = Primitive.Abyss900;
        public static readonly Color32 SurfaceSecondary = Primitive.Slate800;
        public static readonly Color32 BorderSubtle = Primitive.Slate700;

        public static readonly Color32 StatePositive = Primitive.Moss500;
        public static readonly Color32 StateWarning = Primitive.Amber500;
        public static readonly Color32 StateDanger = Primitive.Blood500;

        public static readonly Color32 ValueBase = Primitive.Bone050;
        public static readonly Color32 ValueBonus = Primitive.Moss500;
        public static readonly Color32 ValueFinal = Primitive.Gold500;

        public static readonly Color32 StabilityNormal = Primitive.Moss500;
        public static readonly Color32 StabilityWarning = Primitive.Amber500;
        public static readonly Color32 StabilityDanger = Primitive.Blood500;

        public static readonly Color32 DiceFaceDefault = Primitive.Abyss900;
        public static readonly Color32 DiceFaceDisabled = Primitive.Mist300;
        public static readonly Color32 DiceFaceRolling = Primitive.Slate700;
        public static readonly Color32 DiceFaceSuccess = Primitive.Moss600;
        public static readonly Color32 DiceFaceFailure = Primitive.Blood600;

        public static readonly Color32 DiceBackgroundActive = new Color32(0xF4, 0xEF, 0xE4, 0xF8);
        public static readonly Color32 DiceBackgroundInactive = new Color32(0xC3, 0xCB, 0xD9, 0xF2);
    }
}
