using System.Collections.Generic;
using UnityEngine;

public static class GameConfig
{
    public static int BaseIncome = 5;
    public static int ItemSlotCount = 12;
    public static int ItemSlotsPerRow = 4;
    public static int MaxUpgradesPerItem = 3;
    public static int InterestCurrencyPerUnit = 5;
    public static int InterestMax = 5;
    public static int ShopItemWeight = 60;
    public static int ShopUpgradeWeight = 40;

    // Player/Item base stats   
    public static float PlayerBaseMoveSpeed = 500f;
    public static float ItemBaseProjectileSize = 1f;
    public static float ItemBaseProjectileSpeed = 1000f;
    public static float ProjectileStationaryStartSpeedMultiplier = 1f;
    public static float ProjectileStationaryDecayExponent = 2f;
    public static float ProjectileHomingTurnRateDegrees = 720f;
    public static float BlockFallSpeed = 100;
    public static float DamageTickIntervalSeconds = 0.2f;
    public static float ProjectileExplosionRadiusUnit = 128f;
    public static float VirtualJoystickDeadzonePixels = 64f;

    // Beam VFX
    public static float BeamPulsePercent = 0.1f;
    public static float BeamPulseHzMin = 0.5f;
    public static float BeamPulseHzMax = 1f;
    public static float BeamFlickerMinAlpha = 0.5f;
    public static float BeamFlickerHzMin = 4f;
    public static float BeamFlickerHzMax = 8f;

    // Explosion VFX
    public static float ExplosionSpeedPerRadius = 2f;
    public static float ExplosionSizePerRadius = 0.1f;
}
