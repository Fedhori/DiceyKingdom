using System.Collections.Generic;
using UnityEngine;

public static class GameConfig
{
    public static string BasicPinId = "pin.basic";
    public static int BaseIncome = 10;
    public static int MaxRuleCount = 4;
    public static int BaseBallIncome = 1;
    public static int TokenSlotCount = 8;
    public static float BallSpeed = 1000f;
    public static float BallRadius = 16f;

    // Player/Item base stats
    public static float PlayerBaseMoveSpeed = 500f;
    public static float ItemBaseBulletSize = 1f;
    public static float ItemBaseBulletSpeed = 1000f;
}

public static class LayerMaskUtil
{
    public static readonly int PinLayer = LayerMask.NameToLayer("Pin");
    public static readonly LayerMask PinMask = 1 << PinLayer;

    public static bool Contains(this LayerMask mask, GameObject go)
    {
        int bit = 1 << go.layer;
        return (mask.value & bit) != 0;
    }
}
